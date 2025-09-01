// Cliente/Enums.cs

namespace Cliente
{
    // Enum para controlar o estado geral do jogo
    public enum EstadoDoJogo
    {
        Menu,
        Conectando,
        Jogando,
        ErroDeConexao
    }

    // Enum para controlar o indicador visual da conexão
    public enum StatusConexao
    {
        NaoConectado,
        Tentando,
        Conectado,
        Erro
    }
}