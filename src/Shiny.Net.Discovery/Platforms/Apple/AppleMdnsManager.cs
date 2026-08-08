using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CoreFoundation;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;

namespace Shiny.Net.Discovery;


/// <summary>
/// Browses and publishes through Bonjour (NSNetService), which talks to the system mDNSResponder
/// daemon rather than opening multicast sockets. That is why iOS apps using this do not need the
/// com.apple.developer.networking.multicast entitlement - only NSBonjourServices and
/// NSLocalNetworkUsageDescription in Info.plist.
/// </summary>
public class AppleMdnsManager(ILogger<AppleMdnsManager> logger) : IMdnsManager
{
    static readonly TimeSpan defaultResolveTimeout = TimeSpan.FromSeconds(5);


    public async IAsyncEnumerable<MdnsBrowseResult> Browse(
        MdnsBrowseConfig config,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(config);
        var serviceType = ServiceTypeParser.Parse(config.ServiceType, config.Domain);

        var channel = Channel.CreateUnbounded<MdnsBrowseResult>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

        // NSNetService instances are only alive while something references them, and a resolve
        // in flight is easy to collect out from under. Hold them until we are done with each.
        var resolving = new Dictionary<string, NSNetService>();
        NSNetServiceBrowser? browser = null;

        void OnFound(object? sender, NSNetServiceEventArgs args)
            => this.OnServiceFound(args.Service, serviceType, config, channel, resolving);

        void OnRemoved(object? sender, NSNetServiceEventArgs args)
        {
            lock (resolving)
                resolving.Remove(args.Service.Name);

            channel.Writer.TryWrite(new MdnsBrowseResult(
                MdnsBrowseStatus.Lost,
                new MdnsService
                {
                    InstanceName = args.Service.Name,
                    ServiceType = serviceType.ServiceType,
                    Domain = serviceType.Domain
                }
            ));
        }

        void OnNotSearched(object? sender, NSNetServiceErrorEventArgs args)
        {
            var error = Describe(args.Errors);
            logger.LogError("Bonjour refused to browse {ServiceType} - {Error}", serviceType.ServiceType, error);
            channel.Writer.TryComplete(new MdnsException(
                $"Bonjour refused to browse '{serviceType.ServiceType}' ({error}). Check that it is listed in the NSBonjourServices array in Info.plist."
            ));
        }

        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            browser = new NSNetServiceBrowser();
            browser.FoundService += OnFound;
            browser.ServiceRemoved += OnRemoved;
            browser.NotSearched += OnNotSearched;
            browser.SearchForServices(serviceType.ServiceType.ToBonjour(), serviceType.Domain.ToBonjour());
        });

        try
        {
            await foreach (var result in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                yield return result;
        }
        finally
        {
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                if (browser != null)
                {
                    browser.FoundService -= OnFound;
                    browser.ServiceRemoved -= OnRemoved;
                    browser.NotSearched -= OnNotSearched;
                    browser.Stop();
                    browser.Dispose();
                }

                lock (resolving)
                {
                    foreach (var service in resolving.Values)
                        service.Stop();

                    resolving.Clear();
                }
            });
        }
    }


    void OnServiceFound(
        NSNetService service,
        ServiceTypeInfo serviceType,
        MdnsBrowseConfig config,
        Channel<MdnsBrowseResult> channel,
        Dictionary<string, NSNetService> resolving
    )
    {
        if (!config.ResolveServices)
        {
            channel.Writer.TryWrite(new MdnsBrowseResult(
                MdnsBrowseStatus.Found,
                new MdnsService
                {
                    InstanceName = service.Name,
                    ServiceType = serviceType.ServiceType,
                    Domain = serviceType.Domain
                }
            ));
            return;
        }

        lock (resolving)
            resolving[service.Name] = service;

        void OnResolved(object? sender, EventArgs args)
        {
            channel.Writer.TryWrite(new MdnsBrowseResult(MdnsBrowseStatus.Found, Convert(service, serviceType)));
            Complete();
        }

        void OnFailed(object? sender, NSNetServiceErrorEventArgs args)
        {
            logger.LogDebug("Could not resolve {Name} - {Error}", service.Name, Describe(args.Errors));

            // still surface it: the caller learns the instance exists even without an endpoint
            channel.Writer.TryWrite(new MdnsBrowseResult(MdnsBrowseStatus.Found, Convert(service, serviceType)));
            Complete();
        }

        void Complete()
        {
            // unhook before dropping the reference so the handlers cannot fire against a
            // service the browse has already finished with
            service.AddressResolved -= OnResolved;
            service.ResolveFailure -= OnFailed;

            lock (resolving)
                resolving.Remove(service.Name);
        }

        service.AddressResolved += OnResolved;
        service.ResolveFailure += OnFailed;
        service.Resolve(config.ResolveTimeout.TotalSeconds);
    }


    public async Task<MdnsService?> Resolve(
        string instanceName,
        string serviceType,
        TimeSpan? timeout = null,
        CancellationToken ct = default
    )
    {
        ServiceTypeParser.ValidateInstanceName(instanceName);
        var info = ServiceTypeParser.Parse(serviceType);
        var window = timeout ?? defaultResolveTimeout;
        var completion = new TaskCompletionSource<MdnsService?>(TaskCreationOptions.RunContinuationsAsynchronously);

        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            var service = new NSNetService(info.Domain.ToBonjour(), info.ServiceType.ToBonjour(), instanceName);

            service.AddressResolved += (_, _) =>
            {
                completion.TrySetResult(Convert(service, info));
                service.Stop();
            };
            service.ResolveFailure += (_, args) =>
            {
                logger.LogDebug("Could not resolve {Name} - {Error}", instanceName, Describe(args.Errors));
                completion.TrySetResult(null);
                service.Stop();
            };
            service.Resolve(window.TotalSeconds);
        });

        try
        {
            return await completion.Task.WaitAsync(window, ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            logger.LogDebug("Resolving {Name} timed out after {Timeout}", instanceName, window);
            return null;
        }
    }


    public Task<IMdnsPublication> Publish(MdnsServiceRegistration registration, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ServiceTypeParser.ValidateInstanceName(registration.InstanceName);

        if (registration.Port is <= 0 or > 65535)
            throw new MdnsException($"The port must be between 1 and 65535 but was {registration.Port}.");

        var serviceType = ServiceTypeParser.Parse(registration.ServiceType, registration.Domain);
        var tcs = new TaskCompletionSource<IMdnsPublication>(TaskCreationOptions.RunContinuationsAsynchronously);

        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            try
            {
                var service = new NSNetService(
                    serviceType.Domain.ToBonjour(),
                    serviceType.ServiceType.ToBonjour(),
                    registration.InstanceName,
                    registration.Port
                );

                if (registration.TxtRecords?.Count > 0)
                    service.SetTxtRecordData(registration.TxtRecords.ToTxtRecordData());

                service.Published += (_, _) => tcs.TrySetResult(
                    new ApplePublication(service, serviceType, registration.Port)
                );
                service.PublishFailure += (_, args) => tcs.TrySetException(
                    new MdnsException($"Bonjour refused to publish '{registration.InstanceName}' - {Describe(args.Errors)}")
                );

                // the default (no NoAutoRename option) lets Bonjour resolve name conflicts by
                // appending " (2)", which is exactly the contract IMdnsPublication documents
                service.Publish();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(new MdnsException($"Failed to publish '{registration.InstanceName}'", ex));
            }
        });

        return tcs.Task.WaitAsync(ct);
    }


    static MdnsService Convert(NSNetService service, ServiceTypeInfo serviceType)
    {
        var addresses = service.Addresses?
            .Select(x => x.ToIPAddress())
            .Where(x => x != null)
            .Select(x => x!)
            .Distinct()
            .ToList() ?? [];

        return new MdnsService
        {
            InstanceName = service.Name,
            ServiceType = serviceType.ServiceType,
            Domain = serviceType.Domain,
            HostName = service.HostName?.TrimEnd('.'),
            Port = (int)service.Port,
            Addresses = addresses,
            TxtRecords = service.GetTxtRecordData().ToTxtRecords()
        };
    }


    static string Describe(NSDictionary? errors)
    {
        if (errors == null || errors.Count == 0)
            return "no error detail";

        return String.Join(", ", errors.Select(x => $"{x.Key}={x.Value}"));
    }
}
