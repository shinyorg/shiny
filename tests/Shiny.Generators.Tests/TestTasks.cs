using System;
using System.Collections.Generic;
using System.Text;

namespace Shiny.Generators.Tests
{
    public class RegisterMe : IShinyStartupTask
    {
        public void Start() => throw new NotImplementedException();
    }


    public class DontRegisterMe : IShinyStartupTask, ICloneable
    {
        public void Start() => throw new NotImplementedException();
        public object Clone() => throw new NotImplementedException();
    }
}
