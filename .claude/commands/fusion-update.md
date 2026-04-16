---
allowed-tools: Read, Edit, Bash(dotnet:*), Bash(ls:*), Bash(sleep:*), Bash(npm:*), Bash(cat:*)
description: Update Fusion to the latest version from local artifacts
argument-hint: [optional-version]
---

# Update Fusion to Latest Version

Update ActualLab.Fusion packages to the latest version built locally.

Arguments: $ARGUMENTS

## Instructions

1. Determine the version to use:
   - If an [optional-version] argument is provided (e.g., `11.4.8`), use that version
   - Otherwise, list files in @D:\Projects\ActualLab.Fusion\artifacts\nupkg and extract the version number from ActualLab.Fusion.*.nupkg filename (e.g., ActualLab.Fusion.11.4.7.nupkg → version is 11.4.7)

2. Update the version in @Directory.Packages.props:
   Change `<ActualLabFusionVersion>` to the new version number

3. Run restore with cache bypass:
   ```
   dotnet restore --no-http-cache Samples.sln
   ```

4. If restore fails because packages aren't available on NuGet yet:
   - Wait 1 minute (use `sleep 60` or equivalent)
   - Retry the restore
   - Do up to 10 retries

5. Build the solution:
   ```
   dotnet build Samples.sln
   ```

6. If there are build errors, try to fix them:
   - Read @D:\Projects\ActualLab.Fusion\CHANGELOG.md for breaking changes
   - Check recent commit history in D:\Projects\ActualLab.Fusion using `git log --oneline -20`
   - Look at changes in `/samples` and `/tests` folders in the Fusion repo - these are the most useful for understanding how to adapt code:
     ```
     git -C "D:\Projects\ActualLab.Fusion" log --oneline -20 -- samples/ tests/
     git -C "D:\Projects\ActualLab.Fusion" diff HEAD~10 -- samples/ tests/
     ```
   - Apply similar fixes to this repository
   - Rebuild and repeat until all errors are fixed

7. Report the update result (old version → new version)

## Updating Fusion's npm packages (TodoApp TypeScriptUI)

Fusion also ships TypeScript packages on npm. The TodoApp sample's TypeScriptUI consumes them.

- File to edit: @src/TodoApp/TypeScriptUI/package.json
- Packages to update (all share the same version):
  - `@actuallab/core`
  - `@actuallab/rpc`
  - `@actuallab/fusion`
  - `@actuallab/fusion-rpc`
  - `@actuallab/fusion-react`

Steps:
1. Look up the latest version published on npm (any of the packages above will do, since they're released together):
   ```
   npm view @actuallab/fusion version
   ```
2. Update the `^x.y.z` version for all five `@actuallab/*` entries in `package.json` to that version.
3. Install to refresh `node_modules` and `package-lock.json`:
   ```
   cd src/TodoApp/TypeScriptUI && npm install
   ```
4. Verify the installed version matches what was requested:
   ```
   cat src/TodoApp/TypeScriptUI/node_modules/@actuallab/fusion/package.json
   ```

Note: npm and NuGet versions are released independently and may not match — always look up each separately.
