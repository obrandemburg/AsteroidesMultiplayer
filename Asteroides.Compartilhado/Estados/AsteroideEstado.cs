using Asteroides.Compartilhado.Contratos;
using Asteroides.Compartilhado.Interfaces;
using System.Numerics;

namespace Asteroides.Compartilhado.Estados;

public class AsteroideEstado : MensagemBase, IEstadoComId
{

    public int Id { get; set; }
    public float PosicaoX { get; set; }
    public float PosicaoY { get; set; }
    public float VelocidadeX;
    public float VelocidadeY;
    public float Raio { get; }

    public AsteroideEstado()
    {
        Tipo = "Asteroide_Estado";
    }
}
