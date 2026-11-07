using Xunit;
using Shiny.BluetoothLE.AccessorySetup.Infrastructure;

namespace Shiny.BluetoothLE.AccessorySetup.Tests;


public class AccessoryFilterValidatorTests
{
    static AccessoryPickerItem Item(AccessoryDiscoveryFilter filter) => new("Sensor", filter);

    static AccessorySetupException Invalid(AccessoryDiscoveryFilter filter)
    {
        var ex = Assert.Throws<AccessorySetupException>(() => AccessoryFilterValidator.Validate([Item(filter)]));
        Assert.Equal(AccessorySetupErrorCode.InvalidRequest, ex.Code);
        return ex;
    }


    [Fact]
    public void ServiceUuid_Alone_IsValid()
        => AccessoryFilterValidator.Validate([Item(new() { ServiceUuid = "180D" })]);


    [Fact]
    public void CompanyIdentifier_Alone_IsValid()
        => AccessoryFilterValidator.Validate([Item(new() { CompanyIdentifier = 0x004C })]);


    [Fact]
    public void FullFilter_IsValid()
        => AccessoryFilterValidator.Validate([Item(new()
        {
            ServiceUuid = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E",
            ServiceData = [1, 2],
            ServiceDataMask = [0xFF, 0x00],
            CompanyIdentifier = 0x0059,
            ManufacturerData = [0xAA],
            ManufacturerDataMask = [0xF0],
            NameSubstring = "Shiny",
            Range = AccessoryRange.Immediate,
            RequiresPairing = true
        })]);


    [Fact]
    public void EmptyItems_Throws()
    {
        var ex = Assert.Throws<AccessorySetupException>(() => AccessoryFilterValidator.Validate([]));
        Assert.Equal(AccessorySetupErrorCode.InvalidRequest, ex.Code);
    }


    [Fact]
    public void MissingName_Throws()
    {
        var ex = Assert.Throws<AccessorySetupException>(() => AccessoryFilterValidator.Validate([new AccessoryPickerItem(" ", new() { ServiceUuid = "180D" })]));
        Assert.Equal(AccessorySetupErrorCode.InvalidRequest, ex.Code);
    }


    [Fact]
    public void NameOnly_Throws()
        => Assert.Contains("ServiceUuid or a CompanyIdentifier", Invalid(new() { NameSubstring = "Shiny" }).Message);


    [Fact]
    public void BadUuid_Throws()
        => Assert.Contains("not a 16-bit", Invalid(new() { ServiceUuid = "not-a-uuid" }).Message);


    [Fact]
    public void ServiceDataWithoutService_Throws()
        => Invalid(new() { CompanyIdentifier = 1, ServiceData = [1] });


    [Fact]
    public void ManufacturerDataWithoutCompany_Throws()
        => Invalid(new() { ServiceUuid = "180D", ManufacturerData = [1] });


    [Fact]
    public void MaskLengthMismatch_Throws()
    {
        Invalid(new() { CompanyIdentifier = 1, ManufacturerData = [1, 2], ManufacturerDataMask = [0xFF] });
        Invalid(new() { ServiceUuid = "180D", ServiceData = [1], ServiceDataMask = [0xFF, 0xFF] });
        Invalid(new() { CompanyIdentifier = 1, ManufacturerDataMask = [0xFF] });
    }


    [Fact]
    public void EmptyNameSubstring_Throws()
        => Invalid(new() { ServiceUuid = "180D", NameSubstring = "" });


    [Theory]
    [InlineData("180D", "0000180d-0000-1000-8000-00805f9b34fb")]
    [InlineData("0x180d", "0000180d-0000-1000-8000-00805f9b34fb")]
    [InlineData("1234ABCD", "1234abcd-0000-1000-8000-00805f9b34fb")]
    [InlineData("6E400001-B5A3-F393-E0A9-E50E24DCCA9E", "6e400001-b5a3-f393-e0a9-e50e24dcca9e")]
    public void ToFullUuid_ExpandsOnBluetoothBase(string input, string expected)
        => Assert.Equal(Guid.Parse(expected), AccessoryFilterValidator.ToFullUuid(input));
}
