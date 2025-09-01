namespace Cliente;

public static class Program
{
    public static void Main()
    {
        Console.Title = "CLIENTE ASTEROIDES";

        using var jogo = new JogoAsteroides();
        jogo.Run();
    }
}
//string ipServidor = "187.20.76.23";