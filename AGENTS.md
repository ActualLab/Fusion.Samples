# Build and test instructions for ActualLab.Fusion.Samples repository

## Scope

This file applies to the entire repository: all code, documentation,
and scripts under this directory tree.

IMPORTANT: Read `README.md` to learn about available samples.
This repository contains sample projects for [ActualLab.Fusion](https://github.com/ActualLab/Fusion).

## Technology Stack

- **Language and Platform**: .NET 10 (and C# 13)
- **Databases**: some samples use Entity Framework Core
- **Other technologies**: samples with `Blazor` in their name use Blazor
- **Testing**: samples may include basic tests using xUnit

## Project Structure

- Main solution: `Samples.sln`
- Files are organized as:
    - `src/` folder contains all sample projects
    - Sample categories:
        - `HelloCart`, `HelloWorld` - basic console samples
        - `HelloBlazorServer`, `HelloBlazorHybrid` - Blazor samples
        - `Blazor`, `TodoApp` - more advanced Blazor samples
        - `MiniRpc`, `MultiServerRpc`, `MeshRpc` - RPC samples
        - `Benchmark`, `RpcBenchmark` - performance benchmarks

## Build Prerequisites

- Install .NET 10 SDK
- Run:
  ```powershell
  dotnet restore
  dotnet tool restore
  ```

## Building

The most important files related to build process are:
- `*.sln` and `*.csproj` files
- `Directory.Build.props` (also located in some sub-folders) and `Directory.Build.targets` files
- `Directory.Packages.props` file listing versions of C# project dependencies.

- To build the entire solution, use:
  ```powershell
  dotnet build Samples.sln
  ```

- You can build individual samples by specifying their `.csproj` file:
  ```powershell
  dotnet build src/HelloWorld/HelloWorld.csproj
  ```

## Running Samples

- Console samples:
  ```powershell
  dotnet run -p src/HelloWorld/HelloWorld.csproj
  dotnet run -p src/HelloCart/HelloCart.csproj
  ```

- Blazor/Web samples (open http://localhost:5005/ after running):
  ```powershell
  dotnet run -p src/HelloBlazorServer/HelloBlazorServer.csproj
  dotnet run -p src/Blazor/Server/Server.csproj
  dotnet run -p src/TodoApp/Host/Host.csproj
  ```

- Benchmarks:
  ```powershell
  dotnet run -c:Release -p src/Benchmark/Benchmark.csproj
  dotnet run -c:Release -p src/RpcBenchmark/RpcBenchmark.csproj
  ```

## Coding Conventions

See [`CODING_STYLE.md`](CODING_STYLE.md) for complete coding style guidelines.

YOU MUST ABSOLUTELY FOLLOW THESE CONVENTIONS.

## Pull Request Messages
- When creating a PR, include a brief summary of changes with a standard "feat:", "fix:", "refactor:", "chore:", or "docs:" prefix.
- Reference related issues or discussions if applicable.

## Programmatic Checks
- After making changes, run at least `dotnet build Samples.sln` to verify they don't break the build.
- Ensure all builds pass before submitting changes.

## Additional Notes

AGENTS.md in other folders may extend and override instructions provided here.
