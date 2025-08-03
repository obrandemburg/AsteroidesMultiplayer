using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;   // só para comparar com Keys.*
using Monogame.Processing;

namespace Asteroides;

class Tiro
{
    Vector2 pos, vel;
    public Tiro(Vector2 p, Vector2 v)
    {
        pos = p;
        vel = v;
    }

    public void Atualizar() => pos += vel;

    public bool ForaDaTela(int h) => pos.Y < -5;
    public Vector2 Pos => pos;
}

