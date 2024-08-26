using ActualLab.Fusion.EntityFramework.LogProcessing;
using ActualLab.Fusion.EntityFramework.Operations;
using ActualLab.Resilience;

namespace Samples.HelloCart;

public static class AppSettings
{
    public static readonly bool UseAutoRunner = false;
    public static readonly bool EnableRandomLogMessageCommandFailures = false;

    public static class Db
    {
        public static readonly bool UsePostgreSql = false;
        public static readonly bool UseOperationLogWatchers = true;
        public static readonly bool UseRedisOperationLogWatchers = false;
        public static readonly bool UseOperationReprocessor = true;

        public static readonly bool UseChaosMaker = false;
        public static readonly ChaosMaker ChaosMaker = (
            (0.1*ChaosMaker.Delay(0.75, 1)) |
            (0.1*ChaosMaker.TransientError)
        ).Filtered("OF types", o => o is DbOperationScope or IDbLogReader).Gated();
    }
}
