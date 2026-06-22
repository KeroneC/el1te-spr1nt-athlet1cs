# .NET 10 Upgrade

## What Was Built

All API and test projects target `net10.0`. `global.json` selects SDK `10.0.301`, permits newer .NET 10 feature bands, and disallows prerelease SDKs. NuGet references were updated to compatible versions while preserving authentication and CMS behavior.

## Why It Exists

Pinning the SDK gives local machines and automation a shared compiler/tooling baseline. An upgrade is not complete when code compiles once: package compatibility, generated EF artifacts, runtime behavior, and tests must agree.

## Important Files

- `global.json`
- `apps/api/El1teSpr1ntTrack.sln`
- Project files under `apps/api/src` and `apps/api/tests`
- `NuGet.Config`

## How It Works

The .NET CLI searches upward for `global.json`, chooses a compatible SDK, and uses each project target framework and package graph for restore/build. `rollForward: latestFeature` permits a newer .NET 10 feature band when `10.0.301` is unavailable without crossing into .NET 11.

## Request or Data Flow

This phase changed the toolchain rather than HTTP flow. The validation flow is SDK selection -> NuGet restore -> compile -> test discovery -> unit/integration execution.

## How to Test It

```powershell
dotnet --version
dotnet restore apps/api/El1teSpr1ntTrack.sln --configfile NuGet.Config
dotnet build apps/api/El1teSpr1ntTrack.sln --configuration Release
dotnet test apps/api/El1teSpr1ntTrack.sln --configuration Release
```

## Common Problems

- SDK not found: install a compatible .NET 10 SDK and restart the terminal/IDE.
- Visual Studio cannot load projects: install a version/workload supporting .NET 10.
- Restore errors: verify `NuGet.Config`, network access, and package compatibility.
- EF CLI mismatch: install the documented 10.x `dotnet-ef` tool.

## Concepts to Study

Target frameworks, SDK resolution, semantic versioning, NuGet dependency graphs, breaking changes, and regression testing.

## What Was Intentionally Deferred

No application features, deployment changes, or framework-specific redesign was part of the upgrade.
