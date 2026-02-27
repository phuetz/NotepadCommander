using Xunit;
using NotepadCommander.Core.Services.Terminal;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class TerminalServiceTests : IDisposable
{
    private readonly TerminalService _service;

    public TerminalServiceTests()
    {
        _service = new TerminalService(NullLogger<TerminalService>.Instance);
    }

    [Fact]
    public void IsRunning_ReturnsFalse_BeforeStart()
    {
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public void Stop_DoesNotThrow_WhenNotRunning()
    {
        _service.Stop();
    }

    [Fact]
    public void Dispose_CallsStop()
    {
        _service.Dispose();
        Assert.False(_service.IsRunning);
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}
