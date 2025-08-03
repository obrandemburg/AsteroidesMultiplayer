using System.Net.Sockets;

namespace Asteroides.Cliente
{
    public class GerenciadorDeRede
    {
        TcpClient client = null;
        NetworkStream fluxo = null;
        StreamReader leitor = null;
        StreamWriter escritor = null;


        public GerenciadorDeRede()
        {
            client = new TcpClient();
        }

        public async Task ConectarAsync()
        {
            try
            {
                Console.WriteLine("Tentando conectar ao servidor...");
                await client.ConnectAsync("187.20.76.23", 12345);
                Console.WriteLine("Conectado ao servidor!");

                fluxo = client.GetStream();
                leitor = new StreamReader(fluxo);
                escritor = new StreamWriter(fluxo) { AutoFlush = true };
            }
            catch (SocketException)
            {
                Console.WriteLine("Não foi possível conectar ao servidor.");
            }
        }
        public async Task EnviarMensagemAsync(string mensagem)
        {
            if (escritor == null)
            {
                throw new InvalidOperationException("Não foi possível enviar a mensagem. Conexão não estabelecida.");
            }
            try
            {
                await escritor.WriteLineAsync(mensagem);
                Console.WriteLine($"Mensagem enviada: {mensagem}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
            }
        }

        public async Task<string?> LerMensagemAsync()
        {
            if (leitor == null)
            {
                throw new InvalidOperationException("Não foi possível ler a mensagem. Conexão não estabelecida.");
            }
            try
            {
                string mensagem = await leitor.ReadLineAsync();
                Console.WriteLine($"Mensagem recebida: {mensagem}");
                return mensagem;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Erro ao ler mensagem: {ex.Message}");
                return null;
            }
        }
        public void Dispose()
        {
            escritor?.Dispose();
            leitor?.Dispose();
            fluxo?.Dispose();
            client?.Close();

        }
    }
}