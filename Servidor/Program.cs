using Asteroides;
using Asteroides.Compartilhado.Contratos;
using Asteroides.Compartilhado.Estados;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
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
    int aid = 0;


    //Variáveis de controle
    CancellationTokenSource cts = new();
    int contagemDeFrames = 0;


    private readonly Servidor.GerenciadorDeRede _servidor;

    public Programa()
    {
        _servidor = new Servidor.GerenciadorDeRede();
        _servidor.OnMensagemRecebida += ProcessarLogicaDoJogo;
        naves.Add (new Nave(new Vector2(100, 100), 1));
        naves.Add(new Nave(new Vector2(200, 100), 2));
        Task.Run(() => ContaFrames(cts.Token));
    }

    public async Task ExecutarAsync()
    {
        Console.WriteLine("Aplicação iniciada.");
        Console.Title = "Servidor do Jogo";

        try
        {
            // FASE 1: INICIAR
            Console.WriteLine("Iniciando o servidor de rede...");
            await _servidor.IniciarEConectarClientesAsync();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Servidor de rede está online. Todas as tarefas de fundo estão ativas.");
            Console.ResetColor();

            // FASE 2: AGUARDAR
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
            // FASE 3: ENCERRAR
            Console.WriteLine("Sinal de desligamento recebido. Iniciando processo de encerramento...");
            _servidor.Encerrar();
        }

        Console.WriteLine("Aplicação finalizada.");
    }

    private void ProcessarLogicaDoJogo(MensagemRecebida msg)
    {

        Console.WriteLine("conteúdo json chegando como parâmetro de ProcessarLogicadoJogo: " + msg.inputCliente);
        Console.WriteLine();

        switch (msg.idCliente)
        {
            case 1:
                //atualiza a nave 1
                naves[0].ConverterParaVariavel(msg.inputCliente);
                break;

            case 2:
                //atualiza a nave 2
                naves[1].ConverterParaVariavel(msg.inputCliente);
                break;
        }
        //atualiza os tiros
        foreach (var t in tiros)
        {
            t.Atualizar();
            if (t.ForaDaTela(height))
            {
                tiros.Remove(t);
                break;
            }
        }
        //atualiza os asteroides
        foreach (var a in asteroides)
        {
            a.Atualizar();
            //verifica colisão com tiros
            foreach (var t in tiros)
            {
                if (Vector2.Distance(a.pos, t.Pos) < a.Raio)
                {
                    asteroides.Remove(a);
                    tiros.Remove(t);
                    pontos += 100;
                    break;
                }
            }
        }
        /*
        //verifica colisão com naves
        foreach (var a in asteroides)
        {
            foreach (var n in naves)
            {
                if (Vector2.Distance(a.pos, n.Posicao) < a.Raio + 8)
                {
                    asteroides.Remove(a);
                    break;
                }
            }
        }
        */
        //envia o estado do mundo atualizado para os clientes
        var estadoDoMundo = CriarEstadoDoMundo();
        var estadoJson = JsonSerializer.Serialize(estadoDoMundo);
        _servidor.EnviarMensagem(estadoJson);

        Console.WriteLine($"Enviado EstadoMundo para o cliente a string: {estadoJson}");
        Console.WriteLine();


    }
    private EstadoMundoMensagem CriarEstadoDoMundo()
    {
        // Usa .Select() em TODAS as listas!
        var navesEstado = naves.Select(n => new NaveEstado
        {
            Id = n.Id,
            PosicaoX = n.Posicao.X,
            PosicaoY = n.Posicao.Y
        }).ToList();

        var tirosEstado = tiros.Select(t => new TiroEstado
        {
            Id = t.Id,
            PosicaoX = t.Pos.X,
            PosicaoY = t.Pos.Y,
        }).ToList();

        var asteroidesEstado = asteroides.Select(a => new AsteroideEstado
        {
            Id = a.Id,
            PosicaoX = a.pos.X,
            PosicaoY = a.pos.Y,
        }).ToList();


        // Monta o objeto final para enviar
        var estado = new EstadoMundoMensagem
        {
            Naves = navesEstado,
            Tiros = tirosEstado,
            Asteroides = asteroidesEstado,
            Pontos = this.pontos
        };

        return estado;
    }
    Asteroide NovoAsteroide()
    {
        float x = rnd.Next(width);
        float velY = 2f + (float)rnd.NextDouble() * 2f;   // 2–4 px/frame
        return new Asteroide(new Vector2(x, -30), new Vector2(0, velY), 25, aid);
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

            if (contagemDeFrames % 40 == 0)
            {
                asteroides.Add(NovoAsteroide());
                aid++;
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