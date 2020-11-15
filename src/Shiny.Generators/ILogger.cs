using System;


namespace Shiny.Generators
{
    public interface ILogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
    }

    public class ConsoleLogger : ILogger
    {
        public void Error(string msg) => Write(ConsoleColor.Red, msg);
        public void Info(string msg) => Write(ConsoleColor.White, msg);
        public void Warn(string msg) => Write(ConsoleColor.Yellow, msg);
        void Write(ConsoleColor color, string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
        }
    }
}
