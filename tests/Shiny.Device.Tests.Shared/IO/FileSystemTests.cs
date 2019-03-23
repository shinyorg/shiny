using System;
using Shiny.IO;
using Xunit;


namespace Shiny.Device.Tests.IO
{
    public class FileSystemTests
    {
        [Fact]
        public void AppData()
            => Assert.Equal("", ShinyHost.Resolve<IFileSystem>().AppData.FullName);
    }
}
