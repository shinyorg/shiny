using System.Collections.Generic;

namespace Shiny.Printers.Document;


/// <summary>Word-wrapping helper used by <see cref="PrintDocument.WrapText"/> and available for direct use.</summary>
public static class TextWrap
{
    /// <summary>
    /// Word-wraps <paramref name="text"/> to at most <paramref name="width"/> characters per line. Words
    /// longer than the width are hard-split. Existing newlines in the input are honoured as breaks.
    /// </summary>
    public static IEnumerable<string> Wrap(string text, int width)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        // validated eagerly; WrapLine is the lazy iterator
        return text
            .Replace("\r\n", "\n")
            .Split('\n')
            .SelectMany(rawLine => rawLine.Length == 0 ? [string.Empty] : WrapLine(rawLine, width));
    }


    static IEnumerable<string> WrapLine(string rawLine, int width)
    {
        var line = new System.Text.StringBuilder();
        foreach (var word in rawLine.Split(' '))
        {
            var piece = word;

            // Hard-split words that can never fit.
            while (piece.Length > width)
            {
                if (line.Length > 0)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                yield return piece[..width];
                piece = piece[width..];
            }

            if (piece.Length > 0)
            {
                var needed = line.Length == 0 ? piece.Length : line.Length + 1 + piece.Length;
                if (needed > width)
                {
                    yield return line.ToString();
                    line.Clear();
                    line.Append(piece);
                }
                else
                {
                    if (line.Length > 0)
                        line.Append(' ');
                    line.Append(piece);
                }
            }
        }

        yield return line.ToString();
    }
}
