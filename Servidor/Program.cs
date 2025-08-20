using Microsoft.Xna.Framework;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using Asteroides.Compartilhado.Contratos;
using Microsoft.VisualBasic;

public class Programa
{
    private readonly Servidor.GerenciadorDeRede _servidor;

    public Programa()
    {
        _servidor = new Servidor.GerenciadorDeRede();
        _servidor.OnMensagemRecebida += ProcessarLogicaDoJogo;
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

                break;

            case 2:
                break;
        }
    }
    /*
    private static void AtualizarNave(bool left, bool right, bool up, bool down, int w, int h)
    {
        Vector2 dir = Vector2.Zero;
        if (left) dir.X -= 2;
        if (right) dir.X += 2;
        if (up) dir.Y -= 2;
        if (down) dir.Y += 2;

        if (dir != Vector2.Zero) dir.Normalize();
        Posicao += dir * Vel;

        /* mantém dentro da tela 
        Posicao.X = Math.Clamp(Posicao.X, HalfW, w - HalfW);
        Posicao.Y = Math.Clamp(Posicao.Y, HalfH, h - HalfH);
    }

*/

    public static async Task Main(string[] args)
    {
        var programa = new Programa();
        await programa.ExecutarAsync();
    }
}