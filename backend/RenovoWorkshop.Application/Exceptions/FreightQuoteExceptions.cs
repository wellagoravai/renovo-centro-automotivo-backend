namespace RenovoWorkshop.Application.Exceptions;

// Origem/destino vazios ou preço/km <= 0 — falha antes de gastar chamada à Google.
public class EnderecoOuPrecoInvalidoException : Exception
{
    public EnderecoOuPrecoInvalidoException(string message) : base(message)
    {
    }
}

// A Google Routes API não encontrou uma rota rodoviária entre origem e destino.
public class RotaNaoEncontradaException : Exception
{
    public RotaNaoEncontradaException(string origem, string destino)
        : base($"Nenhuma rota encontrada entre '{origem}' e '{destino}'.")
    {
    }
}

// API key da Google Routes inválida, sem permissão, ou API não habilitada no projeto GCP.
public class GoogleRoutesCredenciaisInvalidasException : Exception
{
    public GoogleRoutesCredenciaisInvalidasException(string message) : base(message)
    {
    }
}

// 5xx, timeout, ou cota da Google Routes API excedida.
public class GoogleRoutesIndisponivelException : Exception
{
    public GoogleRoutesIndisponivelException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
