namespace Samples.Blazor.Abstractions;

public interface ITimeService
{
    [ComputeMethod(KeepAliveTime = 1)]
    Task<DateTime> GetTime(CancellationToken cancellationToken = default);
    [ComputeMethod(KeepAliveTime = 60)]
    Task<double> GetUptime(double updatePeriod, CancellationToken cancellationToken = default);
}
