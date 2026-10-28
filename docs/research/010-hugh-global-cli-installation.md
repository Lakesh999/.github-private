# Research: Hugh Global CLI Installation

## Document Information

| Field | Value |
|-------|-------|
| Document | 010-hugh-global-cli-installation.md |
| Created | 2026-02-05 |
| Status | ✅ Complete |
| Current Phase | 5 of 5 |

## Overview

This research provides comprehensive guidance for distributing Hugh as a globally-installable Windows CLI tool. The investigation covered installer technologies, .NET packaging, SDK credential handling, CI/CD automation, and architectural adaptations.

### Key Findings Summary

1. **Inno Setup is the optimal installer technology** — Proven by VS Code and Git for Windows, provides excellent PATH management, silent installation support, and single-file distribution. Low complexity compared to WiX with all necessary features.

2. **36 MB self-contained executable is the recommended package** — Single-file publish without trimming avoids 24+ reflection-related warnings. ReadyToRun provides minimal benefit for a long-running tool.

3. **No credential configuration needed for global CLI** — The Copilot SDK's `UseLoggedInUser=true` default detects GitHub CLI credentials from the user's home directory, making authentication location-independent.

4. **Two-job GitHub Actions workflow** — Build on Windows runner, create installer with Inno Setup Action, publish to GitHub Releases with `softprops/action-gh-release@v2`.

5. **Clear separation of installation vs. workspace paths** — Hugh's config and agents should resolve from assembly location; workspace (docs/, .orchestrator/) correctly remains CWD-relative.

### Cross-Cutting Patterns

- **PATH integration is critical** — Inno Setup's `ChangesEnvironment=yes` notifies running applications without restart
- **Configuration discovery chain** — Assembly location → environment variable → CWD provides flexibility for installed, CI, and dev scenarios
- **Prerequisites validation** — PrerequisiteChecker already validates Copilot CLI; can be enhanced for auth status

### Actionable Conclusions

1. **Immediate:** Update `Program.cs` configuration loading to use assembly location for `appsettings.json`
2. **Immediate:** Refactor `AgentRegistry` to use configurable agent base path
3. **Next:** Create `installer/hugh-setup.iss` Inno Setup script
4. **Next:** Add `.github/workflows/release.yml` with tag-triggered builds
5. **Future:** Consider user-level agent overrides at `~/.hugh/agents/`

### Open Questions

- Single-file publish behavior for `Assembly.GetExecutingAssembly().Location` needs verification
- ARM64 Windows build priority (nice-to-have vs. required)
- Code signing certificate acquisition for enterprise deployment

---

## Research Objectives

This research supports the vision document: `/docs/vision/003-hugh-global-cli-installation.md`

**Goal:** Gather comprehensive information to enable Hugh's distribution as a globally-installable Windows CLI tool with automatic credential detection from GitHub Copilot.

### Key Questions to Answer

1. What Windows installer technology is best suited for self-contained .NET applications?
2. How can Hugh leverage the Copilot CLI SDK's credential detection for zero-configuration operation?
3. How should Hugh be packaged as a self-contained executable for distribution?
4. What GitHub Actions workflow can automate Windows installer builds and GitHub Releases publishing?
5. How should Hugh's current architecture be adapted to support current-working-directory operation?

---

## Research Phases

| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 1 | Windows Installer Technologies | ✅ Complete | 2026-02-05 |
| 2 | Self-Contained .NET Packaging | ✅ Complete | 2026-02-05 |
| 3 | Copilot SDK Credential Detection | ✅ Complete | 2026-02-05 |
| 4 | GitHub Releases Automation | ✅ Complete | 2026-02-05 |
| 5 | Hugh CWD Architecture Adaptation | ✅ Complete | 2026-02-05 |

---

## Phase 1: Windows Installer Technologies

**Status:** ✅ Complete
**Scope:** 
- Compare Windows installer technologies (WiX, Inno Setup, MSIX, NSIS)
- Evaluate suitability for self-contained .NET executables
- Investigate PATH environment variable management
- Research enterprise-friendly installer approaches
- Recommend preferred technology with rationale

### Findings

#### Technology Comparison

| Technology | Type | License | Complexity | PATH Support | Enterprise Ready | Notable Users |
|------------|------|---------|------------|--------------|------------------|---------------|
| **Inno Setup** | Setup.exe | Open Source (free for commercial) | Low | Excellent | Yes | VS Code, Git for Windows, Delphi |
| **WiX Toolset** | MSI | Open Source | High | Good | Excellent | Microsoft products |
| **NSIS** | Setup.exe | zlib/libpng (OSS) | Medium | Good | Yes | Many open source projects |
| **MSIX** | .msix/.appx | N/A (native) | Medium | Limited | Excellent | Modern Windows apps |

#### Inno Setup (Recommended)

**Overview:** Inno Setup is an open-source installation builder trusted since 1997. It creates compact, single-file setup executables with comprehensive Windows support.

**Key Features:**
- **Small Overhead:** Only 1.78 MB runtime overhead with all features
- **Single EXE:** Creates one self-extracting installer for easy distribution
- **Windows Coverage:** Supports Windows 7 through Windows 11, including ARM64
- **Administrative & Non-Admin Installs:** Supports both installation modes
- **Silent Installation:** Full `/SILENT` and `/VERYSILENT` command-line support
- **Uninstall Support:** Complete uninstaller generation
- **Digital Signing:** Built-in support for code signing
- **Pascal Scripting:** Powerful customization via integrated scripting engine
- **Dark Mode Support:** Modern UI with dark mode and custom styles

**Enterprise Considerations:**
- Generates standard Windows setup executables familiar to IT departments
- Supports silent deployment for enterprise rollout
- Well-documented and widely adopted
- Used by Microsoft Visual Studio Code — sets a strong precedent for developer tools

**PATH Environment Variable Support:**

Inno Setup provides excellent PATH modification capabilities through registry manipulation and built-in scripting:

```iss
[Setup]
ChangesEnvironment=yes

[Registry]
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
    ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; \
    Check: NeedsAddPath('{app}')

[Code]
function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then begin
    Result := True;
    exit;
  end;
  { Check if path already exists (case-insensitive) }
  Result := Pos(';' + UpperCase(Param) + ';', ';' + UpperCase(OrigPath) + ';') = 0;
end;
```

The `ChangesEnvironment=yes` directive automatically notifies running applications (including Windows Explorer) to reload environment variables without requiring a restart.

**Reusable PATH Management Module:**

A well-tested pattern includes separate functions for adding and removing paths:

```iss
[Code]
const EnvironmentKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment';

procedure EnvAddPath(Path: string);
var
    Paths: string;
begin
    if not RegQueryStringValue(HKEY_LOCAL_MACHINE, EnvironmentKey, 'Path', Paths)
    then Paths := '';

    if Pos(';' + Uppercase(Path) + ';', ';' + Uppercase(Paths) + ';') > 0 then exit;

    Paths := Paths + ';' + Path + ';';
    RegWriteStringValue(HKEY_LOCAL_MACHINE, EnvironmentKey, 'Path', Paths);
end;

procedure EnvRemovePath(Path: string);
var
    Paths: string;
    P: Integer;
begin
    if not RegQueryStringValue(HKEY_LOCAL_MACHINE, EnvironmentKey, 'Path', Paths) then exit;

    P := Pos(';' + Uppercase(Path) + ';', ';' + Uppercase(Paths) + ';');
    if P = 0 then exit;

    Delete(Paths, P - 1, Length(Path) + 1);
    RegWriteStringValue(HKEY_LOCAL_MACHINE, EnvironmentKey, 'Path', Paths);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
    if CurStep = ssPostInstall then EnvAddPath(ExpandConstant('{app}'));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
    if CurUninstallStep = usPostUninstall then EnvRemovePath(ExpandConstant('{app}'));
end;
```

#### WiX Toolset

**Overview:** WiX (Windows Installer XML) creates standard Windows Installer (.msi) packages. Available as an MSBuild SDK (`WixToolset.Sdk`) or .NET CLI tool.

**Key Features:**
- Creates true MSI packages (Windows Installer database)
- Deep integration with MSBuild and dotnet build
- Comprehensive Windows Installer features
- Visual Studio support via HeatWave Community Edition

**Considerations:**
- Steeper learning curve than Inno Setup
- More verbose XML-based authoring
- Better suited for complex enterprise deployments
- Requires more infrastructure setup

**Usage:**
```bash
# Install WiX CLI tool
dotnet tool install --global wix

# Build MSI
wix build -o output.msi installer.wxs
```

#### NSIS (Nullsoft Scriptable Install System)

**Overview:** NSIS is a script-based open-source installer system known for extremely small overhead and flexibility.

**Key Features:**
- Only 34 KB overhead (smallest of all options)
- Script-based with full programming capabilities
- Extensive plugin ecosystem
- LZMA compression support

**Considerations:**
- Custom scripting language (learning curve)
- Less modern UI compared to Inno Setup
- Active but smaller community than Inno Setup

#### MSIX

**Overview:** MSIX is Microsoft's modern app packaging format with container-based application isolation.

**Key Features:**
- 99.96% installation success rate
- Clean uninstall guaranteed
- File system and registry virtualization
- Differential updates

**Significant Limitations for CLI Tools:**
- **PATH Limitations:** MSIX apps run in isolation; adding to system PATH is problematic
- **App Execution Aliases:** Creates alias stubs in WindowsApps folder, not true PATH entries
- **Enterprise Distribution:** Requires either Microsoft Store, sideloading configuration, or MSIX App Attach
- **Signing Requirements:** All MSIX packages must be signed
- **Console App Complications:** CLI tools in MSIX containers have quirks with working directories and I/O

**Not Recommended for Hugh** because:
1. CLI tools need true PATH integration, not app execution aliases
2. Working directory operation is more complex with MSIX virtualization
3. Enterprise users expect traditional installers, not Store apps
4. Overhead of signing and distribution infrastructure

### Key Discoveries

- **Inno Setup is the optimal choice** for Hugh's distribution — it's used by VS Code (a strong precedent), provides excellent PATH management, and creates familiar enterprise-friendly installers
- **PATH modification requires `ChangesEnvironment=yes`** in Inno Setup to notify running applications without requiring restart
- **MSIX is not suitable** for CLI tools that need true system PATH integration due to its isolation model
- **WiX provides MSI packages** but has significantly higher complexity for simple CLI tool distribution
- **Silent installation support** is critical for enterprise deployment — all technologies support this
- **Uninstall must clean PATH** — Inno Setup's Pascal scripting makes this straightforward

### Relevant Files/Resources

- [Hugh/Hugh.csproj](Hugh/Hugh.csproj) — .NET 8.0 project file, target for self-contained packaging
- [docs/vision/003-hugh-global-cli-installation.md](docs/vision/003-hugh-global-cli-installation.md) — Vision document specifying installer requirements

### External Sources

- https://jrsoftware.org/isinfo.php — Inno Setup official site
- https://jrsoftware.org/ishelp/topic_setup_changesenvironment.htm — Inno Setup environment variable documentation
- https://docs.firegiant.com/wix/using-wix/ — WiX Toolset documentation
- https://nsis.sourceforge.io/Features — NSIS feature list
- https://learn.microsoft.com/en-us/windows/msix/overview — MSIX overview
- https://stackoverflow.com/questions/3304463/how-do-i-modify-the-path-environment-variable-when-running-an-inno-setup-install — PATH modification patterns

### Recommendation

**Use Inno Setup** for Hugh's Windows installer with the following configuration:

1. **Installation Location:** `C:\Program Files\Hugh` (default) or user-specified
2. **PATH Integration:** System PATH modification with environment notification
3. **Silent Support:** Full `/SILENT` and `/VERYSILENT` modes for enterprise deployment
4. **Uninstaller:** Complete removal including PATH cleanup
5. **Code Signing:** Sign both installer and uninstaller for enterprise trust

**Rationale:**
- Proven by VS Code and Git for Windows — familiar to target audience
- Excellent PATH management with no restart required
- Single setup.exe is easy to distribute via GitHub Releases
- Low complexity, well-documented, active community
- Supports both admin and non-admin installation scenarios

---

## Phase 2: Self-Contained .NET Packaging

**Status:** ✅ Complete
**Scope:**
- Research `dotnet publish` options for self-contained deployment
- Investigate single-file packaging with trimming
- Analyze resulting executable size and optimization options
- Document runtime identifiers (RID) for Windows targeting
- Evaluate ReadyToRun (R2R) compilation benefits

### Findings

#### Current Project Configuration

Hugh's `Hugh.csproj` is a straightforward .NET 8.0 console application:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <!-- Package references for logging, configuration, System.CommandLine, GitHub.Copilot.SDK -->
</Project>
```

**Key Dependencies Affecting Packaging:**
- `Serilog` + `Serilog.Settings.Configuration` — Uses reflection for configuration binding
- `Microsoft.Extensions.Configuration.*` — Uses reflection for `ConfigurationBinder.Bind()`
- `System.CommandLine` — Pre-release beta, generally trim-safe
- `GitHub.Copilot.SDK` — Contains `StreamJsonRpc` which uses reflection

#### Self-Contained Deployment Options

Self-contained deployment bundles the .NET runtime with the application, requiring no pre-installed .NET on the target machine.

| Publish Mode | Command Flag | Description |
|--------------|--------------|-------------|
| **Self-Contained** | `--self-contained` | Includes .NET runtime, produces multiple files |
| **Single-File** | `-p:PublishSingleFile=true` | Bundles all files into one executable |
| **Trimmed** | `-p:PublishTrimmed=true` | Removes unused code via IL Linker |
| **Compressed** | `-p:EnableCompressionInSingleFile=true` | Compresses embedded files |
| **ReadyToRun** | `-p:PublishReadyToRun=true` | Ahead-of-time (AOT) compilation |

#### Measured File Sizes for Hugh

Actual publish tests performed on Hugh project:

| Configuration | File Count | Size | Startup | Notes |
|---------------|------------|------|---------|-------|
| **Self-Contained (multi-file)** | 275 files | 76.75 MB | Fast | Baseline, all DLLs separate |
| **Single-File** | 3 files | 70.68 MB | Fast | `Hugh.exe` + `appsettings.json` + `.pdb` |
| **Single-File + Compressed** | 3 files | 35.92 MB | Slower first run | **Best size/compatibility balance** |
| **Single-File + Trimmed** | 3 files | 22.15 MB | Fast | ⚠️ 24 trim warnings |
| **Single-File + R2R** | 3 files | 99.11 MB | Fastest | Contains both IL and native code |

#### Trimming Analysis

Trimming reduces Hugh from 70.68 MB to 22.15 MB (69% reduction), but produces **24 trim warnings** indicating potential runtime failures:

**Hugh Source Code Warnings (7):**
```
DecisionLogger.cs: JsonSerializer.Deserialize<TValue> - RequiresUnreferencedCodeAttribute
LoopStateManager.cs: JsonSerializer.Deserialize/Serialize - RequiresUnreferencedCodeAttribute  
Program.cs: ConfigurationBinder.Bind - RequiresUnreferencedCodeAttribute
```

**Dependency Warnings (6 assemblies):**
- `Serilog.dll` — IL2104: Produced trim warnings
- `Serilog.Settings.Configuration.dll` — Uses reflection for sink configuration
- `Newtonsoft.Json.dll` — Part of GitHub.Copilot.SDK dependency chain
- `StreamJsonRpc.dll` — Uses runtime IL generation
- `Microsoft.VisualStudio.Threading.dll` — Part of SDK dependency chain
- `System.Data.Common` — DataSet/DataTable XML serialization

**Trimming Verdict:** ❌ **Not Recommended for Hugh**

The combination of:
1. Hugh's use of `System.Text.Json` for state persistence
2. `ConfigurationBinder` reflection-based binding
3. Serilog's reflection-based configuration
4. GitHub.Copilot.SDK's StreamJsonRpc dependency

Makes trimming risky without significant code changes. The SDK dependency is particularly problematic since we cannot modify it.

**If Trimming is Desired (Future Work):**
1. Add source generators for JSON serialization (`JsonSerializerContext`)
2. Replace `ConfigurationBinder.Bind()` with explicit property assignment
3. Consider `TrimmerRootAssembly` or `TrimmerRootDescriptor` for SDK assemblies
4. Add `<SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>` to catch new issues

#### ReadyToRun (R2R) Compilation

R2R pre-compiles assemblies to native code, reducing JIT time at startup.

**Pros:**
- Faster application startup (JIT for critical paths already done)
- Predictable startup performance

**Cons:**
- Larger file size (99 MB vs 70.68 MB for Hugh — 40% increase)
- Contains both IL and native code (IL kept for fallback/debugging)
- Longer publish time (27s vs 5s for single-file)

**R2R Verdict:** ⚠️ **Optional for Hugh**

Hugh is a CLI tool that runs for extended periods orchestrating AI agents. Startup time is not its primary optimization target. R2R adds 28 MB with minimal benefit.

#### Compression Analysis

Single-file compression (`EnableCompressionInSingleFile=true`) reduces Hugh from 70.68 MB to 35.92 MB (49% reduction).

**Trade-offs:**
- **Pro:** Significantly smaller download/installer size
- **Con:** First-run extraction to temp folder adds ~1-2 seconds
- **Con:** Antivirus may scan during extraction

**Compression Verdict:** ✅ **Recommended for Distribution**

The 35 MB size is acceptable for a CLI tool download. First-run delay is negligible for a tool that runs for minutes/hours.

#### Windows Runtime Identifiers (RIDs)

| RID | Target | Use Case |
|-----|--------|----------|
| `win-x64` | 64-bit Windows (Intel/AMD) | **Primary target** — vast majority of Windows machines |
| `win-x86` | 32-bit Windows | Legacy systems only, declining need |
| `win-arm64` | ARM64 Windows | Surface Pro X, Snapdragon laptops |
| `win10-x64` | Windows 10+ x64 | More specific, allows OS-specific optimizations |

**Recommended RID:** `win-x64`
- Covers Windows 7, 8, 10, and 11 on x64 processors
- Most developer machines run x64 Windows
- GitHub Copilot itself targets x64

**Consider Adding:** `win-arm64`
- Growing segment (Surface devices, new ARM laptops)
- Same source code, just different RID at publish time

#### Recommended Publish Configuration

**For Distribution (Recommended):**

```bash
dotnet publish -c Release -r win-x64 --self-contained \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o ./publish
```

**Resulting Files:**
- `Hugh.exe` — ~36 MB self-contained executable
- `appsettings.json` — Configuration file (must be distributed alongside)
- `Hugh.pdb` — Debug symbols (optional, exclude from installer)

**MSBuild Properties for .csproj (Alternative):**

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <DebugType>embedded</DebugType>
</PropertyGroup>
```

#### appsettings.json Handling

Hugh requires `appsettings.json` at runtime for configuration. Options:

1. **Distribute alongside EXE (Recommended):** Include in installer, place next to Hugh.exe
2. **Embed as resource:** Requires code changes to load from embedded resource
3. **Generate defaults at runtime:** Create if missing, with sensible defaults

The current `CopyToOutputDirectory` setting ensures the file is included in publish output:
```xml
<None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

#### Complete Publish Command Examples

**Standard Distribution Build:**
```bash
dotnet publish Hugh/Hugh.csproj -c Release -r win-x64 --self-contained \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -o ./artifacts/win-x64
```

**ARM64 Build (for Surface devices):**
```bash
dotnet publish Hugh/Hugh.csproj -c Release -r win-arm64 --self-contained \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -o ./artifacts/win-arm64
```

**Maximum Size Reduction (Risky — Trimming):**
```bash
dotnet publish Hugh/Hugh.csproj -c Release -r win-x64 --self-contained \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=partial \
    -o ./artifacts/win-x64-trimmed
```

**Fastest Startup (Larger Size):**
```bash
dotnet publish Hugh/Hugh.csproj -c Release -r win-x64 --self-contained \
    -p:PublishSingleFile=true \
    -p:PublishReadyToRun=true \
    -o ./artifacts/win-x64-r2r
```

### Key Discoveries

- **Compressed single-file is optimal for Hugh** — 36 MB is acceptable, no trim warnings, proven to work
- **Trimming is not viable** without significant code changes due to reflection usage in Hugh's code and dependencies (Serilog, ConfigurationBinder, GitHub.Copilot.SDK)
- **ReadyToRun provides minimal benefit** for a long-running CLI tool, adds 28 MB
- **appsettings.json must be distributed** alongside Hugh.exe — it's not embedded in single-file
- **24 trim warnings from 7 source files and 6 dependency assemblies** would require extensive work to resolve
- **win-x64 is the primary RID** — covers vast majority of target machines; win-arm64 is nice-to-have

### Relevant Files/Resources

- [Hugh/Hugh.csproj](Hugh/Hugh.csproj) — Current project configuration, no publish properties defined
- [Hugh/appsettings.json](Hugh/appsettings.json) — Runtime configuration, must be distributed with executable
- [Hugh/Logging/DecisionLogger.cs](Hugh/Logging/DecisionLogger.cs) — Uses JsonSerializer (trim warning source)
- [Hugh/Orchestration/LoopStateManager.cs](Hugh/Orchestration/LoopStateManager.cs) — Uses JsonSerializer (trim warning source)
- [Hugh/Program.cs](Hugh/Program.cs) — Uses ConfigurationBinder.Bind (trim warning source)

### External Sources

- https://learn.microsoft.com/en-us/dotnet/core/deploying/ — .NET deployment overview
- https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview — Single-file deployment
- https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options — Trimming options
- https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run — ReadyToRun compilation
- https://learn.microsoft.com/en-us/dotnet/core/rid-catalog — Runtime Identifier catalog

---

## Phase 3: Copilot SDK Credential Detection

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-05
**Scope:**
- Analyze how Hugh's existing CopilotService initializes SDK authentication
- Research `CopilotClientOptions.UseLoggedInUser` mechanism
- Investigate fallback credential approaches
- Determine if any changes needed for portable/global installation
- Review existing research in `009-github-copilot-sdk-integration.md`

### Findings

#### Hugh's Current SDK Initialization

Hugh's `CopilotService` initializes the Copilot SDK with **default options**, meaning zero explicit credential configuration:

```csharp
// From CopilotService.cs - EnsureClientStartedAsync()
private async Task EnsureClientStartedAsync(CancellationToken ct)
{
    if (_clientStarted) return;

    _logger.Debug("Initializing CopilotClient...");
    _client = new CopilotClient();  // ← Default options, no CopilotClientOptions
    await _client.StartAsync();
    _clientStarted = true;
}
```

This means Hugh relies entirely on the SDK's default credential detection behavior.

#### UseLoggedInUser Mechanism

The `CopilotClientOptions.UseLoggedInUser` property controls SDK authentication:

| Property | Default | Behavior |
|----------|---------|----------|
| `UseLoggedInUser` | `true` | Uses GitHub CLI's (`gh`) authenticated session |
| `GithubToken` | `null` | Alternative: explicit PAT or fine-grained token |
| `ProviderConfig` | `null` | Alternative: BYOK for custom LLM providers |

When `UseLoggedInUser = true` (the default):

1. **SDK spawns Copilot CLI** as a child process
2. **Copilot CLI checks for GitHub CLI auth** via `gh auth status`
3. **Credentials retrieved from** `~/.config/gh/hosts.yml` (or `%APPDATA%\GitHub CLI\hosts.yml` on Windows)
4. **Token forwarded** to Copilot service for LLM access

##### Credential Storage Locations

| Platform | GitHub CLI Config Path |
|----------|----------------------|
| Windows | `%APPDATA%\GitHub CLI\hosts.yml` |
| macOS | `~/.config/gh/hosts.yml` |
| Linux | `~/.config/gh/hosts.yml` |

The token is stored in the user's home directory, making it **location-independent** — credentials work from any directory on the machine.

#### Authentication Prerequisites

For Hugh to work with the SDK's default credential detection:

**Required:**
1. **GitHub CLI installed** — `gh` command available in PATH
2. **User authenticated** — `gh auth login` completed with Copilot scope
3. **Copilot subscription** — Active GitHub Copilot Individual, Business, or Enterprise

**Optional but Recommended:**
4. **Copilot CLI extension** — `gh extension install github/gh-copilot`

**Verification Commands:**
```powershell
# Check GitHub CLI installation
gh --version

# Check authentication status
gh auth status

# Check Copilot access (requires extension)
gh copilot --help
```

#### Fallback Credential Approaches

If `UseLoggedInUser` fails or isn't suitable, the SDK supports alternatives:

**Option 1: Explicit GitHub Token**
```csharp
var options = new CopilotClientOptions
{
    UseLoggedInUser = false,
    GithubToken = "ghp_xxxx..."  // Personal Access Token
};
await using var client = new CopilotClient(options);
```
**Use case:** CI/CD environments, service accounts

**Option 2: Environment Variable (Custom Implementation)**
```csharp
var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
var options = new CopilotClientOptions
{
    UseLoggedInUser = string.IsNullOrEmpty(token),
    GithubToken = token
};
```
**Use case:** Flexible deployment, user can override via environment

#### Impact on Global Installation

**Good news: No code changes required for global installation to work.**

##### Why It Works

1. **Credential storage is user-profile based** — Stored in `~/.config/gh/` or `%APPDATA%\GitHub CLI\`, not the working directory
2. **SDK uses default UseLoggedInUser=true** — Automatically finds credentials
3. **Copilot CLI discovery via PATH** — Works if `gh` is in system PATH

##### One-Time Setup Flow

```
User installs Hugh globally (%PROGRAMFILES%\Hugh or via PATH)
           ↓
User runs `gh auth login` (one-time, if not already done)
           ↓
User runs `hugh "Add feature X"` from any repo directory
           ↓
SDK detects credentials from user profile → Works seamlessly
```

### Key Discoveries

- Hugh uses `new CopilotClient()` with default options — zero credential configuration in code
- `UseLoggedInUser = true` is the SDK default, leveraging GitHub CLI's authenticated session
- Credentials are stored in user's home directory, making global installation location-independent
- **No code changes needed** for portable/global installation — credential detection is path-agnostic
- Prerequisites: GitHub CLI installed, `gh auth login` completed, active Copilot subscription
- Fallback options exist: explicit `GithubToken` or `ProviderConfig` for enterprise/CI scenarios

### Relevant Files/Resources

- [Hugh/Agents/CopilotService.cs](Hugh/Agents/CopilotService.cs) — SDK initialization with default CopilotClient()
- [Hugh/Configuration/HughConfig.cs](Hugh/Configuration/HughConfig.cs) — CopilotConfig with no credential settings
- [Hugh/PrerequisiteChecker.cs](Hugh/PrerequisiteChecker.cs) — Validates gh copilot CLI availability
- [docs/research/009-github-copilot-sdk-integration.md](docs/research/009-github-copilot-sdk-integration.md) — Comprehensive SDK documentation

---

## Phase 4: GitHub Releases Automation

**Status:** ✅ Complete
**Scope:**
- Research GitHub Actions workflows for Windows builds
- Investigate automated installer creation in CI/CD
- Document GitHub Releases API for publishing artifacts
- Identify existing workflows in comparable projects
- Recommend workflow structure

### Findings

#### Existing Workflow Analysis

The repository already contains one GitHub Actions workflow at `.github/workflows/deploy-presentation.yml` for deploying presentations to GitHub Pages. This provides a baseline for the repository's CI/CD patterns:
- Uses `workflow_dispatch` for manual triggering
- Runs on `ubuntu-latest`
- Uses modern action versions (v4)

However, Hugh's release workflow requires **Windows runners** for building the self-contained .NET executable and Inno Setup installer.

#### GitHub Actions for .NET Windows Builds

**Windows Runner Selection:**

| Runner | Best For | Notes |
|--------|----------|-------|
| `windows-latest` | Production builds | Currently Windows Server 2022, includes .NET 8 SDK |
| `windows-2022` | Specific Windows version | More predictable, recommended for reproducible builds |

**Key Actions Required:**

1. **actions/checkout@v4** — Clone repository
2. **actions/setup-dotnet@v4** — Install specific .NET SDK version (8.x for Hugh)
3. **dotnet publish** — Build self-contained executable
4. **Minionguyjpro/Inno-Setup-Action@v1.2.2** — Compile Inno Setup installer
5. **softprops/action-gh-release@v2** — Create GitHub Release and upload artifacts

#### Inno Setup Action for CI/CD

The `Minionguyjpro/Inno-Setup-Action` is the recommended GitHub Action for compiling Inno Setup scripts in CI/CD:

**Features:**
- Runs on `windows-latest` runners
- Supports all Inno Setup compiler options
- Used by 62+ projects on GitHub
- Simple configuration

**Usage:**
```yaml
- name: Compile Installer
  uses: Minionguyjpro/Inno-Setup-Action@v1.2.2
  with:
    path: installer/hugh-setup.iss
    options: /O+
```

**Path Handling Note:** On Windows runners, the workflow clones to `D:\a\<repo>\<repo>\`. The Inno Setup script's output directory should account for this.

#### GitHub Releases Publishing

**Recommended Action: `softprops/action-gh-release@v2`**

This is the most popular action for creating GitHub Releases with 5,400+ stars:

**Key Features:**
- Cross-platform (works on Windows runners)
- Upload multiple artifacts with glob patterns
- Auto-generates release notes from commits
- Creates draft releases for review
- Supports tag-based triggering

**Required Permissions:**
```yaml
permissions:
  contents: write
```

**Basic Release Example:**
```yaml
- name: Release
  uses: softprops/action-gh-release@v2
  if: github.ref_type == 'tag'
  with:
    files: |
      artifacts/Hugh-Setup-*.exe
      artifacts/Hugh-*.zip
    generate_release_notes: true
```

**Path Handling for Windows:**
> ⚠️ For Windows builds: Paths must use `/` as separator, not `\`. Use `$Path = $Path -replace '\\','/'` in PowerShell if needed.

#### GitHub Releases API (Alternative to Action)

For direct API integration (e.g., from scripts), the REST API provides:

**Create Release:**
```bash
curl -L \
  -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  https://api.github.com/repos/OWNER/REPO/releases \
  -d '{"tag_name":"v1.0.0","name":"v1.0.0","body":"Release notes","draft":false}'
```

**Upload Asset:**
```bash
curl -L \
  -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  -H "Content-Type: application/octet-stream" \
  "https://uploads.github.com/repos/OWNER/REPO/releases/{release_id}/assets?name=Hugh-Setup.exe" \
  --data-binary @Hugh-Setup.exe
```

**API Features:**
- `generate_release_notes: true` — Auto-generates changelog from commits
- `make_latest: true` — Sets release as latest (default for non-prerelease)
- `prerelease: true` — Marks as prerelease
- `draft: true` — Creates unpublished draft

**Permissions Required:**
- "Contents" repository permissions (write)
- GITHUB_TOKEN works for same-repository releases

#### Versioning and Artifact Naming Conventions

**Recommended Versioning Scheme:**

| Format | Example | When to Use |
|--------|---------|-------------|
| `v{major}.{minor}.{patch}` | `v1.0.0` | Stable releases |
| `v{major}.{minor}.{patch}-beta.{n}` | `v1.0.0-beta.1` | Pre-release testing |
| `v{major}.{minor}.{patch}-rc.{n}` | `v1.0.0-rc.1` | Release candidates |

**Version Source Options:**

1. **Git Tags (Recommended):** Trigger on `v*` tag push, extract version from `github.ref_name`
2. **Project File:** Read from `Hugh.csproj` `<Version>` property
3. **Environment Variable:** Set in workflow

**Recommended Artifact Naming:**

| Artifact Type | Naming Pattern | Example |
|---------------|----------------|---------|
| Windows Installer | `Hugh-Setup-{version}.exe` | `Hugh-Setup-1.0.0.exe` |
| Portable ZIP (x64) | `Hugh-{version}-win-x64.zip` | `Hugh-1.0.0-win-x64.zip` |
| Portable ZIP (ARM64) | `Hugh-{version}-win-arm64.zip` | `Hugh-1.0.0-win-arm64.zip` |
| Debug Symbols | `Hugh-{version}-symbols.zip` | `Hugh-1.0.0-symbols.zip` |

**Version Extraction in Workflow:**
```yaml
- name: Get version from tag
  id: version
  run: |
    $version = "${{ github.ref_name }}" -replace '^v', ''
    echo "VERSION=$version" >> $env:GITHUB_OUTPUT
  shell: pwsh
```

#### Recommended Workflow Structure for Hugh

**Trigger Strategy:**

```yaml
on:
  push:
    tags:
      - 'v*.*.*'  # Trigger on semantic version tags
  workflow_dispatch:  # Allow manual trigger for testing
    inputs:
      version:
        description: 'Version to build (without v prefix)'
        required: true
```

**Complete Recommended Workflow:**

```yaml
name: Build and Release Hugh

on:
  push:
    tags:
      - 'v*.*.*'
  workflow_dispatch:
    inputs:
      version:
        description: 'Version (e.g., 1.0.0)'
        required: true

permissions:
  contents: write

jobs:
  build:
    runs-on: windows-2022
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.x'
      
      - name: Get version
        id: version
        run: |
          if ("${{ github.event_name }}" -eq "workflow_dispatch") {
            $version = "${{ github.event.inputs.version }}"
          } else {
            $version = "${{ github.ref_name }}" -replace '^v', ''
          }
          echo "VERSION=$version" >> $env:GITHUB_OUTPUT
          echo "Building version: $version"
        shell: pwsh
      
      - name: Run tests
        run: dotnet test Hugh.Tests/Hugh.Tests.csproj --configuration Release
      
      - name: Publish x64
        run: |
          dotnet publish Hugh/Hugh.csproj -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=true `
            -p:EnableCompressionInSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -o ./publish/win-x64
        shell: pwsh
      
      - name: Publish ARM64
        run: |
          dotnet publish Hugh/Hugh.csproj -c Release -r win-arm64 --self-contained `
            -p:PublishSingleFile=true `
            -p:EnableCompressionInSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -o ./publish/win-arm64
        shell: pwsh
      
      - name: Create portable ZIP (x64)
        run: |
          Compress-Archive -Path ./publish/win-x64/* -DestinationPath ./artifacts/Hugh-${{ steps.version.outputs.VERSION }}-win-x64.zip
        shell: pwsh
      
      - name: Create portable ZIP (ARM64)
        run: |
          Compress-Archive -Path ./publish/win-arm64/* -DestinationPath ./artifacts/Hugh-${{ steps.version.outputs.VERSION }}-win-arm64.zip
        shell: pwsh
      
      - name: Build Inno Setup Installer
        uses: Minionguyjpro/Inno-Setup-Action@v1.2.2
        with:
          path: installer/hugh-setup.iss
          options: /DMyAppVersion=${{ steps.version.outputs.VERSION }}
      
      - name: Move installer to artifacts
        run: |
          Move-Item ./installer/Output/Hugh-Setup-*.exe ./artifacts/
        shell: pwsh
      
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: hugh-release-${{ steps.version.outputs.VERSION }}
          path: ./artifacts/*
          retention-days: 30

  release:
    needs: build
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && startsWith(github.ref, 'refs/tags/')
    
    steps:
      - name: Download artifacts
        uses: actions/download-artifact@v4
        with:
          name: hugh-release-${{ needs.build.outputs.version }}
          path: ./artifacts
      
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: |
            artifacts/Hugh-Setup-*.exe
            artifacts/Hugh-*-win-x64.zip
            artifacts/Hugh-*-win-arm64.zip
          generate_release_notes: true
          draft: false
          prerelease: ${{ contains(github.ref_name, '-beta') || contains(github.ref_name, '-rc') }}
```

#### Inno Setup Script Integration

The Inno Setup script should accept the version as a compiler define:

```iss
; At the top of hugh-setup.iss
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

[Setup]
AppVersion={#MyAppVersion}
OutputBaseFilename=Hugh-Setup-{#MyAppVersion}
; ... rest of script
```

This allows the CI workflow to pass the version dynamically:
```yaml
options: /DMyAppVersion=${{ steps.version.outputs.VERSION }}
```

#### Workflow Stages Summary

```
┌────────────────┐     ┌────────────────┐     ┌────────────────┐
│  Tag Push      │────>│  Build Job     │────>│  Release Job   │
│  v1.0.0        │     │  windows-2022  │     │  ubuntu-latest │
└────────────────┘     └────────────────┘     └────────────────┘
                              │                       │
                              ▼                       ▼
                       ┌────────────────┐     ┌────────────────┐
                       │ • Run tests    │     │ • Download     │
                       │ • Publish x64  │     │   artifacts    │
                       │ • Publish ARM  │     │ • Create       │
                       │ • Create ZIPs  │     │   release      │
                       │ • Build Inno   │     │ • Upload       │
                       │ • Upload       │     │   assets       │
                       │   artifacts    │     │                │
                       └────────────────┘     └────────────────┘
```

#### Alternative: Draft-Then-Publish Pattern

For extra review before publishing:

```yaml
- name: Create Draft Release
  uses: softprops/action-gh-release@v2
  with:
    files: artifacts/*
    draft: true  # Creates unpublished release
    generate_release_notes: true
```

Then manually publish via GitHub UI after reviewing artifacts.

#### Workflow Secrets and Environment

**Required Secrets:**
- None for same-repository releases — `GITHUB_TOKEN` is automatic

**Optional Secrets (for code signing):**
- `SIGNING_CERTIFICATE` — Base64-encoded .pfx
- `SIGNING_PASSWORD` — Certificate password

**Code Signing Integration (Future Enhancement):**
```yaml
- name: Sign executable
  run: |
    $cert = [System.Convert]::FromBase64String("${{ secrets.SIGNING_CERTIFICATE }}")
    [System.IO.File]::WriteAllBytes("cert.pfx", $cert)
    & signtool sign /f cert.pfx /p "${{ secrets.SIGNING_PASSWORD }}" /tr http://timestamp.digicert.com /td sha256 ./publish/win-x64/Hugh.exe
    Remove-Item cert.pfx
  shell: pwsh
```

### Key Discoveries

- **`softprops/action-gh-release@v2`** is the recommended action for GitHub Releases — 5,400+ stars, cross-platform, supports Windows runners
- **`Minionguyjpro/Inno-Setup-Action@v1.2.2`** provides Inno Setup compilation in GitHub Actions — simple integration, supports all ISCC options
- **Windows path handling requires `/` separators** in glob patterns for action inputs
- **Tag-based triggering (`v*.*.*`)** is the standard pattern for release workflows
- **Version can be passed to Inno Setup** via `/DMyAppVersion=` compiler define
- **Two-job pattern (build → release)** separates Windows-specific build from release publishing
- **`generate_release_notes: true`** auto-creates changelogs from commit history
- **`GITHUB_TOKEN`** provides sufficient permissions for same-repository releases

### Relevant Files/Resources

- [.github/workflows/deploy-presentation.yml](.github/workflows/deploy-presentation.yml) — Existing workflow demonstrating CI patterns
- [Hugh/Hugh.csproj](Hugh/Hugh.csproj) — Project file to target for `dotnet publish`
- [Hugh.Tests/Hugh.Tests.csproj](Hugh.Tests/Hugh.Tests.csproj) — Test project to run before release

### External Sources

- https://github.com/marketplace/actions/inno-setup-action — Inno Setup Action marketplace page
- https://github.com/softprops/action-gh-release — softprops/action-gh-release repository
- https://docs.github.com/en/rest/releases/releases — GitHub Releases REST API documentation
- https://docs.github.com/en/actions/use-cases-and-examples/building-and-testing/building-and-testing-net — GitHub Actions .NET guide
- https://docs.github.com/en/actions/security-guides/automatic-token-authentication — GITHUB_TOKEN permissions

---

## Phase 5: Hugh CWD Architecture Adaptation

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-05
**Scope:**
- Analyze Hugh's current directory assumptions
- Review configuration file loading patterns
- Investigate agent prompt file discovery
- Determine necessary changes for any-folder operation
- Document impact on existing `DocumentManager`, `AgentPromptLoader`

### Findings

#### Current Architecture Analysis

##### Configuration Loading (Program.cs)

Hugh's configuration loading is the primary CWD-dependent component:

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())  // ⚠️ CWD assumption
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();
```

This means `appsettings.json` must exist in whatever directory Hugh runs from. For global CLI operation, this needs to resolve to Hugh's installation directory instead.

##### Serilog File Logging

```csharp
.WriteTo.File(
    path: "logs/hugh-.log",  // ⚠️ Relative to CWD
    rollingInterval: RollingInterval.Day, ...)
```

Log files are written relative to CWD. For global CLI, logs should go to a dedicated location (e.g., `~/.hugh/logs/` or installation directory).

##### HughConfig Path Properties

All paths in `HughConfig` default to relative paths:

```csharp
public string AgentsPath { get; set; } = "./agents";
public string DocsPath { get; set; } = "./docs";
public string OrchestratorPath { get; set; } = "./.orchestrator";
```

These are interpreted relative to CWD via `appsettings.json`.

##### Workspace Root Pattern

Multiple components receive `workspaceRoot` as a constructor parameter:

| Component | Workspace Usage |
|-----------|-----------------|
| `DocumentManager` | All document path resolution via `_workspaceRoot` |
| `LoopStateManager` | State file at `{workspaceRoot}/.orchestrator/` |
| `DecisionLogger` | Decisions at `{workspaceRoot}/.orchestrator/decisions.jsonl` |

In `Program.cs`:
```csharp
var workspaceRoot = Directory.GetCurrentDirectory();
var stateManager = new LoopStateManager(workspaceRoot, Log.Logger);
var decisionLogger = new DecisionLogger(workspaceRoot, Log.Logger);
```

This is **intentionally CWD-dependent** — the workspace is meant to be the user's project. This is correct behavior.

##### AgentPromptLoader Path Resolution

```csharp
var fullPath = Path.IsPathRooted(promptPath)
    ? promptPath
    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), promptPath));

// Also try resolving relative to configured agents path
if (!File.Exists(fullPath))
{
    var agentFileName = Path.GetFileName(promptPath);
    var alternativePath = Path.GetFullPath(Path.Combine(_agentsPath, agentFileName));
    if (File.Exists(alternativePath)) { fullPath = alternativePath; }
}
```

The loader tries:
1. CWD + relative path
2. Configured `AgentsPath` + filename

**Problem:** If `AgentsPath` is relative (e.g., `./agents`), it resolves from CWD, not from Hugh's installation location.

##### AgentRegistry Hardcoded Paths

```csharp
LoopPhase.Vision => new AgentDefinition
{
    Name = "pch-visionary",
    PromptPath = "agents/pch-visionary.md",  // ⚠️ Hardcoded relative path
    ...
}
```

All agent definitions use hardcoded relative paths. These must be made configurable or resolved against Hugh's installation directory.

#### Impact Summary

| Component | CWD Dependency | Required Change |
|-----------|---------------|------------------|
| Configuration loading | High | Resolve `appsettings.json` from assembly location |
| Serilog logs | Medium | Configurable log path, default to user home or assembly dir |
| AgentsPath | High | Must resolve from Hugh installation, not CWD |
| DocumentManager | Low (intentional) | CWD = workspace is correct behavior |
| LoopStateManager | Low (intentional) | State stored in user's workspace — correct |
| DecisionLogger | Low (intentional) | Decisions stored in user's workspace — correct |
| AgentRegistry | High | Paths should use configurable base or assembly location |
| AgentPromptLoader | High | Primary path resolution needs config awareness |

#### Recommended Design

##### 1. Configuration Discovery Order

```
1. Assembly location (for installed global CLI)
2. Environment variable HUGH_CONFIG_PATH
3. Current working directory (for dev scenarios)
```

```csharp
private static string GetConfigBasePath()
{
    // Check environment override first
    var envPath = Environment.GetEnvironmentVariable("HUGH_CONFIG_PATH");
    if (!string.IsNullOrEmpty(envPath) && File.Exists(Path.Combine(envPath, "appsettings.json")))
        return envPath;
    
    // Try assembly location (global CLI install)
    var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    if (assemblyPath != null && File.Exists(Path.Combine(assemblyPath, "appsettings.json")))
        return assemblyPath;
    
    // Fall back to CWD (dev mode)
    return Directory.GetCurrentDirectory();
}
```

##### 2. AgentsPath Resolution

```csharp
public class HughConfig
{
    // Computed property that resolves relative paths against install location
    public string ResolvedAgentsPath => ResolveRelativePath(AgentsPath);
    
    private static string ResolveRelativePath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        var basePath = GetInstallationPath();
        return Path.GetFullPath(Path.Combine(basePath, path));
    }
}
```

##### 3. Separate Concerns

| Concern | Location Source |
|---------|-----------------|
| Hugh installation files (appsettings.json, agents/) | Assembly directory or configured path |
| User workspace (docs/, .orchestrator/) | CWD (user's project) |
| Logging | Configurable, default to `~/.hugh/logs/` |

##### 4. AgentRegistry Refactor

Make `AgentRegistry` instance-based with configurable base path:

```csharp
public class AgentRegistry
{
    private readonly string _agentsBasePath;
    
    public AgentRegistry(string agentsBasePath)
    {
        _agentsBasePath = agentsBasePath;
    }
    
    public AgentDefinition GetAgent(LoopPhase phase) => phase switch
    {
        LoopPhase.Vision => new AgentDefinition
        {
            PromptPath = Path.Combine(_agentsBasePath, "pch-visionary.md"),
            ...
        },
        ...
    };
}
```

### Key Discoveries

- Configuration loading uses `Directory.GetCurrentDirectory()` for `appsettings.json` — incompatible with global CLI
- Agent prompt paths in `AgentRegistry` are hardcoded as relative paths
- `AgentPromptLoader` falls back to CWD when resolving relative paths
- Workspace-related paths (docs, .orchestrator) **should** remain CWD-relative — this is intentional
- Serilog writes logs to CWD-relative `logs/` directory
- Six files contain `Directory.GetCurrentDirectory()` calls that need review
- `HughConfig` uses relative path defaults that resolve incorrectly when run globally

### Relevant Files/Resources

- [Hugh/Program.cs](Hugh/Program.cs) — Configuration builder sets base path to CWD; 5 GetCurrentDirectory() calls
- [Hugh/Configuration/HughConfig.cs](Hugh/Configuration/HughConfig.cs) — Relative path defaults
- [Hugh/Agents/AgentPromptLoader.cs](Hugh/Agents/AgentPromptLoader.cs) — Resolves prompt paths with CWD fallback
- [Hugh/Agents/AgentRegistry.cs](Hugh/Agents/AgentRegistry.cs) — Hardcoded relative paths for agents
- [Hugh/Documents/DocumentManager.cs](Hugh/Documents/DocumentManager.cs) — Uses workspaceRoot parameter, correct design
- [Hugh/Orchestration/LoopStateManager.cs](Hugh/Orchestration/LoopStateManager.cs) — Uses workspaceRoot, correct design

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|

*Standards will be queried during phase execution.*

---

## Conclusions

### Answers to Key Questions

1. **What Windows installer technology is best suited for self-contained .NET applications?**
   → **Inno Setup** — proven by VS Code, excellent PATH management, low complexity, enterprise-ready

2. **How can Hugh leverage the Copilot CLI SDK's credential detection for zero-configuration operation?**
   → **No changes needed** — default `UseLoggedInUser=true` automatically finds GitHub CLI credentials from user profile

3. **How should Hugh be packaged as a self-contained executable for distribution?**
   → **Single-file publish without trimming** — `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true` producing ~36 MB executable

4. **What GitHub Actions workflow can automate Windows installer builds and GitHub Releases publishing?**
   → **Two-job workflow** — Windows runner for .NET build + Inno Setup compilation, then `softprops/action-gh-release@v2` for publishing

5. **How should Hugh's current architecture be adapted to support current-working-directory operation?**
   → **Separate installation files from workspace** — `appsettings.json` and `agents/` resolve from assembly location; `docs/` and `.orchestrator/` remain CWD-relative for user workspace

### Implementation Priority

| Priority | Item | Complexity |
|----------|------|------------|
| P0 | Update `Program.cs` config loading to use assembly location | Low |
| P0 | Refactor `AgentRegistry` with configurable base path | Low |
| P1 | Create `installer/hugh-setup.iss` Inno Setup script | Medium |
| P1 | Add `.github/workflows/release.yml` | Medium |
| P2 | Enhance `PrerequisiteChecker` for auth status validation | Low |
| P2 | Move logs to `~/.hugh/logs/` | Low |

### Risk Assessment

| Risk | Mitigation |
|------|------------|
| Single-file `Assembly.Location` may behave differently | Test during implementation; fallback to `AppContext.BaseDirectory` |
| 24 trim warnings if trimming attempted in future | Document as known limitation; trimming requires significant refactoring |
| Code signing for enterprise trust | Document as future enhancement; installers work without signing |

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** 2026-02-05
**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent Orchestration)
**Document Location:** `/docs/research/010-hugh-global-cli-installation.md`

### Next Steps

- Review the Overview section for synthesized findings
- Hand off to `@pch-planner` to create an implementation plan based on these findings
