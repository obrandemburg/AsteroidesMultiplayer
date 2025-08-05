namespace Asteroides.Compartilhado.Interfaces
{
    // Esta interface é uma promessa: "Qualquer classe que me implementar
    // TERÁ OBRIGATORIAMENTE uma propriedade 'Estado' do tipo TEstado."
    public interface IEntidadeComEstado<TEstado>
    {
        public TEstado Estado { get; set; }
    }
}