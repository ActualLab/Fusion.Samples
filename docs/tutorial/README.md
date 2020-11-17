# Fusion Tutorial

> All project updates are published on its [Discord Server]; it's also the best place for Q/A.\
> [![Build](https://github.com/servicetitan/Stl.Fusion/workflows/Build/badge.svg)](https://github.com/servicetitan/Stl.Fusion/actions?query=workflow%3A%22Build%22)
> [![codecov](https://codecov.io/gh/servicetitan/Stl.Fusion/branch/master/graph/badge.svg)](https://codecov.io/gh/servicetitan/Stl.Fusion)
> [![NuGetVersion](https://img.shields.io/nuget/v/Stl.Fusion)](https://www.nuget.org/packages?q=Owner%3Aservicetitan+Tags%3Astl_fusion)
> [![Discord Server](https://img.shields.io/discord/729970863419424788.svg)](https://discord.gg/EKEwv6d)

This is an *interactive* tutorial for [Fusion] - a .NET Core library
trying to make real-time a new normal for any connected apps.
And although you can simply browse it, you can also run and modify any
C# code featured here. All you need is [Try .NET] or [Docker].

The simplest way to run this tutorial:

- Install [Docker](https://docs.docker.com/get-docker/) and
  [Docker Compose](https://docs.docker.com/compose/install/)
- Run `docker-compose up --build tutorial` in the root folder of this repository
- Open https://localhost:50005/README.md.

Alternatively, you can run it with `dotnet try` CLI tool:

- Install **both**
  [.NET 5.0 SDK](https://dotnet.microsoft.com/download) and
  [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core)
- Install [Try .NET](https://github.com/dotnet/try/blob/master/DotNetTryLocal.md).
  If its release version fails to run the code, install its preview version.
- Run `dotnet try --port 50005 docs/tutorial` in the root folder of this repository
- Open https://localhost:50005/README.md.

## Tutorial

The code based on Fusion might look completely weird at first -
that's because it is based on abstractions you need to learn about
before starting to dig into the code.

Understanding how they work will also eliminate a lot
of questions you might get further, so we highly recommend you
to complete this tutorial *before* digging into the source
code of Fusion samples.

Without further ado:

* [Part 0: NuGet packages](./Part00.md)
* [Part 1: Compute Services](./Part01.md)
* [Part 2: Computed Values: IComputed&lt;T&gt;](./Part02.md)
* [Part 3: State: IState&lt;T&gt; and Its Flavors](./Part03.md)
* [Part 4: Replica Services](./Part04.md)
* [Part 5: Caching and Fusion on Server-Side Only](./Part05.md)
* [Part 6: Real-time UI in Blazor Apps](./Part06.md)
* [Part 7: Real-time UI in JS / React Apps](./Part07.md)
* [Part 8: Scaling Fusion Services](./Part08.md)
* [Epilogue](./PartFF.md)

Check out the
[Overview](https://github.com/servicetitan/Stl.Fusion/blob/master/docs/Overview.md)
as well - it provides a high-level description of Fusion abstractions.

Join our [Discord Server] or [Gitter] to ask questions and track project updates.

[Discord Server]: https://discord.gg/EKEwv6d
[Gitter]: https://gitter.im/Stl-Fusion/community
[Fusion Feedback Form]: https://forms.gle/TpGkmTZttukhDMRB6
[Try .NET]: https://github.com/dotnet/try/blob/master/DotNetTryLocal.md
[Docker]: https://www.docker.com/
