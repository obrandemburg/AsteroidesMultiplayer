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
    private GerenciadorDeRede _gerenciadorDeRede;

    private readonly Dictionary<int, Nave> _naves = new();
    private readonly Dictionary<int, Tiro> _tiros = new();
    private readonly Dictionary<int, Asteroide> _asteroides = new();

    private PImage _spriteNave, _spriteAsteroide, _spriteTiro;
    private bool fimDeJogo = false;
    private int _pontuacao;

    private bool _esquerda, _direita, _cima, _baixo, _atirando;

    public JogoAsteroides(GerenciadorDeRede gerenciadorDeRede)
    {
        _gerenciadorDeRede = gerenciadorDeRede;
    }

    public override void Setup()
    {
        size(1280, 720);
        _spriteNave = loadImage("Content/nave.png");
        _spriteAsteroide = loadImage("Content/AsteroidBrown.png");
        _spriteTiro = loadImage("Content/tiro.png");

        _gerenciadorDeRede.OnMensagemRecebida += ProcessarMensagemDoServidor;
    }

    public override void Draw()
    {
        if (!fimDeJogo)
        {
            Teclas();
            InputCliente inputCliente = new InputCliente
            {
                Cima = _cima,
                Baixo = _baixo,
                Esquerda = _esquerda,
                Direita = _direita,
                Atirando = _atirando
            };
            _gerenciadorDeRede.EnviarMensagem(inputCliente);

            background(10, 10, 20);

            foreach (var asteroide in _asteroides.Values)
            {
                asteroide.Desenhar(this);
            }

            foreach (var tiro in _tiros.Values)
            {
                tiro.Desenhar(this);
            }

            foreach (var nave in _naves.Values)
            {
                nave.Desenhar(this);
            }

            fill(255);
            text($"Pontos: {_pontuacao}", 10, 10);
        }
        else
        {
            background(20, 10, 10);

            textSize(40);
            fill(255, 0, 0);
            text("GAME OVER", width / 2, height / 2);

            textSize(32);
            fill(255);
            text($"Pontuacao Final: {_pontuacao}", width / 2, height / 2 + 100);

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

                _pontuacao = estadoMundo.Pontos;
                fimDeJogo = estadoMundo.FimDeJogo;

                if (!fimDeJogo)
                {
                    AtualizarEntidades(estadoMundo);
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
        List<TEstado> listaDeEstados,
        Dictionary<int, TEntidade> dicionarioDeEntidades,
        Func<TEstado, TEntidade> criarNovaEntidade)
        where TEstado : IEstadoComId
        where TEntidade : class, IEntidadeComEstado<TEstado>
    {
        var idsRecebidos = new HashSet<int>(listaDeEstados.Select(e => e.Id));

        var idsParaRemover = dicionarioDeEntidades.Keys.Where(id => !idsRecebidos.Contains(id)).ToList();
        foreach (var id in idsParaRemover)
        {
            dicionarioDeEntidades.Remove(id);
        }

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
        _esquerda = keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left);
        _direita = keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right);
        _cima = keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up);
        _baixo = keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down);
        _atirando = keyboardState.IsKeyDown(Keys.Space);

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }
    }
}