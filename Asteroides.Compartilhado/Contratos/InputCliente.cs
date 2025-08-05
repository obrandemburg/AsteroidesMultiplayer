namespace Asteroides.Compartilhado.Contratos
{
    public class InputCliente : MensagemBase
    {
        public bool Cima { get; set; }
        public bool Baixo { get; set; }
        public bool Esquerda { get; set; }
        public bool Direita { get; set; }
        public bool Atirando { get; set; }
        public InputCliente() {
            Tipo = "ClienteEstado";
        }
    }
}
