using Asteroides.Compartilhado.Contratos;
using Cliente.Entidades;
using Cliente.Servicos;
using Microsoft.Xna.Framework.Input;
using Monogame.Processing;
using System.Text.Json;
using Asteroides.Compartilhado.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cliente;

public class JogoAsteroides : Processing
{
    private GerenciadorDeRede _gerenciadorDeRede;

    private readonly Dictionary<int, Nave> _naves = new();
    private readonly Dictionary<int, Tiro> _tiros = new();
    private readonly Dictionary<int, Asteroide> _asteroides = new();

    private PImage _spriteNave, _spriteAsteroide, _spriteTiro;

    private int _pontuacao;

    private bool _esquerda, _direita, _cima, _baixo, _atirando;

    public JogoAsteroides(GerenciadorDeRede gerenciadorDeRede)
    {
        _gerenciadorDeRede = gerenciadorDeRede;
        _gerenciadorDeRede.OnMensagemRecebida += ProcessarMensagemDoServidor;

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
        InputCliente inputCliente = new InputCliente
        {
            Cima = _cima,
            Baixo = _baixo,
            Esquerda = _esquerda,
            Direita = _direita,
            Atirando = _atirando
        };
        _gerenciadorDeRede.EnviarMensagem(inputCliente);
        _atirando = false;

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
    private void ProcessarMensagemDoServidor(string json) //Recebe uma string JSON e cria um objeto que ela representa
    {
        try
        {
            var msgBase = JsonSerializer.Deserialize<MensagemBase>(json);

            switch (msgBase?.Tipo)
            {
                case "ESTADO_MUNDO":
                    var estadoMundo = JsonSerializer.Deserialize<EstadoMundoMensagem>(json);

                    AtualizarEntidades(estadoMundo);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar mensagem JSON: {ex.Message}");
        }
    }
    private void AtualizarEntidades(EstadoMundoMensagem estadoMundo)
    {
        // Sincroniza a lista de estados de Naves com o nosso dicionário de Naves visuais
        SincronizarListaComDicionario(estadoMundo.Naves, _naves, estado => new Nave(estado, _spriteNave));

        // Sincroniza a lista de estados de Asteroides com o nosso dicionário de Asteroides visuais
        SincronizarListaComDicionario(estadoMundo.Asteroides, _asteroides, estado => new Asteroide(estado, _spriteAsteroide));

        // Sincroniza a lista de estados de Tiros com o nosso dicionário de Tiros visuais
        SincronizarListaComDicionario(estadoMundo.Tiros, _tiros, estado => new Tiro(estado, _spriteTiro));
    }

    //TEstado = Coisas que tem ID
    // TEntidade = Coisas que tem Estado e ID
    //SincronizarListaComDicionario recebe uma lista de coisas que possuem um ID (Estados)
    //Um dicionário de coisas que possuem um estado que tem ID
    //uma função que cria uma nova entidade a partir de um estado (entidade que possui ID) e retorna uma entidade que possui o estado que foi passado
    private void SincronizarListaComDicionario<TEstado, TEntidade>(
        List<TEstado> listaDeEstados,
        Dictionary<int, TEntidade> dicionarioDeEntidades,
        Func<TEstado, TEntidade> criarNovaEntidade)
        where TEstado : IEstadoComId // Regra: TEstado DEVE ter uma propriedade Id
        where TEntidade : class, IEntidadeComEstado<TEstado> // Regra: TEntidade DEVE ter uma propriedade Estado
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
                // ATUALIZAR: Acesso direto e seguro à propriedade Estado!
                entidadeExistente.Estado = estado;
            }
            else
            {
                // ADICIONAR: A mesma lógica de antes.
                dicionarioDeEntidades[estado.Id] = criarNovaEntidade(estado);
            }
        }
    }

    /* ====================== input ============================= */
    public void Teclas()
    {
        _esquerda = false;
        _direita = false;
        _cima = false;
        _baixo = false;

        if (!keyPressed) return;  // nada pressionado

        /* tecla “única” (letras) */
        switch (char.ToUpperInvariant(key))
        {
            case 'A': _esquerda = true; break;
            case 'D': _direita = true; break;
            case 'W': _cima = true; break;
            case 'S': _baixo = true; break;
        }

        /* teclas especiais (setas, espaço, esc) */
        switch (keyCode)
        {
            case Keys.Left: _esquerda = true; break;
            case Keys.Right: _direita = true; break;
            case Keys.Up: _cima = true; break;
            case Keys.Down: _baixo = true; break;

            case Keys.Space: _atirando = true; break;
            case Keys.Escape: Exit(); break;
        }
    }

    public override void KeyReleased(Keys pkey)
    {
        switch (char.ToUpperInvariant(key))
        {
            case 'A': _esquerda = false; break;
            case 'D': _direita = false; break;
            case 'W': _cima = false; break;
            case 'S': _baixo = false; break;
        }

        switch (keyCode)
        {
            case Keys.Left: _esquerda = false; break;
            case Keys.Right: _direita = false; break;
            case Keys.Up: _cima = false; break;
            case Keys.Down: _baixo = false; break;
            case Keys.Space: _atirando = false; break;
        }
    }
}
