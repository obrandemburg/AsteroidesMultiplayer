using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Cliente.Servicos
{
    public class GerenciadorDeRede
    {
        private TcpClient _cliente;
        private NetworkStream _fluxo;
        private StreamReader _leitor;
        private StreamWriter _escritor;

        private readonly ConcurrentQueue<string> _filaDeEnvio = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _filaDeRecebimento = new ConcurrentQueue<string>();

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private GerenciadorDeRede() {
            _cliente = new TcpClient();
        }

        public static async Task<GerenciadorDeRede> CriaEConecta(string ipServidor, int portaServidor)
        {

            GerenciadorDeRede gerenciador = new GerenciadorDeRede();

            await gerenciador.ConectarAsync(ipServidor, portaServidor);
            gerenciador.IniciarLoopsDeRede();

            return gerenciador;
        }
        private async Task ConectarAsync(string ipServidor, int portaServidor)
        {
            try
            {
                Console.WriteLine("Tentando conectar ao servidor...");
                await _cliente.ConnectAsync(ipServidor, portaServidor);
                Console.WriteLine("Conectado ao servidor!");

                _fluxo = _cliente.GetStream();
                _leitor = new StreamReader(_fluxo);
                _escritor = new StreamWriter(_fluxo) { AutoFlush = true };
            }
            catch (SocketException)
            {
                Console.WriteLine("Não foi possível conectar ao servidor.");
                throw;
            }
        }
        private void IniciarLoopsDeRede()
        {
            _cts = new CancellationTokenSource();

            Task.Run(() => LoopDeRecebimentoAsync(_cts.Token));
            Task.Run(() => LoopDeEnvioAsync(_cts.Token));
        }

        public void EnviarMensagem(string mensagem)
        {
            if (!string.IsNullOrEmpty(mensagem))
            {
                _filaDeEnvio.Enqueue(mensagem);
            }
        }
        private async Task LoopDeEnvioAsync(CancellationToken token)
        {
            Console.WriteLine("Thread de envio iniciada.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_filaDeEnvio.TryDequeue(out string mensagem))
                    {
                        await _escritor.WriteLineAsync(mensagem);
                    }
                    await Task.Delay(10, token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Console.WriteLine($"Erro no envio: {ex.Message}"); break; }
            }
            Console.WriteLine("Thread de envio finalizada.");
        }

        public bool TentarReceberMensagem(out string mensagem)
        {
            return _filaDeRecebimento.TryDequeue(out mensagem);
        }
        private async Task LoopDeRecebimentoAsync(CancellationToken token)
        {
            Console.WriteLine("Thread de recebimento iniciada.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string? dadosRecebidos = await _leitor.ReadLineAsync();

                    
                    if (dadosRecebidos == null)
                    {
                        Console.WriteLine("O servidor fechou a conexão.");
                        break;
                    }

                    _filaDeRecebimento.Enqueue(dadosRecebidos);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Console.WriteLine($"Erro no recebimento: {ex.Message}"); break; }
            }
            Console.WriteLine("Thread de recebimento finalizada.");
        }
        public void Desconectar()
        {
            Console.WriteLine("Desconectando...");
            _cts?.Cancel();
            _escritor?.Dispose();
            _leitor?.Dispose();
            _cliente?.Close();
        }

    }
}