---
allowed-tools: Read, Edit, Write, Bash(find:*), Bash(stat:*), Bash(ls:*), Bash(wc:*)
description: Sync samples from ActualLab.Fusion/samples to this project
argument-hint: [folder-name]
---

# Samples Sync Tool

Synchronize sample code between ActualLab.Fusion `/samples` folder and this project's `/src` folder.

Arguments: $ARGUMENTS

## Folder Mapping

| Fusion Path | Samples Path | Notes |
|-------------|--------------|-------|
| `samples/TodoApp/*` | `src/TodoApp/*` | Main web demo app |
| `samples/HelloCart/*` | `src/HelloCart/*` | Cart demo |
| `samples/MeshRpc/*` | `src/MeshRpc/*` | Mesh RPC demo |
| `samples/MiniRpc/*` | `src/MiniRpc/*` | Mini RPC demo |
| `samples/MultiServerRpc/*` | `src/MultiServerRpc/*` | Multi-server RPC demo |
| `samples/NativeAot/*` | `src/NativeAot/*` | Native AOT demo |
| `samples/Directory.Build.props` | N/A | **DO NOT SYNC** - this project has its own structure |

## Files to Sync

Sync these file types:
- `*.cs` - C# source files
- `*.razor` - Razor components
- `*.csproj` - Project files (review carefully for path differences)
- `*.props` - MSBuild props (except root Directory.Build.props)

## Files to NEVER Sync

- `Directory.Build.props` at project root (different structure)
- `bin/` and `obj/` folders
- `*.json` configuration files (may have environment-specific settings)
- `appsettings*.json` files
- `wwwroot/` static assets (review manually)

## Instructions

1. **Determine scope**:
   - If a [folder-name] argument is provided (e.g., `TodoApp`), only sync that folder
   - Otherwise, scan all mapped folders

2. **Build file lists**:
   For each mapped folder, use `find` to list all syncable files:
   ```bash
   # Fusion side
   find "$AC_Project1Path/samples/{FolderName}" \( -name "*.cs" -o -name "*.razor" -o -name "*.csproj" \) -type f | grep -v '/obj/' | grep -v '/bin/'

   # Samples side
   find "/proj/ActualLab.Fusion.Samples/src/{FolderName}" \( -name "*.cs" -o -name "*.razor" -o -name "*.csproj" \) -type f | grep -v '/obj/' | grep -v '/bin/'
   ```

3. **Compare files**:
   For each file found, compute relative path and check if:
   - File exists only in Fusion (NEW → should be copied)
   - File exists only in Samples (ORPHAN → may need deletion or is Samples-specific)
   - File exists in both with DIFFERENT sizes (MODIFIED → needs review)
   - File exists in both with SAME size (UNCHANGED → skip)
   - File is in the **Known Differences** list below (KEEP → intentional, do not copy)

   **Caveat — line endings:** Fusion is CRLF, this repo is LF, so almost every
   file differs in size by exactly its line count with no real content change.
   Confirm real differences with a CRLF-agnostic diff before treating a file as
   MODIFIED, e.g. `diff --strip-trailing-cr <samples> <fusion>`. A size delta
   equal to the line count = line endings only → treat as UNCHANGED.

4. **Present findings** in a table. **Always show KEEP rows too** — never
   silently drop a known-intentional difference; the point is to make it visible
   that it was seen and deliberately not synced.
   ```
   | Status | Relative Path | Fusion Size | Samples Size |
   |--------|---------------|-------------|--------------|
   | NEW | Abstractions/IStockApi.cs | 1234 | - |
   | MODIFIED | Host/Program.cs | 5678 | 5432 |
   | KEEP | UI/Program.cs | 2873 | 2363 |
   | ORPHAN | UI/CustomPage.razor | - | 2345 |
   ```

5. **For MODIFIED files**, read and compare:
   - Show a brief summary of what changed (if possible)
   - Use diff-style analysis: new methods, removed code, etc.

6. **Ask user** which files to sync:
   - NEW files: copy from Fusion to Samples
   - MODIFIED files: overwrite Samples with Fusion version (or skip)
   - ORPHAN files: leave as-is (Samples-specific) or delete
   - KEEP files: do NOT offer to sync — they're intentional (see Known
     Differences). List them so the user knows they were checked, nothing more.

7. **Execute sync**:
   - For NEW files: read from Fusion, write to Samples
   - For MODIFIED files: read from Fusion, write to Samples
   - Report what was synced

## Known Differences (DO NOT SYNC These)

Some files are intentionally different in this project:

### .csproj Files (NEVER sync)
All `.csproj` files use different reference styles:
- **Fusion samples**: Use `<ProjectReference>` to local Fusion source projects
- **This project**: Use `<PackageReference>` to NuGet packages

### Global Usings — kept in sync (should stay identical)
Project-level global usings are deliberately aligned so any `.cs` file is
portable between the two repos. Keep them identical going forward:
- Root `Directory.Build.props` (Fusion `samples/Directory.Build.props` ↔ this
  repo's repo-root `Directory.Build.props`) — same `<Using>` set and order.
- `TodoApp/Directory.Build.props` and `HelloCart/Directory.Build.props` — same
  `<Using>` set on both sides.

If a per-file `using` is already covered by a global using (e.g.
`using System.Collections.Concurrent;`), it's redundant — the file should NOT
carry it on either side.

### Intentional code differences — OK to keep, but STILL SHOW them
Deliberate Fusion-side dev/test tweaks. Mark these **KEEP** in the findings
(show them so it's clear they were checked), but do NOT copy them into this repo:
- `TodoApp/Host/Program.cs` — Fusion-only fr-FR culture middleware (localization testing).
- `TodoApp/UI/Program.cs` — Fusion-only fr-FR culture setup + `ComponentInfo.DebugLog`
  debug logging + a commented-out benchmark snippet.
- `TodoApp/UI/ClientStartup.cs` — RPC serialization default differs (this repo:
  `msgpack6c`; Fusion: `json5np`) + Fusion-only commented `ComputedSynchronizer.DefaultCurrent` line.

### Configuration Files
- `Host/appsettings.json` has environment-specific configuration
- `UI/Program.cs` may have different startup logic for the standalone Samples project

## Example Usage

```
# Sync all samples
/samples-sync

# Sync only TodoApp
/samples-sync TodoApp

# Sync only MeshRpc
/samples-sync MeshRpc
```

## Post-Sync

After syncing, build the project to verify:
```bash
dotnet build src/{FolderName}/Host/Host.csproj
```

If build fails, check for:
- Missing package references
- Namespace differences
- Path differences in imports
