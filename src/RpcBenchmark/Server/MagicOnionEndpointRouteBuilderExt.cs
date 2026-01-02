using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Samples.RpcBenchmark.Server;

/// <summary>
/// Workaround for MagicOnion MapMagicOnionService() issue with .NET 10.
/// The original method uses reflection to find MapGrpcService which now has
/// multiple overloads causing an "Ambiguous match found" error.
/// </summary>
public static class MagicOnionEndpointRouteBuilderExt
{
    private static readonly MethodInfo MapGrpcServiceMethod;

    static MagicOnionEndpointRouteBuilderExt()
    {
        // Get the specific MapGrpcService<TService>(IEndpointRouteBuilder) method
        // without the optional Action<GrpcServiceEndpointConventionBuilder> parameter
        MapGrpcServiceMethod = typeof(GrpcEndpointRouteBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m is { Name: "MapGrpcService", IsGenericMethodDefinition: true }
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IEndpointRouteBuilder));
    }

    public static void MapGrpcServiceFixed<TService>(this IEndpointRouteBuilder endpoints)
        where TService : class
    {
        // Use the specific MapGrpcService<TService>(IEndpointRouteBuilder) overload
        var method = MapGrpcServiceMethod.MakeGenericMethod(typeof(TService));
        method.Invoke(null, [endpoints]);
    }
}
