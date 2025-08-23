using Asteroides;
using Asteroides.Compartilhado.Contratos;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

public class Programa
{
    //Variáveis do jogo:
    Nave nave1;
    Nave nave2;
    readonly List<Tiro> tiros = new();
    readonly List<Asteroide> asteroides = new();
    readonly Random rnd = new();
    int pontos = 0;
    const int width = 1280, height = 720;
    

    //Variáveis de controle
    CancellationTokenSource cts = new();
    int contagemDeFrames = 0;


    private readonly Servidor.GerenciadorDeRede _servidor;

    public Programa()
    {
        _servidor = new Servidor.GerenciadorDeRede();
        _servidor.OnMensagemRecebida += ProcessarLogicaDoJogo;
        nave1 = new Nave(new Vector2(100, 100));
        nave2 = new Nave(new Vector2(200, 100));
        Task.Run(() => ContaFrames(cts.Token));
    }

    /// <summary>
    /// Orquestra o ciclo de vida da aplicação: Iniciar, Aguardar e Encerrar.
    /// </summary>
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

    private void ProcessarLogicaDoJogo(string mensagemJson)
    {
        var mensagemDesserializada = JsonSerializer.Deserialize<InputCliente>(mensagemJson);

        switch (mensagemDesserializada.id)
        {
            case 1:
                //atualiza a nave 1
                nave1.ConverterParaVariavel(mensagemDesserializada);
                break;

            case 2:
                //atualiza a nave 2
                nave2.ConverterParaVariavel(mensagemDesserializada);
                break;
        }


    }
    Asteroide NovoAsteroide()
    {
        float x = rnd.Next(width);
        float velY = 2f + (float)rnd.NextDouble() * 2f;   // 2–4 px/frame
        return new Asteroide(new Vector2(x, -30), new Vector2(0, velY), 25);
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