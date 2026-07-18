using System.Diagnostics;

namespace ActualLab.Benchmarking;

// These benchmarks talk over localhost (127.0.0.1), so Windows loopback settings directly gate
// their throughput. Enabling "loopback large MTU" (netsh int ipv4/ipv6 set gl loopbacklargemtu=enable)
// roughly doubles loopback stream throughput and is required to reproduce the documented RpcBenchmark
// stream results; when it's disabled, stream throughput is throttled ~2x. This helper warns (in red)
// when large MTU is off.
//
// NOTE: large MTU also breaks the many-short-connection HTTP loopback path (it can hang the plain
// Benchmark's HTTP/DB tests), so that benchmark is meant to run with large MTU DISABLED. Only the
// RpcBenchmark (streams) needs it enabled. See Set-LoopbackMode.ps1 and Benchmarks.md.
public static class LoopbackInfo
{
    public const string FixScript = "Set-LoopbackMode.ps1";

    // Returns whether Windows loopback large MTU is enabled, or null if it can't be determined
    // (non-Windows, netsh missing/blocked, unexpected output).
    public static bool? TryGetWindowsLargeMtuEnabled()
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
                if (line.IndexOf("Loopback Large Mtu", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var colon = line.IndexOf(':');
                if (colon >= 0)
                    return line[(colon + 1)..].Trim().StartsWith("enabled", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch {
            // Diagnostics only - never fail the benchmark because of this check.
        }
        return null;
    }

    // Prints a red warning if Windows loopback large MTU is disabled (throttles stream throughput).
    public static void WarnIfLoopbackThrottled()
    {
        if (TryGetWindowsLargeMtuEnabled() != false)
            return; // enabled, or unknown/non-Windows - nothing to warn about

        var previous = ForegroundColor;
        ForegroundColor = ConsoleColor.Red;
        try {
            WriteLine("WARNING: Windows loopback large MTU is disabled.");
            WriteLine("  This throttles localhost stream throughput ~2x and understates these results.");
            WriteLine($"  Fix (elevated PowerShell): .\\{FixScript} enable");
        }
        finally {
            ForegroundColor = previous;
        }
    }
}
