# Implementation Plan: Hugh Prerequisite Detection and Installation Guidance

## Document Information

| Field | Value |
|-------|-------|
| Document | 011-hugh-prerequisite-detection-installation.md |
| Feature | Prerequisite Detection and Guided Installation |
| Created | 2026-02-05 |
| Status | ✅ Implementation Complete |
| Owner | pch-planner |
| Version | v3.0 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-05 | pch-planner | Initial plan creation |
| v1.1 | 2026-02-05 | pch-planner | Completed 7 planning decisions |
| v2.0 | 2026-02-05 | pch-planner | Technical design complete, holistic review done |
| v2.1 | 2026-02-05 | pch-plan-reviewer | Review in progress |
| v2.2 | 2026-02-05 | pch-plan-reviewer | Review complete; 6 issues resolved; added IProcessRunner, exit codes, auth fallback |

---

## Review Session Log

**Questions Pending:** 0 | **Questions Resolved:** 6 | **Last Updated:** 2026-02-05

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | Target framework mismatch (net8.0 vs net9.0) | Correctness | C — Leave as-is | No change needed; mismatch is acceptable |
| 2 | ManualInstaller constructor dependency cycle | Correctness | B — Returns instructions array | ManualInstaller section + Step 3.4 updated |
| 3 | `--check` backward compat should return exit code | Completeness | A — Both return exit codes | Steps 6.1, 6.3 updated with exit code AC |
| 4 | Missing Copilot CLI `/login` fallback when gh CLI unavailable | Completeness | A — Add /login as fallback | ManualInstaller + Implementation Notes updated |
| 5 | Winget package ID needs verification | Specificity | ✅ Verified: GitHub.Copilot, GitHub.cli | No change needed; IDs are correct |
| 6 | Test mocking strategy for process execution | Clarity | A — Add IProcessRunner abstraction | Directory structure, Phase 1, Phase 2 updated |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | 2026-02-05 | Command Interface Approach | B — Single `hugh doctor` with `--fix` | Follows CLI conventions (brew doctor), simpler mental model, --fix makes interactive behavior opt-in |
| 2 | 2026-02-05 | Prerequisite Detection Scope | C — Copilot CLI + gh CLI + Authentication | Comprehensive coverage of common failure modes; enables gh auth login as auth fallback |
| 3 | 2026-02-05 | Installation Strategy | A — Auto-Select Best Available Installer | Faster UX with fewer decisions; auto-detects winget > npm > brew; falls back to manual instructions |
| 4 | 2026-02-05 | Authentication Workflow | A — Delegate to `gh auth login --web` | Proven OAuth implementation; handles edge cases; stores credentials properly |
| 5 | 2026-02-05 | Spectre.Console Integration | C — Separate Concerns: ConsoleOutput + ISetupUserInterface | Maintains separation; enables testing; follows existing interface patterns |
| 6 | 2026-02-05 | PrerequisiteChecker Migration | A — Delete and Replace Entirely | Clean break; no legacy code; --check updated to use new system |
| 7 | 2026-02-05 | Cross-Platform Support | A — Windows Only (MVP) | Hugh is Windows-focused; simplest implementation; fastest delivery |

---

## Holistic Review Summary

### Key Decision Interactions

| Decision Pair | Interaction | Resolution |
|---------------|-------------|------------|
| Single `doctor` command + Delete PrerequisiteChecker | Must ensure `--check` backward compatibility routes to new orchestrator | Step 6.3 explicitly handles this |
| Windows-only + Auto-select installer | Only winget available; ManualInstaller becomes critical fallback | ManualInstaller must have excellent instructions for gh CLI `/login` |
| Separate ConsoleOutput + ISetupUserInterface | Two console output systems coexist | Clear separation: ConsoleOutput for orchestration, ISetupUserInterface for setup only |
| Auth via `gh auth login` + gh CLI as prerequisite | Auth flow depends on gh CLI being installed first | Detector priority ordering (gh CLI = 30, Auth = 50) ensures correct sequence |

### Architectural Considerations Addressed

1. **Dependency Ordering:** Prerequisite priority system ensures CLI installation before auth verification
2. **Non-Interactive Support:** `Console.IsInputRedirected` check + `--no-interactive` flag provide explicit CI/CD support
3. **Clean Migration:** Deleting `PrerequisiteChecker.cs` completely in Phase 6.4 after all functionality migrated

### Trade-offs Accepted

| Trade-off | Benefit | Cost |
|-----------|---------|------|
| Windows-only installers | Faster MVP delivery, simpler testing | Users on macOS/Linux get manual instructions only |
| Delete vs deprecate PrerequisiteChecker | No legacy code, cleaner codebase | Breaking change if any external code references it |
| Single `doctor` command | Simpler mental model | No separate first-run wizard experience |

### Risks Acknowledged

| Risk | Mitigation Strategy |
|------|---------------------|
| ManualInstaller is the only fallback | Provide comprehensive, copy-paste-ready instructions in `ShowManualInstructions()` |
| PATH refresh needed after winget install | `InstallationResult.RequiresRestart` flag + clear user notification |
| User aborts mid-fix-workflow | Each install is independent; partial progress is acceptable; re-run `doctor` to continue |

### Additional Considerations

1. **PATH Refresh:** WingetInstaller should output a hint about restarting terminal or offer to refresh PATH programmatically:
   ```powershell
   $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
   ```

2. **Spectre.Console Thread Safety:** Prompts must be sequential; never mix progress spinners with prompts (research finding). Orchestrator workflow is inherently sequential, so this is safe.

3. **Exit Codes:** `hugh doctor` should return exit code 0 if all satisfied, 1 if issues remain. This enables scripting: `hugh doctor || echo "Issues detected"`

---

## Overview

This plan implements a comprehensive prerequisite detection and guided installation system for Hugh. The goal is to transform blocking prerequisite errors into guided resolution workflows, enabling both technical developers and less experienced users to set up Hugh with minimal friction.

### Source Research

- [011-hugh-prerequisite-detection-installation.md](../../research/011-hugh-prerequisite-detection-installation.md)

### Objectives

1. Replace the static `PrerequisiteChecker` with an extensible, DI-friendly architecture
2. Add comprehensive prerequisite detection (Copilot CLI, gh CLI, authentication status)
3. Implement interactive installation wizards using Spectre.Console
4. Provide `hugh doctor` command with `--fix` option
5. Support both interactive and non-interactive execution modes

---

## Referenced Standards

⚠️ **Could not access organizational standards from pch-standards-space. Proceeding without standards context.**

No organizational standards applicable to this plan.

---

## Requirements

### Functional Requirements

1. **FR-1**: Detect all prerequisites (Copilot CLI, gh CLI, authentication, agents folder, workspace)
2. **FR-2**: Provide interactive installation prompts when `--fix` flag is used
3. **FR-3**: Support winget installer for Windows (MVP scope)
4. **FR-4**: Verify authentication status (GH_TOKEN, GITHUB_TOKEN, gh auth status)
5. **FR-5**: Implement `hugh doctor` command for check-and-report
6. **FR-6**: Implement `hugh doctor --fix` for interactive installation workflow
7. **FR-7**: Maintain backward compatibility with `--check` flag (alias to `doctor`)
8. **FR-8**: Support non-interactive mode for CI/CD environments
9. **FR-9**: Delegate authentication to `gh auth login --web` when gh CLI available

### Non-Functional Requirements

1. **NFR-1**: All new components must use DI and interfaces for testability
2. **NFR-2**: Installation operations must not require admin elevation (per-user scope)
3. **NFR-3**: Interactive prompts must gracefully degrade in non-TTY environments
4. **NFR-4**: Error messages must include actionable remediation hints

### Out of Scope

- Cross-platform installers (npm, Homebrew, apt/dnf) — Windows winget only for MVP
- Separate `hugh setup` command — unified under `hugh doctor --fix`
- Custom OAuth device flow implementation — delegate to gh CLI
- Token persistence/storage — use gh CLI's credential management
- Spectre.Console integration with ConsoleOutput — separate concerns

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Program.cs                                │
│  ┌─────────────┐  ┌──────────────────┐  ┌───────────────────┐   │
│  │ hugh doctor │  │ hugh doctor --fix│  │ hugh --check      │   │
│  └──────┬──────┘  └────────┬─────────┘  └─────────┬─────────┘   │
│         │                  │                      │              │
│         └──────────────────┴──────────────────────┘              │
│                            │                                     │
└────────────────────────────┼─────────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ISetupOrchestrator                            │
│   Coordinates: detect → report/prompt → install → verify         │
└────────────────────────────┬────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────┐
│ IPrerequisite   │ │ IInstaller      │ │ ISetupUserInterface     │
│ Detector[]      │ │ Registry        │ │                         │
│                 │ │                 │ │ ├─ InteractiveSetupUI   │
│ ├─ CopilotCli   │ │ ├─ WingetInst.  │ │ │   (Spectre.Console)   │
│ ├─ GitHubCli    │ │ └─ ManualInst.  │ │ └─ NonInteractiveSetupUI│
│ ├─ GitHubAuth   │ │                 │ │     (report only)       │
│ ├─ AgentsFolder │ └─────────────────┘ │                         │
│ └─ Workspace    │                     └─────────────────────────┘
└─────────────────┘
```

**Data Flow:**
1. `Program.cs` parses `doctor` command and options (`--fix`, `--check` alias)
2. `SetupOrchestrator` runs all detectors in priority order
3. Results collected as `PrerequisiteStatus[]`
4. If `--fix` specified and issues found:
   - `ISetupUserInterface` prompts user for each fixable issue
   - `IInstallerRegistry` selects best available installer
   - Installer executes, result verified by re-running detector
5. Final report displayed

### Codebase Patterns (Reference)

| Pattern | Location | Usage in This Plan |
|---------|----------|--------------------|
| Interface/DI pattern | `Hugh/Agents/ICopilotService.cs` | All new services will follow this pattern |
| Static console output | `Hugh/ConsoleOutput.cs` | Will be enhanced/wrapped for Spectre.Console |
| Service registration | `Hugh/Program.cs` | New setup services registered in BuildOrchestrator |
| Configuration binding | `Hugh/Configuration/HughConfig.cs` | Setup config section added |

### Directory Structure

```
Hugh/
├── Setup/                              # NEW: Setup subsystem
│   ├── ISetupOrchestrator.cs           # Main coordination interface  
│   ├── SetupOrchestrator.cs            # Implementation
│   ├── SetupResult.cs                  # Result types (SetupResult, PrerequisiteReport)
│   │
│   ├── Detection/                      # Prerequisite detectors
│   │   ├── IPrerequisiteDetector.cs    # Detector interface
│   │   ├── IProcessRunner.cs           # Process execution abstraction (for testability)
│   │   ├── ProcessRunner.cs            # Real process execution implementation
│   │   ├── PrerequisiteStatus.cs       # Status enum and record
│   │   ├── CopilotCliDetector.cs       # Detects copilot CLI installation
│   │   ├── GitHubCliDetector.cs        # Detects gh CLI installation
│   │   ├── GitHubAuthDetector.cs       # Detects authentication status
│   │   ├── AgentsFolderDetector.cs     # Detects agents folder and files
│   │   └── WorkspaceDetector.cs        # Detects workspace write access
│   │
│   ├── Installation/                   # Installers (Strategy pattern)
│   │   ├── IInstaller.cs               # Installer interface
│   │   ├── IInstallerRegistry.cs       # Registry interface
│   │   ├── InstallerRegistry.cs        # Auto-selects best installer
│   │   ├── InstallationResult.cs       # Result types
│   │   ├── WingetInstaller.cs          # Windows winget implementation
│   │   └── ManualInstaller.cs          # Displays manual instructions
│   │
│   └── UserInterface/                  # UI abstraction
│       ├── ISetupUserInterface.cs      # UI interface
│       ├── InteractiveSetupUI.cs       # Spectre.Console implementation
│       └── NonInteractiveSetupUI.cs    # Report-only implementation
│
└── PrerequisiteChecker.cs              # DELETE (replaced by Setup subsystem)
```

### Component Design

#### Prerequisite Detectors

Each detector implements `IPrerequisiteDetector` and checks a single prerequisite:

```csharp
public interface IPrerequisiteDetector
{
    /// <summary>Unique identifier (e.g., "copilot-cli", "github-auth")</summary>
    string PrerequisiteId { get; }
    
    /// <summary>Human-readable name for display</summary>
    string DisplayName { get; }
    
    /// <summary>Execution priority (lower = earlier). Auth depends on CLI.</summary>
    int Priority { get; }
    
    /// <summary>Whether this prerequisite can be auto-installed</summary>
    bool IsInstallable { get; }
    
    /// <summary>Detect the current state of this prerequisite</summary>
    Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default);
}

public record PrerequisiteStatus
{
    public required string PrerequisiteId { get; init; }
    public required PrerequisiteState State { get; init; }
    public string? Version { get; init; }
    public string? Message { get; init; }
    public string[]? Hints { get; init; }
}

public enum PrerequisiteState
{
    Satisfied,      // ✓ Installed and working
    Missing,        // ✗ Not installed
    Outdated,       // ⚠ Installed but needs update (future use)
    Misconfigured   // ⚠ Installed but not properly configured
}
```

**Detector Priority Order:**
| Priority | Detector | Depends On |
|----------|----------|------------|
| 10 | AgentsFolderDetector | None |
| 20 | WorkspaceDetector | None |
| 30 | GitHubCliDetector | None |
| 40 | CopilotCliDetector | None |
| 50 | GitHubAuthDetector | GitHubCliDetector (for `gh auth status`) |

**Detection Methods:**

| Detector | Detection Approach |
|----------|-------------------|
| `CopilotCliDetector` | Run `copilot --version`, check exit code 0 |
| `GitHubCliDetector` | Run `gh --version`, check exit code 0 |
| `GitHubAuthDetector` | Check `GH_TOKEN`/`GITHUB_TOKEN` env vars, then `gh auth status` |
| `AgentsFolderDetector` | `Directory.Exists()` + enumerate `pch-*.md` files |
| `WorkspaceDetector` | Create `.orchestrator/` and test write access |

#### Process Runner Abstraction (for Testability)

```csharp
public interface IProcessRunner
{
    /// <summary>Run a process and return stdout, stderr, and exit code</summary>
    Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct = default);
}

public record ProcessResult
{
    public required int ExitCode { get; init; }
    public required string StandardOutput { get; init; }
    public required string StandardError { get; init; }
}
```

**Usage:** CopilotCliDetector, GitHubCliDetector, GitHubAuthDetector, and WingetInstaller all accept `IProcessRunner` via constructor injection. This enables unit testing with mocked process results.

#### Installers (Strategy Pattern)

```csharp
public interface IInstaller
{
    /// <summary>Installer identifier (e.g., "winget", "npm", "manual")</summary>
    string InstallerId { get; }
    
    /// <summary>Check if this installer is available on the system</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    
    /// <summary>Check if this installer can install the given prerequisite</summary>
    bool CanInstall(string prerequisiteId);
    
    /// <summary>Install the prerequisite</summary>
    Task<InstallationResult> InstallAsync(string prerequisiteId, CancellationToken ct = default);
}

public record InstallationResult
{
    public required bool Success { get; init; }
    public string? Version { get; init; }
    public string? ErrorMessage { get; init; }
    public bool RequiresRestart { get; init; }  // Terminal restart for PATH
    public string[]? Instructions { get; init; }  // Manual instructions (populated by ManualInstaller)
}
```

**WingetInstaller Package Mappings:**

| Prerequisite ID | Winget Package ID | Command |
|-----------------|-------------------|---------|
| `copilot-cli` | `GitHub.Copilot` | `winget install GitHub.Copilot --silent --accept-source-agreements --accept-package-agreements --scope user` |
| `github-cli` | `GitHub.cli` | `winget install GitHub.cli --silent --accept-source-agreements --accept-package-agreements --scope user` |

**ManualInstaller:**
- Always available (fallback)
- `InstallAsync()` returns `InstallationResult` with `Instructions` string array (populated for the prerequisite)
- Orchestrator passes the `Instructions` to `ISetupUserInterface.ShowManualInstructions()` after receiving results
- Returns `Success = true` with `RequiresRestart = true` (user must complete manually)
- **Auth fallback:** When returning instructions for `github-auth`, include Copilot CLI `/login` as alternative:
  - "Option 1: Run `gh auth login --web` (if gh CLI installed)"
  - "Option 2: Run `copilot` then type `/login` and follow browser prompts"

#### InstallerRegistry

```csharp
public interface IInstallerRegistry
{
    /// <summary>Get the best available installer for a prerequisite</summary>
    Task<IInstaller?> GetInstallerAsync(string prerequisiteId, CancellationToken ct = default);
    
    /// <summary>Get all installers for a prerequisite (for user choice)</summary>
    Task<IReadOnlyList<IInstaller>> GetAvailableInstallersAsync(string prerequisiteId, CancellationToken ct = default);
}
```

**Auto-Selection Logic:**
1. Find all installers where `CanInstall(prerequisiteId)` returns true
2. Filter to installers where `IsAvailableAsync()` returns true
3. Return first available in priority order: `WingetInstaller` > `ManualInstaller`

#### User Interface Abstraction

```csharp
public interface ISetupUserInterface
{
    /// <summary>Display prerequisite check results</summary>
    void ShowPrerequisiteReport(IReadOnlyList<PrerequisiteStatus> statuses);
    
    /// <summary>Prompt user to fix a missing prerequisite</summary>
    Task<FixDecision> PromptToFixAsync(PrerequisiteStatus status, IReadOnlyList<IInstaller> availableInstallers, CancellationToken ct = default);
    
    /// <summary>Show installation progress</summary>
    void ShowInstallationProgress(string prerequisiteId, string message);
    
    /// <summary>Show installation result</summary>
    void ShowInstallationResult(string prerequisiteId, InstallationResult result);
    
    /// <summary>Show manual installation instructions</summary>
    void ShowManualInstructions(string prerequisiteId, string[] instructions);
    
    /// <summary>Prompt for authentication (browser vs skip)</summary>
    Task<AuthDecision> PromptForAuthenticationAsync(CancellationToken ct = default);
}

public enum FixDecision { Install, Skip, Abort }
public enum AuthDecision { Browser, Skip }
```

**InteractiveSetupUI (Spectre.Console):**
- Uses `SelectionPrompt<T>` for fix decisions
- Uses `AnsiConsole.Status()` with spinner for installation progress
- Uses `AnsiConsole.Confirm()` for simple yes/no prompts
- Colorized output with markup: `[green]✓[/]`, `[red]✗[/]`, `[yellow]⚠[/]`

**NonInteractiveSetupUI:**
- All prompts return `FixDecision.Skip` or `AuthDecision.Skip`
- Report-only output to stdout
- Used when `Console.IsInputRedirected` or `--no-interactive` flag

#### Setup Orchestrator

```csharp
public interface ISetupOrchestrator
{
    /// <summary>Run prerequisite checks and optionally fix issues</summary>
    Task<SetupResult> RunAsync(SetupOptions options, CancellationToken ct = default);
}

public record SetupOptions
{
    public bool Fix { get; init; } = false;          // --fix flag
    public bool NonInteractive { get; init; } = false;  // CI/CD mode
    public string? AgentsPath { get; init; }         // Override from config
    public string? WorkspacePath { get; init; }      // Current directory
}

public record SetupResult
{
    public required bool AllSatisfied { get; init; }
    public required IReadOnlyList<PrerequisiteStatus> Statuses { get; init; }
    public IReadOnlyList<InstallationResult>? InstallationResults { get; init; }
}
```

**Orchestrator Workflow:**
1. Sort detectors by priority
2. Run all detectors, collect `PrerequisiteStatus[]`
3. Display report via `ISetupUserInterface.ShowPrerequisiteReport()`
4. If `--fix` and issues exist:
   - For each `Missing` or `Misconfigured` status:
     - Get available installers from registry
     - Prompt user via `ISetupUserInterface.PromptToFixAsync()`
     - If `Install`: execute installer, verify with detector re-run
     - If `Skip`: continue to next
     - If `Abort`: stop and return
5. Return `SetupResult` with final statuses

### CLI Integration

**New `doctor` Subcommand:**

```csharp
var doctorCommand = new Command("doctor", "Check and optionally fix Hugh prerequisites")
{
    new Option<bool>("--fix", "Attempt to install missing prerequisites"),
    new Option<bool>("--no-interactive", "Disable interactive prompts (CI/CD mode)")
};

doctorCommand.SetHandler(async (fix, noInteractive) =>
{
    var options = new SetupOptions
    {
        Fix = fix,
        NonInteractive = noInteractive || Console.IsInputRedirected,
        AgentsPath = config.GetResolvedAgentsPath(),
        WorkspacePath = Directory.GetCurrentDirectory()
    };
    
    var result = await orchestrator.RunAsync(options, ct);
    return result.AllSatisfied ? 0 : 1;
}, fixOption, noInteractiveOption);

rootCommand.AddCommand(doctorCommand);
```

**`--check` Backward Compatibility:**

```csharp
// Existing --check flag becomes alias to doctor command
if (check)
{
    var result = await orchestrator.RunAsync(new SetupOptions { Fix = false }, ct);
    return;
}
```

---

## Dependencies and Prerequisites

| Dependency | Type | Current State | Required Action |
|------------|------|---------------|-----------------|
| Spectre.Console | NuGet package | Not installed | Add `<PackageReference Include="Spectre.Console" Version="0.49.1" />` |
| System.CommandLine | NuGet package | v2.0.0-beta4 | Already installed — supports subcommands |
| PrerequisiteChecker.cs | Source file | Exists | Delete after migration complete |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Spectre.Console threading issues | Low | Medium | Use sequential prompts, no parallel progress displays |
| PATH refresh requirement after installation | High | Low | Notify user to restart terminal; offer programmatic PATH refresh in PowerShell |
| Silent winget installation failures | Medium | Medium | Catch specific exit codes; fall back to ManualInstaller with clear instructions |
| gh CLI not available for auth | Medium | Low | Show manual Copilot CLI `/login` instructions as fallback |
| Non-TTY environment detection failure | Low | Medium | Check `Console.IsInputRedirected` AND `--no-interactive` flag |

---

## Progress Summary

| Phase | Description | Status | Est. Size |
|-------|-------------|--------|-----------|
| Phase 1 | Project Setup & Core Types | ✅ Complete | Small (6 tasks) |
| Phase 2 | Prerequisite Detectors | ✅ Complete | Medium (6 tasks) |
| Phase 3 | Installers | ✅ Complete | Medium (5 tasks) |
| Phase 4 | User Interface | ✅ Complete | Medium (4 tasks) |
| Phase 5 | Setup Orchestrator | ✅ Complete | Medium (5 tasks) |
| Phase 6 | CLI Integration & Cleanup | ✅ Complete | Small (4 tasks) |
| Phase 7 | Testing | ✅ Complete | Medium (6 tasks) |

---

## Execution Plan

### Phase 1: Project Setup & Core Types

**Estimated Session Size:** Small
**Files to Modify:** 6
**Prerequisites:** None
**Entry Point:** Start by reading `Hugh/Hugh.csproj`
**Verification:** Project builds successfully with Spectre.Console

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Add Spectre.Console NuGet package | `Hugh/Hugh.csproj` | `<PackageReference Include="Spectre.Console" Version="0.49.1" />` added; `dotnet restore` succeeds |
| 1.2 | Create Setup directory structure | `Hugh/Setup/`, `Hugh/Setup/Detection/`, `Hugh/Setup/Installation/`, `Hugh/Setup/UserInterface/` | Directories exist |
| 1.3 | Create PrerequisiteStatus types | `Hugh/Setup/Detection/PrerequisiteStatus.cs` | Contains `PrerequisiteState` enum and `PrerequisiteStatus` record with all properties from Technical Design |
| 1.4 | Create InstallationResult types | `Hugh/Setup/Installation/InstallationResult.cs` | Contains `InstallationResult` record with `Success`, `Version`, `ErrorMessage`, `RequiresRestart`, `Instructions` properties |
| 1.5 | Create IProcessRunner interface | `Hugh/Setup/Detection/IProcessRunner.cs` | Interface with `RunAsync(fileName, arguments, ct)` returning `Task<ProcessResult>`; includes `ProcessResult` record |
| 1.6 | Implement ProcessRunner | `Hugh/Setup/Detection/ProcessRunner.cs` | Implements `IProcessRunner` using `Process.Start()`; handles stdout/stderr capture and exit code |

---

### Phase 2: Prerequisite Detectors

**Estimated Session Size:** Medium
**Files to Modify:** 7
**Prerequisites:** Phase 1 complete
**Entry Point:** Start by reading `Hugh/Setup/Detection/PrerequisiteStatus.cs`
**Verification:** All detectors compile; unit tests for detectors pass

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Create IPrerequisiteDetector interface | `Hugh/Setup/Detection/IPrerequisiteDetector.cs` | Interface with `PrerequisiteId`, `DisplayName`, `Priority`, `IsInstallable`, `DetectAsync()` per Technical Design |
| 2.2 | Implement CopilotCliDetector | `Hugh/Setup/Detection/CopilotCliDetector.cs` | Takes `IProcessRunner` via DI; runs `copilot --version`; returns `Satisfied` with version or `Missing`; Priority=40 |
| 2.3 | Implement GitHubCliDetector | `Hugh/Setup/Detection/GitHubCliDetector.cs` | Takes `IProcessRunner` via DI; runs `gh --version`; returns `Satisfied` with version or `Missing`; Priority=30 |
| 2.4 | Implement GitHubAuthDetector | `Hugh/Setup/Detection/GitHubAuthDetector.cs` | Takes `IProcessRunner` via DI; checks `GH_TOKEN`/`GITHUB_TOKEN` env vars first, then `gh auth status`; Priority=50; `IsInstallable=false` |
| 2.5 | Implement AgentsFolderDetector | `Hugh/Setup/Detection/AgentsFolderDetector.cs` | Checks directory exists + contains `pch-*.md` files; lists missing agents in `Hints`; Priority=10 |
| 2.6 | Implement WorkspaceDetector | `Hugh/Setup/Detection/WorkspaceDetector.cs` | Creates `.orchestrator/` and tests write access; Priority=20 |

---

### Phase 3: Installers

**Estimated Session Size:** Medium  
**Files to Modify:** 5
**Prerequisites:** Phase 1 complete
**Entry Point:** Start by reading `Hugh/Setup/Installation/InstallationResult.cs`
**Verification:** WingetInstaller can detect winget availability; ManualInstaller returns success

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Create IInstaller interface | `Hugh/Setup/Installation/IInstaller.cs` | Interface with `InstallerId`, `IsAvailableAsync()`, `CanInstall()`, `InstallAsync()` per Technical Design |
| 3.2 | Create IInstallerRegistry interface | `Hugh/Setup/Installation/IInstallerRegistry.cs` | Interface with `GetInstallerAsync()`, `GetAvailableInstallersAsync()` |
| 3.3 | Implement WingetInstaller | `Hugh/Setup/Installation/WingetInstaller.cs` | Takes `IProcessRunner` via DI; runs `winget --version` for availability; installs with `--silent --accept-source-agreements --accept-package-agreements --scope user`; package mappings for `copilot-cli`, `github-cli` |
| 3.4 | Implement ManualInstaller | `Hugh/Setup/Installation/ManualInstaller.cs` | Always available; returns `InstallationResult` with `Instructions` array; orchestrator displays via UI; returns `Success=true, RequiresRestart=true` |
| 3.5 | Implement InstallerRegistry | `Hugh/Setup/Installation/InstallerRegistry.cs` | Constructor takes `IEnumerable<IInstaller>`; auto-selects first available in priority order (winget > manual) |

---

### Phase 4: User Interface

**Estimated Session Size:** Medium
**Files to Modify:** 4
**Prerequisites:** Phase 1 complete, Spectre.Console package installed
**Entry Point:** Start by reading `Hugh/ConsoleOutput.cs` for style patterns
**Verification:** Interactive prompts render correctly in terminal; non-interactive mode returns skip decisions

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 4.1 | Create ISetupUserInterface interface | `Hugh/Setup/UserInterface/ISetupUserInterface.cs` | Interface with all methods from Technical Design: `ShowPrerequisiteReport`, `PromptToFixAsync`, `ShowInstallationProgress`, `ShowInstallationResult`, `ShowManualInstructions`, `PromptForAuthenticationAsync`; includes `FixDecision` and `AuthDecision` enums |
| 4.2 | Implement NonInteractiveSetupUI | `Hugh/Setup/UserInterface/NonInteractiveSetupUI.cs` | All prompts return `Skip`; report output uses `Console.WriteLine` with status icons matching `ConsoleOutput` style |
| 4.3 | Implement InteractiveSetupUI | `Hugh/Setup/UserInterface/InteractiveSetupUI.cs` | Uses Spectre.Console `SelectionPrompt`, `AnsiConsole.Status()`, `AnsiConsole.Confirm()`; colorized with `[green]✓[/]`, `[red]✗[/]`, `[yellow]⚠[/]` |
| 4.4 | Add authentication prompt to InteractiveSetupUI | `Hugh/Setup/UserInterface/InteractiveSetupUI.cs` | `PromptForAuthenticationAsync` offers "Open browser" and "Skip" options; "Open browser" executes `gh auth login --web` |

---

### Phase 5: Setup Orchestrator

**Estimated Session Size:** Medium
**Files to Modify:** 4
**Prerequisites:** Phases 2, 3, 4 complete
**Entry Point:** Start by reading `Hugh/Setup/Detection/IPrerequisiteDetector.cs`
**Verification:** Orchestrator runs all detectors in order; `--fix` triggers installation flow

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 5.1 | Create SetupOptions and SetupResult types | `Hugh/Setup/SetupResult.cs` | `SetupOptions` record with `Fix`, `NonInteractive`, `AgentsPath`, `WorkspacePath`; `SetupResult` with `AllSatisfied`, `Statuses`, `InstallationResults` |
| 5.2 | Create ISetupOrchestrator interface | `Hugh/Setup/ISetupOrchestrator.cs` | Interface with `RunAsync(SetupOptions, CancellationToken)` returning `Task<SetupResult>` |
| 5.3 | Implement SetupOrchestrator | `Hugh/Setup/SetupOrchestrator.cs` | Constructor takes `IEnumerable<IPrerequisiteDetector>`, `IInstallerRegistry`, `ISetupUserInterface`; implements workflow from Technical Design |
| 5.4 | Add authentication handling to orchestrator | `Hugh/Setup/SetupOrchestrator.cs` | When `GitHubAuthDetector` returns `Missing` and `--fix` enabled: call `PromptForAuthenticationAsync()`; if Browser selected, run `gh auth login --web` via `Process.Start` |
| 5.5 | Add re-verification after installation | `Hugh/Setup/SetupOrchestrator.cs` | After each successful installation, re-run the detector to confirm `Satisfied`; update status in results |

---

### Phase 6: CLI Integration & Cleanup

**Estimated Session Size:** Small
**Files to Modify:** 4
**Prerequisites:** Phase 5 complete
**Entry Point:** Start by reading `Hugh/Program.cs`
**Verification:** `hugh doctor` command works; `hugh --check` still works; `PrerequisiteChecker.cs` deleted

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 6.1 | Add `doctor` subcommand to CLI | `Hugh/Program.cs` | New `doctor` command with `--fix` and `--no-interactive` options per Technical Design; returns exit code 0 if all satisfied, 1 if issues remain |
| 6.2 | Wire up DI for Setup services | `Hugh/Program.cs` | Create and register all detectors, installers, UI, orchestrator in `BuildOrchestrator` or equivalent |
| 6.3 | Update `--check` to use new orchestrator | `Hugh/Program.cs` | `--check` handler calls `orchestrator.RunAsync(new SetupOptions { Fix = false })`; returns exit code 0 if all satisfied, 1 if issues remain |
| 6.4 | Delete PrerequisiteChecker | `Hugh/PrerequisiteChecker.cs` | File deleted; build succeeds with no references to deleted class |

---

### Phase 7: Testing

**Estimated Session Size:** Medium
**Files to Modify:** 6
**Prerequisites:** Phase 6 complete
**Entry Point:** Start by reading `Hugh.Tests/Hugh.Tests.csproj`
**Verification:** All new tests pass; existing tests still pass

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 7.1 | Add Spectre.Console test dependency | `Hugh.Tests/Hugh.Tests.csproj` | Package reference added if needed for test infrastructure |
| 7.2 | Create detector unit tests | `Hugh.Tests/Setup/CopilotCliDetectorTests.cs`, `Hugh.Tests/Setup/GitHubCliDetectorTests.cs` | Tests for `DetectAsync()` with mocked process execution; verify correct status returned |
| 7.3 | Create GitHubAuthDetectorTests | `Hugh.Tests/Setup/GitHubAuthDetectorTests.cs` | Tests for env var detection and `gh auth status` parsing |
| 7.4 | Create InstallerRegistryTests | `Hugh.Tests/Setup/InstallerRegistryTests.cs` | Tests for auto-selection logic; first available installer returned |
| 7.5 | Create SetupOrchestratorTests | `Hugh.Tests/Setup/SetupOrchestratorTests.cs` | Tests for workflow: detection → prompt → install → verify cycle; mock all dependencies |
| 7.6 | Create integration test | `Hugh.Tests/Setup/SetupIntegrationTests.cs` | End-to-end test with `NonInteractiveSetupUI`; verifies actual prerequisite detection on test machine |

---

## Implementation Notes

### For AI Agents

- Follow existing patterns in `Hugh/Agents/` for interface definitions
- Use `async/await` consistently throughout
- All public types must have XML documentation comments
- Test file naming: `{ClassName}Tests.cs` in `Hugh.Tests/`
- Process execution pattern: See `PrerequisiteChecker.ValidateCopilotCliAsync()` for reference (before deletion)
- Namespace: Use `Hugh.Setup`, `Hugh.Setup.Detection`, etc.
- DI Pattern: Constructor injection with interfaces; no static classes

### Key Files to Reference

| Existing File | Pattern to Follow |
|---------------|-------------------|
| `Hugh/Agents/ICopilotService.cs` | Interface structure with XML docs |
| `Hugh/Agents/CopilotService.cs` | Service implementation with async/await |
| `Hugh/Agents/IAgentRegistry.cs` | Registry pattern |
| `Hugh/ConsoleOutput.cs` | Console styling (colors, icons) for NonInteractiveSetupUI |
| `Hugh/PrerequisiteChecker.cs` | Process execution for CLI version checks (migrate before delete) |

### Spectre.Console Usage Notes

```csharp
// Selection prompt pattern
var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Would you like to install [green]GitHub Copilot CLI[/]?")
        .AddChoices("Install with winget", "Show manual instructions", "Skip"));

// Status spinner pattern
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Installing...", async ctx => { /* work */ });

// Confirmation pattern
if (AnsiConsole.Confirm("Open browser to authenticate?"))
{
    Process.Start(new ProcessStartInfo("gh", "auth login --web") { UseShellExecute = true });
}
```

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** 2026-02-05
**Reviewed By:** pch-plan-reviewer
**Review Date:** 2026-02-05
**Status:** ✅ Ready for Implementation
**Next Agent:** pch-coder
**Plan Location:** /docs/plans/011-hugh-prerequisite-detection-installation.md

---

## Review Summary

**Review Date:** 2026-02-05
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.2

### Review Metrics
- Issues Found: 6 (Critical: 0, Major: 4, Minor: 2)
- Clarifying Questions Asked: 5 (1 auto-verified)
- Sections Updated: Phase 1, Phase 2, Phase 3, Phase 6, Technical Design

### Key Improvements Made
1. Added `IProcessRunner` abstraction for testability - CLI detectors and WingetInstaller can now be unit tested with mocked process results
2. Added `Instructions` property to `InstallationResult` - ManualInstaller returns instructions instead of calling UI directly, improving separation of concerns
3. Added exit code requirements to `doctor` and `--check` commands (0=success, 1=issues) for scripting support
4. Added Copilot CLI `/login` as fallback authentication path when gh CLI is unavailable
5. Verified winget package IDs (`GitHub.Copilot`, `GitHub.cli`) against winget source

### Remaining Considerations
- Framework mismatch (net8.0/net9.0) was reviewed and deemed acceptable - .NET 9 test project can reference .NET 8 library
- Phase 1 now has 6 tasks (was 4) due to IProcessRunner additions
- Implementers should register `ProcessRunner` as singleton in DI container

### Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | ~20 new files across Setup subsystem |
| New patterns introduced | 3 | Spectre.Console, IProcessRunner, Strategy pattern |
| External dependencies | 2 | Spectre.Console, winget CLI |
| Migration complexity | 2 | Replace PrerequisiteChecker; clean break |
| Test coverage required | 3 | Unit + Integration tests; mock IProcessRunner |
| **Overall Complexity** | 13/25 | **Medium** |

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**
