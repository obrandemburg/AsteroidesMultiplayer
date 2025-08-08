using Asteroides;
using Servidor;

Console.Title = "SERVIDOR";
Console.WriteLine("Iniciando servidor...");

using (var gerenciador = new GerenciadorDeRede())
{
    await gerenciador.IniciarEConectarClientesAsync();

    try
    {
        

        List<Tiro> tiros = new();
        List<Asteroide> asteroides = new();
        Random rnd = new();


    }
    catch (IOException)
    {
        Console.WriteLine("Cliente desconectado abruptamente.");
    }
}

static void CarregaJogo()
{

}
static void AtualizaAsteroide()
{

}
static void AtualizaNave()
{

}
static void AtualizaTiro()
{

}