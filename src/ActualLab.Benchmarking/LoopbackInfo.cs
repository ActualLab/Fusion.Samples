using System.Diagnostics;

namespace ActualLab.Benchmarking;

// These benchmarks talk over localhost (127.0.0.1), so the Windows "loopback execution mode"
// directly gates their throughput. The default 'inline' mode favors latency over throughput and
// can cap loopback bandwidth ~20x, which silently skews localhost benchmark results. This helper
// warns (in red) when the machine isn't in the throughput-friendly 'worker' mode.
public static class LoopbackInfo
{
    public const string FixScript = "Set-LoopbackMode.ps1";

    // Returns the Windows loopback execution mode ("worker"/"inline"/"adaptive"), or null if it
    // can't be determined (non-Windows, netsh missing/blocked, unexpected output).
    public static string? TryGetWindowsExecutionMode()
    {
        if (!OperatingSystem.IsWindows())
            return null;
        try {
            var psi = new ProcessStartInfo("netsh", "int ipv4 show global") {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            if (process == null)
                return null;
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(2000)) {
                try { process.Kill(); } catch { /* ignore */ }
                return null;
            }
            foreach (var line in output.Split('\n')) {
                if (line.IndexOf("Loopback Execution Mode", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var colon = line.IndexOf(':');
                if (colon >= 0)
                    return line[(colon + 1)..].Trim();
            }
        }
        catch {
            // Diagnostics only - never fail the benchmark because of this check.
        }
        return null;
    }

    // Prints a red warning if Windows loopback isn't in throughput ('worker') mode.
    public static void WarnIfLoopbackThrottled()
    {
        var mode = TryGetWindowsExecutionMode();
        if (mode == null || string.Equals(mode, "worker", StringComparison.OrdinalIgnoreCase))
            return;

        var previous = ForegroundColor;
        ForegroundColor = ConsoleColor.Red;
        try {
            WriteLine($"WARNING: Windows loopback execution mode is '{mode}', not 'worker'.");
            WriteLine("  This throttles localhost throughput (~20x) and skews these benchmarks.");
            WriteLine($"  Fix (elevated PowerShell): .\\{FixScript} enable");
        }
        finally {
            ForegroundColor = previous;
        }
    }
}
