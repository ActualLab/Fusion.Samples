using Samples.Blazor.Abstractions;

namespace Samples.Blazor.Server.Services;

public class SumService(StateFactory stateFactory) : ISumService
{
    private readonly IMutableState<double> _accumulator = stateFactory.NewMutable<double>();

    public Task Reset(CancellationToken cancellationToken)
    {
        _accumulator.Set(0);
        return Task.CompletedTask;
    }

    public Task Accumulate(double value, CancellationToken cancellationToken)
    {
        _accumulator.Set(x => x + value);
        return Task.CompletedTask;
    }

    // Compute methods

    public virtual async Task<double> GetAccumulator(CancellationToken cancellationToken)
        => await _accumulator.Use(cancellationToken);

    public virtual async Task<double> GetSum(double[] values, bool addAccumulator, CancellationToken cancellationToken)
    {
        var sum = values.Sum();
        if (addAccumulator)
            sum += await GetAccumulator(cancellationToken);
        return sum;
    }
}
