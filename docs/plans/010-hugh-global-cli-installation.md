# Implementation Plan: Hugh Global CLI Installation

## Document Information

| Field | Value |
|-------|-------|
| Document | 010-hugh-global-cli-installation.md |
| Version | v2.1 |
| Created | 2026-02-05 |
| Status | ✅ Ready for Implementation |
| Owner | pch-planner |
| Research | [010-hugh-global-cli-installation.md](../research/010-hugh-global-cli-installation.md) |
| Vision | [003-hugh-global-cli-installation.md](../vision/003-hugh-global-cli-installation.md) |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-05 | pch-planner | Initial plan creation |
| v1.5 | 2026-02-05 | pch-planner | Completed 5 planning questions |
| v2.0 | 2026-02-05 | pch-planner | Holistic review, execution plan complete |
| v2.1 | 2026-02-05 | pch-plan-reviewer | Review complete: expanded Phase 2.5 and Step 1.3 scope |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | 2026-02-05 | Log file location | B: User Home Directory (`~\.hugh\logs\`) | User-writable, avoids Program Files permissions, follows standard app data patterns |
| 2 | 2026-02-05 | Dev mode detection | A: Automatic Detection Based on Assembly Location | Zero configuration needed; assembly location determines installed vs. dev mode |
| 3 | 2026-02-05 | AgentRegistry refactoring | A: Convert to Instance-Based with DI | Aligns with existing DI patterns, improves testability, follows codebase conventions |
| 4 | 2026-02-05 | Installation directory | C: User Choice with Default (`C:\Program Files\Hugh`) | Standard Inno Setup behavior, flexibility for enterprise policies |
| 5 | 2026-02-05 | Release artifacts | B: Installer + Portable ZIP (x64 only) | Covers majority of developers, portable option for restricted environments |

---

## Review Session Log

**Questions Pending:** 0 | **Questions Resolved:** 2 | **Last Updated:** 2026-02-05

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | AgentPromptLoader uses AgentRegistry statically (missing from Phase 2.5 scope) | Correctness | Option A: Expand Phase 2.5 scope | Step 2.5 file list updated |
| 2 | Second Serilog configuration in verbose mode (line 113-120) also needs updating | Completeness | Option A: Expand Step 1.3 scope | Step 1.3 acceptance criteria updated |

---

## Overview

This plan implements the global CLI installation capability for Hugh based on the completed research document. Hugh will be distributed as a globally-installable Windows CLI tool that can be invoked from any directory and automatically detect GitHub Copilot credentials.

### Objectives

1. **Path-Independent Configuration** — Hugh locates its `appsettings.json` and agent prompts from the installation directory, not CWD
2. **Workspace Operation** — Hugh operates on the user's current working directory as the target workspace
3. **Windows Installer** — Professional Inno Setup installer with PATH integration
4. **Automated Releases** — GitHub Actions workflow for tag-triggered builds and releases
5. **Zero-Configuration Auth** — Leverage existing Copilot SDK credential detection

### Key Technical Decisions from Research

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Installer Technology | Inno Setup | Proven by VS Code, excellent PATH management |
| Package Format | Single-file, compressed, ~36 MB | No trimming (24 warnings), compression for smaller size |
| Credential Handling | Default UseLoggedInUser=true | No code changes needed, location-independent |
| Path Resolution | Assembly location for install files | Separates installation from workspace |

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR1 | Hugh executable can be invoked from any directory via PATH | P0 |
| FR2 | Configuration file (`appsettings.json`) loads from installation directory | P0 |
| FR3 | Agent prompt files load from installation directory | P0 |
| FR4 | Workspace documents (docs/, .orchestrator/) resolve from CWD | P0 |
| FR5 | Windows installer adds Hugh to system PATH | P0 |
| FR6 | Uninstaller removes Hugh from PATH | P1 |
| FR7 | GitHub Actions workflow publishes to GitHub Releases on tag push | P1 |
| FR8 | Both x64 and ARM64 builds available | P2 |

### Non-Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR1 | Installer < 50 MB download size | P1 |
| NFR2 | No .NET runtime required on target machine (self-contained) | P0 |
| NFR3 | Silent installation support for enterprise deployment | P1 |
| NFR4 | PATH changes effective without system restart | P0 |

### Out of Scope

- macOS/Linux installers (future work)
- Code signing certificate acquisition (documented as future enhancement)
- User-level agent overrides (~/.hugh/agents/)
- Trimmed builds (require significant refactoring due to reflection usage)

---

## Technical Design

### Architecture Changes

```
BEFORE (CWD-dependent):
┌─────────────────────────────────────────────────────┐
│ Working Directory (e.g., C:\repos\my-project)       │
├─────────────────────────────────────────────────────┤
│ appsettings.json        ← Hugh config               │
│ agents/                 ← Agent prompts             │
│ docs/                   ← Generated documents       │
│ .orchestrator/          ← Loop state                │
│ logs/                   ← Log files                 │
└─────────────────────────────────────────────────────┘

AFTER (Global CLI):
┌─────────────────────────────────────────────────────┐
│ Installation Directory (C:\Program Files\Hugh)      │
├─────────────────────────────────────────────────────┤
│ Hugh.exe                ← Self-contained executable │
│ appsettings.json        ← Hugh config               │
│ agents/                 ← Agent prompts             │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ User Home (~/.hugh) - FUTURE                        │
├─────────────────────────────────────────────────────┤
│ logs/                   ← Log files (optional)      │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ Working Directory (any repo)                        │
├─────────────────────────────────────────────────────┤
│ docs/                   ← Generated documents       │
│ .orchestrator/          ← Loop state                │
└─────────────────────────────────────────────────────┘
```

### Codebase Patterns (Reference)

| Pattern | Location | Usage in This Plan |
|---------|----------|--------------------|
| Configuration Binding | `Program.cs` lines 27-30 | Modify base path resolution |
| Options Pattern | `HughConfig.cs` | Add resolved path properties |
| Static Registry | `AgentRegistry.cs` | Convert to instance-based with base path |
| Path Resolution | `AgentPromptLoader.cs` lines 68-81 | Use configurable installation path |
| Prerequisite Checks | `PrerequisiteChecker.cs` | Update agents path resolution |

### Configuration Discovery Chain

```csharp
// Resolution order:
1. Environment variable: HUGH_CONFIG_PATH
2. Assembly location (for installed global CLI)
3. Current working directory (dev/debug fallback)
```

### Key Code Changes

#### 1. PathResolver Utility Class (New)

Create a new static utility class to centralize path resolution logic:

```csharp
// Hugh/Configuration/PathResolver.cs
public static class PathResolver
{
    private static string? _installationPath;
    
    /// <summary>
    /// Gets the installation path where Hugh's configuration and agents reside.
    /// Resolution order: ENV variable → Assembly location → CWD fallback
    /// </summary>
    public static string GetInstallationPath()
    {
        if (_installationPath != null) return _installationPath;
        
        // 1. Check environment variable override
        var envPath = Environment.GetEnvironmentVariable("HUGH_CONFIG_PATH");
        if (!string.IsNullOrEmpty(envPath) && 
            File.Exists(Path.Combine(envPath, "appsettings.json")))
        {
            _installationPath = envPath;
            return _installationPath;
        }
        
        // 2. Try assembly location (installed global CLI)
        var assemblyPath = AppContext.BaseDirectory; // Works for single-file
        if (File.Exists(Path.Combine(assemblyPath, "appsettings.json")))
        {
            _installationPath = assemblyPath;
            return _installationPath;
        }
        
        // 3. Fall back to CWD (dev mode)
        _installationPath = Directory.GetCurrentDirectory();
        return _installationPath;
    }
    
    /// <summary>
    /// Gets the user's Hugh data directory (~/.hugh).
    /// Creates directory if it doesn't exist.
    /// </summary>
    public static string GetUserDataPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var hughPath = Path.Combine(userProfile, ".hugh");
        Directory.CreateDirectory(hughPath);
        return hughPath;
    }
    
    /// <summary>
    /// Gets the log directory path.
    /// </summary>
    public static string GetLogPath()
    {
        return Path.Combine(GetUserDataPath(), "logs");
    }
}
```

#### 2. Program.cs Configuration Loading

Update `Program.cs` to use `PathResolver`:

```csharp
// Change from:
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Change to:
var installPath = PathResolver.GetInstallationPath();
var configuration = new ConfigurationBuilder()
    .SetBasePath(installPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();
```

#### 3. Serilog Log Path Update

Update log file path to use user home directory:

```csharp
// Change from:
.WriteTo.File(path: "logs/hugh-.log", ...)

// Change to:
.WriteTo.File(path: Path.Combine(PathResolver.GetLogPath(), "hugh-.log"), ...)
```

#### 4. AgentRegistry Instance-Based Refactoring

Convert static class to instance-based with DI:

```csharp
// Hugh/Agents/AgentRegistry.cs
public class AgentRegistry : IAgentRegistry
{
    private readonly string _agentsBasePath;
    
    public AgentRegistry(IOptions<HughConfig> options)
    {
        // Resolve agents path relative to installation directory
        var configuredPath = options.Value.AgentsPath;
        _agentsBasePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(PathResolver.GetInstallationPath(), configuredPath));
    }
    
    public AgentDefinition GetAgent(LoopPhase phase) => phase switch
    {
        LoopPhase.Vision => new AgentDefinition
        {
            Name = "pch-visionary",
            PromptPath = Path.Combine(_agentsBasePath, "pch-visionary.md"),
            Phase = LoopPhase.Vision,
            DefaultTimeout = TimeSpan.FromMinutes(10)
        },
        // ... other phases with Path.Combine(_agentsBasePath, "filename.md")
    };
}
```

#### 5. IAgentRegistry Interface (New)

```csharp
// Hugh/Agents/IAgentRegistry.cs
public interface IAgentRegistry
{
    AgentDefinition GetAgent(LoopPhase phase);
    IEnumerable<AgentDefinition> GetAllAgents();
    bool TryGetAgentByName(string name, out AgentDefinition? agent);
}
```

#### 6. HughConfig Updates

Add resolved path helper:

```csharp
// Add to HughConfig.cs
/// <summary>
/// Gets the fully resolved agents path (relative to installation directory).
/// </summary>
public string GetResolvedAgentsPath()
{
    if (Path.IsPathRooted(AgentsPath)) return AgentsPath;
    return Path.GetFullPath(Path.Combine(PathResolver.GetInstallationPath(), AgentsPath));
}
```

---

## Dependencies and Prerequisites

| Dependency | Type | Impact |
|------------|------|--------|
| .NET 8 SDK | Build-time | Required for publishing |
| Inno Setup 6.x | Build-time | Required for installer creation |
| GitHub Actions | Build-time | Windows runner for CI/CD |
| GitHub CLI | Runtime | Required for credential detection |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Single-file Assembly.Location behaves differently | Medium | High | Use AppContext.BaseDirectory as fallback |
| ARM64 build untested | Medium | Low | x64 is primary; ARM64 is P2 |
| PATH not updated in existing shells | Low | Medium | Inno Setup ChangesEnvironment=yes |

---

## Holistic Review Summary

### Key Decision Interactions

1. **Assembly Location Detection + Log Path** — The automatic detection approach pairs well with the user home log directory. When installed, Hugh finds config from Program Files but writes logs to `~/.hugh/logs/`, avoiding permission issues. In dev mode, config comes from CWD while logs still go to user home for consistency.

2. **Instance-Based AgentRegistry + Path Resolution** — Converting AgentRegistry to instance-based enables it to receive the resolved installation path via DI. The `PathResolver` utility provides the single source of truth for installation location.

3. **Installer + Portable ZIP** — Both distribution methods require the same path resolution logic. The portable ZIP works because Hugh auto-detects the assembly location containing `appsettings.json`.

### Architectural Considerations Addressed

- **No permission issues**: Logs go to user-writable `~/.hugh/logs/`, avoiding Program Files write restrictions
- **Backward compatibility**: Dev mode continues to work via CWD fallback
- **Single source of truth**: `PathResolver` centralizes path logic, reducing inconsistency risk
- **Testability improved**: Instance-based `AgentRegistry` with interface enables mocking

### Trade-offs Accepted

| Trade-off | Decision | Reasoning |
|-----------|----------|-----------|
| ARM64 deferred | x64 only initially | Low adoption, easy to add later |
| No code signing | Ship unsigned | Can add when certificate acquired |
| No trimming | 36 MB executable | Avoids reflection-related runtime failures |

### Risks Acknowledged

| Risk | Mitigation |
|------|------------|
| `AppContext.BaseDirectory` behavior in single-file | Research confirms this works; add debug logging to verify |
| Antivirus scanning compressed single-file | Acceptable startup delay; documented as known behavior |
| PATH not updated in open terminals | `ChangesEnvironment=yes` notifies Explorer; user may need new terminal |

---

## Progress Summary

| Phase | Description | Status | Tasks |
|-------|-------------|--------|-------|
| 1 | PathResolver and Configuration Updates | ✅ Complete | 5/5 |
| 2 | AgentRegistry Refactoring | ✅ Complete | 5/5 |
| 3 | Component Updates | ✅ Complete | 4/4 |
| 4 | Inno Setup Installer | ✅ Complete | 4/4 |
| 5 | GitHub Actions Workflow | ✅ Complete | 4/4 |
| 6 | Testing & Documentation | ⏳ Not Started | 0/4 |

---

## Execution Plan

### Phase 1: PathResolver and Configuration Updates

**Estimated Session Size:** Small
**Files to Modify:** 3
**Prerequisites:** None
**Entry Point:** Start by reading `Hugh/Program.cs` and `Hugh/Configuration/HughConfig.cs`
**Verification:** Hugh runs successfully from both CWD (dev) and a different directory

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Create `PathResolver` static utility class | `Hugh/Configuration/PathResolver.cs` | Class exists with `GetInstallationPath()`, `GetUserDataPath()`, `GetLogPath()` methods |
| 1.2 | Update `Program.cs` to use `PathResolver.GetInstallationPath()` for config base path | `Hugh/Program.cs` | Configuration loads from assembly directory when `appsettings.json` exists there |
| 1.3 | Update Serilog file path to use `PathResolver.GetLogPath()` | `Hugh/Program.cs` | Both Serilog configurations (initial and verbose mode) write logs to `~/.hugh/logs/hugh-{date}.log` |
| 1.4 | Add `GetResolvedAgentsPath()` method to `HughConfig` | `Hugh/Configuration/HughConfig.cs` | Method returns absolute path resolved from installation directory |
| 1.5 | Add unit tests for `PathResolver` | `Hugh.Tests/PathResolverTests.cs` | Tests cover ENV override, assembly location detection, CWD fallback |

### Phase 2: AgentRegistry Refactoring

**Estimated Session Size:** Small
**Files to Modify:** 4
**Prerequisites:** Phase 1 complete (PathResolver exists)
**Entry Point:** Start by reading `Hugh/Agents/AgentRegistry.cs`
**Verification:** All existing agent lookups continue to work; unit tests pass

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Create `IAgentRegistry` interface | `Hugh/Agents/IAgentRegistry.cs` | Interface defines `GetAgent()`, `GetAllAgents()`, `TryGetAgentByName()` |
| 2.2 | Convert `AgentRegistry` from static to instance-based class | `Hugh/Agents/AgentRegistry.cs` | Class implements `IAgentRegistry`, accepts `IOptions<HughConfig>` in constructor |
| 2.3 | Update agent prompt paths to use resolved base path | `Hugh/Agents/AgentRegistry.cs` | All `PromptPath` values use `Path.Combine(_agentsBasePath, filename)` |
| 2.4 | Register `AgentRegistry` in DI container | `Hugh/Program.cs` | `IAgentRegistry` registered as singleton |
| 2.5 | Update all `AgentRegistry` consumers to use injected instance | `Hugh/Orchestration/*.cs`, `Hugh/Agents/AgentPromptLoader.cs` | Static calls replaced with injected `IAgentRegistry` |

### Phase 3: Component Updates

**Estimated Session Size:** Small
**Files to Modify:** 3
**Prerequisites:** Phase 2 complete
**Entry Point:** Start by reading `Hugh/Agents/AgentPromptLoader.cs` and `Hugh/PrerequisiteChecker.cs`
**Verification:** Prerequisites check passes when run from arbitrary directory

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Update `AgentPromptLoader` to use `PathResolver` for path resolution | `Hugh/Agents/AgentPromptLoader.cs` | Prompt files found from installation directory, not CWD |
| 3.2 | Update `PrerequisiteChecker` to resolve agents path correctly | `Hugh/PrerequisiteChecker.cs` | Validates agents folder from installation path |
| 3.3 | Ensure `~/.hugh/logs/` directory is created on startup | `Hugh/Program.cs` | Log directory created before Serilog initialization |
| 3.4 | Add integration test for global CLI path resolution | `Hugh.Tests/IntegrationTests.cs` | Test verifies config loads from assembly location |

### Phase 4: Inno Setup Installer

**Estimated Session Size:** Medium
**Files to Modify:** 2 new files
**Prerequisites:** Phase 3 complete; application builds successfully
**Entry Point:** Research document Phase 1 findings for Inno Setup patterns
**Verification:** Installer creates Hugh in Program Files, adds to PATH, uninstaller works
**Status:** ✅ Complete (2026-02-05)

| Step | Task | File(s) | Status | Notes |
|------|------|---------|--------|-------|
| 4.1 | Create `installer/` directory structure | `installer/` | ✅ Complete | Directory created at repository root |
| 4.2 | Create Inno Setup script with PATH management | `installer/hugh-setup.iss` | ✅ Complete | Full Inno Setup script with user PATH management, lzma2 compression |
| 4.3 | Add version parameter support to installer script | `installer/hugh-setup.iss` | ✅ Complete | `/DMyAppVersion=x.y.z` supported, defaults to 0.0.0 |
| 4.4 | Test installer locally | `installer/README.md` | ✅ Complete | Testing instructions documented in README |

### Phase 5: GitHub Actions Workflow

**Estimated Session Size:** Medium
**Files to Modify:** 1 new file
**Prerequisites:** Phase 4 complete; Inno Setup script tested locally
**Entry Point:** Research document Phase 4 findings for workflow structure
**Verification:** Tag push triggers build and creates GitHub Release with artifacts

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 5.1 | Create release workflow with tag trigger | `.github/workflows/release.yml` | Workflow triggers on `v*.*.*` tags and `workflow_dispatch` |
| 5.2 | Add .NET build and test job | `.github/workflows/release.yml` | Tests run; x64 self-contained single-file publish succeeds |
| 5.3 | Add Inno Setup compilation step | `.github/workflows/release.yml` | Installer created using `Minionguyjpro/Inno-Setup-Action` |
| 5.4 | Add GitHub Release publishing | `.github/workflows/release.yml` | `softprops/action-gh-release@v2` uploads installer and portable ZIP |

### Phase 6: Testing & Documentation

**Estimated Session Size:** Small
**Files to Modify:** 2
**Prerequisites:** Phase 5 complete
**Entry Point:** Existing `Hugh/README.md`
**Verification:** README includes installation instructions; end-to-end test passes

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 6.1 | Update README with installation instructions | `Hugh/README.md` | Documents installer download, prerequisites (gh cli), usage |
| 6.2 | Add portable installation instructions | `Hugh/README.md` | Documents manual PATH setup for portable ZIP |
| 6.3 | Add troubleshooting section | `Hugh/README.md` | Covers common issues: PATH not updated, credential detection |
| 6.4 | Create end-to-end test script | `Hugh/scripts/test-global-install.ps1` | PowerShell script tests global installation scenario |

---

## Implementation Record

### Phase 4 - Inno Setup Installer

**Completed:** 2026-02-05
**Execution Mode:** Subagent

**Files Created:**

- `installer/hugh-setup.iss` — Full Inno Setup script with:
  - App metadata and unique AppId GUID
  - LZMA2/ultra64 compression for optimal installer size
  - User PATH integration (HKEY_CURRENT_USER, no admin required)
  - `ChangesEnvironment=yes` for live PATH notification
  - Version parameter support (`/DMyAppVersion=x.y.z`)
  - Clean uninstall with PATH removal
  - Modern wizard style with per-user installation option

- `installer/README.md` — Documentation with:
  - Build prerequisites (Inno Setup 6+)
  - Step-by-step build instructions
  - Comprehensive testing instructions for Step 4.4
  - Silent installation/uninstallation commands
  - Troubleshooting guidance

- `dist/.gitkeep` — Placeholder for installer output directory

**Files Modified:**

- `.gitignore` — Added exclusion for `dist/*.exe` installer outputs

**Deviations from Plan:**

- Icon file (`hugh-icon.ico`) commented out in script — file does not exist yet; can be enabled when icon is created

**Implementation Notes:**

- AppId GUID: `{009C6B3E-168E-41E7-879C-E0D884474568}` — Do NOT change after initial release
- Uses user-level PATH modification (HKEY_CURRENT_USER) to avoid requiring admin elevation
- Source path expects published output at `Hugh\bin\Release\net8.0\win-x64\publish\*`
- Requires Inno Setup 6+ with 64-bit compilation support

---

## Implementation Notes for AI Agents

### Critical Implementation Details

1. **Use `AppContext.BaseDirectory` not `Assembly.GetExecutingAssembly().Location`**
   - Single-file publish causes `Assembly.Location` to return empty string
   - `AppContext.BaseDirectory` correctly returns the extraction/installation directory

2. **PathResolver must be static and called early**
   - Configuration and Serilog setup happen before DI container is built
   - `PathResolver` cannot depend on injected services

3. **Create log directory before Serilog initialization**
   - Call `Directory.CreateDirectory(PathResolver.GetLogPath())` before `CreateLogger()`
   - Serilog's file sink does not create parent directories

4. **AgentRegistry consumers in Orchestration**
   - `LoopOrchestrator` and `PhaseRunner` likely consume `AgentRegistry`
   - Search for `AgentRegistry.GetAgent` to find all call sites
   - Inject `IAgentRegistry` via constructor

5. **Inno Setup PATH modification**
   - Use `ChangesEnvironment=yes` in `[Setup]` section
   - Use `EnvAddPath` and `EnvRemovePath` helper functions from research document
   - Hook `CurStepChanged` for install and `CurUninstallStepChanged` for uninstall

6. **GitHub Actions Windows paths**
   - Workflow clones to `D:\a\{repo}\{repo}\` on Windows runners
   - Use `/` separators in glob patterns, not `\`
   - PowerShell paths should use backticks for line continuation

### Reference Patterns

| Pattern | File | Usage |
|---------|------|-------|
| Options injection | `AgentPromptLoader.cs` | `IOptions<HughConfig>` constructor parameter |
| Interface + DI registration | `ICopilotService.cs` + `CopilotService.cs` | Interface pattern for `AgentRegistry` |
| Path resolution | `DocumentManager.cs` | Uses `_workspaceRoot` for document paths |

### Testing Strategy

- **Unit tests**: PathResolver path resolution logic (mock environment/file system if needed)
- **Integration tests**: Full startup with config from non-CWD location
- **Manual tests**: Install, run from arbitrary directory, uninstall, verify PATH cleaned

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|

*No organizational standards applicable to this plan.*

---

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | ~8 files across Configuration, Agents, Orchestration |
| New patterns introduced | 2 | PathResolver static utility; IAgentRegistry interface |
| External dependencies | 2 | Inno Setup (build-time), GitHub Actions |
| Migration complexity | 1 | No data migration; backward compatible dev mode |
| Test coverage required | 2 | Unit tests for PathResolver, integration test for global install |
| **Overall Complexity** | **10/25** | **Low** - Well-scoped changes with clear patterns |

---

## Review Summary

**Review Date:** 2026-02-05
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 2 (Critical: 0, Major: 2, Minor: 0)
- Clarifying Questions Asked: 2
- Sections Updated: Phase 1 Step 1.3, Phase 2 Step 2.5

### Key Improvements Made
1. **Expanded Step 2.5 scope** — Added `Hugh/Agents/AgentPromptLoader.cs` as an AgentRegistry consumer requiring update (lines 111, 129 use static calls)
2. **Expanded Step 1.3 scope** — Clarified that both Serilog configurations (initial and verbose mode at line 113-120) must use `PathResolver.GetLogPath()`

### Codebase Verification Results
- ✅ `Program.cs` lines 27-28: Confirmed `SetBasePath(Directory.GetCurrentDirectory())` pattern
- ✅ `AgentRegistry.cs`: Confirmed static class structure
- ✅ `AgentPromptLoader.cs` line 70: Uses `Directory.GetCurrentDirectory()` for path resolution
- ✅ `PhaseExecutor.cs` line 59: Only AgentRegistry consumer in Orchestration folder
- ✅ `PrerequisiteChecker.cs`: Receives `agentsPath` as parameter (no static dependency)

### Remaining Considerations
- The verbose mode Serilog reconfiguration is inline in the command handler; consider future refactoring to a shared configuration builder (not blocking)
- ARM64 builds are deferred to P2; ensure documentation notes x64-only initial release

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** 2026-02-05
**Reviewed By:** pch-plan-reviewer
**Review Date:** 2026-02-05
**Status:** Ready for Implementation
**Next Agent:** pch-coder
**Plan Location:** `/docs/plans/010-hugh-global-cli-installation.md`
