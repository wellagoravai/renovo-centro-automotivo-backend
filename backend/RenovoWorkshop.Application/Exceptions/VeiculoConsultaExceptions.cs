namespace RenovoWorkshop.Application.Exceptions;

// Placa em formato inválido — falha antes de gastar cota da APIBrasil.
public class PlacaInvalidaException : Exception
{
    public PlacaInvalidaException(string placa)
        : base($"Placa '{placa}' está em formato inválido. Use o padrão antigo (ABC1234) ou Mercosul (ABC1D23).")
    {
    }
}

// Nenhum veículo encontrado para a placa consultada (404 da APIBrasil ou corpo vazio).
public class VeiculoNaoEncontradoException : Exception
{
    public VeiculoNaoEncontradoException(string placa)
        : base($"Nenhum veículo encontrado para a placa '{placa}'.")
    {
    }
}

// DeviceToken/Bearer Token inválidos ou expirados (401/403 da APIBrasil).
public class ApiBrasilCredenciaisInvalidasException : Exception
{
    public ApiBrasilCredenciaisInvalidasException(string message) : base(message)
    {
    }
}

// Cota diária de requisições gratuitas excedida (429 da APIBrasil) — falha rápido,
// sem retry, para o chamador decidir se bloqueia o Admin ou apenas avisa.
public class ApiBrasilLimiteExcedidoException : Exception
{
    public ApiBrasilLimiteExcedidoException(string message) : base(message)
    {
    }
}

// Serviço externo fora do ar: 5xx, timeout, ou circuit breaker aberto após falhas repetidas.
public class ApiBrasilIndisponivelException : Exception
{
    public ApiBrasilIndisponivelException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
