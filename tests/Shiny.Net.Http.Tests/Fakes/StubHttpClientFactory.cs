using System.Net.Http;

namespace Shiny.Net.Http.Tests.Fakes;


/// <summary>
/// Minimal <see cref="IHttpClientFactory"/> that hands out a client wrapping a fixed
/// <see cref="HttpMessageHandler"/> - lets the transfer loop be driven by a fake handler.
/// </summary>
public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
