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
            Console.WriteLine("Iniciando o servidor de rede...");
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
            // ConcurrentBag não tem um método 'Remove' fácil, a maneira é recriar a lista sem o item.
            var navesAtuais = naves.Except(new[] { naveParaRemover }).ToList();
            naves.Clear(); // Limpa a coleção original
            foreach (var n in navesAtuais) naves.Add(n); // Readiciona os itens restantes

            Console.WriteLine($"Nave do cliente {idCliente} removida.");

            // Se não houver mais naves, o jogo acaba.
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

    private void AtualizarEstadoDoJogo()
    {
        if (fimDeJogo) return;

        // 1. Atualizar Posição dos Tiros e Remover os que saíram da tela
        var tirosAtuais = new List<Tiro>();
        while (tiros.TryTake(out var tiro))
        {
            tiro.Atualizar();
            if (!tiro.ForaDaTela(height))
            {
                tirosAtuais.Add(tiro);
            }
        }
        foreach (var t in tirosAtuais) tiros.Add(t);

        // 2. Atualizar Posição dos Asteroides e Remover os que saíram da tela
        var asteroidesAtuais = new List<Asteroide>();
        while (asteroides.TryTake(out var asteroide))
        {
            asteroide.Atualizar();
            if (!asteroide.ForaDaTela(height))
            {
                asteroidesAtuais.Add(asteroide);
            }
        }
        foreach (var a in asteroidesAtuais) asteroides.Add(a);


        // 3. Verificar Colisão: Tiros vs Asteroides
        var tirosAtingidos = new HashSet<Tiro>();
        var asteroidesAtingidos = new HashSet<Asteroide>();

        foreach (var tiro in tiros)
        {
            foreach (var asteroide in asteroides)
            {
                if (asteroide.Colide(tiro))
                {
                    tirosAtingidos.Add(tiro);
                    asteroidesAtingidos.Add(asteroide);
                    pontos += 100;
                }
            }
        }

        // Remove os itens atingidos
        if (tirosAtingidos.Any() || asteroidesAtingidos.Any())
        {
            var tirosRestantes = tiros.Except(tirosAtingidos).ToList();
            tiros.Clear();
            foreach (var t in tirosRestantes) tiros.Add(t);

            var asteroidesRestantes = asteroides.Except(asteroidesAtingidos).ToList();
            asteroides.Clear();
            foreach (var a in asteroidesRestantes) asteroides.Add(a);
        }


        // 4. Verificar Colisão: Naves vs Asteroides
        foreach (var nave in naves)
        {
            foreach (var asteroide in asteroides)
            {
                if (asteroide.Colide(nave))
                {
                    fimDeJogo = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("GAME OVER! Nave atingida por asteroide.");
                    Console.ResetColor();
                    return; // Sai imediatamente do método
                }
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
                if (contagemDeFrames >= 60)
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