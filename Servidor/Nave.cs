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

    public Nave(Vector2 start, int id)
    {
        Posicao = start;
        Id = id;
    }
    public void AtualizarPosicao(bool left, bool right, bool up, bool down, int w, int h)
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

    public Tiro? AtualizarEProcessarAcoes(InputCliente input, int idTiro)
    {

        if (input.Atirando)
        {
            return new Tiro(Posicao + new Vector2(0, -12), new Vector2(0, -8), idTiro);
        }
        else
        {
            AtualizarPosicao(input.Esquerda, input.Direita, input.Cima, input.Baixo, 1280, 720);
            return null;
        }

    }
}
