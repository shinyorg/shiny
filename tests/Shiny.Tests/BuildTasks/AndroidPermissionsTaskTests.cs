//using FluentAssertions;
//using Microsoft.Build.Framework;
//using Moq;
//using Xunit.Abstractions;

//namespace Shiny.Build.Tests;


//public class AndroidPermissionsTaskTests
//{
//    readonly ITestOutputHelper output;
//    public AndroidPermissionsTaskTests(ITestOutputHelper output) => this.output = output;


//    [Theory]
//    [InlineData(true, 0, "BluetoothLE", "Android.Manifest.Permission.Bluetooth")]
//    [InlineData(true, 0, "BeaconMonitoring", "Android.Manifest.Permission.ForegroundService")]
//    public void Test(bool success, int errorCount, string permission, string? shouldContain)
//    {
//        var buildEngine = new Mock<IBuildEngine>();
//        var taskItem = new Mock<ITaskItem>();
//        var errors = new List<BuildErrorEventArgs>();

//        taskItem.Setup(x => x.ItemSpec).Returns(permission);
//        buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e =>
//        {
//            errors.Add(e);
//            this.output.WriteLine(e.ToString());
//        });

//        var temp = Path.GetTempFileName();
//        var task = new AndroidPermissionsTask
//        {
//            IntermediateOutputPath = Path.GetDirectoryName(temp)!,
//            ManifestOutputFile = Path.GetFileName(temp),
//            Permissions = new[] { taskItem.Object },
//            BuildEngine = buildEngine.Object
//        };

//        task.Execute().Should().Be(success);
//        errors.Count.Should().Be(errorCount);

//        var content = File.ReadAllText(temp);
//        this.output.WriteLine(content);

//        if (shouldContain != null)
//            content.Should().Contain(shouldContain);
//    }
//}