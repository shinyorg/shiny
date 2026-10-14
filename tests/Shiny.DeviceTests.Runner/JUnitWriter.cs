using System.Globalization;
using System.Xml.Linq;

namespace Shiny.DeviceTests.Runner;


public static class JUnitWriter
{
    public static void Write(string path, TargetDevice target, TestRunStatus status)
    {
        var suites = status.Results
            .GroupBy(x => x.Suite)
            .Select(g => new XElement("testsuite",
                new XAttribute("name", $"{target.Platform}.{g.Key}"),
                new XAttribute("tests", g.Count()),
                new XAttribute("failures", g.Count(x => x.Outcome == TestOutcome.Failed)),
                new XAttribute("skipped", g.Count(x => x.Outcome == TestOutcome.Skipped)),
                new XAttribute("time", Seconds(g.Sum(x => x.DurationMs))),
                g.Select(r => new XElement("testcase",
                    new XAttribute("classname", $"{target.Platform}.{r.Suite}"),
                    new XAttribute("name", r.Name),
                    new XAttribute("time", Seconds(r.DurationMs)),
                    r.Outcome switch
                    {
                        TestOutcome.Failed => new XElement("failure", new XAttribute("message", r.Message ?? ""), r.StackTrace),
                        TestOutcome.Skipped => new XElement("skipped", new XAttribute("message", r.Message ?? "")),
                        _ => null
                    },
                    r.Log.Length == 0 ? null : new XElement("system-out", String.Join('\n', r.Log))
                ))
            ));

        var doc = new XDocument(new XElement("testsuites",
            new XAttribute("name", $"Shiny.DeviceTests ({target.Platform})"),
            new XAttribute("tests", status.Results.Count),
            new XAttribute("failures", status.Failed),
            new XAttribute("skipped", status.Skipped),
            suites
        ));

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (dir != null)
            Directory.CreateDirectory(dir);

        doc.Save(path);
    }


    static string Seconds(double ms) => (ms / 1000).ToString("0.###", CultureInfo.InvariantCulture);
}
