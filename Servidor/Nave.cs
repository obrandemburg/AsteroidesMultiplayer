using Asteroides.Compartilhado.Contratos;
using Microsoft.Xna.Framework;
using Monogame.Processing;


namespace Asteroides;

class Nave
{
    public Vector2 Posicao;
    public int Id { get; set; }
    const float Vel = 4f;
    const float HalfW = 10, HalfH = 10;
    int tid = 0;

    public Nave(Vector2 start, int id)
    {
        Posicao = start;
        Id = id;
    }
    public void ConverterParaVariavel(InputCliente mensagemJson)
    {
        bool left = mensagemJson.Esquerda;
        bool right = mensagemJson.Direita;
        bool up = mensagemJson.Cima;
        bool down = mensagemJson.Baixo;
        //bool atirando = mensagemJson.Atirando;
        Atualizar(left, right, up, down, 1280, 720);
    }

    public void Atualizar(bool left, bool right, bool up, bool down, int w, int h)
    {
        Vector2 dir = Vector2.Zero;
        if (left) dir.X -= 2;
        if (right) dir.X += 2;
        if (up) dir.Y -= 2;
        if (down) dir.Y += 2;

        if (dir != Vector2.Zero) dir.Normalize();
        Posicao += dir * Vel;

        /* mantém dentro da tela */
        Posicao.X = Math.Clamp(Posicao.X, HalfW, w - HalfW);
        Posicao.Y = Math.Clamp(Posicao.Y, HalfH, h - HalfH);
    }


    public Tiro Atirar() => new(Posicao + new Vector2(0, -12), new Vector2(0, -8), tid);
}
