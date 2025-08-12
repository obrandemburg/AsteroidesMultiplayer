using Cliente.Servicos;

namespace Cliente;

public static class Program
{

    public static async Task Main()
    {
        //string ipServidor = "187.20.76.23";
        string ipServidor = "localhost";
        int portaServidor = 12345;

        Console.Title = "CLIENTE";
        Console.WriteLine("Iniciando cliente...");
        try
        {
            GerenciadorDeRede gerenciador = await GerenciadorDeRede.CriaEConecta(ipServidor, portaServidor);
            Console.WriteLine("Entrando no jogo");
            using var jogo = new JogoAsteroides(gerenciador);
            jogo.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERRO: " + ex.Message);
        }
    }

}

