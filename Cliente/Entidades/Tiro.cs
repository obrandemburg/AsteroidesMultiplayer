using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;   // só para comparar com Keys.*
using Monogame.Processing;

namespace Asteroides;

class Tiro
{
    Vector2 pos, vel;
    PImage spriteTiro;
    public Tiro(Vector2 p, Vector2 v, PImage spriteTiro)
    {
        pos = p;
        vel = v;
        this.spriteTiro = spriteTiro;
    }

    public void Atualizar() => pos += vel;

    public void Desenhar(Processing g)
    {
        float novaLargura = 80;
        float novaAltura = 60;

        float topLeftX = pos.X - (novaLargura / 2f);
        float topLeftY = pos.Y - (novaAltura / 2f);

        g.image(spriteTiro, topLeftX, topLeftY, novaLargura, novaAltura);
    }

    public bool ForaDaTela(int h) => pos.Y < -5;
    public Vector2 Pos => pos;
}

