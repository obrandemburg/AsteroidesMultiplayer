using Asteroides;
using Asteroides.Compartilhado.Contratos;
using Asteroides.Compartilhado.Estados;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using static Servidor.GerenciadorDeRede;

public class Programa
{
    readonly ConcurrentBag<Nave> naves = new();
    readonly ConcurrentBag<Tiro> tiros = new();
    readonly ConcurrentBag<Asteroide> asteroides = new();

    readonly Random rnd = new();
    int pontos = 0;
    const int width = 1280, height = 720;

    private int _idTiroCounter = 1;
    private int _idAsteroideCounter = 1;

    CancellationTokenSource cts = new();
    int contagemDeFrames = 0;
    bool fimDeJogo = false;

    private readonly Servidor.GerenciadorDeRede _servidor;

    public Programa()
    {
        _servidor = new Servidor.GerenciadorDeRede();
        _servidor.OnMensagemRecebida += ProcessarLogicaDoJogo;
        _servidor.OnClienteConectado += AdicionarJogador;
        _servidor.OnClienteDesconectado += RemoverJogador;

        Task.Run(() => ContaFrames(cts.Token));
    }

    public async Task ExecutarAsync()
    {
        Console.WriteLine("Aplicação iniciada.");
        Console.Title = "Servidor do Jogo";

        try
        {
            _servidor.IniciarServidor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Servidor de rede está online e aceitando clientes.");
            Console.ResetColor();

            Console.WriteLine("\n*** APLICAÇÃO PRINCIPAL RODANDO. PRESSIONE ENTER PARA DESLIGAR. ***\n");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERRO CRÍTICO NA APLICAÇÃO: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            Console.WriteLine("Sinal de desligamento recebido. Iniciando processo de encerramento...");
            cts.Cancel();
            _servidor.Encerrar();
        }

        Console.WriteLine("Aplicação finalizada.");
    }

    private void AdicionarJogador(int idCliente)
    {
        Console.WriteLine($"Adicionando nave para o cliente {idCliente}");
        var posicaoInicial = new Vector2(width / 2 + (naves.Count % 2 == 0 ? -100 : 100), height / 2);
        naves.Add(new Nave(posicaoInicial, idCliente));
    }

    private void RemoverJogador(int idCliente)
    {
        var naveParaRemover = naves.FirstOrDefault(n => n.Id == idCliente);
        if (naveParaRemover != null)
        {
            var navesAtuais = naves.Except(new[] { naveParaRemover }).ToList();
            naves.Clear();
            foreach (var n in navesAtuais) naves.Add(n);

            Console.WriteLine($"Nave do cliente {idCliente} removida.");

            if (naves.IsEmpty)
            {
                fimDeJogo = true;
                Console.WriteLine("Todos os jogadores saíram. Fim de jogo.");
            }
        }
    }

    private void ProcessarLogicaDoJogo(MensagemRecebida msg)
    {
        if (fimDeJogo) return;

        var naveDoJogador = naves.FirstOrDefault(n => n.Id == msg.idCliente);
        if (naveDoJogador == null) return;

        int novoIdTiro = Interlocked.Increment(ref _idTiroCounter);
        Tiro? novoTiro = naveDoJogador.AtualizarEProcessarAcoes(msg.inputCliente, novoIdTiro);
        if (novoTiro != null)
        {
            tiros.Add(novoTiro);
        }
    }

    // --- MÉTODO ATUALIZARESTADODOJOGO TOTALMENTE REFEITO PARA SER THREAD-SAFE ---
    private void AtualizarEstadoDoJogo()
    {
        if (fimDeJogo) return;

        var navesAtuais = naves.ToList();
        var tirosAtuais = tiros.ToList();
        var asteroidesAtuais = asteroides.ToList();

        // Listas para guardar os sobreviventes do frame
        var tirosSobreviventes = new List<Tiro>();
        var asteroidesSobreviventes = new List<Asteroide>();

        // 2. Atualizar Posição dos Tiros e filtrar os que saíram da tela
        foreach (var tiro in tirosAtuais)
        {
            tiro.Atualizar();
            if (!tiro.ForaDaTela(height))
            {
                tirosSobreviventes.Add(tiro);
            }
        }

        // 3. Atualizar Posição dos Asteroides e filtrar os que saíram da tela
        foreach (var asteroide in asteroidesAtuais)
        {
            asteroide.Atualizar();
            if (!asteroide.ForaDaTela(height))
            {
                asteroidesSobreviventes.Add(asteroide);
            }
        }

        // 4. Verificar Colisão: Tiros vs Asteroides
        var tirosAtingidos = new HashSet<Tiro>();
        var asteroidesAtingidos = new HashSet<Asteroide>();

        // Agora iteramos sobre as listas locais (e seguras) de sobreviventes
        foreach (var tiro in tirosSobreviventes)
        {
            foreach (var asteroide in asteroidesSobreviventes)
            {
                if (asteroide.Colide(tiro))
                {
                    tirosAtingidos.Add(tiro);
                    asteroidesAtingidos.Add(asteroide);
                    Interlocked.Add(ref pontos, 100);
                }
            }
        }

        // 5. Verificar Colisão: Naves vs Asteroides
        foreach (var nave in navesAtuais)
        {
            // Usamos a lista de asteroides sobreviventes da etapa anterior
            foreach (var asteroide in asteroidesSobreviventes)
            {
                if (asteroide.Colide(nave))
                {
                    fimDeJogo = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("GAME OVER! Nave atingida por asteroide.");
                    Console.ResetColor();
                    break; // Sai do loop de asteroides
                }
            }
            if (fimDeJogo) break; // Sai do loop de naves
        }

        // --- Passo 3: Atualizar as coleções globais com o resultado do frame ---

        // Limpa o ConcurrentBag de tiros e o repopula com os tiros que não saíram da tela E não colidiram
        tiros.Clear();
        foreach (var tiro in tirosSobreviventes)
        {
            if (!tirosAtingidos.Contains(tiro))
            {
                tiros.Add(tiro);
            }
        }

        // Faz o mesmo para os asteroides
        asteroides.Clear();
        foreach (var asteroide in asteroidesSobreviventes)
        {
            if (!asteroidesAtingidos.Contains(asteroide))
            {
                asteroides.Add(asteroide);
            }
        }
    }


    private async Task ContaFrames(CancellationToken token)
    {
        const double targetFps = 60.0;
        const double targetFrameTimeMilliseconds = 1000.0 / targetFps;
        var stopwatch = new Stopwatch();

        while (!token.IsCancellationRequested)
        {
            stopwatch.Restart();

            AtualizarEstadoDoJogo();

            if (!fimDeJogo)
            {
                contagemDeFrames++;
                if (contagemDeFrames >= 60) // Gera asteroides a cada 1 segundo
                {
                    asteroides.Add(NovoAsteroide());
                    contagemDeFrames = 0;
                }
            }

            var estadoDoMundo = CriarEstadoDoMundo();
            var estadoJson = JsonSerializer.Serialize(estadoDoMundo);
            _servidor.EnviarMensagemParaTodos(estadoJson);

            double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            var timeToWait = (int)(targetFrameTimeMilliseconds - elapsedMilliseconds);
            if (timeToWait > 0)
            {
                try
                {
                    await Task.Delay(timeToWait, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }

    private EstadoMundoMensagem CriarEstadoDoMundo()
    {
        // Esta operação é segura, pois .Select (LINQ) trabalha em um snapshot momentâneo do ConcurrentBag
        var navesEstado = naves.Select(n => new NaveEstado { Id = n.Id, PosicaoX = n.Posicao.X, PosicaoY = n.Posicao.Y }).ToList();
        var tirosEstado = tiros.Select(t => new TiroEstado { Id = t.Id, PosicaoX = t.Pos.X, PosicaoY = t.Pos.Y }).ToList();
        var asteroidesEstado = asteroides.Select(a => new AsteroideEstado { Id = a.Id, PosicaoX = a.pos.X, PosicaoY = a.pos.Y, Raio = a.Raio }).ToList();

        return new EstadoMundoMensagem
        {
            Naves = navesEstado,
            Tiros = tirosEstado,
            Asteroides = asteroidesEstado,
            Pontos = this.pontos,
            FimDeJogo = this.fimDeJogo
        };
    }

    Asteroide NovoAsteroide()
    {
        int novoIdAsteroide = Interlocked.Increment(ref _idAsteroideCounter);
        float x = rnd.Next(width);
        float velY = 2f + (float)rnd.NextDouble() * 2f;
        return new Asteroide(new Vector2(x, -30), new Vector2(0, velY), 25, novoIdAsteroide);
    }

    public static async Task Main(string[] args)
    {
        var programa = new Programa();
        await programa.ExecutarAsync();
    }
}