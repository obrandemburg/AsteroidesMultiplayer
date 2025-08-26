using Asteroides.Compartilhado.Contratos;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;

namespace Cliente.Servicos
{
    public class GerenciadorDeRede
    {

        private TcpClient _cliente;
        private NetworkStream _fluxo;
        private StreamReader _leitor;
        private StreamWriter _escritor;

        private readonly BlockingCollection<string> _filaDeEnvio = new BlockingCollection<string>();
        private readonly BlockingCollection<string> _filaDeRecebimento = new BlockingCollection<string>();

        public event Action<string> OnMensagemRecebida;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private GerenciadorDeRede()
        {
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
                Console.ReadLine();
                throw;
            }
        }
        private void IniciarLoopsDeRede()
        {
            _cts = new CancellationTokenSource();

            Task.Run(() => LoopDeRecebimentoAsync(_cts.Token));
            Task.Run(() => LoopDeEnvioAsync(_cts.Token));
            Task.Run(() => AvisoMensagemRecebida(_cts.Token));
        }

        public void EnviarMensagem(MensagemBase mensagem)
        {
            if (mensagem != null)
            {
                string jsonMensagem = JsonSerializer.Serialize(mensagem, mensagem.GetType());
                _filaDeEnvio.Add(jsonMensagem);
            }
        }

        private async Task LoopDeEnvioAsync(CancellationToken token)
        {
            Console.WriteLine("Thread de envio iniciada.");

            try
            {
                foreach (var mensagemjson in _filaDeEnvio.GetConsumingEnumerable(token))
                {
                    await _escritor.WriteLineAsync(mensagemjson);
                }
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine($"Erro: {e.Message}");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no envio: {ex.Message}");
                Console.ReadLine();
            }

            Console.WriteLine("Thread de envio finalizada.");
            Console.ReadLine();

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

                    _filaDeRecebimento.Add(dadosRecebidos);
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine($"Erro: {e.Message}");
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro no recebimento: {ex.Message}");
                    Console.ReadLine();
                }
            }
            Console.WriteLine("Thread de recebimento finalizada.");
        }

        private void AvisoMensagemRecebida(CancellationToken token)
        {
            Console.WriteLine("Iniciado Alerta de recebimento de mensagens");
            foreach (var mensagemJson in _filaDeRecebimento.GetConsumingEnumerable(token))
            {
                OnMensagemRecebida?.Invoke(mensagemJson);
            }

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