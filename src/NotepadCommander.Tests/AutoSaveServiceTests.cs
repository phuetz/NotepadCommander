using Xunit;
using NotepadCommander.Core.Services.AutoSave;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class AutoSaveServiceTests : IDisposable
{
    private readonly AutoSaveService _service;

    public AutoSaveServiceTests()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<AutoSaveService>();
        _service = new AutoSaveService(logger);
    }

    [Fact]
    public void DefaultValues()
    {
        Assert.False(_service.IsEnabled);
        Assert.Equal(60, _service.IntervalSeconds);
    }

    [Fact]
    public void Start_WhenDisabled_DoesNotThrow()
    {
        _service.IsEnabled = false;
        _service.Start(() => Task.CompletedTask);
    }

    [Fact]
    public void Start_WhenEnabled_DoesNotThrow()
    {
        _service.IsEnabled = true;
        _service.IntervalSeconds = 300;
        _service.Start(() => Task.CompletedTask);
    }

    [Fact]
    public void Stop_DoesNotThrow()
    {
        _service.Stop();
    }

    [Fact]
    public void Stop_AfterStart_DoesNotThrow()
    {
        _service.IsEnabled = true;
        _service.IntervalSeconds = 300;
        _service.Start(() => Task.CompletedTask);
        _service.Stop();
    }

    [Fact]
    public void Dispose_CleansUp()
    {
        _service.IsEnabled = true;
        _service.IntervalSeconds = 300;
        _service.Start(() => Task.CompletedTask);
        _service.Dispose();
    }

    [Fact]
    public void Start_WithZeroInterval_DoesNotStart()
    {
        _service.IsEnabled = true;
        _service.IntervalSeconds = 0;
        _service.Start(() => Task.CompletedTask);
        // Should not throw, timer not created
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}
