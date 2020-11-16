using System;

using Microsoft.CodeAnalysis;

namespace Shiny.Generators
{
    public interface ILogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
    }

    public class SourceGeneratorLogger : ILogger
    {
        readonly GeneratorExecutionContext context;
        public SourceGeneratorLogger(GeneratorExecutionContext context) => this.context = context;

        public void Error(string msg) => Write(ConsoleColor.Red, msg);
        public void Info(string msg) => Write(ConsoleColor.White, msg);
        public void Warn(string msg) => Write(ConsoleColor.Yellow, msg);
        void Write(ConsoleColor color, string msg)
        {
            //this.context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SHINY", msg, msg, "", DiagnosticSeverity.Warning, true), Location.Create(syntaxTree, textSpan), true))
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
        }
    }
}
