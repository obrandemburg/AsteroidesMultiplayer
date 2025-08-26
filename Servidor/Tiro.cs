using Microsoft.Xna.Framework;


namespace Asteroides;

class Tiro
{
    Vector2 pos, vel;
    public int Id { get; set; }
    public Tiro(Vector2 p, Vector2 v, int id)
    {
        pos = p;
        vel = v;
        Id = id;
    }

    public void Atualizar() => pos += vel;

    public bool ForaDaTela(int h) => pos.Y < -5;
    public Vector2 Pos => pos;
}

