using Asteroides.Compartilhado.Estados;
using Asteroides.Compartilhado.Interfaces;
using Microsoft.Xna.Framework;
using Monogame.Processing;

namespace Cliente.Entidades;

class Asteroide : IEntidadeComEstado<AsteroideEstado>
{

    PImage spriteAsteroide;
    public AsteroideEstado Estado { get; set; }

    public Vector2 Posicao => new Vector2(Estado.PosicaoX, Estado.PosicaoY);
    public Asteroide(AsteroideEstado estado, PImage SpriteAsteroide)
    {
        this.Estado = estado;
        this.spriteAsteroide = SpriteAsteroide;
    }

    public void Desenhar(Processing g)
    {
        float novaLargura = 80;
        float novaAltura = 60;

        float topLeftX = Posicao.X - (novaLargura / 2f);
        float topLeftY = Posicao.Y - (novaAltura / 2f);

        g.image(spriteAsteroide, topLeftX, topLeftY, novaLargura, novaAltura);
    }

    //public bool Colide(TiroEstado t) => Vector2.Distance(t.Posicao, Posicao) < Estado.Raio;
    //public bool Colide(NaveEstado n) => Vector2.Distance(n.Posicao, Posicao) < Estado.Raio + 8;
}
