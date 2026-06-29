using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Net;
using Shiny.Net.Http.Infrastructure;
using Shiny.Net.Http.Tests.Fakes;
using Xunit;

namespace Shiny.Net.Http.Tests;


public class DiResolutionTests
{
    sealed class NoopDelegate : IHttpTransferDelegate
    {
        public Task OnError(HttpTransferRequest request, int statusCode, Exception ex) => Task.CompletedTask;
        public Task OnCompleted(HttpTransferRequest request) => Task.CompletedTask;
    }


    [Fact(DisplayName = "DI - AddHttpClientTransfers wires IHttpClientFactory into the transfer process")]
    public void AddHttpClientTransfers_Resolves_Process_With_Factory_Client()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConnectivity>(new FakeConnectivity());
        services.AddHttpClientTransfers<NoopDelegate>();

        using var provider = services.BuildServiceProvider();

        // the IHttpClientFactory named client must be registered for this to construct
        var factory = provider.GetService<IHttpClientFactory>();
        Assert.NotNull(factory);
        Assert.NotNull(factory!.CreateClient(HttpClientHttpTransferProcess.HttpClientName));

        var process = provider.GetRequiredService<HttpClientHttpTransferProcess>();
        Assert.NotNull(process);

        var manager = provider.GetRequiredService<IHttpTransferManager>();
        Assert.NotNull(manager);
    }
}
