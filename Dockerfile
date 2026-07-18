# Samples

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
RUN apt update \
    && apt install -y --no-install-recommends python3 python3-pip libatomic1 \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet workload install wasm-tools aspire
RUN dotnet dev-certs https
WORKDIR /samples
COPY ["src/", "src/"]
COPY ["docs/", "docs/"]
COPY ["*.props", "."]
COPY Samples.sln .
RUN dotnet build -c:Debug
RUN dotnet build -c:Release --no-restore

# Create HelloWorld sample image
FROM build AS sample_hello_world
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/HelloWorld/debug/Samples.HelloWorld.dll"]

# Create HelloCart sample image
FROM build AS sample_hello_cart
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/HelloCart/debug/Samples.HelloCart.dll"]

# Create HelloBlazorServer sample image
FROM build AS sample_hello_blazor_server
WORKDIR /samples/src/HelloBlazorServer
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/HelloBlazorServer/debug/Samples.HelloBlazorServer.dll"]

# Create HelloBlazorHybrid sample image
FROM build AS sample_hello_blazor_hybrid
WORKDIR /samples/src/HelloBlazorHybrid/Server
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/HelloBlazorHybrid.Server/debug/Samples.HelloBlazorHybrid.Server.dll"]

# Create Blazor sample image
FROM build AS sample_blazor
WORKDIR /samples/src/Blazor/Server
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/Blazor.Server/debug/Samples.Blazor.Server.dll"]

# Create TodoApp sample image
FROM build AS sample_todoapp
WORKDIR /samples/src/TodoApp/Host
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/TodoApp.Host/debug/Samples.TodoApp.Host.dll"]

# Create MiniRpc sample image
FROM build AS sample_mini_rpc
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/MiniRpc/release/Samples.MiniRpc.dll"]

# Create MultiServerRpc sample image
FROM build AS sample_multi_server_rpc
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/MultiServerRpc/release/Samples.MultiServerRpc.dll"]

# Create MeshRpc sample image
FROM build AS sample_mesh_rpc
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/MeshRpc/release/Samples.MeshRpc.dll"]

# Create Benchmark sample image
FROM build AS sample_benchmark
ENV DbHost=host.docker.internal
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/Benchmark/release/Samples.Benchmark.dll"]

# Create RpcBenchmark sample image
# Lean, self-contained build of ONLY the RpcBenchmark project (+ its project refs).
# The shared `build` stage compiles the whole solution, which drags in TodoApp/Host's
# npm + TypeScript UI build; the RPC benchmark needs none of that. Building it standalone
# sidesteps that dependency entirely (grpc protoc comes from the Grpc.Tools NuGet package).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS rpc_benchmark_build
RUN apt update \
    && apt install -y --no-install-recommends libatomic1 \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet dev-certs https
WORKDIR /samples
COPY ["src/", "src/"]
COPY ["*.props", "."]
COPY Samples.sln .
RUN dotnet build -c:Release src/RpcBenchmark/RpcBenchmark.csproj

FROM rpc_benchmark_build AS sample_rpc_benchmark
ENTRYPOINT ["dotnet", "/samples/artifacts/bin/RpcBenchmark/release/Samples.RpcBenchmark.dll"]

# Websites

FROM build AS publish
WORKDIR /samples
RUN dotnet publish -c:Release --no-build --no-restore src/Blazor/Server/Server.csproj

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
WORKDIR /app
COPY --from=publish /samples/artifacts/publish/Blazor.Server/release .

# Create Blazor sample image for website
FROM runtime AS sample_blazor_ws
ENV Logging__Console__FormatterName=
ENV Server__GitHubClientId=Iv23liclgDFiYO8LJoHM
ENV Server__GitHubClientSecret=fdbae91e208663689f2c519b424f6bee46260ee7
ENTRYPOINT ["dotnet", "/app/Samples.Blazor.Server.dll"]

# --- Lean, self-contained website images ---------------------------------
# These publish only the single web project (+ its project references) instead
# of the whole solution. That keeps the build fast and, importantly, avoids
# projects that don't build cleanly on arm64 (e.g. RpcBenchmark, whose grpc
# protoc tool segfaults there). Used by deploy/docker-compose.prod.yml.

# Blazor sample website
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS web_build_blazor
# python3 + libatomic1 are needed by the WASM native relinking during Release publish
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 libatomic1 \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet workload install wasm-tools
WORKDIR /samples
COPY ["src/", "src/"]
COPY ["*.props", "."]
COPY Samples.sln .
RUN dotnet publish -c:Release -o /publish src/Blazor/Server/Server.csproj

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web_blazor
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV Logging__Console__FormatterName=
ENV Server__GitHubClientId=Iv23liclgDFiYO8LJoHM
ENV Server__GitHubClientSecret=fdbae91e208663689f2c519b424f6bee46260ee7
WORKDIR /app
COPY --from=web_build_blazor /publish .
ENTRYPOINT ["dotnet", "Samples.Blazor.Server.dll"]

# TodoApp sample website (its Host build runs npm for the optional TypeScript UI)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS web_build_todoapp
# nodejs/npm for the TypeScript UI; python3/libatomic1 for WASM native relinking
RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm python3 libatomic1 \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet workload install wasm-tools
WORKDIR /samples
COPY ["src/", "src/"]
COPY ["*.props", "."]
COPY Samples.sln .
RUN dotnet publish -c:Release -o /publish src/TodoApp/Host/Host.csproj

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web_todoapp
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV Logging__Console__FormatterName=
WORKDIR /app
COPY --from=web_build_todoapp /publish .
ENTRYPOINT ["dotnet", "Samples.TodoApp.Host.dll"]
