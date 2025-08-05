namespace Asteroides.Compartilhado.Interfaces
{
    // Esta interface é uma promessa: "Qualquer classe que me implementar
    // TERÁ OBRIGATORIAMENTE uma propriedade 'Id' do tipo int."
    public interface IEstadoComId
    {
        public int Id { get; set; }
    }
}