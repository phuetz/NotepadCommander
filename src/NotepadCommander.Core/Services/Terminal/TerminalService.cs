using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.Terminal;

public class TerminalService : ITerminalService, IDisposable
{
    private readonly ILogger<TerminalService> _logger;
    private Process? _process;

    public event Action<string>? OutputReceived;
    public event Action? ProcessExited;

    public bool IsRunning => _process != null && !_process.HasExited;

    public TerminalService(ILogger<TerminalService> logger)
    {
        _logger = logger;
    }

    public void Start(string workingDirectory)
    {
        Stop();

        try
        {
            var shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "powershell.exe"
                : "/bin/bash";

            var psi = new ProcessStartInfo
            {
                FileName = shell,
                WorkingDirectory = workingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = Process.Start(psi);
            if (_process == null)
            {
                OutputReceived?.Invoke("[Erreur: impossible de demarrer le processus shell]");
                return;
            }

            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    OutputReceived?.Invoke(e.Data);
            };

            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    OutputReceived?.Invoke(e.Data);
            };

            _process.Exited += (_, _) =>
            {
                ProcessExited?.Invoke();
            };

            _process.EnableRaisingEvents = true;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            OutputReceived?.Invoke($"[Terminal demarre: {shell}]");
            OutputReceived?.Invoke($"[Repertoire: {workingDirectory}]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du demarrage du terminal");
            OutputReceived?.Invoke($"[Erreur: {ex.Message}]");
        }
    }

    public void SendInput(string input)
    {
        if (_process == null || _process.HasExited) return;

        try
        {
            _process.StandardInput.WriteLine(input);
            _process.StandardInput.Flush();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erreur d'envoi au terminal");
            OutputReceived?.Invoke($"[Erreur: {ex.Message}]");
        }
    }

    public void Stop()
    {
        if (_process == null) return;

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(2000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erreur lors de l'arret du terminal");
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
