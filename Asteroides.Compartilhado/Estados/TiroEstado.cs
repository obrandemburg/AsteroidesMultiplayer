using Asteroides.Compartilhado.Contratos;
using Asteroides.Compartilhado.Interfaces;

namespace Asteroides.Compartilhado.Estados;
public class TiroEstado : MensagemBase, IEstadoComId
{
    public int Id { get; set; }
    public float PosicaoX;
    public float PosicaoY;
    public float VelocidadeX;
    public float VelocidadeY;
    public TiroEstado()
    {
        Tipo = "TiroEstado";
    }
}
