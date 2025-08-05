using Asteroides.Compartilhado.Contratos;
using Asteroides.Compartilhado.Interfaces;
using System.Numerics;

namespace Asteroides.Compartilhado.Estados;

public class NaveEstado : MensagemBase, IEstadoComId
{
    public int Id { get; set; }
    public float PosicaoX;
    public float PosicaoY;
    const float Vel = 4f;
    const float HalfW = 10, HalfH = 10;
    public NaveEstado()
    {
        Tipo = "NaveEstado";
    }
}
