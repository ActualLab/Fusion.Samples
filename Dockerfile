# Tutorial

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS tutorial
WORKDIR /samples
COPY ["docs/", "docs/"]
WORKDIR /samples/docs/tutorial
ENV DOTNET_TRY_CLI_TELEMETRY_OPTOUT=1
RUN dotnet tool update -g Microsoft.dotnet-try
ENV PATH="$PATH:/root/.dotnet/tools"
RUN apt-get update
RUN apt-get install -y simpleproxy
RUN echo "simpleproxy -L 50005 -R localhost:50004 -v &" >start.sh
RUN echo "dotnet try --port 50004 /samples/docs/tutorial" >>start.sh
ENTRYPOINT ["sh", "start.sh"]

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
FROM build AS sample_rpc_benchmark
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
ENV Server__GitHubClientId=7d519556dd8207a36355
ENV Server__GitHubClientSecret=8e161ca4799b7e76e1c25429728db6b2430f2057
ENTRYPOINT ["dotnet", "/app/Samples.Blazor.Server.dll"]
