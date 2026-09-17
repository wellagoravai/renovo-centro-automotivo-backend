using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Tests.TestDoubles;

public class FakeGoogleRoutesTollClient : IGoogleRoutesTollClient
{
    private readonly Func<string, string, RotaInfo> _responder;

    public int CallCount { get; private set; }
    public (string Origem, string Destino)? LastCall { get; private set; }

    public FakeGoogleRoutesTollClient(Func<string, string, RotaInfo> responder)
    {
        _responder = responder;
    }

    public Task<RotaInfo> ObterRotaAsync(string origem, string destino, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastCall = (origem, destino);
        return Task.FromResult(_responder(origem, destino));
    }
}
