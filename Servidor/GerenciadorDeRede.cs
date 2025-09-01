using Asteroides.Compartilhado.Contratos;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace Servidor
{
    internal class GerenciadorDeRede : IDisposable
    {
        // NOVO: Evento para notificar a classe principal que um cliente conectou
        public event Action<int> OnClienteConectado;
        // NOVO: Evento para notificar que um cliente desconectou
        public event Action<int> OnClienteDesconectado;
        public event Action<MensagemRecebida> OnMensagemRecebida;

        public record class MensagemRecebida(int idCliente, InputCliente inputCliente);

        private TcpListener _listener;

        // NOVO: Dicionários para gerenciar múltiplos clientes de forma segura entre threads
        private readonly ConcurrentDictionary<int, TcpClient> _clientes = new();
        private readonly ConcurrentDictionary<int, StreamWriter> _escritores = new();

        private readonly BlockingCollection<MensagemRecebida> _mensagensRecebidas = new();
        private readonly BlockingCollection<string> _mensagensEnviadas = new();

        private int _proximoIdCliente = 1;
        private const int MAX_CLIENTS = 2;

        private readonly CancellationTokenSource _cts = new();

        public GerenciadorDeRede()
        {
            _listener = new TcpListener(IPAddress.Any, 12345);
        }

        // ALTERADO: O nome e a lógica mudaram. Agora ele apenas inicia o servidor.
        public void IniciarServidor()
        {
            _listener.Start();
            Console.WriteLine("Servidor iniciado. Aguardando clientes...");

            // Inicia as tarefas de fundo que processam as filas de mensagens
            Task.Run(() => EnviarMensagensLoopAsync(_cts.Token));
            Task.Run(() => ProcessarRecebidosLoop(_cts.Token));

            // Inicia a tarefa que fica aceitando novas conexões
            Task.Run(() => AceitarClientesLoopAsync(_cts.Token));
        }

        private async Task AceitarClientesLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _clientes.Count < MAX_CLIENTS)
            {
                try
                {
                    TcpClient novoCliente = await _listener.AcceptTcpClientAsync(token);

                    int idCliente = _proximoIdCliente++;
                    _clientes[idCliente] = novoCliente;

                    var stream = novoCliente.GetStream();
                    var reader = new StreamReader(stream);
                    _escritores[idCliente] = new StreamWriter(stream) { AutoFlush = true };

                    Console.WriteLine($"Cliente {idCliente} conectado!");

                    // Dispara o evento para a classe Programa saber que um novo jogador entrou
                    OnClienteConectado?.Invoke(idCliente);

                    // Inicia uma tarefa dedicada para ouvir apenas este cliente
                    Task.Run(() => OuvirClienteAsync(reader, token, idCliente, ConsoleColor.Cyan));
                }
                catch (OperationCanceledException)
                {
                    // O servidor está sendo desligado, saia do loop.
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao aceitar cliente: {ex.Message}");
                }
            }
            Console.WriteLine("Servidor não está mais aceitando novos clientes (limite atingido ou desligando).");
        }

        private async Task OuvirClienteAsync(StreamReader clienteReader, CancellationToken token, int idCliente, ConsoleColor cor)
        {
            Console.ForegroundColor = cor;
            Console.WriteLine($"Ouvindo Cliente {idCliente}.");
            try
            {
                string? json;
                while ((json = await clienteReader.ReadLineAsync(token)) != null)
                {
                    var dadosCliente = JsonSerializer.Deserialize<InputCliente>(json);
                    if (dadosCliente != null)
                    {
                        _mensagensRecebidas.Add(new MensagemRecebida(idCliente, dadosCliente));
                    }
                }
            }
            catch (OperationCanceledException) { /* Silencioso, é esperado ao desligar */ }
            catch (IOException) { /* Ocorre quando o cliente desconecta abruptamente */ }
            catch (Exception ex) { Console.WriteLine($"Erro inesperado na escuta de {idCliente}: {ex.Message}"); }
            finally
            {
                Console.WriteLine($"Cliente {idCliente} desconectado.");
                // Limpa os recursos deste cliente
                _clientes.TryRemove(idCliente, out var client);
                _escritores.TryRemove(idCliente, out var writer);
                client?.Close();
                writer?.Dispose();
                OnClienteDesconectado?.Invoke(idCliente);
            }
        }

        // ALTERADO: Renomeado e agora envia para TODOS os clientes conectados
        private async Task EnviarMensagensLoopAsync(CancellationToken token)
        {
            try
            {
                foreach (var mensagem in _mensagensEnviadas.GetConsumingEnumerable(token))
                {
                    var tarefasDeEnvio = new List<Task>();
                    foreach (var writer in _escritores.Values)
                    {
                        tarefasDeEnvio.Add(writer.WriteLineAsync(mensagem));
                    }
                    await Task.WhenAll(tarefasDeEnvio);
                }
            }
            catch (OperationCanceledException) { Console.WriteLine("Tarefa de envio de mensagens cancelada."); }
        }

        private void ProcessarRecebidosLoop(CancellationToken token)
        {
            try
            {
                foreach (var inputCliente in _mensagensRecebidas.GetConsumingEnumerable(token))
                {
                    OnMensagemRecebida?.Invoke(inputCliente);
                }
            }
            catch (OperationCanceledException) { Console.WriteLine("Tarefa de processamento cancelada."); }
        }

        public void EnviarMensagemParaTodos(string mensagem)
        {
            if (!_mensagensEnviadas.IsAddingCompleted)
            {
                _mensagensEnviadas.Add(mensagem);
            }
        }

        public void Encerrar()
        {
            if (!_cts.IsCancellationRequested)
            {
                Console.WriteLine("--- Encerrando o servidor... ---");
                _cts.Cancel();
                _listener.Stop();

                // Fecha todas as conexões de clientes
                foreach (var cliente in _clientes.Values)
                {
                    cliente.Close();
                }
                _mensagensEnviadas.CompleteAdding();
                _mensagensRecebidas.CompleteAdding();
            }
        }

        public void Dispose()
        {
            Encerrar();
            _cts.Dispose();
        }
    }
}