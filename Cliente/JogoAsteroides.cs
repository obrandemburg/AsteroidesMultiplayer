using Asteroides.Compartilhado.Contratos;
using Cliente.Entidades;
using Cliente.Servicos;
using Microsoft.Xna.Framework.Input;
using Monogame.Processing;
using System.Text.Json;
using Asteroides.Compartilhado.Interfaces;

namespace Cliente;

public class JogoAsteroides : Processing
{
    private EstadoDoJogo _estadoAtual;
    private StatusConexao _statusConexao;
    private GerenciadorDeRede? _gerenciadorDeRede;

    private readonly Dictionary<int, Nave> _naves = new();
    private readonly Dictionary<int, Tiro> _tiros = new();
    private readonly Dictionary<int, Asteroide> _asteroides = new();

    private PImage _spriteNave, _spriteAsteroide, _spriteTiro;
    private bool fimDeJogo = false;
    private int _pontuacao;

    private bool _esquerda, _direita, _cima, _baixo, _atirando;
    private string _mensagemDeErro = "";

    public JogoAsteroides()
    {
        _estadoAtual = EstadoDoJogo.Menu;
        _statusConexao = StatusConexao.NaoConectado;
    }

    public override void Setup()
    {
        size(1280, 720);
        _spriteNave = loadImage("Content/nave.png");
        _spriteAsteroide = loadImage("Content/AsteroidBrown.png");
        _spriteTiro = loadImage("Content/tiro.png");
    }

    public override void Draw()
    {
        Teclas();

        switch (_estadoAtual)
        {
            case EstadoDoJogo.Menu:
            case EstadoDoJogo.Conectando:
            case EstadoDoJogo.ErroDeConexao:
                DesenharMenu();
                break;
            case EstadoDoJogo.Jogando:
                DesenharJogo();
                break;
        }
    }

    private void DesenharMenu()
    {
        background(10, 10, 20);
        textAlign(Monogame.Processing.TextAlign.CENTER);

        textSize(64);
        fill(255);
        text("ASTEROIDES MULTIPLAYER", width / 2, height / 3);

        textSize(32);
        fill(200);
        if (_estadoAtual == EstadoDoJogo.Menu || _estadoAtual == EstadoDoJogo.ErroDeConexao)
        {
            text("Pressione ENTER para conectar", width / 2, height / 2);
        }

        string statusTexto = "";
        switch (_statusConexao)
        {
            case StatusConexao.Tentando:
                fill(255, 255, 0);
                statusTexto = "Conectando ao servidor...";
                break;
            case StatusConexao.Conectado:
                fill(0, 255, 0);
                statusTexto = "Conectado com sucesso!";
                break;
            case StatusConexao.Erro:
                fill(255, 0, 0);
                statusTexto = $"Falha ao conectar: {_mensagemDeErro}";
                break;
        }

        if (_statusConexao != StatusConexao.NaoConectado)
        {
            float statusY = height * 0.7f;
            ellipse(width / 2 - 250, statusY, 20, 20);

            textSize(24);
            textAlign(Monogame.Processing.TextAlign.LEFT);
            text(statusTexto, width / 2 - 230, statusY - 12);
        }
    }

    private void DesenharJogo()
    {
        if (fimDeJogo)
        {
            background(20, 10, 10);
            textSize(40);
            fill(255, 0, 0);
            textAlign(Monogame.Processing.TextAlign.CENTER);
            text("GAME OVER", width / 2, height / 2);
            textSize(32);
            fill(255);
            text($"Pontuacao Final: {_pontuacao}", width / 2, height / 2 + 100);
            return;
        }

        InputCliente inputCliente = new InputCliente
        {
            Cima = _cima,
            Baixo = _baixo,
            Esquerda = _esquerda,
            Direita = _direita,
            Atirando = _atirando
        };
        _gerenciadorDeRede?.EnviarMensagem(inputCliente);

        background(10, 10, 20);

        foreach (var asteroide in _asteroides.Values) asteroide.Desenhar(this);
        foreach (var tiro in _tiros.Values) tiro.Desenhar(this);
        foreach (var nave in _naves.Values) nave.Desenhar(this);

        // CORREÇÃO: Usando o nome completo da enumeração.
        textAlign(Monogame.Processing.TextAlign.LEFT);
        fill(255);
        text($"Pontos: {_pontuacao}", 10, 10);
    }

    private async Task IniciarConexaoAsync()
    {
        if (_estadoAtual == EstadoDoJogo.Conectando) return;

        _estadoAtual = EstadoDoJogo.Conectando;
        _statusConexao = StatusConexao.Tentando;
        _mensagemDeErro = "";

        try
        {
            string ipServidor = "localhost";
            int portaServidor = 12345;
            _gerenciadorDeRede = await GerenciadorDeRede.CriaEConecta(ipServidor, portaServidor);
            _gerenciadorDeRede.OnMensagemRecebida += ProcessarMensagemDoServidor;

            _statusConexao = StatusConexao.Conectado;
            await Task.Delay(1000);
            _estadoAtual = EstadoDoJogo.Jogando;
        }
        catch (Exception ex)
        {
            _statusConexao = StatusConexao.Erro;
            _estadoAtual = EstadoDoJogo.ErroDeConexao;
            _mensagemDeErro = ex.Message;
            _gerenciadorDeRede = null;
        }
    }

    private void ProcessarMensagemDoServidor(string json)
    {
        try
        {
            var msgBase = JsonSerializer.Deserialize<MensagemBase>(json);

            if (msgBase?.Tipo == "ESTADO_MUNDO")
            {
                var estadoMundo = JsonSerializer.Deserialize<EstadoMundoMensagem>(json);
                if (estadoMundo != null)
                {
                    _pontuacao = estadoMundo.Pontos;
                    fimDeJogo = estadoMundo.FimDeJogo;
                    if (!fimDeJogo)
                    {
                        AtualizarEntidades(estadoMundo);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar mensagem JSON: {ex.Message}");
        }
    }

    private void AtualizarEntidades(EstadoMundoMensagem estadoMundo)
    {
        SincronizarListaComDicionario(estadoMundo.Naves, _naves, estado => new Nave(estado, _spriteNave));
        SincronizarListaComDicionario(estadoMundo.Asteroides, _asteroides, estado => new Asteroide(estado, _spriteAsteroide));
        SincronizarListaComDicionario(estadoMundo.Tiros, _tiros, estado => new Tiro(estado, _spriteTiro));
    }

    private void SincronizarListaComDicionario<TEstado, TEntidade>(
        List<TEstado> listaDeEstados, Dictionary<int, TEntidade> dicionarioDeEntidades,
        Func<TEstado, TEntidade> criarNovaEntidade)
        where TEstado : IEstadoComId
        where TEntidade : class, IEntidadeComEstado<TEstado>
    {
        var idsRecebidos = new HashSet<int>(listaDeEstados.Select(e => e.Id));
        var idsParaRemover = dicionarioDeEntidades.Keys.Where(id => !idsRecebidos.Contains(id)).ToList();
        foreach (var id in idsParaRemover) dicionarioDeEntidades.Remove(id);

        foreach (var estado in listaDeEstados)
        {
            if (dicionarioDeEntidades.TryGetValue(estado.Id, out var entidadeExistente))
            {
                entidadeExistente.Estado = estado;
            }
            else
            {
                dicionarioDeEntidades[estado.Id] = criarNovaEntidade(estado);
            }
        }
    }

    public void Teclas()
    {
        var keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            _gerenciadorDeRede?.Desconectar();
            Exit();
        }

        if (_estadoAtual == EstadoDoJogo.Menu || _estadoAtual == EstadoDoJogo.ErroDeConexao)
        {
            // Adicionado um pequeno delay para não registrar o Enter múltiplas vezes
            if (keyboardState.IsKeyDown(Keys.Enter) && !IsKeyPressed(Keys.Enter))
            {
                Task.Run(IniciarConexaoAsync);
            }
        }
        else if (_estadoAtual == EstadoDoJogo.Jogando)
        {
            _esquerda = keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left);
            _direita = keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right);
            _cima = keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up);
            _baixo = keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down);
            _atirando = keyboardState.IsKeyDown(Keys.Space);
        }

        // Atualiza o estado anterior do teclado para o controle do Enter
        UpdateKeyStates();
    }

    // Pequeno sistema para evitar pressionamento repetido de teclas
    private KeyboardState _previousKeyboardState;
    private void UpdateKeyStates() => _previousKeyboardState = Keyboard.GetState();
    private bool IsKeyPressed(Keys key) => _previousKeyboardState.IsKeyDown(key);
}
