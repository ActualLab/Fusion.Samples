using System.IO.Pipelines;
using System.Reflection;
using System.Reflection.Emit;

namespace ActualLab.Benchmarking;

/// <summary>
/// Makes <see cref="PipeScheduler.ThreadPool"/> queue to the global thread pool queue, as it did before .NET 11.
/// .NET 11 switched it to <c>preferLocal: true</c>, so pipe readers (e.g. SignalR send loop) run almost
/// immediately and flush tiny batches - this cuts SignalR call throughput ~6x under load.
/// Set <c>ActualLab_PipeSchedulerFix</c> environment variable to <c>0</c> or <c>false</c> to disable it.
/// </summary>
public sealed class PipeSchedulerFix : PipeScheduler
{
    public const string EnvVarName = "ActualLab_PipeSchedulerFix";

    public static bool IsNeeded => Environment.Version.Major >= 11;
    public static bool IsEnabled {
        get {
            var value = Environment.GetEnvironmentVariable(EnvVarName)?.Trim() ?? "";
            return !(value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase));
        }
    }
    public static bool IsApplied { get; private set; }

    public static bool TryApply()
    {
        if (IsApplied)
            return true;
        if (!(IsNeeded && IsEnabled))
            return false;

        try {
            // The field is static readonly and typed as an internal sealed ThreadPoolScheduler,
            // so neither reflection nor UnsafeAccessor can assign another PipeScheduler to it.
            // This must run before any code reading PipeScheduler.ThreadPool gets JIT-compiled.
            var field = typeof(PipeScheduler).GetField(
                "s_threadPoolScheduler",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            var setter = new DynamicMethod(
                "SetThreadPoolPipeScheduler",
                null,
                [typeof(PipeScheduler)],
                typeof(PipeScheduler).Module,
                skipVisibility: true);
            var il = setter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);
            setter.CreateDelegate<Action<PipeScheduler>>().Invoke(new PipeSchedulerFix());
            IsApplied = ReferenceEquals(ThreadPool, field.GetValue(null)) && ThreadPool is PipeSchedulerFix;
        }
        catch (Exception e) {
            Error.WriteLine($"PipeSchedulerFix failed: {e.GetType().Name}: {e.Message}");
        }
        return IsApplied;
    }

    public override void Schedule(Action<object?> action, object? state)
        => System.Threading.ThreadPool.UnsafeQueueUserWorkItem(action, state, preferLocal: false);
}
