@echo off
dotnet publish src/NativeAot/NativeAot.csproj
"artifacts/publish/NativeAot/release/Samples.NativeAot.exe"
