using Microsoft.Xna.Framework;
using Monogame.Processing;
using Asteroides.Compartilhado.Estados;
using Asteroides.Compartilhado.Interfaces;


namespace Cliente.Entidades;

class Nave : IEntidadeComEstado<NaveEstado>
{
    public NaveEstado Estado { get; set; }
    PImage spriteNave;

    Vector2 Posicao => new Vector2(Estado.PosicaoX, Estado.PosicaoY);

    public Nave(NaveEstado estado, PImage spriteNave)
    {
        this.spriteNave = spriteNave;
        this.Estado = estado;
    }


    public void Desenhar(Processing g)
    {

        float novaLargura = 120;
        float novaAltura = 100;

        float topLeftX = Posicao.X - (novaLargura / 2f);
        float topLeftY = Posicao.Y - (novaAltura / 2f);

        g.image(spriteNave, topLeftX, topLeftY, novaLargura, novaAltura);
    }

}
