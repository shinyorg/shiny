﻿namespace Shiny.Tests.Generators
{
    public static class CodeBlocks
    {
        public const string GpsDelegate = @"
namespace Test
{
    public class TestGpsDelegate : Shiny.Locations.IGpsDelegate
    {
        public Task OnReading(IGpsReading reading) => throw new NotImplementedException();
    }
}";


        public const string PushDelegate = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Test
{
    public class TestPushDelegate : Shiny.Push.IPushDelegate
    {
        public async Task OnEntry(PushEntryArgs args) { }
        public async Task OnReceived(IDictionary<string, string> data) { }
        public async Task OnTokenChanged(string token) { }
    }
}";
    }
}