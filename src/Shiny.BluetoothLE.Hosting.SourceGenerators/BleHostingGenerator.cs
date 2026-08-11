using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// Turns <c>[BleService]</c> / <c>[L2CapService]</c> partial classes into the imperative
/// <c>IBleHostingManager</c> calls Shiny expects, with no reflection anywhere.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class BleHostingGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var services = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Names.BleServiceAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => Parser.ParseService(ctx)
            )
            .Collect();

        var l2caps = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Names.L2CapServiceAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => Parser.ParseL2Cap(ctx)
            )
            .Collect();

        // handlers on a type that never declared [BleService] would otherwise vanish silently
        var orphans = ImmutableArray
            .Create(
                Names.ReadCharacteristicAttribute,
                Names.WriteCharacteristicAttribute,
                Names.NotifyCharacteristicAttribute,
                Names.RequestResponseCharacteristicAttribute
            )
            .Select(attribute => context.SyntaxProvider.ForAttributeWithMetadataName(
                attribute,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, _) => Parser.ParseOrphanHandler(ctx)
            ))
            .Aggregate((left, right) => left.Collect().Combine(right.Collect()).SelectMany((pair, _) => pair.Left.AddRange(pair.Right)))
            .Where(static d => d is not null)
            .Collect();

        var rootNamespace = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
        {
            provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value);
            return String.IsNullOrWhiteSpace(value) ? Names.Namespace : value!;
        });

        var hasDependencyInjection = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection") != null
        );

        var source = services
            .Combine(l2caps)
            .Combine(orphans)
            .Combine(rootNamespace)
            .Combine(hasDependencyInjection);

        context.RegisterSourceOutput(source, static (spc, input) => Execute(
            spc,
            input.Left.Left.Left.Left,
            input.Left.Left.Left.Right,
            input.Left.Left.Right,
            input.Left.Right,
            input.Right
        ));
    }


    static void Execute(
        SourceProductionContext context,
        ImmutableArray<ParseResult<ServiceModel>> serviceResults,
        ImmutableArray<ParseResult<L2CapModel>> l2capResults,
        ImmutableArray<DiagnosticInfo?> orphans,
        string rootNamespace,
        bool hasDependencyInjection
    )
    {
        foreach (var result in serviceResults)
        {
            foreach (var diagnostic in result.Diagnostics)
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        foreach (var result in l2capResults)
        {
            foreach (var diagnostic in result.Diagnostics)
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        foreach (var orphan in orphans)
            context.ReportDiagnostic(orphan!.ToDiagnostic());

        // ordered so the emitted source is stable across runs
        var services = serviceResults
            .Select(r => r.Model)
            .Where(m => m != null)
            .Select(m => m!)
            .OrderBy(m => m.ServiceUuid, StringComparer.Ordinal)
            .ThenBy(m => m.FullyQualifiedName, StringComparer.Ordinal)
            .ToList();

        var l2caps = l2capResults
            .Select(r => r.Model)
            .Where(m => m != null)
            .Select(m => m!)
            .OrderBy(m => m.FullyQualifiedName, StringComparer.Ordinal)
            .ToList();

        if (services.Count == 0 && l2caps.Count == 0)
            return;

        foreach (var service in services)
        {
            context.AddSource($"{FileHint(service.Namespace, service.ClassName)}.Context.g.cs", ServiceEmitter.EmitContext(service));
            context.AddSource($"{FileHint(service.Namespace, service.ClassName)}.Service.g.cs", ServiceEmitter.EmitService(service));
        }

        foreach (var l2cap in l2caps)
            context.AddSource($"{FileHint(l2cap.Namespace, l2cap.ClassName)}.L2Cap.g.cs", L2CapEmitter.Emit(l2cap));

        var groups = BuildGroups(context, services, l2caps);
        context.AddSource(
            "BleHostedServiceRegistration.g.cs",
            RegistrationEmitter.Emit(rootNamespace, groups, l2caps, hasDependencyInjection)
        );
    }


    /// <summary>
    /// Merges classes that declare the same service UUID into one registration, and attaches any
    /// L2CAP listener publishing its PSM there.
    /// </summary>
    static IReadOnlyList<ServiceGroup> BuildGroups(
        SourceProductionContext context,
        IReadOnlyList<ServiceModel> services,
        IReadOnlyList<L2CapModel> l2caps
    )
    {
        var groups = new List<ServiceGroup>();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var members in services.GroupBy(s => s.ServiceUuid, StringComparer.Ordinal))
        {
            var ordered = members.ToList();
            var first = ordered[0];

            foreach (var member in ordered.Skip(1).Where(m => m.Primary != first.Primary))
            {
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    Diagnostics.MergedPrimaryMismatch,
                    member.Location,
                    member.ClassName,
                    member.Primary.ToString(),
                    first.ServiceUuid,
                    first.Primary.ToString()
                ).ToDiagnostic());
            }

            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var member in ordered)
            {
                foreach (var characteristic in member.Characteristics)
                {
                    if (owners.TryGetValue(characteristic.Uuid, out var existing))
                    {
                        context.ReportDiagnostic(DiagnosticInfo.Create(
                            Diagnostics.MergedCharacteristicConflict,
                            characteristic.Location,
                            characteristic.Uuid, existing, member.ClassName, first.ServiceUuid
                        ).ToDiagnostic());
                    }
                    else
                    {
                        owners.Add(characteristic.Uuid, member.ClassName);
                    }
                }
            }

            var psmOwner = l2caps.FirstOrDefault(l => l.PsmServiceUuid == first.ServiceUuid);
            if (psmOwner?.PsmCharacteristicUuid != null && owners.TryGetValue(psmOwner.PsmCharacteristicUuid, out var clash))
            {
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    Diagnostics.MergedCharacteristicConflict,
                    psmOwner.Location,
                    psmOwner.PsmCharacteristicUuid, clash, psmOwner.ClassName, first.ServiceUuid
                ).ToDiagnostic());
            }

            // an explicit Name on any member names the whole merge group; otherwise fall back to the
            // first class name in the deterministic ordering
            var preferred = (ordered.FirstOrDefault(m => m.NameIsExplicit) ?? first).RegistrationName;
            var name = preferred;
            var suffix = 2;
            while (!usedNames.Add(name))
                name = preferred + suffix++;

            groups.Add(new ServiceGroup(
                Uuid: first.ServiceUuid,
                Primary: first.Primary,
                RegistrationName: name,
                Advertise: ordered.Any(m => m.Advertise),
                Members: ordered,
                PsmOwner: psmOwner,
                PsmCharacteristicUuid: psmOwner?.PsmCharacteristicUuid
            ));
        }

        // an L2CAP class pointing its PSM at a service nobody declared
        foreach (var l2cap in l2caps.Where(l => l.PsmServiceUuid != null))
        {
            if (groups.Any(g => g.Uuid == l2cap.PsmServiceUuid))
                continue;

            context.ReportDiagnostic(DiagnosticInfo.Create(
                Diagnostics.InvalidPsmPublication,
                l2cap.Location,
                $"'{l2cap.ClassName}' publishes its PSM on service '{l2cap.PsmServiceUuid}', which no [BleService] in this compilation declares"
            ).ToDiagnostic());
        }

        return groups;
    }


    static string FileHint(string ns, string className)
        => String.IsNullOrEmpty(ns) ? className : ns + "." + className;
}
