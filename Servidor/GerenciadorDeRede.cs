using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Servidor
{
    internal class GerenciadorDeRede : IDisposable
    {

        public event Action<string> OnMensagemRecebida;
        public StreamReader Reader1 { get; private set; }
        public StreamWriter Writer1 { get; private set; }
        public StreamReader Reader2 { get; private set; }
        public StreamWriter Writer2 { get; private set; }

        private TcpListener _listener;
        private TcpClient _client1;
        private TcpClient _client2;

        private readonly BlockingCollection<string> _mensagensRecebidas = new BlockingCollection<string>();
        private readonly BlockingCollection<string> _mensagensEnviadas = new BlockingCollection<string>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public GerenciadorDeRede()
        {
            _listener = new TcpListener(IPAddress.Any, 12345);
        }

        public async Task IniciarEConectarClientesAsync()
        {
            _listener.Start();
            Console.WriteLine("Servidor iniciado. Aguardando 2 clientes...");

            _client1 = await _listener.AcceptTcpClientAsync();
            var stream1 = _client1.GetStream();
            Reader1 = new StreamReader(stream1);
            Writer1 = new StreamWriter(stream1) { AutoFlush = true };

            Console.WriteLine("Cliente 1 conectado!");

            _client2 = await _listener.AcceptTcpClientAsync();
            var stream2 = _client2.GetStream();
            Reader2 = new StreamReader(stream2);
            Writer2 = new StreamWriter(stream2) { AutoFlush = true };

            Console.WriteLine("Cliente 2 conectado!");

            Console.WriteLine("Ambos os clientes estão conectados e prontos para comunicação.");
            Console.WriteLine("Iniciando Leitor e escritor em threads diferentes");
            Task tarefa1 = Task.Run(() => OuvirClienteAsync(Reader1, _cts.Token, "Cliente 1", ConsoleColor.Cyan));
            Task tarefa2 = Task.Run(() => OuvirClienteAsync(Reader2, _cts.Token, "Cliente 2", ConsoleColor.Yellow));
            Task tarefa3 = Task.Run(() => EnviarMensagemAsync(_cts.Token));
            Task tarefa4 = Task.Run(() => ProcessarRecebidosLoop(_cts.Token));
        }

        private async Task OuvirClienteAsync(StreamReader clienteReader, CancellationToken token, string cliente, ConsoleColor cor)
        {
            ConsoleColor corOriginal = Console.ForegroundColor;
            Console.ForegroundColor = cor;
            Console.WriteLine($"Ouvindo {cliente}.");
            try
            {
                string? json;
                while ((json = await clienteReader.ReadLineAsync(token)) != null)
                {
                    Console.ForegroundColor = cor;
                    _mensagensRecebidas.Add(json);
                    Console.WriteLine($"[Gerenciador de Rede] Mensagem recebida de {cliente}: {json}");
                    Console.WriteLine();
                    Console.ForegroundColor = corOriginal;
                }

                Console.WriteLine($"{cliente} desconectou de forma limpa.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"A escuta de {cliente} foi cancelada.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"A conexão com {cliente} foi perdida: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado na escuta de {cliente}: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Parando de ouvir {cliente}.");
            }
        }

        private async Task EnviarMensagemAsync(CancellationToken cts)
        {
            try
            {
                foreach (var mensagem in _mensagensEnviadas.GetConsumingEnumerable(cts))
                {

                    Task tarefa1 = Writer1.WriteLineAsync(mensagem);
                    Task tarefa2 = Writer2.WriteLineAsync(mensagem);
                    await Task.WhenAll(tarefa1, tarefa2);
                    Console.WriteLine($"[Gerenciador de Rede] Mensagem enviada: {mensagem}");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Tarefa de envio de mensagens cancelada.");
            }
        }
        private void ProcessarRecebidosLoop(CancellationToken token)
        {
            Console.WriteLine("[TAREFA INICIADA] Processador de Mensagens Recebidas.");
            try
            {
                // Este foreach espera passivamente até que uma mensagem seja adicionada à fila _mensagensRecebidas.
                foreach (var json in _mensagensRecebidas.GetConsumingEnumerable(token))
                {
                    // Dispara o evento para notificar o código externo.
                    OnMensagemRecebida?.Invoke(json);
                }
            }
            catch (OperationCanceledException) { Console.WriteLine("Tarefa de processamento cancelada."); }
        }

        public void EnviarMensagem(string mensagem)
        {
            _mensagensEnviadas.Add(mensagem);
        }

        public void Encerrar()
        {
            if (!_cts.IsCancellationRequested)
            {
                Console.WriteLine("--- Encerrando o servidor... ---");
                _cts.Cancel();
            }
        }


        public void Dispose()
        {
            Console.WriteLine("Encerrando conexões...");
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