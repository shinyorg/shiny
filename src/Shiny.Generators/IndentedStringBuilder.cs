using System;
using System.Globalization;
using System.Text;


namespace Shiny.Generators
{
    public class DisposableAction : IDisposable
    {
        public DisposableAction(Action action) => this.Action = action;
        public Action Action { get; }
        public void Dispose() => this.Action();
    }


    public class IndentedStringBuilder
    {
        readonly StringBuilder builder = new StringBuilder();
        public int CurrentLevel { get; private set; }


        public virtual IDisposable Indent(int count = 1)
        {
            this.CurrentLevel += count;
            return new DisposableAction(() => this.CurrentLevel -= count);
        }


        public virtual IDisposable Block(int count = 1)
        {
            var current = this.CurrentLevel;

            this.CurrentLevel += count;
            this.Append("{".Indent(current));
            this.AppendLine();

            return new DisposableAction(() =>
            {
                this.CurrentLevel -= count;
                this.Append("}".Indent(current));
                this.AppendLine();
            });
        }


        //public virtual IDisposable Block(string value, int count = 1)
        //{
        //    var current = this.CurrentLevel;

        //    this.CurrentLevel += count;
        //    this.Append("{".Indent(current));
        //    this.AppendLine();

        //    return new DisposableAction(() =>
        //    {
        //        this.CurrentLevel -= count;
        //        this.Append("}".Indent(current));
        //        this.AppendLine();
        //    });
        //}

        public void AppendLine(IFormatProvider formatProvider, string pattern, params object[] replacements)
        {
            this.AppendFormat(formatProvider, pattern, replacements);
            this.AppendLine();
        }

        public void AppendLine(IFormatProvider formatProvider, int indentLevel, string pattern, params object[] replacements)
        {
            this.AppendFormat(formatProvider, pattern.Indent(indentLevel), replacements);
            this.AppendLine();
        }

        public virtual IDisposable Block(IFormatProvider formatProvider, string pattern, params object[] parameters)
        {
            this.AppendFormat(formatProvider, pattern, parameters);
            this.AppendLine();

            return this.Block();
        }

        public void AppendLineInvariant(string pattern, params object[] replacements)
            => this.AppendLine(CultureInfo.InvariantCulture, pattern, replacements);

        public void AppendLineInvariant(int indentLevel, string pattern, params object[] replacements)
            => this.AppendLine(CultureInfo.InvariantCulture, indentLevel, pattern, replacements);

        public void AppendFormatInvariant(string pattern, params object[] replacements)
            => this.AppendFormat(CultureInfo.InvariantCulture, pattern, replacements);

        public IDisposable BlockInvariant(string pattern, params object[] parameters)
            => this.Block(CultureInfo.InvariantCulture, pattern, parameters);

        public void AppendLine() => this.builder.AppendLine();
        public void AppendLine(string text) => this.builder.AppendLine(text.Indent(this.CurrentLevel));
        public virtual void Append(string text) => this.builder.Append(text);
        public virtual void AppendFormat(IFormatProvider formatProvider, string pattern, params object[] replacements)
            => this.builder.AppendFormat(formatProvider, pattern.Indent(this.CurrentLevel), replacements);

        public override string ToString() => this.builder.ToString();
    }


    static class StringExtensions
    {
        internal static string Indent(this string value, int level)
        {
            var newValue = "";
            for (var i = 0; i < level; i++)
                newValue += "\t";

            newValue += value;
            return newValue;
        }
    }
}
