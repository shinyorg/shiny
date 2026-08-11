using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators.Tests;


public class DiagnosticTests
{
    [Fact]
    public void SBH001_NonPartialClass()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [BleService("180D")]
            public class NotPartial
            {
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            }
            """));

        Assert.Contains("SBH001", run.Ids());
    }


    [Fact]
    public void SBH001_NestedClass()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            public partial class Outer
            {
                [BleService("180D")]
                public partial class Inner
                {
                    [ReadCharacteristic("2A37")]
                    byte[] Read() => new byte[] { 1 };
                }
            }
            """));

        Assert.Contains("SBH001", run.Ids());
    }


    [Fact]
    public void SBH002_InvalidUuid()
    {
        var run = GeneratorHarness.Run(Snippets.Service(
            """
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            """,
            "[BleService(\"not-a-uuid\")]"
        ));

        Assert.Contains("SBH002", run.Ids());
    }


    [Fact]
    public void SBH003_HandlerOutsideABleService()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            public partial class Loose
            {
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            }
            """));

        Assert.Contains("SBH003", run.Ids());
    }


    [Fact]
    public void SBH004_TwoReadHandlersOnOneCharacteristic()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                byte[] ReadOne() => new byte[] { 1 };

                [ReadCharacteristic("2A37")]
                byte[] ReadTwo() => new byte[] { 2 };
            """));

        Assert.Contains("SBH004", run.Ids());
    }


    [Fact]
    public void SBH004_TwoWriteHandlersOnOneCharacteristic()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39")]
                void WriteOne(byte[] data) { }

                [WriteCharacteristic("2A39")]
                void WriteTwo(byte[] data) { }
            """));

        Assert.Contains("SBH004", run.Ids());
    }


    [Fact]
    public void SBH005_RequestResponseAlongsideAWrite()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A3B")]
                void Write(byte[] data) { }

                [RequestResponseCharacteristic("2A3B")]
                byte[] Exchange(byte[] data) => data;
            """));

        Assert.Contains("SBH005", run.Ids());
    }


    [Fact]
    public void SBH005_RequestResponseAlongsideANotify()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [RequestResponseCharacteristic("2A3B")]
                byte[] Exchange(byte[] data) => data;

                [NotifyCharacteristic("2A3B", Name = "Extra")]
                void OnSubscription(BleSubscription subscription) { }
            """));

        Assert.Contains("SBH005", run.Ids());
    }


    [Fact]
    public void SBH006_UnbindableParameter()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                byte[] Read(string nonsense) => new byte[] { 1 };
            """));

        Assert.Contains("SBH006", run.Ids());
    }


    [Fact]
    public void SBH006_UnsupportedReturnType()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                string Read() => "nope";
            """));

        Assert.Contains("SBH006", run.Ids());
    }


    [Fact]
    public void SBH006_WriteCannotReturnBytes()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39")]
                byte[] Write(byte[] data) => data;
            """));

        Assert.Contains("SBH006", run.Ids());
    }


    [Fact]
    public void SBH007_StaticHandler()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                static byte[] Read() => new byte[] { 1 };
            """));

        Assert.Contains("SBH007", run.Ids());
    }


    [Fact]
    public void SBH008_PsmServiceWithoutPsmCharacteristic()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [L2CapService(PsmService = "180D")]
            public partial class StreamService
            {
                [OnChannelOpened]
                Task Serve(L2CapChannel channel) => Task.CompletedTask;
            }
            """));

        Assert.Contains("SBH008", run.Ids());
    }


    [Fact]
    public void SBH008_PsmServiceNobodyDeclares()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [L2CapService(PsmService = "180D", PsmCharacteristic = "2ABC")]
            public partial class StreamService
            {
                [OnChannelOpened]
                Task Serve(L2CapChannel channel) => Task.CompletedTask;
            }
            """));

        Assert.Contains("SBH008", run.Ids());
    }


    [Fact]
    public void SBH009_L2CapWithoutAChannelHandler()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [L2CapService]
            public partial class StreamService
            {
            }
            """));

        Assert.Contains("SBH009", run.Ids());
    }


    [Fact]
    public void SBH009_L2CapWithTwoChannelHandlers()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [L2CapService]
            public partial class StreamService
            {
                [OnChannelOpened]
                Task One(L2CapChannel channel) => Task.CompletedTask;

                [OnChannelOpened]
                Task Two(L2CapChannel channel) => Task.CompletedTask;
            }
            """));

        Assert.Contains("SBH009", run.Ids());
    }


    [Fact]
    public void SBH010_SameCharacteristicFromTwoMergedClasses()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [BleService("180D")]
            public partial class First
            {
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            }

            [BleService("180D")]
            public partial class Second
            {
                [WriteCharacteristic("2A37")]
                void Write(byte[] data) { }
            }
            """));

        Assert.Contains("SBH010", run.Ids());
    }


    [Fact]
    public void SBH010_PsmCharacteristicClashingWithADeclaredOne()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [BleService("180D")]
            public partial class GattService
            {
                [ReadCharacteristic("2ABC")]
                byte[] Read() => new byte[] { 1 };
            }

            [L2CapService(PsmService = "180D", PsmCharacteristic = "2ABC")]
            public partial class StreamService
            {
                [OnChannelOpened]
                Task Serve(L2CapChannel channel) => Task.CompletedTask;
            }
            """));

        Assert.Contains("SBH010", run.Ids());
    }


    [Fact]
    public void SBH011_MergedServicesDisagreeingOnPrimary()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [BleService("180D", Primary = true)]
            public partial class First
            {
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            }

            [BleService("180D", Primary = false)]
            public partial class Second
            {
                [ReadCharacteristic("2A38")]
                byte[] Read() => new byte[] { 2 };
            }
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("SBH011", run.Ids(DiagnosticSeverity.Warning));
    }


    [Fact]
    public void SBH012_OptionCombinationThatTheEnumCannotExpress()
    {
        // Shiny's WriteOptions declares its members without explicit flag values, so
        // EncryptionRequired (3) is indistinguishable from WriteWithoutResponse | AuthenticatedSignedWrites
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39", WriteWithoutResponse = true, EncryptionRequired = true)]
                void Write(byte[] data) { }
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("SBH012", run.Ids(DiagnosticSeverity.Warning));
        // the security flag wins - silently dropping encryption is worse than the wrong write mode
        Assert.Contains("WriteOptions.EncryptionRequired", run.AllSource);
    }


    [Fact]
    public void SBH013_ManualRespondWithoutAWriteRequest()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39", ManualRespond = true)]
                void Write(byte[] data) { }
            """));

        Assert.Contains("SBH013", run.Ids());
    }


    [Fact]
    public void SBH013_ManualRespondReturningAStatus()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39", ManualRespond = true)]
                GattState Write(WriteRequest request) => GattState.Success;
            """));

        Assert.Contains("SBH013", run.Ids());
    }


    [Fact]
    public void SBH014_ClassLevelNotifyWithoutAName()
    {
        var run = GeneratorHarness.Run(Snippets.Service(
            """
                [ReadCharacteristic("2A19")]
                byte[] Read() => new byte[] { 1 };
            """,
            "[BleService(\"180F\")]\n[NotifyCharacteristic(\"2A19\")]"
        ));

        Assert.Contains("SBH014", run.Ids());
    }
}
