namespace RenovoWorkshop.Tests.TestDoubles;

// Handler de teste para HttpClient: o responder pode devolver uma resposta ou
// lançar (usado para simular exceções que, em produção, viriam das policies de
// Polly penduradas no HttpClient real — ex.: TimeoutRejectedException).
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public int CallCount { get; private set; }
    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequest = request;
        return Task.FromResult(_responder(request));
    }
}
