using Asteroides;
using Microsoft.Xna.Framework.Input;
using Monogame.Processing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Cliente.Servicos;
using System.Linq.Expressions;

namespace Asteroides.Cliente;

public static class Program
{

    public static async Task Main()
    {
        string ipServidor = "187.20.76.23";
        int portaServidor = 12345;

        Console.Title = "CLIENTE";
        Console.WriteLine("Iniciando cliente...");
        try
        {
            GerenciadorDeRede gerenciador = await GerenciadorDeRede.CriaEConecta(ipServidor, portaServidor);

            using var jogo = new JogoAsteroides(gerenciador);
            jogo.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERRO: " + ex.Message);
        }
    }

}

