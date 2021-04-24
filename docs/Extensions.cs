using System;
using System.Collections.Generic;
using System.Linq;
using Statiq.Common;


namespace Docs
{
    public static class Extensions
    {
        public static string GetDescription(this IDocument document)
            => document?.GetString("Description", String.Empty) ?? String.Empty;

        public static bool IsVisible(this IDocument document)
            => !document.GetBool("Hidden", false);

        public static bool ShowLink(this IDocument document)
            => !document.GetBool("NoLink", false);

        public static IEnumerable<IDocument> OnlyVisible(this IEnumerable<IDocument> source)
            => source.Where(x => x.IsVisible());
    }
}
