using System;
using System.Text;


namespace Shiny.Generators
{
    internal class DisposableAction : IDisposable
    {
        public Action Action { get; init set; }
        public void Dispose() => this.Action();
    }

    public class IndentedStringBuilder
    {
        readonly StringBuilder _stringBuilder = new StringBuilder();
        public int CurrentLevel { get; private set; }


        public virtual IDisposable Indent(int count = 1)
        {
            CurrentLevel += count;
            return new DisposableAction(() => CurrentLevel -= count);
        }

        public virtual IDisposable Block(int count = 1)
        {
            var current = CurrentLevel;

            CurrentLevel += count;
            Append("{".Indent(current));
            AppendLine();

            return new DisposableAction(() =>
            {
                CurrentLevel -= count;
                Append("}".Indent(current));
                AppendLine();
            });
        }

        public void AppendLine(IFormatProvider formatProvider, string pattern, params object[] replacements)
        {
            builder.AppendFormat(formatProvider, pattern, replacements);
            builder.AppendLine();
        }

        public static void AppendLine(this IIndentedStringBuilder builder, IFormatProvider formatProvider, int indentLevel, string pattern, params object[] replacements)
        {
            builder.AppendFormat(formatProvider, pattern.Indent(indentLevel), replacements);
            builder.AppendLine();
        }

        public static void AppendLineInvariant(this IIndentedStringBuilder builder, string pattern, params object[] replacements)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, pattern, replacements);
        }

        public static void AppendLineInvariant(this IIndentedStringBuilder builder, int indentLevel, string pattern, params object[] replacements)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, indentLevel, pattern, replacements);
        }

        public static void AppendFormatInvariant(this IIndentedStringBuilder builder, string pattern, params object[] replacements)
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, pattern, replacements);
        }

        public static IDisposable BlockInvariant(this IIndentedStringBuilder builder, string pattern, params object[] parameters)
        {
            return builder.Block(CultureInfo.InvariantCulture, pattern, parameters);
        }

        public virtual IDisposable Block(IFormatProvider formatProvider, string pattern, params object[] parameters)
        {
            AppendFormat(formatProvider, pattern, parameters);
            AppendLine();

            return Block();
        }

        public virtual void Append(string text)
        {
            _stringBuilder.Append(text);
        }

        public virtual void AppendFormat(IFormatProvider formatProvider, string pattern, params object[] replacements)
        {
            _stringBuilder.AppendFormat(formatProvider, pattern.Indent(CurrentLevel), replacements);
        }

        public virtual void AppendLine()
        {
            _stringBuilder.AppendLine();
        }

        public virtual void AppendLine(string text)
        {
            _stringBuilder.Append(text.Indent(CurrentLevel));
        }

        public override string ToString()
        {
            return _stringBuilder.ToString();
        }
    }
}
}
