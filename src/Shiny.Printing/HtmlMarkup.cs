using System.Globalization;

namespace Shiny.Printing;


static class HtmlMarkup
{
    /// <summary>
    /// Adds a default CSS <c>@page</c> margin ahead of the document's own styles, so a margin rule the
    /// document declares itself still wins. Goes inside <c>&lt;head&gt;</c> when there is one.
    /// </summary>
    public static string WithDefaultPageMargin(string html, float marginPoints)
    {
        var style = $"<style>@page {{ margin: {marginPoints.ToString(CultureInfo.InvariantCulture)}pt; }}</style>";

        var head = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
        if (head >= 0)
        {
            var close = html.IndexOf('>', head);
            if (close >= 0)
                return html.Insert(close + 1, style);
        }
        return style + html;
    }
}
