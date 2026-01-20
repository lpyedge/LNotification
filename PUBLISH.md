# Publish NuGet Package

## Prerequisites

1. **NuGet API key**: create one at https://www.nuget.org/account/apikeys
2. **GitHub Actions secret**: add `NUGET_API_KEY` under Settings > Secrets and variables > Actions > New repository secret

## Target frameworks

The package targets:
- .NET 5.0
- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0
- .NET 10.0
- .NET Standard 2.0
- .NET Standard 2.1

## Automated publish (recommended)

The workflow `.github/workflows/publish.yml` handles build, test, pack, and push.
It uses the project version in `src/LNotification/LNotification.csproj` and does not
override `Version` from inputs.

### Option 1: Publish from a release tag

1. Create and push a tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
2. Create a GitHub Release from that tag.
3. The workflow will build, test, pack, and publish to NuGet.org.

### Option 2: Manual workflow dispatch

1. Go to GitHub Actions.
2. Select the **Publish NuGet Package** workflow.
3. Click **Run workflow** (no inputs required).

## Manual publish

### 1. Update the version

Edit `src/LNotification/LNotification.csproj`:
```xml
<Version>1.0.0</Version>
```

### 2. Build and test

```bash
cd src/LNotification
dotnet clean
dotnet build --configuration Release
cd ../../tests/LNotification.Tests
dotnet test --configuration Release
```

### 3. Pack

```bash
cd ../../src/LNotification
dotnet pack --configuration Release --output ../../artifacts
```

### 4. Push to NuGet

```bash
cd ../../
dotnet nuget push ./artifacts/LNotification.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## Conditional compilation notes

- `GeneratedRegex` is used only on .NET 7+; older targets use precompiled `Regex`.
- `ImplicitUsings` is enabled on .NET 6+.
- `System.Net.Http.Json` 8.0.1 is referenced for .NET 5 and .NET Standard (fixes security vulnerability).
- `string.Replace(..., StringComparison)` is avoided on .NET Standard 2.0.

## Security updates

**v0.1.0**: Upgraded `System.Net.Http.Json` to 8.0.1 to fix HIGH severity vulnerability in `System.Text.Json 6.0.0` (GHSA-8g4q-xg66-9fp4).

## Troubleshooting

- **GitHub Actions error: "does not contain a project or solution file"**
  - Fixed: All dotnet commands now specify project paths explicitly.
- **NuGet push fails: package already exists**
  - You cannot push the same version twice. Bump the version and retry.
- **Build fails on a target framework**
  - Verify package versions and conditional compilation symbols.
- **GitHub Actions fails**
  - Check that `NUGET_API_KEY` is valid and not expired.
  - Verify the secret name is exactly `NUGET_API_KEY` (case-sensitive).

## Repository URLs

Update the URLs in `src/LNotification/LNotification.csproj`:
```xml
<RepositoryUrl>https://github.com/lpyedge/LNotification</RepositoryUrl>
<PackageProjectUrl>https://github.com/lpyedge/LNotification</PackageProjectUrl>
```
