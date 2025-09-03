using Asteroides.Compartilhado.Contratos;
using Cliente.Entidades;
using Cliente.Servicos;
using Microsoft.Xna.Framework.Input;
using Monogame.Processing;
using System.Text.Json;
using Asteroides.Compartilhado.Interfaces;
using System.Collections.Concurrent; // Necessário para a ConcurrentQueue

namespace Cliente;

public class JogoAsteroides : Processing
{
    private EstadoDoJogo _estadoAtual;
    private StatusConexao _statusConexao;
    private GerenciadorDeRede? _gerenciadorDeRede;

    // --- ALTERAÇÃO: Trocamos as coleções para ConcurrentDictionary ---
    // Embora a lógica de fila resolva o problema, usar ConcurrentDictionary é uma boa prática
    // para quando há potencial de acesso concorrente, mesmo que seja apenas para leitura.
    private readonly ConcurrentDictionary<int, Nave> _naves = new();
    private readonly ConcurrentDictionary<int, Tiro> _tiros = new();
    private readonly ConcurrentDictionary<int, Asteroide> _asteroides = new();

    // --- NOVO: Fila thread-safe para receber atualizações do servidor ---
    private readonly ConcurrentQueue<EstadoMundoMensagem> _filaDeEstados = new();

    private PImage _spriteNave, _spriteAsteroide, _spriteTiro;
    private bool fimDeJogo = false;
    private int _pontuacao;

    private bool _esquerda, _direita, _cima, _baixo, _atirando;
    private string _mensagemDeErro = "";
    private string ipInput = "localhost"; // Mantenha a funcionalidade do menu
    private bool isEditingIp = false;

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
        // --- ALTERAÇÃO: Processa a fila de estados antes de qualquer outra coisa ---
        // Este é o momento seguro para aplicar as atualizações recebidas pela rede.
        ProcessarFilaDeEstados();

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

    // --- NOVO MÉTODO: Responsável por aplicar as atualizações da fila ---
    private void ProcessarFilaDeEstados()
    {
        // Tenta retirar um estado da fila. Se conseguir, atualiza as entidades.
        // O loop garante que processemos todos os estados que chegaram desde o último frame.
        while (_filaDeEstados.TryDequeue(out var estadoMundo))
        {
            AtualizarEntidades(estadoMundo);
        }
    }

    private void DesenharMenu()
    {
        // (O código do DesenharMenu permanece o mesmo da versão anterior)
        background(10, 10, 20);
        textAlign(Monogame.Processing.TextAlign.CENTER);

        textSize(64);
        fill(255);
        text("ASTEROIDES MULTIPLAYER", width / 2, height / 4);

        textSize(28);
        fill(200);
        if (_estadoAtual != EstadoDoJogo.Conectando)
        {
            text("Pressione ENTER para Conectar", width / 2, height / 2 - 20);
            text("Pressione F2 para Editar o IP", width / 2, height / 2 + 20);
        }

        textSize(24);
        fill(180);
        text("IP do Servidor:", width / 2, height * 0.65f - 20);

        if (isEditingIp) { fill(255, 255, 0); } else { fill(255); }

        string textoIpParaMostrar = $"[ {ipInput} ]";
        text(textoIpParaMostrar, width / 2, height * 0.65f + 20);

        if (isEditingIp && (millis() / 500) % 2 == 0)
        {
            text("|", width / 2 / 2 - 12, height * 0.65f + 20);
        }

        string statusTexto = "";
        switch (_statusConexao)
        {
            case StatusConexao.Tentando: fill(255, 255, 0); statusTexto = "Conectando ao servidor..."; break;
            case StatusConexao.Conectado: fill(0, 255, 0); statusTexto = "Conectado com sucesso!"; break;
            case StatusConexao.Erro: fill(255, 0, 0); statusTexto = $"Falha ao conectar: {_mensagemDeErro}"; break;
        }

        if (_statusConexao != StatusConexao.NaoConectado)
        {
            float statusY = height * 0.85f;
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
            // (Código da tela de Game Over)
            background(20, 10, 10);
            textSize(40); fill(255, 0, 0);
            textAlign(Monogame.Processing.TextAlign.CENTER);
            text("GAME OVER", width / 2, height / 2);
            textSize(32); fill(255);
            text($"Pontuacao Final: {_pontuacao}", width / 2, height / 2 + 100);
            return;
        }

        InputCliente inputCliente = new InputCliente { Cima = _cima, Baixo = _baixo, Esquerda = _esquerda, Direita = _direita, Atirando = _atirando };
        _gerenciadorDeRede?.EnviarMensagem(inputCliente);

        background(10, 10, 20);

        // A leitura dos dicionários para desenho agora é segura
        foreach (var asteroide in _asteroides.Values) asteroide.Desenhar(this);
        foreach (var tiro in _tiros.Values) tiro.Desenhar(this);
        foreach (var nave in _naves.Values) nave.Desenhar(this);

        textAlign(Monogame.Processing.TextAlign.LEFT);
        fill(255);
        text($"Pontos: {_pontuacao}", 10, 10);
    }

    private async Task IniciarConexaoAsync()
    {
        // (O código de IniciarConexaoAsync permanece o mesmo)
        if (_estadoAtual == EstadoDoJogo.Conectando) return;
        _estadoAtual = EstadoDoJogo.Conectando;
        _statusConexao = StatusConexao.Tentando;
        _mensagemDeErro = "";
        try
        {
            _gerenciadorDeRede = await GerenciadorDeRede.CriaEConecta(ipInput, 12345);
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

    // --- ALTERAÇÃO: Este método agora apenas enfileira o trabalho ---
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
                    // Em vez de modificar as coleções aqui (o que causa o erro),
                    // nós adicionamos o estado recebido na fila para ser processado pela thread principal.
                    _filaDeEstados.Enqueue(estadoMundo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar mensagem JSON: {ex.Message}");
        }
    }

    // Este método agora é chamado apenas pela thread principal, tornando-o seguro.
    private void AtualizarEntidades(EstadoMundoMensagem estadoMundo)
    {
        SincronizarListaComDicionario(estadoMundo.Naves, _naves, estado => new Nave(estado, _spriteNave));
        SincronizarListaComDicionario(estadoMundo.Asteroides, _asteroides, estado => new Asteroide(estado, _spriteAsteroide));
        SincronizarListaComDicionario(estadoMundo.Tiros, _tiros, estado => new Tiro(estado, _spriteTiro));

        _pontuacao = estadoMundo.Pontos;
        fimDeJogo = estadoMundo.FimDeJogo;
    }

    // Trocado o parâmetro para ConcurrentDictionary
    private void SincronizarListaComDicionario<TEstado, TEntidade>(
        List<TEstado> listaDeEstados, ConcurrentDictionary<int, TEntidade> dicionarioDeEntidades,
        Func<TEstado, TEntidade> criarNovaEntidade)
        where TEstado : IEstadoComId
        where TEntidade : class, IEntidadeComEstado<TEstado>
    {
        var idsRecebidos = new HashSet<int>(listaDeEstados.Select(e => e.Id));

        // Remove os que não vieram mais do servidor
        var idsParaRemover = dicionarioDeEntidades.Keys.Where(id => !idsRecebidos.Contains(id)).ToList();
        foreach (var id in idsParaRemover) dicionarioDeEntidades.TryRemove(id, out _);

        // Atualiza os existentes e adiciona os novos
        foreach (var estado in listaDeEstados)
        {
            dicionarioDeEntidades[estado.Id] = criarNovaEntidade(estado);
        }
    }

    // O resto da classe (Teclas, auxiliares de input, etc.) permanece o mesmo.
    public void Teclas()
    {
        // (O código do Teclas permanece o mesmo da versão anterior)
        var keyboardState = Keyboard.GetState();
        var pressedKeys = keyboardState.GetPressedKeys();

        if (IsKeyJustPressed(Keys.Escape))
        {
            _gerenciadorDeRede?.Desconectar();
            Exit();
        }

        if (_estadoAtual == EstadoDoJogo.Menu || _estadoAtual == EstadoDoJogo.ErroDeConexao)
        {
            if (isEditingIp)
            {
                if (IsKeyJustPressed(Keys.Enter) || IsKeyJustPressed(Keys.F2)) isEditingIp = false;
                else ProcessarInputDeTexto(pressedKeys);
            }
            else
            {
                if (IsKeyJustPressed(Keys.Enter)) Task.Run(IniciarConexaoAsync);
                if (IsKeyJustPressed(Keys.F2)) isEditingIp = true;
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

        UpdatePreviousKeyboardState();
    }

    private KeyboardState _previousKeyboardState;
    private void UpdatePreviousKeyboardState() => _previousKeyboardState = Keyboard.GetState();
    private bool IsKeyJustPressed(Keys key) => Keyboard.GetState().IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);

    private void ProcessarInputDeTexto(Keys[] pressedKeys) { /* ... O código deste método não muda ... */ }
}
