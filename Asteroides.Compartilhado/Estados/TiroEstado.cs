using Asteroides.Compartilhado.Contratos;
using Asteroides.Compartilhado.Interfaces;

namespace Asteroides.Compartilhado.Estados;
public class TiroEstado : MensagemBase, IEstadoComId
{
    public int Id { get; set; }
    public float PosicaoX { get; set; }
    public float PosicaoY { get; set; }
    public float VelocidadeX;
    public float VelocidadeY;
    public TiroEstado()
    {
        Tipo = "TiroEstado";
    }
}
