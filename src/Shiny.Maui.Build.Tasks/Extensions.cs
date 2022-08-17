using System;
using System.Linq;

namespace Shiny.Build;


internal static class Extensions
{
    public static string ToCamelCase(this string str)
    {
        var words = str.Split(new[] { "_", " " }, StringSplitOptions.RemoveEmptyEntries);
        words = words
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower())
            .ToArray();

        return string.Join(string.Empty, words);
    }
}
