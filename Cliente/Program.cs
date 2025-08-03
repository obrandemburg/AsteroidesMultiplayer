using Asteroides;
using Asteroides.Cliente;
using Microsoft.Xna.Framework.Input;
using Monogame.Processing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public static class Program
{
    public static GerenciadorDeRede gerenciador = new();

    public static async Task Main()
    {
        Console.Title = "CLIENTE";
        Console.WriteLine("Iniciando cliente...");

        await gerenciador.ConectarAsync();

        using var jogo = new JogoAsteroides();
        jogo.Run();
    }
}

