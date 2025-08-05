using Asteroides.Compartilhado.Estados;
using System.Collections.Generic;

namespace Asteroides.Compartilhado.Contratos
{
    public class EstadoMundoMensagem : MensagemBase
    {
        public List<NaveEstado> Naves { get; set; }
        public List<AsteroideEstado> Asteroides { get; set; }
        public List<TiroEstado> Tiros { get; set; }

        public EstadoMundoMensagem()
        {
            Tipo = "ESTADO_MUNDO";
            Naves = new List<NaveEstado>();
            Asteroides = new List<AsteroideEstado>();
            Tiros = new List<TiroEstado>();
        }
    }
}