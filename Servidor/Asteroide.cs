using Microsoft.Xna.Framework;

namespace Asteroides;

class Asteroide
{
    public Vector2 pos, vel;
    public float Raio { get; set; }
    public int Id { get; set; }

    public Asteroide(Vector2 p, Vector2 v, float r, int id)
    {
        pos = p; vel = v; Raio = r;
        Id = id;
    }

    public void Atualizar() => pos += vel;


    public bool Colide(Tiro t) => Vector2.Distance(t.Pos, pos) < Raio;
    public bool Colide(Nave n) => Vector2.Distance(n.Posicao, pos) < Raio + 8;
}
