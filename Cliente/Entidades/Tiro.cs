using Asteroides.Compartilhado.Estados;
using Microsoft.Xna.Framework;
using Monogame.Processing;
using Asteroides.Compartilhado.Interfaces; 

namespace Cliente.Entidades;

class Tiro : IEntidadeComEstado<TiroEstado>
{
    public TiroEstado Estado { get; set; }
    public Vector2 Posicao => new Vector2(Estado.PosicaoX, Estado.PosicaoY);
    //public Vector2 Velocidade => new Vector2(Estado.Velocidade.X, Estado.Velocidade.Y);

    PImage spriteTiro;
    public Tiro(TiroEstado estado, PImage SpriteTiro)
    {
        this.spriteTiro = SpriteTiro;
        this.Estado = estado;
    }

    public void Desenhar(Processing g)
    {
        float novaLargura = 80;
        float novaAltura = 60;

        float topLeftX = Posicao.X - (novaLargura / 2f);
        float topLeftY = Posicao.Y - (novaAltura / 2f);

        g.image(spriteTiro, topLeftX, topLeftY, novaLargura, novaAltura);
    }

    public bool ForaDaTela(int h) => Posicao.Y < -5;
}

