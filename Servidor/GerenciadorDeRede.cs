using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Servidor
{
    // A classe agora implementa IDisposable para gerenciar os recursos
    internal class GerenciadorDeRede : IDisposable
    {
        // Propriedades para acessar os leitores e escritores de fora da classe
        public StreamReader Reader1 { get; private set; }
        public StreamWriter Writer1 { get; private set; }
        public StreamReader Reader2 { get; private set; }
        public StreamWriter Writer2 { get; private set; }

        private TcpListener _listener;
        private TcpClient _client1;
        private TcpClient _client2;

        // O construtor agora é simples e apenas prepara o listener
        public GerenciadorDeRede()
        {
            _listener = new TcpListener(IPAddress.Any, 12345);
        }

        // Método async para iniciar o servidor e aceitar os clientes
        public async Task IniciarEConectarClientesAsync()
        {
            _listener.Start();
            Console.WriteLine("Servidor iniciado. Aguardando 2 clientes...");

            // Aceita o primeiro cliente
            _client1 = await _listener.AcceptTcpClientAsync();
            var stream1 = _client1.GetStream();
            Reader1 = new StreamReader(stream1);
            Writer1 = new StreamWriter(stream1) { AutoFlush = true };
            await Writer1.WriteLineAsync("1");
            
            Console.WriteLine("Cliente 1 conectado!");

            // Aceita o segundo cliente
            _client2 = await _listener.AcceptTcpClientAsync();
            var stream2 = _client2.GetStream(); // Correção do erro de lógica
            Reader2 = new StreamReader(stream2);
            Writer2 = new StreamWriter(stream2) { AutoFlush = true };
            await Writer2.WriteLineAsync("2");

            Console.WriteLine("Cliente 2 conectado!");

            Console.WriteLine("Ambos os clientes estão conectados e prontos para comunicação.");
        }

        public async Task EnviarMensagemParaClienteAsync(int clienteId, string mensagem)
        {
            if (clienteId == 1 && Writer1 != null)
            {
                await Writer1.WriteLineAsync(mensagem);
            }
            else if (clienteId == 2 && Writer2 != null)
            {
                await Writer2.WriteLineAsync(mensagem);
            }
            else
            {
                throw new ArgumentException("ID do cliente inválido.");
            }
        }

        public async Task<string> LerMensagemDoClienteAsync(int clienteId)
        {
            if (clienteId == 1 && Reader1 != null)
            {
                return await Reader1.ReadLineAsync();
            }
            else if (clienteId == 2 && Reader2 != null)
            {
                return await Reader2.ReadLineAsync();
            }
            else
            {
                throw new ArgumentException("ID do cliente inválido.");
            }
        }

        // Método para limpar todos os recursos quando o objeto for descartado
        public void Dispose()
        {
            Console.WriteLine("Encerrando conexões...");
            // Fecha todos os recursos na ordem correta
            Writer1?.Dispose();
            Reader1?.Dispose();
            _client1?.Close();

            Writer2?.Dispose();
            Reader2?.Dispose();
            _client2?.Close();

            _listener?.Stop();
        }
    }
}