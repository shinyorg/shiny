namespace Shiny.DeviceTests.Framework;


public sealed record DeviceTestCase(string Suite, string Name, Func<TestContext, Task> Run, TimeSpan Timeout);


/// <summary>
/// A group of end-to-end tests for one module. Register with AddDeviceTestSuite&lt;T&gt;() and declare each
/// case in Define() with Test(...). Suites are resolved per run, so constructor-injected Shiny services are
/// the real platform implementations.
/// </summary>
public abstract class DeviceTestSuite
{
    List<DeviceTestCase>? tests;

    public abstract string Name { get; }

    // declare the suite's tests with Test(...)
    protected abstract void Define();

    public IReadOnlyList<DeviceTestCase> Tests
    {
        get
        {
            if (this.tests == null)
            {
                this.tests = [];
                this.Define();
            }
            return this.tests;
        }
    }

    // default per-test timeout - background/OS-scheduled work passes a longer one
    protected virtual TimeSpan DefaultTimeout => TimeSpan.FromSeconds(60);

    protected void Test(string name, Func<TestContext, Task> run, TimeSpan? timeout = null)
        => this.tests!.Add(new DeviceTestCase(this.Name, name, run, timeout ?? this.DefaultTimeout));

    // runs once before the suite's first selected test; throw SkipException to skip the whole suite
    public virtual Task SetupAsync(TestContext context) => Task.CompletedTask;

    // runs once after the suite's last selected test, even when tests failed
    public virtual Task TeardownAsync(TestContext context) => Task.CompletedTask;
}


public sealed class SkipException(string reason) : Exception(reason);
