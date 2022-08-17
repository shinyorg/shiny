//using FluentAssertions;
//using Microsoft.Build.Framework;
//using Moq;
//using Xunit.Abstractions;

//namespace Shiny.Build.Tests;


//public class ValidatePermissionsTaskTests
//{
//    readonly ITestOutputHelper output;
//    public ValidatePermissionsTaskTests(ITestOutputHelper output)
//        => this.output = output;


//    [Theory]
//    [InlineData("Jobs", true)]
//    [InlineData("BLOWUP", false)]
//    public void Test(string permission, bool expectedResult)
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
//        var task = new ValidatePermissionsTask
//        {
//            Permissions = new[] { taskItem.Object },
//        };

//        task.Execute().Should().Be(expectedResult);

//        var content = File.ReadAllText(temp);
//        this.output.WriteLine(content);
//    }
//}