using Asteroides;
using Asteroides.Compartilhado.Contratos;
using Asteroides.Compartilhado.Estados;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Text.Json;
using static Servidor.GerenciadorDeRede;

public class Programa
{
    //Variáveis do jogo:
    readonly List<Nave> naves = new();
    readonly List<Tiro> tiros = new();
    readonly List<Asteroide> asteroides = new();
    readonly Random rnd = new();
    int pontos = 0;
    const int width = 1280, height = 720;
    int idTiro = 1;
    int idAsteroide = 1;

    //Variáveis de controle
    CancellationTokenSource cts = new();
    int contagemDeFrames = 0;
    bool fimDeJogo = false;

    private readonly Servidor.GerenciadorDeRede _servidor;

    public Programa()
    {
        _servidor = new Servidor.GerenciadorDeRede();
        _servidor.OnMensagemRecebida += ProcessarLogicaDoJogo;
        naves.Add(new Nave(new Vector2(width / 2 - 100, height / 2), 1));
        naves.Add(new Nave(new Vector2(width / 2 + 100, height / 2), 2));
        Task.Run(() => ContaFrames(cts.Token));
    }

    public async Task ExecutarAsync()
    {
        Console.WriteLine("Aplicação iniciada.");
        Console.Title = "Servidor do Jogo";

        try
        {
            Console.WriteLine("Iniciando o servidor de rede...");
            await _servidor.IniciarEConectarClientesAsync();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Servidor de rede está online. Todas as tarefas de fundo estão ativas.");
            Console.ResetColor();

            Console.WriteLine("\n*** APLICAÇÃO PRINCIPAL RODANDO. PRESSIONE ENTER PARA DESLIGAR. ***\n");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERRO CRÍTICO NA APLICAÇÃO: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Pressione ENTER para sair.");
            Console.ReadLine();
        }
        finally
        {
            Console.WriteLine("Sinal de desligamento recebido. Iniciando processo de encerramento...");
            _servidor.Encerrar();
        }

        Console.WriteLine("Aplicação finalizada.");
    }

    private void ProcessarLogicaDoJogo(MensagemRecebida msg)
    {
        if (fimDeJogo)
        {
            return;
        }

        Tiro? novoTiro = null;

        switch (msg.idCliente)
        {
            case 1:
                novoTiro = naves[0].AtualizarEProcessarAcoes(msg.inputCliente, idTiro);
                idTiro++;
                break;

            case 2:
                novoTiro = naves[1].AtualizarEProcessarAcoes(msg.inputCliente, idTiro);
                idTiro++;
                break;
        }

        if (novoTiro != null)
        {
            tiros.Add(novoTiro);
        }

        for (int i = tiros.Count - 1; i >= 0; i--)
        {
            var t = tiros[i];
            t.Atualizar();
            if (t.ForaDaTela(height))
            {
                tiros.RemoveAt(i);
            }
        }

        for (int i = asteroides.Count - 1; i >= 0; i--)
        {
            var a = asteroides[i];
            a.Atualizar();

            if (a.ForaDaTela(height))
            {
                asteroides.RemoveAt(i);
                continue;
            }

            for (int j = tiros.Count - 1; j >= 0; j--)
            {
                var t = tiros[j];
                if (a.Colide(t))
                {
                    asteroides.RemoveAt(i);
                    tiros.RemoveAt(j);
                    pontos += 100;
                    break;
                }
            }
        }


        for (int i = asteroides.Count - 1; i >= 0; i--)
        {
            var a = asteroides[i];
            for (int j = naves.Count - 1; j >= 0; j--)
            {
                var n = naves[j];
                if (a.Colide(n))
                {
                    fimDeJogo = true;
                    Console.WriteLine("GAME OVER! Nave atingida.");
                    break; // Sai do loop de naves
                }
            }
            if (fimDeJogo)
            {
                break; // Sai do loop de asteroides
            }
        }

        var estadoDoMundo = CriarEstadoDoMundo();
        var estadoJson = JsonSerializer.Serialize(estadoDoMundo);
        _servidor.EnviarMensagem(estadoJson);
    }

    private EstadoMundoMensagem CriarEstadoDoMundo()
    {
        var navesEstado = naves.Select(n => new NaveEstado { Id = n.Id, PosicaoX = n.Posicao.X, PosicaoY = n.Posicao.Y }).ToList();
        var tirosEstado = tiros.Select(t => new TiroEstado { Id = t.Id, PosicaoX = t.Pos.X, PosicaoY = t.Pos.Y }).ToList();
        var asteroidesEstado = asteroides.Select(a => new AsteroideEstado { Id = a.Id, PosicaoX = a.pos.X, PosicaoY = a.pos.Y, Raio = a.Raio }).ToList();

        var estado = new EstadoMundoMensagem
        {
            Naves = navesEstado,
            Tiros = tirosEstado,
            Asteroides = asteroidesEstado,
            Pontos = this.pontos,
            FimDeJogo = this.fimDeJogo
        };

        return estado;
    }

    Asteroide NovoAsteroide()
    {
        float x = rnd.Next(width);
        float velY = 2f + (float)rnd.NextDouble() * 2f;
        return new Asteroide(new Vector2(x, -30), new Vector2(0, velY), 25, idAsteroide);
    }

    private async Task ContaFrames(CancellationToken cts)
    {
        const double targetFps = 60.0;
        const double targetFrameTimeMilliseconds = 1000.0 / targetFps;
        var stopwatch = new Stopwatch();

        while (!cts.IsCancellationRequested)
        {
            stopwatch.Restart();

            contagemDeFrames += 1;
            if (contagemDeFrames >= 60)
            {
                contagemDeFrames = 0;
            }

            if (contagemDeFrames % 40 == 0 && !fimDeJogo)
            {
                asteroides.Add(NovoAsteroide());
                idAsteroide++;
            }

            double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            var timeToWait = (int)(targetFrameTimeMilliseconds - elapsedMilliseconds);
            if (timeToWait > 0)
            {
                await Task.Delay(timeToWait);
            }
        }
    }

    public static async Task Main(string[] args)
    {
        var programa = new Programa();
        await programa.ExecutarAsync();
    }
}