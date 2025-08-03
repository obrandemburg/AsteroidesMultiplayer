using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;   // só para comparar com Keys.*
using Monogame.Processing;

namespace Asteroides;

class Asteroide
{
    Vector2 pos, vel;
    PImage spriteAsteroide;
    public float Raio { get; }

    public Asteroide(Vector2 p, Vector2 v, float r, PImage spriteAsteroide)
    {
        pos = p; vel = v; Raio = r;
        this.spriteAsteroide = spriteAsteroide;
    }

    public void Atualizar() => pos += vel;

    public void Desenhar(Processing g)
    {
        float novaLargura = 80;
        float novaAltura = 60;

        float topLeftX = pos.X - (novaLargura / 2f);
        float topLeftY = pos.Y - (novaAltura / 2f);

        g.image(spriteAsteroide, topLeftX, topLeftY, novaLargura, novaAltura);
    }

    public bool Colide(Tiro t) => Vector2.Distance(t.Pos, pos) < Raio;
    public bool Colide(Nave n) => Vector2.Distance(n.Posicao, pos) < Raio + 8;
}
