# Research: Hugh Prerequisite Detection and Installation Guidance

## Document Information

| Field | Value |
|-------|-------|
| Document | 011-hugh-prerequisite-detection-installation.md |
| Created | 2026-02-05 |
| Status | ✅ Complete |
| Current Phase | 5 of 5 (Complete) |

## Overview

This research investigates how Hugh can provide a seamless prerequisite detection and installation experience, transforming blocking errors into guided resolution workflows. The goal is to enable end-users to set up Hugh with minimal friction, whether they're technical developers or less experienced users.

### Key Findings Summary

1. **Current Detection is Minimal** — PrerequisiteChecker validates only 3 things (agents folder, orchestrator folder, Copilot CLI) and is a static class with no DI, making it hard to test and extend. Critical detection gaps include GitHub authentication status and gh CLI availability.

2. **Installation Can Be Fully Automated** — Both winget and npm support silent installation (`--silent --accept-source-agreements`) without requiring admin elevation (per-user scope). PATH updates require terminal restart.

3. **Spectre.Console Enables Rich UX** — Not currently installed but provides `SelectionPrompt`, `ConfirmationPrompt`, `TextPrompt` with secret masking, and progress displays. Enables CLI wizard patterns matching gh/az/npm.

4. **OAuth Device Flow is Standard for Auth** — Both `gh auth login` and Copilot CLI `/login` use RFC 8628 device flow. Best approach is delegating to `gh auth login --web` rather than reimplementing. GH_TOKEN/GITHUB_TOKEN enable headless auth.

5. **Proposed Architecture Follows Hugh Patterns** — Strategy pattern for installers (IInstaller), detector interfaces (IPrerequisiteDetector), UI abstraction (ISetupUserInterface), and orchestration (ISetupOrchestrator) align with existing ICopilotService/IAgentRegistry patterns.

### Cross-Cutting Patterns

- **Strategy Pattern for Extensibility** — Installers (winget, npm, brew) implement a common IInstaller interface, allowing easy addition of new package managers without modifying orchestration code.

- **UI Abstraction for Testability** — ISetupUserInterface separates prompt logic from console rendering, enabling both interactive (Spectre.Console) and non-interactive (CI/CD) modes with the same orchestration code.

- **Phased Detection with Dependencies** — Authentication should be checked after CLI installation (authentication depends on CLI being available). Priority-based ordering in IPrerequisiteDetector handles this.

- **Graceful Degradation** — If preferred installer unavailable, fall back to alternatives. If all installers fail, show manual instructions. Continue past non-required prerequisites.

### Actionable Conclusions

1. **Add Spectre.Console package** — Required for interactive prompts and progress displays.

2. **Create Setup subsystem** — New `/Hugh/Setup/` directory with Detection/, Installation/, UserInterface/ subdirectories.

3. **Implement two commands** — `hugh setup` (interactive wizard) and `hugh doctor` (check and report).

4. **Deprecate PrerequisiteChecker** — Keep for backward compatibility via `--check`, but mark as obsolete.

5. **Prioritize Windows/winget** — Primary platform; macOS/Linux via npm or Homebrew as secondary.

6. **Handle auth separately** — Authentication is not an installation; prompt for browser OAuth or token input after CLI is installed.

### Open Questions

- **OAuth App Registration** — If implementing native device flow (instead of delegating to `gh`), Hugh needs a registered GitHub OAuth App with client_id.

- **Token Storage** — When user provides PAT via prompt, should Hugh persist it to a config file or only set for current session?

- **Telemetry** — Should setup/doctor commands report anonymous usage metrics for troubleshooting common issues?

---

## Research Objectives

Investigate how Hugh can provide a seamless setup experience by detecting missing prerequisites and guiding end-users through their installation. The goal is to transform prerequisite failures from blocking errors into guided resolution workflows.

### Key Questions to Answer

1. What prerequisites does Hugh currently detect, and what gaps exist in detection coverage?
2. What installation automation approaches exist for each prerequisite (winget, npm, PowerShell scripts)?
3. How can Hugh guide users through multi-step prerequisites (e.g., authentication after CLI installation)?
4. What patterns exist in CLIs for interactive prerequisite setup wizards?
5. What cross-platform considerations affect prerequisite detection and installation?

---

## Research Phases

| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 1 | Current Prerequisite Detection Analysis | ✅ Complete | 2026-02-05 |
| 2 | Prerequisite Installation Methods | ✅ Complete | 2026-02-05 |
| 3 | Interactive Setup Wizard Patterns | ✅ Complete | 2026-02-05 |
| 4 | Authentication Flow Integration | ✅ Complete | 2026-02-05 |
| 5 | Implementation Architecture | ✅ Complete | 2026-02-05 |

---

## Phase 1: Current Prerequisite Detection Analysis

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-05
**Scope:** 
- Analyze existing `PrerequisiteChecker.cs` implementation
- Identify all prerequisites Hugh currently checks for
- Document detection gaps (e.g., gh CLI vs Copilot CLI, authentication status)
- Review error messages and hints currently displayed
- Examine `--check` command functionality

### Key Discoveries

- PrerequisiteChecker validates only 3 things: agents folder, orchestrator folder, and Copilot CLI availability
- Authentication status is acknowledged as undetectable in README ("Hugh cannot detect GitHub authentication")
- Test script has more comprehensive checks (gh CLI, auth status, Copilot extension) than PrerequisiteChecker
- Missing agents produces WARNING (non-blocking), not ERROR (blocking)
- No interface or DI — static class with hard-coded checks
- Hints mention authentication but system doesn't verify it
- No configuration for prerequisite detection behavior

### Findings

#### Current Prerequisite Detection Implementation

Located at [PrerequisiteChecker.cs](Hugh/PrerequisiteChecker.cs), this static class validates prerequisites via `ValidateAllAsync()`:

```csharp
public static async Task<bool> ValidateAllAsync(string agentsPath, string workspacePath)
{
    // 1. Check agents folder exists
    if (!await ValidateAgentsFolderAsync(agentsPath))
    
    // 2. Check orchestrator folder can be created/written
    if (!ValidateOrchestratorFolder(workspacePath))
    
    // 3. Check Copilot CLI (required for SDK)
    if (!await ValidateCopilotCliAsync())
}
```

#### Prerequisites Currently Detected

| Prerequisite | Detection Method | Validation |
|-------------|------------------|------------|
| **Agents Folder** | `Directory.Exists()` | Path exists, contains `pch-*.md` files |
| **Required Agents** | File enumeration | Checks for 5 core agents (visionary, researcher, planner, plan-reviewer, coder) |
| **Orchestrator Folder** | Create + test write | Creates `.orchestrator`, writes test file |
| **Copilot CLI** | `Process.Start("copilot", "--version")` | CLI is in PATH and returns exit code 0 |

#### Agents Folder Validation Details

```csharp
private static async Task<bool> ValidateAgentsFolderAsync(string agentsPath)
{
    // Check directory exists
    if (!Directory.Exists(fullPath)) { /* ERROR */ }
    
    // Check for any pch-*.md files
    var agentFiles = Directory.GetFiles(fullPath, "pch-*.md");
    if (agentFiles.Length == 0) { /* ERROR */ }
    
    // Check for specific required agents (WARNING, not blocking)
    var requiredAgents = new[] { "pch-visionary", "pch-researcher", "pch-planner", 
                                  "pch-plan-reviewer", "pch-coder" };
    var missingAgents = requiredAgents.Where(a => !foundAgents.Contains(a));
    if (missingAgents.Count > 0) { /* WARNING only */ }
}
```

**Key Observation:** Missing required agents produces a **warning**, not a blocking error.

#### Copilot CLI Validation Details

```csharp
private static async Task<bool> ValidateCopilotCliAsync()
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "copilot",
        Arguments = "--version",
        RedirectStandardOutput = true,
        UseShellExecute = false
    };
    // Returns true if exit code == 0
}
```

**Key Observation:** Only checks if `copilot` command exists — no authentication verification.

#### Error Messages and Hints Analysis

Hugh uses `ConsoleOutput` with visual indicators:
- ✓ (green) — Success
- ✗ (red) — Error  
- ⚠ (yellow) — Warning
- ℹ (blue) — Info
- `WriteHint()` — Plain text guidance

| Failure Condition | Error Message | Current Hints |
|------------------|---------------|---------------|
| Agents folder missing | "Agents folder not found: {path}" | "Create the folder and add PCH agent .md files" |
| No agent files | "No PCH agent files found in: {path}" | "Add agent files with naming pattern: pch-*.md" |
| Orchestrator not writable | "Cannot write to orchestrator folder: {path}" | "{exception message}", "Ensure write permissions" |
| Copilot CLI missing | "GitHub Copilot CLI not found" | 3 hints provided (see below) |

#### Copilot CLI Hints (Current)

```csharp
ConsoleOutput.WriteHint("The SDK requires the Copilot CLI to be installed and in PATH");
ConsoleOutput.WriteHint("Install with: winget install GitHub.Copilot");
ConsoleOutput.WriteHint("Or: npm install -g @github/copilot");
ConsoleOutput.WriteHint("Then authenticate with: copilot (and use /login)");
```

**Issues Identified:**
1. Authentication is mentioned but not verified
2. No hint about `gh` CLI as an alternative/requirement
3. No hint about Copilot subscription requirement

#### Detection Gaps Analysis

##### Critical Gap: No Authentication Status Detection

**Documented Issue** (from README.md):
> "Hugh cannot detect GitHub authentication."

The README acknowledges this limitation explicitly. Users must verify authentication manually.

##### Gaps Compared to Test Script

The test script (test-global-install.ps1) performs MORE comprehensive checks than `PrerequisiteChecker`:

| Check | PrerequisiteChecker | Test Script |
|-------|---------------------|-------------|
| Copilot CLI exists | ✅ | ✅ |
| `gh` CLI installed | ❌ | ✅ |
| `gh auth status` | ❌ | ✅ |
| Copilot extension installed | ❌ | ✅ |

##### Complete Detection Gap Inventory

| Missing Check | Impact | Difficulty |
|---------------|--------|------------|
| `gh` CLI availability | Silent failure if gh not installed | Low |
| `gh auth status` | Authentication errors at runtime | Medium |
| Copilot extension (`gh extension list`) | Copilot commands fail | Medium |
| Copilot CLI authentication | 401 errors during execution | Medium |
| GH_TOKEN/GITHUB_TOKEN env var | Alternative auth method | Low |
| Copilot subscription status | Access denied at runtime | High (requires API call) |

#### Architecture Observations

- **Static Class Design:** No dependency injection, no interface for testing, no configuration for which checks to run
- **Console Output Coupling:** Output directly coupled to `ConsoleOutput` — cannot capture results programmatically

### Relevant Files/Resources

- `Hugh/PrerequisiteChecker.cs` — Main prerequisite validation implementation (152 lines)
- `Hugh/Program.cs` — --check command handler (lines 88-135)
- `Hugh/ConsoleOutput.cs` — Output formatting with WriteSuccess/WriteError/WriteHint methods
- `Hugh/scripts/test-global-install.ps1` — More comprehensive test checks (lines 180-240)
- `Hugh/README.md` — Documents prerequisites and known limitations
- `Hugh/appsettings.json` — Configuration for paths used by checker

---

## Phase 2: Prerequisite Installation Methods

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-05
**Scope:**
- Research winget installation for GitHub Copilot CLI
- Research npm installation alternatives
- Investigate GitHub CLI (gh) installation methods
- Document installation commands for each prerequisite
- Explore silent/automated installation options
- Consider elevation requirements (admin vs user-level)

### Findings

#### Major Update: GitHub Copilot CLI Transition

**CRITICAL DISCOVERY:** The `gh-copilot` extension (installed via `gh extension install github/gh-copilot`) was **archived and deprecated on October 30, 2025**. It has been replaced by the standalone **GitHub Copilot CLI** (`copilot` command), which is a completely separate installation.

This transition affects Hugh's prerequisite approach:
- **Old approach:** Install `gh` CLI → authenticate → install `gh-copilot` extension → use `gh copilot` commands
- **New approach:** Install GitHub Copilot CLI directly → authenticate with `/login` → use `copilot` commands

Hugh's current implementation already uses the new standalone Copilot CLI (detects `copilot --version`), which is correct.

---

#### 1. GitHub Copilot CLI Installation Methods

The Copilot CLI is the primary prerequisite Hugh requires for SDK integration.

##### Windows Installation (WinGet - Recommended)

```powershell
# Standard installation
winget install GitHub.Copilot

# Silent/automated installation (no user interaction)
winget install GitHub.Copilot --silent --accept-source-agreements --accept-package-agreements

# Prerelease version
winget install GitHub.Copilot.Prerelease --silent --accept-source-agreements --accept-package-agreements
```

**Elevation Requirements:**
- **Default (per-user):** No admin required — installs to `%LOCALAPPDATA%\Programs\`
- **Machine-wide:** Requires admin — installs to `%ProgramFiles%\`

**Scope Selection:**
```powershell
# Per-user installation (recommended, no admin)
winget install GitHub.Copilot --scope user --silent --accept-source-agreements --accept-package-agreements

# Machine-wide installation (requires admin)
winget install GitHub.Copilot --scope machine --silent --accept-source-agreements --accept-package-agreements
```

**Verification:**
```powershell
copilot --version
```

##### npm Installation (Cross-Platform)

```bash
# Standard installation
npm install -g @github/copilot

# Prerelease version
npm install -g @github/copilot@prerelease

# Specific version
npm install -g @github/copilot@0.0.405
```

**Elevation Requirements:**
- **npm with nvm or user prefix:** No admin required
- **Global npm without nvm:** May require admin on Windows

**Silent Installation Notes:**
- npm is inherently non-interactive when run without a TTY
- Use `--loglevel silent` or `--loglevel error` to reduce output:
  ```bash
  npm install -g @github/copilot --loglevel silent
  ```

##### Homebrew Installation (macOS/Linux)

```bash
# Standard installation
brew install copilot-cli

# Prerelease version
brew install copilot-cli@prerelease
```

**Elevation Requirements:** No admin required (Homebrew uses user-space)

##### Install Script (macOS/Linux)

```bash
# Standard (user-space installation)
curl -fsSL https://gh.io/copilot-install | bash

# System-wide (requires sudo)
curl -fsSL https://gh.io/copilot-install | sudo bash

# Specific version to custom directory
curl -fsSL https://gh.io/copilot-install | VERSION="v0.0.405" PREFIX="$HOME/custom" bash
```

---

#### 2. GitHub CLI (gh) Installation Methods

While Hugh now uses the standalone Copilot CLI, `gh` CLI may still be needed for authentication or additional GitHub operations.

##### Windows Installation (WinGet - Recommended)

```powershell
# Standard installation
winget install --id GitHub.cli

# Silent/automated installation
winget install --id GitHub.cli --silent --accept-source-agreements --accept-package-agreements

# With exact ID matching
winget install --id GitHub.cli -e --silent --accept-source-agreements --accept-package-agreements
```

**Alternative Package Managers:**

```powershell
# Chocolatey
choco install gh

# Scoop
scoop install gh
```

##### macOS Installation (Homebrew)

```bash
brew install gh
```

##### Linux Installation

**Debian/Ubuntu:**
```bash
(type -p wget >/dev/null || (sudo apt update && sudo apt-get install wget -y)) \
&& sudo mkdir -p -m 755 /etc/apt/keyrings \
&& wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
&& sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
&& echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
&& sudo apt update \
&& sudo apt install gh -y
```

**Fedora/RHEL/CentOS:**
```bash
sudo dnf install 'dnf-command(config-manager)'
sudo dnf config-manager --add-repo https://cli.github.com/packages/rpm/gh-cli.repo
sudo dnf install gh
```

---

#### 3. Authentication Methods

##### Copilot CLI Authentication

**Interactive (Recommended for first-time setup):**
```bash
copilot
# Then type: /login
# Follow browser-based OAuth flow
```

**Environment Variable (Headless/Automation):**
```powershell
# Set GH_TOKEN with a fine-grained PAT
$env:GH_TOKEN = "ghp_your_token_here"

# Or GITHUB_TOKEN
$env:GITHUB_TOKEN = "ghp_your_token_here"
```

**PAT Requirements for Copilot CLI:**
- Fine-grained PAT with **"Copilot Requests"** permission enabled
- Create at: https://github.com/settings/personal-access-tokens/new

##### GitHub CLI Authentication

**Interactive (Browser-based):**
```bash
gh auth login --web
```

**Non-Interactive (Token from stdin):**
```bash
echo $MY_GITHUB_TOKEN | gh auth login --with-token
```

**From file:**
```bash
gh auth login --with-token < ~/.secrets/github-token.txt
```

**Verify Authentication:**
```bash
gh auth status
```

---

#### 4. Silent/Automated Installation Summary

| Prerequisite | Windows (winget) | npm | Needs Admin? |
|-------------|------------------|-----|--------------|
| **Copilot CLI** | `winget install GitHub.Copilot --silent --accept-source-agreements --accept-package-agreements` | `npm install -g @github/copilot --loglevel silent` | No (user scope) |
| **GitHub CLI** | `winget install --id GitHub.cli --silent --accept-source-agreements --accept-package-agreements` | N/A | No (user scope) |

##### Key winget Silent Options

| Option | Purpose |
|--------|---------|
| `--silent` | Suppress installer UI completely |
| `--accept-source-agreements` | Auto-accept source license agreements |
| `--accept-package-agreements` | Auto-accept package license agreements |
| `--scope user` | Install per-user (no admin) |
| `--disable-interactivity` | Prevent all interactive prompts |

---

#### 5. Post-Installation Requirements

##### PATH Updates

**Windows (winget):**
- PATH is automatically updated for the current user
- **Important:** Existing terminal sessions must be restarted to pick up PATH changes
- New terminals inherit the updated PATH immediately

**Notification to Reload:**
```powershell
# After installation, notify user:
Write-Host "⚠️ Please restart your terminal or open a new terminal window."
Write-Host "The PATH has been updated but existing sessions won't see the change."
```

**Programmatic PATH Refresh (PowerShell):**
```powershell
# Refresh PATH in current session
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
```

##### Authentication After Installation

The installation only puts the CLI on the system — authentication is a separate step:

1. **Copilot CLI:** Requires running `copilot` then `/login`, OR setting `GH_TOKEN`/`GITHUB_TOKEN`
2. **gh CLI:** Requires running `gh auth login` OR setting `GH_TOKEN`

---

#### 6. Cross-Platform Installation Matrix

| Platform | Copilot CLI | GitHub CLI | Package Manager |
|----------|-------------|------------|-----------------|
| **Windows** | `winget install GitHub.Copilot` | `winget install GitHub.cli` | winget (built-in) |
| **Windows (alt)** | `npm install -g @github/copilot` | `choco install gh` | npm, Chocolatey |
| **macOS** | `brew install copilot-cli` | `brew install gh` | Homebrew |
| **Linux (Debian)** | `npm install -g @github/copilot` or install script | `apt install gh` (after repo setup) | apt, npm |
| **Linux (RHEL)** | `npm install -g @github/copilot` or install script | `dnf install gh` (after repo setup) | dnf, npm |

---

#### 7. Checking If Prerequisites Are Already Installed

Hugh can use these commands to detect existing installations:

```powershell
# Check Copilot CLI
$copilotVersion = & copilot --version 2>&1
if ($LASTEXITCODE -eq 0) { "Copilot CLI installed: $copilotVersion" }

# Check GitHub CLI  
$ghVersion = & gh --version 2>&1
if ($LASTEXITCODE -eq 0) { "GitHub CLI installed: $($ghVersion[0])" }

# Check gh authentication
$authStatus = & gh auth status 2>&1
if ($LASTEXITCODE -eq 0) { "GitHub CLI authenticated" }

# Check GH_TOKEN/GITHUB_TOKEN
if ($env:GH_TOKEN -or $env:GITHUB_TOKEN) { "Token environment variable set" }
```

---

#### 8. Installation Error Handling

| Scenario | Detection | Resolution |
|----------|-----------|------------|
| winget not available | `Get-Command winget -ErrorAction SilentlyContinue` returns null | Fall back to npm or direct download |
| npm not available | `Get-Command npm -ErrorAction SilentlyContinue` returns null | Suggest installing Node.js first |
| No package manager | Neither winget nor npm available | Provide direct download link |
| Installation blocked by policy | winget returns specific exit codes | Suggest IT/admin assistance |
| Network failure | winget/npm timeout or connection errors | Suggest offline installer or retry |

### Key Discoveries

- **gh-copilot extension is DEPRECATED** (Oct 2025) — replaced by standalone GitHub Copilot CLI; Hugh's current detection approach (`copilot --version`) is correct
- **winget supports fully silent installation** via `--silent --accept-source-agreements --accept-package-agreements` — no user interaction required
- **Per-user installation requires NO admin elevation** — both winget and npm can install to user-level directories
- **PATH updates require terminal restart** — cannot be automatically applied to running sessions without explicit refresh
- **Authentication is separate from installation** — CLI installation doesn't authenticate; requires `/login` or environment tokens
- **GH_TOKEN/GITHUB_TOKEN environment variables** enable headless authentication for automation scenarios
- **Copilot CLI requires "Copilot Requests" PAT permission** for token-based authentication

### Relevant Files/Resources

- [PrerequisiteChecker.cs](Hugh/PrerequisiteChecker.cs) — Current prerequisite detection logic with installation hints
- [test-global-install.ps1](Hugh/scripts/test-global-install.ps1) — Test script with comprehensive prerequisite checks (gh CLI, auth status, extensions)
- [Hugh/README.md](Hugh/README.md) — User documentation with installation instructions and troubleshooting
- [installer/README.md](installer/README.md) — Windows installer documentation with silent installation examples

### External Sources Consulted

- https://github.com/github/copilot-cli — Official Copilot CLI repository with installation instructions
- https://docs.github.com/en/copilot/concepts/agents/about-copilot-cli — GitHub Copilot CLI documentation
- https://github.com/cli/cli/blob/trunk/docs/install_windows.md — GitHub CLI Windows installation docs
- https://cli.github.com/manual/gh_auth_login — gh auth login command reference
- https://learn.microsoft.com/en-us/windows/package-manager/winget/install — winget install command reference
- https://github.com/github/gh-copilot — Deprecated gh-copilot extension (archived Oct 2025)

---

## Phase 3: Interactive Setup Wizard Patterns

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-05
**Scope:**
- Research CLI patterns for guided setup experiences (e.g., `gh auth login`, `npm init`, `dotnet new`)
- Analyze Spectre.Console capabilities for interactive prompts
- Study error-to-resolution workflows in popular CLIs
- Explore "fix it for me" automation patterns
- Consider progress indicators for multi-step installations

### Findings

#### 1. Hugh's Current Console Implementation

Hugh currently uses a custom `ConsoleOutput` static class ([ConsoleOutput.cs](Hugh/ConsoleOutput.cs)) that provides:

- **Status Icons:** ✓ (success), ✗ (error), ⚠ (warning), ℹ (info)
- **Colored Output:** Via `WriteColored()` using `Console.ForegroundColor`
- **Section Headers:** Box-drawing characters for visual separation
- **Progress Bars:** Basic `WriteProgressBar()` with filled/empty blocks
- **Phase Status:** Emoji-based status display (✅, 🔄, ❌, ⏸️, ⏳)

**Current Limitations:**
- **No Interactive Input:** The class only writes output, never reads input
- **No User Prompts:** Cannot ask yes/no questions or present selection menus
- **No Spectre.Console:** Hugh uses raw `Console.Write/WriteLine`, not Spectre.Console
- **Static Design:** Hard to test and extend

**Current Package Dependencies (Hugh.csproj):**
- `System.CommandLine` v2.0.0-beta4 for CLI parsing
- Serilog for logging
- Microsoft.Extensions.Configuration for config
- GitHub.Copilot.SDK for agent interactions
- **No Spectre.Console package installed**

---

#### 2. Spectre.Console Capabilities Analysis

Spectre.Console is a .NET library providing rich console output and interactive prompts. It would enable Hugh to implement guided setup wizards.

##### 2.1 Core Prompt Types

| Prompt Type | Use Case | Example |
|-------------|----------|---------|
| **SelectionPrompt** | Choose one option from a list (arrow key navigation) | "Which package manager? [winget/npm/manual]" |
| **MultiSelectionPrompt** | Choose multiple options with checkboxes | "Select prerequisites to install: [x] Copilot CLI [ ] gh CLI" |
| **TextPrompt** | Free-form text input with validation | "Enter your GitHub token:" |
| **ConfirmationPrompt** | Yes/No questions | "Install Copilot CLI now?" via `AnsiConsole.Confirm()` |

##### 2.2 SelectionPrompt Features (Ideal for Setup Wizards)

```csharp
// Basic usage
var packageManager = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("How would you like to install the [green]Copilot CLI[/]?")
        .PageSize(5)
        .AddChoices("winget (recommended)", "npm", "Download manually", "Skip"));

// Hierarchical choices for complex flows
var action = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("What would you like to fix?")
        .Mode(SelectionMode.Leaf)
        .AddChoiceGroup("Missing Prerequisites", new[]
        {
            "Install Copilot CLI",
            "Install GitHub CLI"
        })
        .AddChoiceGroup("Authentication", new[]
        {
            "Log in to GitHub",
            "Set up token"
        }));
```

**Key Features:**
- **Arrow Key Navigation:** Users navigate with ↑/↓
- **Search Filtering:** `.EnableSearch()` for long lists
- **WrapAround:** `.WrapAround()` for circular navigation
- **PageSize:** Control visible items at once
- **Hierarchical Groups:** Categorize options with `.AddChoiceGroup()`
- **Custom Rendering:** `.UseConverter()` for complex objects

##### 2.3 TextPrompt Features (For Token Input)

```csharp
// Password/secret input with masking
var token = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter your [green]GitHub token[/]:")
        .Secret()  // Masks input with asterisks
        .Validate(token => 
            token.StartsWith("ghp_") || token.StartsWith("github_pat_"),
            "[red]Token must start with 'ghp_' or 'github_pat_'[/]"));

// Input with default value
var scope = AnsiConsole.Prompt(
    new TextPrompt<string>("Installation scope:")
        .DefaultValue("user")
        .AddChoices("user", "machine")
        .ShowChoices());
```

##### 2.4 Confirmation Prompts

```csharp
// Simple yes/no
if (AnsiConsole.Confirm("Install Copilot CLI now?"))
{
    // Proceed with installation
}

// With default value
var proceed = AnsiConsole.Confirm("Continue with installation?", defaultValue: true);
```

##### 2.5 Progress Display (For Multi-Step Installations)

```csharp
// Progress bars with multiple tasks
await AnsiConsole.Progress()
    .AutoClear(false)
    .HideCompleted(false)
    .Columns(new ProgressColumn[]
    {
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new SpinnerColumn()
    })
    .StartAsync(async ctx =>
    {
        var downloadTask = ctx.AddTask("Downloading Copilot CLI");
        var installTask = ctx.AddTask("Installing", autoStart: false);
        var verifyTask = ctx.AddTask("Verifying installation", autoStart: false);

        // Simulate download
        while (!downloadTask.IsFinished)
        {
            await Task.Delay(50);
            downloadTask.Increment(2);
        }

        installTask.StartTask();
        // ... etc
    });
```

##### 2.6 Status Display (For Indeterminate Operations)

```csharp
// Spinner during unknown-duration operations
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("green"))
    .StartAsync("Installing Copilot CLI...", async ctx =>
    {
        ctx.Status("Running winget install...");
        await RunWingetInstallAsync();
        
        ctx.Status("Refreshing PATH...");
        await RefreshPathAsync();
        
        ctx.Status("Verifying installation...");
        await VerifyCopilotAsync();
    });
```

**Important Threading Note:** Spectre.Console prompts are **NOT thread-safe** and cannot be mixed with progress displays or other interactive components simultaneously.

---

#### 3. CLI Wizard Patterns from Popular Tools

##### 3.1 GitHub CLI (`gh auth login`)

**Pattern:** Multi-step interactive authentication with clear branching

```
? What account do you want to log into? [Use arrows to move, type to filter]
> GitHub.com
  GitHub Enterprise Server

? What is your preferred protocol for Git operations? [Use arrows to move]
> HTTPS
  SSH

? Authenticate Git with your GitHub credentials? (Y/n) 

? How would you like to authenticate GitHub CLI? [Use arrows to move]
> Login with a web browser
  Paste an authentication token

! First copy your one-time code: XXXX-XXXX
Press Enter to open github.com in your browser...
```

**Key Patterns:**
1. **Progressive Disclosure:** Ask one question at a time
2. **Sensible Defaults:** Mark recommended options with `>`
3. **Clear Actions:** Tell user exactly what will happen next
4. **Web Browser Handoff:** Open browser for OAuth, provide fallback code
5. **Status Feedback:** Show progress with `✓` after each step

##### 3.2 npm init

**Pattern:** Sequential prompts with defaults and validation

```
package name: (my-project) 
version: (1.0.0) 
description: 
entry point: (index.js) 
test command: 
git repository: 
keywords: 
author: 
license: (ISC) 
```

**Key Patterns:**
1. **Defaults in Parentheses:** Show what happens if user presses Enter
2. **--yes Flag:** Skip all prompts with `-y` or `--yes`
3. **Minimal Questions:** Only ask essential questions
4. **Inference:** Auto-detect values from existing files (git origin, etc.)

##### 3.3 Azure CLI (`az init`)

**Pattern:** Guided configuration wizard

```
? Your preferred default output format [Use arrows to move]
> json
  jsonc
  table
  tsv

? Would you like to collect anonymous CLI usage data? [Use arrows to move]
> Yes (recommended)
  No
```

**Key Patterns:**
1. **Configuration Mode Selection:** "Interactive" vs "Automation" presets
2. **Explanation Text:** Each option explains why you might choose it
3. **Persist Choices:** Saves to config file for future sessions
4. **Non-Interactive Mode:** Supports `--only-show-errors` for CI/CD

##### 3.4 Azure CLI (`az login`) Subscription Selector

**Pattern:** Post-action selection with numbered list

```
Retrieving subscriptions for the selection...

No    Subscription name                     Subscription ID                           Tenant
----  ------------------------------------  ----------------------------------------  -------
[1]   Development Subscription              00000000-0000-0000-0000-000000000000      Contoso
[2]   Production Subscription               00000000-0000-0000-0000-000000000000      Contoso
[3] * Default Subscription                  00000000-0000-0000-0000-000000000000      Contoso

The default is marked with an *

Select a subscription and tenant (Type a number or Enter for no changes): 
```

**Key Patterns:**
1. **Numbered List:** Type a number to select
2. **Default Marker:** Asterisk `*` shows current/recommended
3. **Enter = Accept Default:** Minimal friction for common case
4. **Table Format:** Clear alignment for scanning

##### 3.5 dotnet new

**Pattern:** Template-based project creation with parameter options

```
dotnet new console -n MyApp
dotnet new webapi --auth Individual --use-program-main
dotnet new --list  # Show available templates
```

**Key Patterns:**
1. **Short Names:** `console`, `webapi`, `classlib` for quick access
2. **Tab Completion:** Autocomplete for template names and options
3. **Dry Run:** `--dry-run` shows what would be created
4. **Template Parameters:** Each template defines its own options

---

#### 4. "Fix It For Me" Automation Patterns

##### 4.1 Problem Detection → Offer Resolution

**Pattern:** Detect issue, explain problem, offer to fix automatically

```
❌ GitHub Copilot CLI not found

The Copilot CLI is required for Hugh to communicate with GitHub Copilot.

? Would you like to install it now? [Use arrows to move]
> Yes, install with winget (recommended)
  Yes, install with npm
  No, I'll install it manually
  Show me the installation instructions
```

**Implementation Approach:**
```csharp
public async Task<FixResult> DetectAndOfferFixAsync(PrerequisiteCheck check)
{
    if (check.IsSatisfied) return FixResult.NotNeeded;
    
    var action = AnsiConsole.Prompt(
        new SelectionPrompt<FixAction>()
            .Title($"[red]✗ {check.Name} not found[/]\n{check.Description}")
            .AddChoices(
                FixAction.InstallWithWinget,
                FixAction.InstallWithNpm,
                FixAction.SkipForNow,
                FixAction.ShowInstructions));
    
    return action switch
    {
        FixAction.InstallWithWinget => await InstallWithWingetAsync(check),
        FixAction.InstallWithNpm => await InstallWithNpmAsync(check),
        FixAction.ShowInstructions => ShowInstructions(check),
        _ => FixResult.Skipped
    };
}
```

##### 4.2 Azure CLI Extension Dynamic Install

**Pattern:** Install missing extension on demand

```
az aks browse -g myResourceGroup -n myAKSCluster

The command requires the extension aks-preview. Do you want to install it now? (Y/n): y
```

**Configuration:**
```
extension.use_dynamic_install = yes_prompt  # Ask before installing
extension.use_dynamic_install = yes_without_prompt  # Auto-install silently
extension.run_after_dynamic_install = true  # Run command after install
```

This pattern exactly matches Hugh's use case — detect missing prerequisite, offer to install, then continue.

##### 4.3 Git Suggestions Pattern

**Pattern:** Suggest corrections for common mistakes

```
git: 'stauts' is not a git command. See 'git --help'.

The most similar command is
    status
```

Hugh could adapt this for common issues:
```
✗ Could not connect to GitHub Copilot

The most likely issues are:
  1. Copilot CLI not authenticated (run: copilot /login)
  2. GH_TOKEN not set (set: $env:GH_TOKEN = "your-token")
  3. No Copilot subscription (visit: github.com/settings/copilot)

? Would you like to troubleshoot? [Y/n]
```

---

#### 5. Progress Indicators for Multi-Step Installation

##### 5.1 Step-by-Step Checklist Pattern

```
Setting up Hugh...

[✓] Checking system requirements
[✓] Detecting package managers
[→] Installing Copilot CLI
    Running: winget install GitHub.Copilot --silent
[ ] Verifying installation
[ ] Checking authentication
[ ] Testing Copilot connection
```

##### 5.2 Spectre Console Progress with Phases

```csharp
await AnsiConsole.Progress()
    .Columns(new ProgressColumn[]
    {
        new SpinnerColumn(),
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new ElapsedTimeColumn()
    })
    .StartAsync(async ctx =>
    {
        var tasks = new Dictionary<string, ProgressTask>
        {
            ["install"] = ctx.AddTask("Installing Copilot CLI"),
            ["refresh"] = ctx.AddTask("Refreshing PATH", autoStart: false),
            ["verify"] = ctx.AddTask("Verifying installation", autoStart: false),
            ["auth"] = ctx.AddTask("Checking authentication", autoStart: false)
        };

        // Sequential execution with visual feedback
        await InstallAsync(tasks["install"]);
        tasks["refresh"].StartTask();
        await RefreshPathAsync(tasks["refresh"]);
        // ... etc
    });
```

##### 5.3 Status Spinner for Indeterminate Steps

```csharp
var version = await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Waiting for installation to complete...", async ctx =>
    {
        // winget install runs in background
        var result = await RunWingetAsync("install GitHub.Copilot --silent");
        
        ctx.Status("Verifying...");
        await Task.Delay(1000);  // Wait for PATH propagation
        
        return await GetCopilotVersionAsync();
    });

AnsiConsole.MarkupLine($"[green]✓[/] Copilot CLI installed: {version}");
```

---

#### 6. Proposed Hugh Setup Wizard Flow

Based on the research, here's a recommended interactive flow for `hugh setup` or `hugh doctor`:

```
╔══════════════════════════════════════════╗
║         Hugh Setup Wizard                ║
╚══════════════════════════════════════════╝

Checking prerequisites...

[✓] Agents folder found (5 agents)
[✓] Workspace is writable

[!] GitHub Copilot CLI not found

? Would you like to install the Copilot CLI now?
> Install with winget (recommended for Windows)
  Install with npm
  Skip for now
  Show manual installation instructions

[Installing...]
⠋ Running: winget install GitHub.Copilot --silent --accept-source-agreements

[✓] Copilot CLI installed (v0.0.405)

[!] Copilot CLI not authenticated

? How would you like to authenticate?
> Open browser to log in (recommended)
  Enter a personal access token
  Set GH_TOKEN environment variable later
  Skip for now

Opening browser for authentication...
? Press Enter when you've completed login in the browser...

[✓] Authentication successful

══════════════════════════════════════════
✓ All prerequisites satisfied!
══════════════════════════════════════════

Hugh is ready to use. Try:
  hugh "Add a dark mode toggle to the settings page"
```

---

#### 7. Implementation Recommendations

##### 7.1 Add Spectre.Console Package

```xml
<PackageReference Include="Spectre.Console" Version="0.49.1" />
```

##### 7.2 Create InteractiveSetupService

```csharp
public interface IInteractiveSetupService
{
    Task<SetupResult> RunSetupWizardAsync(CancellationToken ct);
    Task<FixResult> OfferFixAsync(PrerequisiteFailure failure, CancellationToken ct);
}
```

##### 7.3 Preserve Non-Interactive Mode

Keep ability to run without prompts for CI/CD:
```csharp
if (!Console.IsInputRedirected && !options.NonInteractive)
{
    // Use Spectre.Console prompts
}
else
{
    // Output errors and hints only (current behavior)
}
```

##### 7.4 Consider Console Capability Detection

```csharp
// Detect if terminal supports interactivity
var capabilities = AnsiConsole.Profile.Capabilities;
if (!capabilities.Interactive)
{
    // Fall back to non-interactive mode
}
```

### Key Discoveries

- **Spectre.Console is not currently installed** in Hugh — adding it would enable rich interactive prompts (`SelectionPrompt`, `TextPrompt`, `ConfirmationPrompt`)
- **`AnsiConsole.Confirm()` provides simple yes/no prompts** — ideal for "Install X now?" questions
- **`SelectionPrompt` supports hierarchical groups** — can organize fix options by category (Missing Prerequisites, Authentication, etc.)
- **Progress/Status displays are NOT thread-safe** — cannot run prompts while showing spinners; design wizard as sequential steps
- **CLI wizard patterns converge on common UX:** one question at a time, sensible defaults with Enter-to-accept, progressive disclosure
- **Azure CLI's `extension.use_dynamic_install` pattern** directly maps to Hugh's "fix it for me" use case
- **`gh auth login` uses web browser + device code fallback** — proven pattern for CLI authentication
- **PATH refresh requires terminal restart** — wizard should notify user and optionally spawn new shell

### Relevant Files/Resources

- [ConsoleOutput.cs](Hugh/ConsoleOutput.cs) — Current output implementation to be enhanced/replaced with Spectre.Console
- [PrerequisiteChecker.cs](Hugh/PrerequisiteChecker.cs) — Static checker to be integrated with interactive fix flow
- [Program.cs](Hugh/Program.cs) — CLI entry point where `--check` handler lives
- [Hugh.csproj](Hugh/Hugh.csproj) — Package references showing no current Spectre.Console dependency
- Spectre.Console documentation — https://spectreconsole.net/
- gh auth login manual — https://cli.github.com/manual/gh_auth_login
- az init configuration — https://learn.microsoft.com/en-us/cli/azure/azure-cli-configuration

---

## Phase 4: Authentication Flow Integration

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-05
**Scope:**
- Research how `gh auth login` works programmatically
- Investigate Copilot CLI `/login` command automation
- Analyze credential verification methods (checking auth status)
- Document token scopes required for Copilot access
- Explore OAuth device flow for CLI authentication

### Findings

#### 1. OAuth Device Flow (RFC 8628) — The Foundation

GitHub uses the OAuth 2.0 Device Authorization Grant (RFC 8628) for CLI authentication. This is the pattern both `gh auth login` and Copilot CLI `/login` use.

##### Device Flow Steps

| Step | Endpoint | Action |
|------|----------|--------|
| 1 | `POST https://github.com/login/device/code` | App requests device_code and user_code |
| 2 | User navigates to `github.com/login/device` | User enters the 8-character user_code (e.g., WDJB-MJHT) |
| 3 | `POST https://github.com/login/oauth/access_token` | App polls for access_token until user completes authorization |

##### Step 1 Response Format

```json
{
  "device_code": "3584d83530557fdd1f46af8289938c8ef79f9dc5",
  "user_code": "WDJB-MJHT",
  "verification_uri": "https://github.com/login/device",
  "expires_in": 900,
  "interval": 5
}
```

| Field | Description |
|-------|-------------|
| `device_code` | 40-character code used to verify the device (kept secret) |
| `user_code` | 8-character code displayed to user (hyphenated) |
| `verification_uri` | Where user enters the code |
| `expires_in` | Codes expire after 900 seconds (15 minutes) |
| `interval` | Minimum seconds between polling requests |

##### Step 3 Polling

App polls with:
```
POST https://github.com/login/oauth/access_token
client_id=<client_id>
device_code=<device_code>
grant_type=urn:ietf:params:oauth:grant-type:device_code
```

**Polling responses:**

| Error | Meaning | Action |
|-------|---------|--------|
| `authorization_pending` | User hasn't completed login yet | Keep polling |
| `slow_down` | Polling too fast | Add 5 seconds to interval |
| `expired_token` | Device code expired (15 min) | Restart flow |
| `access_denied` | User clicked cancel | Abort, don't retry |

**Success response:**
```json
{
  "access_token": "gho_16C7e42F292c6912E7710c838347Ae178B4a",
  "token_type": "bearer",
  "scope": "repo,gist"
}
```

##### Device Flow Rate Limits

- 50 submissions per hour for user codes
- Must respect `interval` between polling requests
- `slow_down` error adds 5 seconds to interval

---

#### 2. GitHub CLI (`gh auth login`) Implementation

The GitHub CLI provides a proven implementation of OAuth device flow for CLI authentication.

##### Authentication Options

| Option | Description |
|--------|-------------|
| `--web` (`-w`) | Open browser for authentication |
| `--with-token` | Read token from stdin (non-interactive) |
| `--scopes` (`-s`) | Request additional OAuth scopes |
| `--hostname` (`-h`) | Target GitHub Enterprise Server |
| `--clipboard` (`-c`) | Copy device code to clipboard |

##### Interactive Flow (Default)

```
$ gh auth login

? What account do you want to log into? GitHub.com
? What is your preferred protocol for Git operations? HTTPS
? Authenticate Git with your GitHub credentials? Yes
? How would you like to authenticate? Login with a web browser

! First copy your one-time code: XXXX-XXXX
Press Enter to open github.com in your browser...
```

**Pattern observations:**
- Progressive disclosure (one question at a time)
- Clear default recommendations
- Device code displayed prominently
- Browser opened automatically with fallback

##### Non-Interactive Options

```bash
# Read token from file
gh auth login --with-token < ~/.secrets/github-token.txt

# Read from environment
echo $MY_GITHUB_TOKEN | gh auth login --with-token

# CI/CD environments
export GH_TOKEN="${{ github.token }}"
gh auth status  # Validates token
```

##### Credential Storage

| Platform | Location | Format |
|----------|----------|--------|
| Windows | `%APPDATA%\GitHub CLI\hosts.yml` | YAML |
| macOS | `~/.config/gh/hosts.yml` | YAML |
| Linux | `~/.config/gh/hosts.yml` | YAML |

**hosts.yml structure:**
```yaml
github.com:
    oauth_token: gho_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    user: username
    git_protocol: https
```

---

#### 3. Copilot CLI `/login` Command

The Copilot CLI uses a similar device flow but with a different entry point.

##### Authentication Methods

| Method | Use Case | Command/Setup |
|--------|----------|---------------|
| **Interactive /login** | First-time setup, user-facing | Run `copilot`, then type `/login` |
| **GH_TOKEN env var** | Automation, CI/CD | `$env:GH_TOKEN = "ghp_..."` |
| **GITHUB_TOKEN env var** | Alternative to GH_TOKEN | `$env:GITHUB_TOKEN = "ghp_..."` |

**Precedence order:** `GH_TOKEN` > `GITHUB_TOKEN`

##### Interactive /login Flow

```
$ copilot
Welcome to GitHub Copilot CLI!
Type /help for available commands.

> /login

Opening browser to authenticate...
```

**Key characteristics:**
- Runs within the Copilot CLI REPL, not as a standalone command
- Opens browser for OAuth device flow
- No direct "copilot login" command-line option
- Cannot be fully automated without token

##### Token-Based Authentication (Headless)

For automation, use environment variables instead of `/login`:

```powershell
# PowerShell
$env:GH_TOKEN = "ghp_your_fine_grained_token"

# Or in profile for persistence
Add-Content $PROFILE '$env:GH_TOKEN = "ghp_your_token"'
```

```bash
# Bash
export GH_TOKEN="ghp_your_fine_grained_token"

# Or in .bashrc/.zshrc
echo 'export GH_TOKEN="ghp_your_token"' >> ~/.bashrc
```

**Critical:** The token must be a fine-grained PAT with "Copilot Requests" permission.

---

#### 4. Token Scopes Required for Copilot Access

##### Fine-Grained Personal Access Tokens (Recommended)

For Copilot CLI, use fine-grained PATs with specific permissions:

| Permission | Required | Purpose |
|------------|----------|---------|
| **Copilot Requests** | ✅ Yes | Access to Copilot API |
| Repository access | Depends | Only if Copilot needs repo context |

**Creating a Copilot PAT:**

1. Navigate to: https://github.com/settings/personal-access-tokens/new
2. Select "Fine-grained token"
3. Under "Permissions", click "Add permissions"
4. Select "Copilot Requests" (under Account permissions)
5. Generate token (starts with `github_pat_`)

##### Classic Personal Access Tokens (OAuth Scopes)

For `gh auth login`, minimum scopes required:

| Scope | Purpose |
|-------|---------|
| `repo` | Full access to repositories |
| `read:org` | Read organization membership |
| `gist` | Gist access |

**Note:** Classic PATs (starting with `ghp_`) don't have a specific "Copilot" scope. Copilot access is determined by the user's subscription, not token scopes.

##### OAuth App Scopes

For OAuth apps using device flow, scopes are requested during authorization:

| Pattern | Scope |
|---------|-------|
| No scope | Read-only public access |
| `repo` | Full repository access |
| `user` | User profile access |
| `codespace` | Codespace management |
| `workflow` | GitHub Actions access |

**Copilot access:** Not controlled by OAuth scopes — requires active Copilot subscription.

---

#### 5. Authentication Status Verification

##### Method 1: `gh auth status` (GitHub CLI)

```powershell
$authStatus = & gh auth status 2>&1
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "Authenticated"
} else {
    Write-Host "Not authenticated"
}
```

**Output format (authenticated):**
```
github.com
  ✓ Logged in to github.com account username (keyring)
  - Active account: true
  - Git operations protocol: https
  - Token: gho_************************************
  - Token scopes: 'admin:org', 'gist', 'repo', 'user', 'workflow'
```

**Exit codes:**
- `0` = Authenticated
- `1` = Not authenticated or error

##### Method 2: Check GH_TOKEN/GITHUB_TOKEN Environment Variables

```csharp
public static bool HasTokenEnvironmentVariable()
{
    var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    return !string.IsNullOrEmpty(ghToken) || !string.IsNullOrEmpty(githubToken);
}
```

##### Method 3: Check hosts.yml File Exists

```csharp
public static bool HasGitHubCliCredentials()
{
    string hostsPath;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        hostsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GitHub CLI", "hosts.yml");
    }
    else
    {
        hostsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "gh", "hosts.yml");
    }
    
    return File.Exists(hostsPath);
}
```

##### Method 4: Validate Token with GitHub API

```csharp
public static async Task<bool> ValidateTokenAsync(string token)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Hugh/1.0");
    
    try
    {
        var response = await client.GetAsync("https://api.github.com/user");
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}
```

**Response headers reveal scopes:**
```
X-OAuth-Scopes: repo, user
X-Accepted-OAuth-Scopes: user
```

##### Method 5: Copilot CLI Test Request

```csharp
public static async Task<bool> ValidateCopilotAccessAsync()
{
    try
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "copilot",
            Arguments = @"suggest ""echo hello""",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(startInfo);
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        // Check for auth errors in stderr
        return !stderr.Contains("401") && 
               !stderr.Contains("authentication") &&
               process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}
```

---

#### 6. Implementing Device Flow in Hugh

##### Recommended Approach: Leverage `gh auth login`

Rather than implementing OAuth device flow from scratch, Hugh should leverage the existing `gh` CLI:

```csharp
public async Task<bool> TriggerAuthenticationAsync()
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "gh",
        Arguments = "auth login --web",
        UseShellExecute = true,  // Opens browser
        CreateNoWindow = false
    };
    
    using var process = Process.Start(startInfo);
    await process.WaitForExitAsync();
    return process.ExitCode == 0;
}
```

**Advantages:**
- Proven, maintained implementation
- Handles edge cases (browser fallback, enterprise servers)
- User-familiar flow
- Stores credentials properly

##### Alternative: Direct Device Flow Implementation

If `gh` CLI isn't available, Hugh could implement device flow directly:

```csharp
public class GitHubDeviceFlow
{
    private const string ClientId = "YOUR_OAUTH_APP_CLIENT_ID";
    private static readonly HttpClient _client = new();
    
    public async Task<DeviceCodeResponse> RequestDeviceCodeAsync(string[] scopes)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["scope"] = string.Join(" ", scopes)
        });
        
        var response = await _client.PostAsync(
            "https://github.com/login/device/code", content);
        var json = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<DeviceCodeResponse>(json);
    }
    
    public async Task<TokenResponse?> PollForTokenAsync(
        string deviceCode, int interval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(interval * 1000, ct);
            
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["device_code"] = deviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            });
            
            var response = await _client.PostAsync(
                "https://github.com/login/oauth/access_token", content);
            var result = await response.Content.ReadAsStringAsync();
            
            if (result.Contains("access_token="))
            {
                return ParseTokenResponse(result);
            }
            else if (result.Contains("slow_down"))
            {
                interval += 5;  // Back off
            }
            else if (result.Contains("expired_token") || result.Contains("access_denied"))
            {
                return null;  // User cancelled or timeout
            }
            // authorization_pending: continue polling
        }
        return null;
    }
}
```

**Considerations:**
- Requires registering an OAuth App on GitHub
- Need to handle rate limiting and errors
- Must securely store the resulting token
- More complex than delegating to `gh`

---

#### 7. Proposed Authentication Verification Flow for Hugh

```csharp
public class AuthenticationChecker
{
    public async Task<AuthStatus> CheckAuthenticationAsync()
    {
        // 1. Check environment variables first (fastest)
        if (HasTokenEnvironmentVariable())
        {
            return AuthStatus.TokenEnvironmentVariable;
        }
        
        // 2. Check if gh is available and authenticated
        if (await IsGhCliAuthenticatedAsync())
        {
            return AuthStatus.GitHubCliAuthenticated;
        }
        
        // 3. Check if credentials file exists
        if (HasGitHubCliCredentials())
        {
            return AuthStatus.CredentialsFilePresent;
        }
        
        return AuthStatus.NotAuthenticated;
    }
    
    private async Task<bool> IsGhCliAuthenticatedAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(startInfo);
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;  // gh not installed
        }
    }
}

public enum AuthStatus
{
    NotAuthenticated,
    TokenEnvironmentVariable,
    GitHubCliAuthenticated,
    CredentialsFilePresent
}
```

---

#### 8. Interactive Authentication Wizard Flow

Based on research, Hugh's setup wizard should offer:

```
[!] GitHub authentication required

Copilot CLI requires GitHub authentication to function.

? How would you like to authenticate?
> Open browser to log in (recommended)
  Enter a personal access token
  Set GH_TOKEN environment variable later
  Skip authentication for now

[Opening browser...]
! First copy your one-time code: WDJB-MJHT
Press Enter to open github.com/login/device in your browser...

[Polling for authentication...]
⠋ Waiting for browser authentication...

[✓] Successfully authenticated as @username
```

**Implementation with Spectre.Console:**

```csharp
var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[yellow]GitHub authentication required[/]")
        .AddChoices(
            "Open browser to log in (recommended)",
            "Enter a personal access token",
            "Set GH_TOKEN environment variable later",
            "Skip authentication for now"));

switch (choice)
{
    case "Open browser to log in (recommended)":
        await TriggerBrowserAuthAsync();
        break;
    case "Enter a personal access token":
        var token = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your GitHub token:")
                .Secret()
                .Validate(t => t.StartsWith("ghp_") || t.StartsWith("github_pat_"),
                    "Token must start with 'ghp_' or 'github_pat_'"));
        Environment.SetEnvironmentVariable("GH_TOKEN", token);
        break;
    // ...
}
```

### Key Discoveries

- **OAuth Device Flow (RFC 8628)** is the standard for CLI auth — 3-step process with device_code, user_code, and polling for access_token
- **Copilot CLI requires "Copilot Requests" permission** on fine-grained PATs — standard OAuth scopes don't grant Copilot access
- **`gh auth login` is a proven pattern** — browser-based with device code fallback; Hugh should delegate to it rather than reimplementing
- **GH_TOKEN/GITHUB_TOKEN environment variables** provide headless authentication — checked before interactive login
- **Multiple verification methods exist:** `gh auth status` (exit code), env vars, hosts.yml file existence, API validation
- **Polling interval must be respected** — GitHub enforces rate limits and adds 5 seconds on `slow_down` errors
- **Device codes expire in 15 minutes** — UX should communicate urgency and handle expiration gracefully
- **Credential storage is platform-specific** — Windows uses `%APPDATA%\GitHub CLI\hosts.yml`, Unix uses `~/.config/gh/hosts.yml`

### Relevant Files/Resources

- [PrerequisiteChecker.cs](Hugh/PrerequisiteChecker.cs) — Current validation logic (no auth verification)
- [CopilotService.cs](Hugh/Agents/CopilotService.cs) — Uses default CopilotClient() with no explicit credential configuration
- [test-global-install.ps1](Hugh/scripts/test-global-install.ps1) — Has `gh auth status` check pattern (lines 198-205)
- [README.md](Hugh/README.md) — Documents GH_TOKEN/GITHUB_TOKEN usage and /login instructions

### External Sources Consulted

- https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#device-flow — OAuth Device Flow documentation
- https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/scopes-for-oauth-apps — OAuth scope reference
- https://cli.github.com/manual/gh_auth_login — gh auth login command reference
- https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli — Copilot CLI installation and authentication
- RFC 8628 — OAuth 2.0 Device Authorization Grant specification

---

## Phase 5: Implementation Architecture

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-05
**Scope:**
- Design prerequisite resolver service architecture
- Define installation strategy pattern for pluggable installers
- Plan user interaction flow (detect → prompt → install → verify)
- Consider rollback/cleanup on partial failures
- Document `hugh setup` or `hugh doctor` command approach
- Plan integration with existing PrerequisiteChecker

### Findings

#### 1. Proposed Architecture Overview

The new prerequisite system follows Hugh's existing patterns while introducing proper DI and extensibility:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Program.cs                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ hugh setup  │  │ hugh doctor │  │ hugh <feature> --check  │  │
│  └──────┬──────┘  └──────┬──────┘  └───────────┬─────────────┘  │
│         │                │                      │                │
│         └────────────────┴──────────────────────┘                │
│                          │                                       │
└──────────────────────────┼───────────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SetupOrchestrator                             │
│   Coordinates: detect → prompt → install → verify workflow       │
└────────────────────────────┬────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────┐
│ IPrerequisite   │ │ IInstaller      │ │ ISetupUserInterface     │
│ Detector        │ │ Registry        │ │                         │
│                 │ │                 │ │ ├─ InteractiveSetupUI   │
│ ├─ CopilotCli   │ │ ├─ WingetInst.  │ │ │   (Spectre.Console)   │
│ ├─ GitHubAuth   │ │ ├─ NpmInstaller │ │ └─ NonInteractiveSetupUI│
│ ├─ AgentsFolder │ │ ├─ BrewInstaller│ │     (current behavior)  │
│ └─ Workspace    │ │ └─ ManualInst.  │ │                         │
└─────────────────┘ └─────────────────┘ └─────────────────────────┘
```

---

#### 2. Proposed Directory Structure

```
Hugh/
├── Setup/                           # NEW: Setup subsystem
│   ├── ISetupOrchestrator.cs       # Main coordination interface
│   ├── SetupOrchestrator.cs        # Implementation
│   ├── SetupResult.cs              # Result types
│   │
│   ├── Detection/                   # Prerequisite detectors
│   │   ├── IPrerequisiteDetector.cs
│   │   ├── PrerequisiteStatus.cs
│   │   ├── CopilotCliDetector.cs
│   │   ├── GitHubAuthDetector.cs
│   │   ├── AgentsFolderDetector.cs
│   │   └── WorkspaceDetector.cs
│   │
│   ├── Installation/                # Installers (Strategy pattern)
│   │   ├── IInstaller.cs
│   │   ├── IInstallerRegistry.cs
│   │   ├── InstallerRegistry.cs
│   │   ├── InstallationResult.cs
│   │   ├── WingetInstaller.cs
│   │   ├── NpmInstaller.cs
│   │   ├── BrewInstaller.cs
│   │   └── ManualInstaller.cs      # Displays instructions only
│   │
│   ├── UserInterface/               # UI abstraction
│   │   ├── ISetupUserInterface.cs
│   │   ├── InteractiveSetupUI.cs   # Spectre.Console implementation
│   │   └── NonInteractiveSetupUI.cs
│   │
│   └── SetupConfig.cs              # Configuration for setup behavior
│
├── PrerequisiteChecker.cs          # DEPRECATED: Keep for backward compat
└── ...
```

---

#### 3. Core Interfaces

##### 3.1 IPrerequisiteDetector

```csharp
namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether a specific prerequisite is satisfied.
/// </summary>
public interface IPrerequisiteDetector
{
    /// <summary>
    /// Unique identifier for this prerequisite.
    /// </summary>
    string PrerequisiteId { get; }

    /// <summary>
    /// Human-readable name for this prerequisite.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of what this prerequisite provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Whether this prerequisite is required (vs recommended).
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Order in which to check this prerequisite (lower = earlier).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if the prerequisite is satisfied.
    /// </summary>
    Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default);
}
```

##### 3.2 PrerequisiteStatus

```csharp
namespace Hugh.Setup.Detection;

public record PrerequisiteStatus
{
    public required string PrerequisiteId { get; init; }
    public required bool IsSatisfied { get; init; }
    public required PrerequisiteState State { get; init; }
    public string? Version { get; init; }
    public string? Message { get; init; }
    public string[]? Hints { get; init; }
    public string[]? AvailableInstallers { get; init; }  // e.g., ["winget", "npm"]
}

public enum PrerequisiteState
{
    Satisfied,          // ✓ Installed and working
    NotInstalled,       // ✗ Not found on system
    WrongVersion,       // ⚠ Installed but version incompatible
    NotAuthenticated,   // ⚠ Installed but requires auth
    PartiallyConfigured // ⚠ Needs additional setup
}
```

##### 3.3 IInstaller (Strategy Pattern)

```csharp
namespace Hugh.Setup.Installation;

/// <summary>
/// Strategy interface for installing a prerequisite.
/// </summary>
public interface IInstaller
{
    /// <summary>
    /// Unique identifier (e.g., "winget", "npm", "brew").
    /// </summary>
    string InstallerId { get; }

    /// <summary>
    /// Human-readable name for display.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether this installer is available on the current platform.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Whether this installer requires admin/elevated privileges.
    /// </summary>
    bool RequiresElevation { get; }

    /// <summary>
    /// Installs the specified prerequisite.
    /// </summary>
    Task<InstallationResult> InstallAsync(
        string prerequisiteId,
        InstallOptions options,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Attempts to rollback a failed installation.
    /// </summary>
    Task<bool> RollbackAsync(string prerequisiteId, CancellationToken ct = default);
}

public record InstallOptions
{
    public bool Silent { get; init; } = false;
    public InstallScope Scope { get; init; } = InstallScope.User;
    public string? Version { get; init; }
}

public enum InstallScope
{
    User,    // Per-user installation (no admin)
    Machine  // Machine-wide (may require admin)
}
```

##### 3.4 IInstallerRegistry

```csharp
namespace Hugh.Setup.Installation;

/// <summary>
/// Registry of available installers for each prerequisite.
/// </summary>
public interface IInstallerRegistry
{
    /// <summary>
    /// Gets all available installers for a prerequisite.
    /// </summary>
    IEnumerable<IInstaller> GetInstallersFor(string prerequisiteId);

    /// <summary>
    /// Gets the recommended installer for a prerequisite on the current platform.
    /// </summary>
    IInstaller? GetRecommendedInstaller(string prerequisiteId);

    /// <summary>
    /// Registers an installer for a prerequisite.
    /// </summary>
    void Register(string prerequisiteId, IInstaller installer);
}
```

##### 3.5 ISetupUserInterface

```csharp
namespace Hugh.Setup.UserInterface;

/// <summary>
/// Abstraction for setup wizard user interaction.
/// </summary>
public interface ISetupUserInterface
{
    /// <summary>
    /// Whether this UI supports interactive prompts.
    /// </summary>
    bool IsInteractive { get; }

    /// <summary>
    /// Displays the setup header.
    /// </summary>
    void ShowHeader(string title);

    /// <summary>
    /// Displays a prerequisite check result.
    /// </summary>
    void ShowPrerequisiteStatus(PrerequisiteStatus status);

    /// <summary>
    /// Asks user to choose an installer for a prerequisite.
    /// </summary>
    Task<InstallerChoice> PromptInstallerChoiceAsync(
        string prerequisiteId,
        IEnumerable<IInstaller> availableInstallers,
        IInstaller? recommended,
        CancellationToken ct = default);

    /// <summary>
    /// Asks user for yes/no confirmation.
    /// </summary>
    Task<bool> ConfirmAsync(string message, bool defaultValue = true, CancellationToken ct = default);

    /// <summary>
    /// Shows installation progress.
    /// </summary>
    IDisposable ShowProgress(string message);

    /// <summary>
    /// Prompts for authentication method selection.
    /// </summary>
    Task<AuthMethod> PromptAuthMethodAsync(CancellationToken ct = default);

    /// <summary>
    /// Prompts for a secret value (masked input).
    /// </summary>
    Task<string?> PromptSecretAsync(string prompt, CancellationToken ct = default);

    /// <summary>
    /// Shows a success message.
    /// </summary>
    void ShowSuccess(string message);

    /// <summary>
    /// Shows an error message.
    /// </summary>
    void ShowError(string message, string[]? hints = null);

    /// <summary>
    /// Shows a warning message.
    /// </summary>
    void ShowWarning(string message);

    /// <summary>
    /// Shows final setup summary.
    /// </summary>
    void ShowSummary(SetupResult result);
}

public record InstallerChoice
{
    public IInstaller? SelectedInstaller { get; init; }
    public bool Skip { get; init; }
    public bool ShowManualInstructions { get; init; }
}

public enum AuthMethod
{
    BrowserLogin,
    PersonalAccessToken,
    EnvironmentVariable,
    Skip
}
```

##### 3.6 ISetupOrchestrator

```csharp
namespace Hugh.Setup;

/// <summary>
/// Orchestrates the complete setup workflow.
/// </summary>
public interface ISetupOrchestrator
{
    /// <summary>
    /// Runs the full setup wizard.
    /// </summary>
    Task<SetupResult> RunSetupAsync(
        SetupOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Runs doctor mode (check only, report issues).
    /// </summary>
    Task<SetupResult> RunDoctorAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Checks prerequisites without prompting.
    /// </summary>
    Task<IReadOnlyList<PrerequisiteStatus>> CheckAllAsync(
        CancellationToken ct = default);
}

public record SetupOptions
{
    /// <summary>
    /// If true, run in non-interactive mode (fail on missing prerequisites).
    /// </summary>
    public bool NonInteractive { get; init; } = false;

    /// <summary>
    /// If true, attempt to fix issues automatically without prompting.
    /// </summary>
    public bool AutoFix { get; init; } = false;

    /// <summary>
    /// If true, continue past failures to show all issues.
    /// </summary>
    public bool ContinueOnError { get; init; } = false;
}
```

---

#### 4. Concrete Detector Implementations

##### 4.1 CopilotCliDetector

```csharp
namespace Hugh.Setup.Detection;

public class CopilotCliDetector : IPrerequisiteDetector
{
    public string PrerequisiteId => "copilot-cli";
    public string DisplayName => "GitHub Copilot CLI";
    public string Description => "Required for Copilot SDK integration";
    public bool IsRequired => true;
    public int Priority => 10;

    public async Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "copilot",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return CreateNotInstalledStatus();
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                var version = output.Trim();
                return new PrerequisiteStatus
                {
                    PrerequisiteId = PrerequisiteId,
                    IsSatisfied = true,
                    State = PrerequisiteState.Satisfied,
                    Version = version,
                    Message = $"Copilot CLI installed: {version}"
                };
            }

            return CreateNotInstalledStatus();
        }
        catch
        {
            return CreateNotInstalledStatus();
        }
    }

    private PrerequisiteStatus CreateNotInstalledStatus() => new()
    {
        PrerequisiteId = PrerequisiteId,
        IsSatisfied = false,
        State = PrerequisiteState.NotInstalled,
        Message = "GitHub Copilot CLI not found",
        Hints = new[]
        {
            "The Copilot CLI is required for Hugh to communicate with GitHub Copilot",
            "Install with: winget install GitHub.Copilot",
            "Or: npm install -g @github/copilot"
        },
        AvailableInstallers = new[] { "winget", "npm", "brew" }
    };
}
```

##### 4.2 GitHubAuthDetector

```csharp
namespace Hugh.Setup.Detection;

public class GitHubAuthDetector : IPrerequisiteDetector
{
    public string PrerequisiteId => "github-auth";
    public string DisplayName => "GitHub Authentication";
    public string Description => "Required for Copilot API access";
    public bool IsRequired => true;
    public int Priority => 20;  // Check after CLI installation

    public async Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        // Check 1: Environment variable tokens (fastest, highest precedence)
        var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (!string.IsNullOrEmpty(ghToken) || !string.IsNullOrEmpty(githubToken))
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                IsSatisfied = true,
                State = PrerequisiteState.Satisfied,
                Message = "Using token from environment variable"
            };
        }

        // Check 2: gh auth status (if gh CLI is available)
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync(ct);

                if (process.ExitCode == 0)
                {
                    return new PrerequisiteStatus
                    {
                        PrerequisiteId = PrerequisiteId,
                        IsSatisfied = true,
                        State = PrerequisiteState.Satisfied,
                        Message = "Authenticated via GitHub CLI"
                    };
                }
            }
        }
        catch
        {
            // gh CLI not installed, continue to other checks
        }

        // Check 3: Credential file exists (weak check, file might be stale)
        var credPath = GetCredentialFilePath();
        if (File.Exists(credPath))
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                IsSatisfied = true,
                State = PrerequisiteState.PartiallyConfigured,
                Message = "Credentials file found (validity not verified)"
            };
        }

        return new PrerequisiteStatus
        {
            PrerequisiteId = PrerequisiteId,
            IsSatisfied = false,
            State = PrerequisiteState.NotAuthenticated,
            Message = "GitHub authentication not configured",
            Hints = new[]
            {
                "Authenticate with: copilot (then /login)",
                "Or set environment variable: $env:GH_TOKEN = \"your-token\"",
                "Token requires 'Copilot Requests' permission (fine-grained PAT)"
            },
            AvailableInstallers = new[] { "auth-browser", "auth-token" }
        };
    }

    private static string GetCredentialFilePath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GitHub CLI", "hosts.yml");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "gh", "hosts.yml");
    }
}
```

---

#### 5. Installer Implementations

##### 5.1 WingetInstaller

```csharp
namespace Hugh.Setup.Installation;

public class WingetInstaller : IInstaller
{
    private static readonly Dictionary<string, string> PackageIds = new()
    {
        ["copilot-cli"] = "GitHub.Copilot",
        ["gh-cli"] = "GitHub.cli"
    };

    public string InstallerId => "winget";
    public string DisplayName => "winget (Windows Package Manager)";
    public bool RequiresElevation => false;  // User scope by default

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            await process!.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<InstallationResult> InstallAsync(
        string prerequisiteId,
        InstallOptions options,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!PackageIds.TryGetValue(prerequisiteId, out var packageId))
        {
            return InstallationResult.Failure($"Unknown prerequisite: {prerequisiteId}");
        }

        progress?.Report(new InstallProgress("Starting winget installation...", 0));

        var args = new List<string>
        {
            "install", packageId,
            "--accept-source-agreements",
            "--accept-package-agreements"
        };

        if (options.Silent)
            args.Add("--silent");

        args.Add("--scope");
        args.Add(options.Scope == InstallScope.Machine ? "machine" : "user");

        if (!string.IsNullOrEmpty(options.Version))
        {
            args.Add("--version");
            args.Add(options.Version);
        }

        progress?.Report(new InstallProgress($"Running: winget {string.Join(" ", args)}", 10));

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return InstallationResult.Failure("Failed to start winget process");
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            progress?.Report(new InstallProgress("Installation complete", 100));

            if (process.ExitCode == 0)
            {
                return InstallationResult.Success(
                    $"Successfully installed {packageId}",
                    requiresPathRefresh: true);
            }

            return InstallationResult.Failure($"winget exited with code {process.ExitCode}: {error}");
        }
        catch (Exception ex)
        {
            return InstallationResult.Failure($"Installation failed: {ex.Message}");
        }
    }

    public Task<bool> RollbackAsync(string prerequisiteId, CancellationToken ct = default)
    {
        // winget uninstall is complex; for now, report that rollback isn't supported
        return Task.FromResult(false);
    }
}
```

##### 5.2 NpmInstaller

```csharp
namespace Hugh.Setup.Installation;

public class NpmInstaller : IInstaller
{
    private static readonly Dictionary<string, string> PackageNames = new()
    {
        ["copilot-cli"] = "@github/copilot"
    };

    public string InstallerId => "npm";
    public string DisplayName => "npm (Node Package Manager)";
    public bool RequiresElevation => false;  // Global npm to user prefix

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            await process!.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<InstallationResult> InstallAsync(
        string prerequisiteId,
        InstallOptions options,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!PackageNames.TryGetValue(prerequisiteId, out var packageName))
        {
            return InstallationResult.Failure($"Unknown prerequisite: {prerequisiteId}");
        }

        progress?.Report(new InstallProgress("Starting npm installation...", 0));

        var args = new List<string> { "install", "-g", packageName };

        if (options.Silent)
            args.Add("--loglevel=error");

        if (!string.IsNullOrEmpty(options.Version))
        {
            args[2] = $"{packageName}@{options.Version}";
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            await process!.WaitForExitAsync(ct);

            progress?.Report(new InstallProgress("Installation complete", 100));

            if (process.ExitCode == 0)
            {
                return InstallationResult.Success($"Successfully installed {packageName}");
            }

            var error = await process.StandardError.ReadToEndAsync(ct);
            return InstallationResult.Failure($"npm exited with code {process.ExitCode}: {error}");
        }
        catch (Exception ex)
        {
            return InstallationResult.Failure($"Installation failed: {ex.Message}");
        }
    }

    public async Task<bool> RollbackAsync(string prerequisiteId, CancellationToken ct = default)
    {
        if (!PackageNames.TryGetValue(prerequisiteId, out var packageName))
            return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = $"uninstall -g {packageName}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            await process!.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
```

---

#### 6. SetupOrchestrator Implementation

```csharp
namespace Hugh.Setup;

public class SetupOrchestrator : ISetupOrchestrator
{
    private readonly IEnumerable<IPrerequisiteDetector> _detectors;
    private readonly IInstallerRegistry _installerRegistry;
    private readonly ISetupUserInterface _ui;
    private readonly ILogger<SetupOrchestrator> _logger;

    public SetupOrchestrator(
        IEnumerable<IPrerequisiteDetector> detectors,
        IInstallerRegistry installerRegistry,
        ISetupUserInterface ui,
        ILogger<SetupOrchestrator> logger)
    {
        _detectors = detectors.OrderBy(d => d.Priority);
        _installerRegistry = installerRegistry;
        _ui = ui;
        _logger = logger;
    }

    public async Task<SetupResult> RunSetupAsync(
        SetupOptions options,
        CancellationToken ct = default)
    {
        _ui.ShowHeader("Hugh Setup Wizard");

        var results = new List<PrerequisiteStatus>();
        var installResults = new List<InstallationResult>();
        var failures = new List<string>();

        foreach (var detector in _detectors)
        {
            var status = await detector.DetectAsync(ct);
            results.Add(status);
            _ui.ShowPrerequisiteStatus(status);

            if (status.IsSatisfied)
            {
                continue;
            }

            // Attempt to resolve
            var resolved = await TryResolveAsync(detector, status, options, installResults, ct);

            if (!resolved && detector.IsRequired)
            {
                failures.Add($"{detector.DisplayName}: {status.Message}");

                if (!options.ContinueOnError)
                {
                    break;
                }
            }
        }

        var setupResult = new SetupResult
        {
            Success = failures.Count == 0,
            PrerequisiteResults = results,
            InstallationResults = installResults,
            Failures = failures,
            RequiresTerminalRestart = installResults.Any(r => r.RequiresPathRefresh)
        };

        _ui.ShowSummary(setupResult);

        return setupResult;
    }

    private async Task<bool> TryResolveAsync(
        IPrerequisiteDetector detector,
        PrerequisiteStatus status,
        SetupOptions options,
        List<InstallationResult> installResults,
        CancellationToken ct)
    {
        // Handle authentication differently from installation
        if (status.State == PrerequisiteState.NotAuthenticated)
        {
            return await HandleAuthenticationAsync(options, ct);
        }

        if (!_ui.IsInteractive && !options.AutoFix)
        {
            return false;  // Can't prompt, can't auto-fix
        }

        var installers = _installerRegistry.GetInstallersFor(detector.PrerequisiteId);
        var recommended = _installerRegistry.GetRecommendedInstaller(detector.PrerequisiteId);

        if (!installers.Any())
        {
            _ui.ShowError($"No installers available for {detector.DisplayName}");
            return false;
        }

        InstallerChoice choice;
        if (options.AutoFix && recommended != null)
        {
            choice = new InstallerChoice { SelectedInstaller = recommended };
        }
        else
        {
            choice = await _ui.PromptInstallerChoiceAsync(
                detector.PrerequisiteId, installers, recommended, ct);
        }

        if (choice.Skip || choice.SelectedInstaller == null)
        {
            return false;
        }

        if (choice.ShowManualInstructions)
        {
            _ui.ShowError($"Manual installation required for {detector.DisplayName}", status.Hints);
            return false;
        }

        // Perform installation
        using var progress = _ui.ShowProgress($"Installing {detector.DisplayName}...");

        var installResult = await choice.SelectedInstaller.InstallAsync(
            detector.PrerequisiteId,
            new InstallOptions { Silent = options.NonInteractive },
            null,
            ct);

        installResults.Add(installResult);

        if (installResult.IsSuccess)
        {
            _ui.ShowSuccess(installResult.Message ?? $"Installed {detector.DisplayName}");

            // Verify installation
            var verification = await detector.DetectAsync(ct);
            return verification.IsSatisfied;
        }

        _ui.ShowError($"Installation failed: {installResult.ErrorMessage}");

        // Attempt rollback if available
        await choice.SelectedInstaller.RollbackAsync(detector.PrerequisiteId, ct);

        return false;
    }

    private async Task<bool> HandleAuthenticationAsync(
        SetupOptions options,
        CancellationToken ct)
    {
        if (!_ui.IsInteractive)
        {
            _ui.ShowError("Authentication required but running non-interactively");
            return false;
        }

        var authMethod = await _ui.PromptAuthMethodAsync(ct);

        switch (authMethod)
        {
            case AuthMethod.BrowserLogin:
                return await TriggerBrowserAuthAsync(ct);

            case AuthMethod.PersonalAccessToken:
                var token = await _ui.PromptSecretAsync("Enter your GitHub token:", ct);
                if (!string.IsNullOrEmpty(token))
                {
                    Environment.SetEnvironmentVariable("GH_TOKEN", token);
                    _ui.ShowSuccess("Token set for current session");
                    _ui.ShowWarning("To persist, add to your shell profile");
                    return true;
                }
                return false;

            case AuthMethod.EnvironmentVariable:
                _ui.ShowWarning("Set GH_TOKEN or GITHUB_TOKEN in your environment");
                return false;

            default:
                return false;
        }
    }

    private async Task<bool> TriggerBrowserAuthAsync(CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth login --web",
                UseShellExecute = true
            };

            using var process = Process.Start(startInfo);
            await process!.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            _ui.ShowError("Could not launch browser authentication");
            return false;
        }
    }

    public async Task<SetupResult> RunDoctorAsync(CancellationToken ct = default)
    {
        var results = await CheckAllAsync(ct);

        _ui.ShowHeader("Hugh Doctor");

        foreach (var result in results)
        {
            _ui.ShowPrerequisiteStatus(result);
        }

        var failed = results.Where(r => !r.IsSatisfied).ToList();

        var setupResult = new SetupResult
        {
            Success = !failed.Any(r => _detectors.First(d => d.PrerequisiteId == r.PrerequisiteId).IsRequired),
            PrerequisiteResults = results.ToList(),
            Failures = failed.Select(f => f.Message ?? f.PrerequisiteId).ToList()!
        };

        _ui.ShowSummary(setupResult);

        return setupResult;
    }

    public async Task<IReadOnlyList<PrerequisiteStatus>> CheckAllAsync(CancellationToken ct = default)
    {
        var results = new List<PrerequisiteStatus>();

        foreach (var detector in _detectors)
        {
            var status = await detector.DetectAsync(ct);
            results.Add(status);
        }

        return results;
    }
}
```

---

#### 7. CLI Command Integration

##### 7.1 New Commands

```csharp
// In Program.cs

var setupCommand = new Command("setup", "Run the interactive setup wizard")
{
    new Option<bool>("--auto", "Automatically fix issues without prompting"),
    new Option<bool>("--non-interactive", "Run without prompts (fail on issues)")
};

setupCommand.SetHandler(async (auto, nonInteractive) =>
{
    var orchestrator = BuildSetupOrchestrator(nonInteractive);
    var result = await orchestrator.RunSetupAsync(new SetupOptions
    {
        AutoFix = auto,
        NonInteractive = nonInteractive
    });
    return result.Success ? 0 : 1;
}, autoOption, nonInteractiveOption);

var doctorCommand = new Command("doctor", "Check prerequisites and report issues");
doctorCommand.SetHandler(async () =>
{
    var orchestrator = BuildSetupOrchestrator(nonInteractive: false);
    var result = await orchestrator.RunDoctorAsync();
    return result.Success ? 0 : 1;
});

rootCommand.AddCommand(setupCommand);
rootCommand.AddCommand(doctorCommand);
```

##### 7.2 Backward Compatibility: --check Flag

```csharp
// Keep --check working (delegates to doctor)
if (check)
{
    var orchestrator = BuildSetupOrchestrator(nonInteractive: true);
    var result = await orchestrator.RunDoctorAsync();
    return result.Success ? 0 : 1;
}
```

---

#### 8. Rollback and Cleanup Strategy

##### 8.1 Transaction-like Approach

```csharp
public record InstallationContext
{
    public List<(string PrerequisiteId, IInstaller Installer)> InstalledItems { get; } = new();

    public async Task RollbackAllAsync(CancellationToken ct)
    {
        // Rollback in reverse order
        foreach (var (prereqId, installer) in InstalledItems.AsEnumerable().Reverse())
        {
            try
            {
                await installer.RollbackAsync(prereqId, ct);
            }
            catch
            {
                // Log but continue rollback
            }
        }
    }
}
```

##### 8.2 Partial Failure Handling

| Scenario | Behavior |
|----------|----------|
| Installation fails | Attempt rollback of that item, report error, offer to continue |
| Verification fails after install | Report as partial success, suggest manual verification |
| PATH not updated | Notify user to restart terminal |
| Auth fails after CLI install | Leave CLI installed, report auth as separate issue |

---

#### 9. Cross-Platform Support Matrix

| Component | Windows | macOS | Linux |
|-----------|---------|-------|-------|
| WingetInstaller | ✅ Primary | ❌ | ❌ |
| BrewInstaller | ❌ | ✅ Primary | ✅ If Homebrew installed |
| NpmInstaller | ✅ Fallback | ✅ Fallback | ✅ Fallback |
| ManualInstaller | ✅ Always | ✅ Always | ✅ Always |
| Browse Auth | ✅ | ✅ | ✅ |
| Token Auth | ✅ | ✅ | ✅ |

##### Platform Detection

```csharp
public static class PlatformDetector
{
    public static InstallerPlatform Current =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? InstallerPlatform.Windows :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? InstallerPlatform.MacOS :
        InstallerPlatform.Linux;
}

public enum InstallerPlatform
{
    Windows,
    MacOS,
    Linux
}
```

---

#### 10. Configuration Integration

##### 10.1 SetupConfig

```csharp
namespace Hugh.Setup;

public class SetupConfig
{
    /// <summary>
    /// Whether to prompt before installing prerequisites.
    /// </summary>
    public bool PromptBeforeInstall { get; set; } = true;

    /// <summary>
    /// Default installation scope.
    /// </summary>
    public InstallScope DefaultScope { get; set; } = InstallScope.User;

    /// <summary>
    /// Whether to skip non-required prerequisites in --auto mode.
    /// </summary>
    public bool SkipOptionalInAutoMode { get; set; } = true;

    /// <summary>
    /// Preferred installer by platform (overrides auto-detection).
    /// </summary>
    public Dictionary<string, string>? PreferredInstallers { get; set; }
}
```

##### 10.2 appsettings.json Integration

```json
{
  "Hugh": {
    "Setup": {
      "PromptBeforeInstall": true,
      "DefaultScope": "User",
      "SkipOptionalInAutoMode": true,
      "PreferredInstallers": {
        "copilot-cli": "winget"
      }
    }
  }
}
```

---

#### 11. InteractiveSetupUI with Spectre.Console

```csharp
namespace Hugh.Setup.UserInterface;

public class InteractiveSetupUI : ISetupUserInterface
{
    public bool IsInteractive => !Console.IsInputRedirected && 
                                  AnsiConsole.Profile.Capabilities.Interactive;

    public void ShowHeader(string title)
    {
        AnsiConsole.Write(new FigletText(title).Centered().Color(Color.Cyan1));
        AnsiConsole.WriteLine();
    }

    public void ShowPrerequisiteStatus(PrerequisiteStatus status)
    {
        var icon = status.State switch
        {
            PrerequisiteState.Satisfied => "[green]✓[/]",
            PrerequisiteState.NotInstalled => "[red]✗[/]",
            PrerequisiteState.NotAuthenticated => "[yellow]![/]",
            _ => "[yellow]⚠[/]"
        };

        AnsiConsole.MarkupLine($"{icon} {status.Message}");

        if (status.Version != null)
        {
            AnsiConsole.MarkupLine($"   Version: [dim]{status.Version}[/]");
        }
    }

    public async Task<InstallerChoice> PromptInstallerChoiceAsync(
        string prerequisiteId,
        IEnumerable<IInstaller> availableInstallers,
        IInstaller? recommended,
        CancellationToken ct = default)
    {
        var choices = new List<string>();

        foreach (var installer in availableInstallers)
        {
            var label = installer.DisplayName;
            if (installer == recommended)
                label += " (recommended)";
            choices.Add(label);
        }

        choices.Add("Show manual instructions");
        choices.Add("Skip for now");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"How would you like to install [green]{prerequisiteId}[/]?")
                .AddChoices(choices));

        if (selection == "Skip for now")
            return new InstallerChoice { Skip = true };

        if (selection == "Show manual instructions")
            return new InstallerChoice { ShowManualInstructions = true };

        var selectedInstaller = availableInstallers.FirstOrDefault(
            i => selection.StartsWith(i.DisplayName));

        return new InstallerChoice { SelectedInstaller = selectedInstaller };
    }

    public Task<bool> ConfirmAsync(string message, bool defaultValue = true, CancellationToken ct = default)
    {
        return Task.FromResult(AnsiConsole.Confirm(message, defaultValue));
    }

    public IDisposable ShowProgress(string message)
    {
        return new StatusContext(AnsiConsole.Status().Start(message, ctx => { }));
    }

    public Task<AuthMethod> PromptAuthMethodAsync(CancellationToken ct = default)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]GitHub authentication required[/]")
                .AddChoices(
                    "Open browser to log in (recommended)",
                    "Enter a personal access token",
                    "Set GH_TOKEN environment variable later",
                    "Skip authentication for now"));

        return Task.FromResult(choice switch
        {
            "Open browser to log in (recommended)" => AuthMethod.BrowserLogin,
            "Enter a personal access token" => AuthMethod.PersonalAccessToken,
            "Set GH_TOKEN environment variable later" => AuthMethod.EnvironmentVariable,
            _ => AuthMethod.Skip
        });
    }

    public Task<string?> PromptSecretAsync(string prompt, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .Secret()
                .AllowEmpty()));
    }

    public void ShowSuccess(string message) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");

    public void ShowError(string message, string[]? hints = null)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {message}");
        if (hints != null)
        {
            foreach (var hint in hints)
            {
                AnsiConsole.MarkupLine($"  [dim]• {hint}[/]");
            }
        }
    }

    public void ShowWarning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {message}");

    public void ShowSummary(SetupResult result)
    {
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Result");
        table.AddColumn("Status");

        if (result.Success)
        {
            table.AddRow("[green]Setup Complete[/]", "✓ All prerequisites satisfied");
        }
        else
        {
            table.AddRow("[red]Setup Incomplete[/]", $"✗ {result.Failures.Count} issue(s)");
        }

        AnsiConsole.Write(table);

        if (result.RequiresTerminalRestart)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]⚠ Please restart your terminal for PATH changes to take effect[/]");
        }
    }
}
```

---

#### 12. Migration Path from PrerequisiteChecker

| Step | Action |
|------|--------|
| 1 | Add Spectre.Console package reference |
| 2 | Create `Setup/` directory structure |
| 3 | Implement interfaces and core classes |
| 4 | Create concrete detectors and installers |
| 5 | Wire up DI in Program.cs |
| 6 | Add `setup` and `doctor` commands |
| 7 | Update `--check` to delegate to doctor |
| 8 | Mark `PrerequisiteChecker` as `[Obsolete]` |
| 9 | Update documentation |

##### Deprecation Notice

```csharp
[Obsolete("Use ISetupOrchestrator.CheckAllAsync() instead. Will be removed in v2.0.")]
public static class PrerequisiteChecker { ... }
```

### Key Discoveries

- **Hugh already uses interface-based DI patterns** (ICopilotService, IAgentRegistry) — new setup system should follow same conventions
- **Strategy pattern ideal for installers** — enables winget/npm/brew/manual pluggability with consistent interface
- **ISetupUserInterface abstraction enables testing** — can mock prompts for unit tests while using Spectre.Console in production
- **Two-command approach (`setup` + `doctor`)** follows industry patterns (gh, npm, dotnet) — setup fixes issues, doctor reports them
- **Rollback is best-effort** — some installers (winget) have limited uninstall support; transaction guarantees not feasible
- **PATH refresh is a platform limitation** — must notify user to restart terminal; cannot programmatically update all shell sessions
- **Authentication is a special case** — not an installation, handled separately via browser OAuth or token input

### Relevant Files/Resources

- [ICopilotService.cs](Hugh/Agents/ICopilotService.cs) — Interface pattern to follow
- [IAgentRegistry.cs](Hugh/Agents/IAgentRegistry.cs) — Registry pattern to follow for IInstallerRegistry
- [LoopOrchestrator.cs](Hugh/Orchestration/LoopOrchestrator.cs) — Orchestrator pattern to follow for SetupOrchestrator
- [HughConfig.cs](Hugh/Configuration/HughConfig.cs) — Configuration pattern to follow for SetupConfig
- [PrerequisiteChecker.cs](Hugh/PrerequisiteChecker.cs) — Current implementation to be deprecated
- [ConsoleOutput.cs](Hugh/ConsoleOutput.cs) — Current output to be replaced by Spectre.Console
- [Program.cs](Hugh/Program.cs) — CLI command integration point

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|
| RFC 8628 (OAuth Device Flow) | Authentication UX | Device code pattern with polling, 15-minute expiration, `slow_down` handling |
| CLI UX Patterns (gh, az, dotnet) | Setup wizard design | One question at a time, sensible defaults, progressive disclosure |

*No organizational standards from pch-standards-space were directly applicable to CLI setup wizard design.*

---

## Conclusions

This research confirms that Hugh can significantly improve its prerequisite experience by:

1. **Replacing static `PrerequisiteChecker`** with a DI-friendly, extensible architecture using detectors and installers.

2. **Adding Spectre.Console** for rich interactive prompts matching modern CLI expectations (SelectionPrompt, progress displays, secret input).

3. **Implementing `hugh setup` and `hugh doctor`** commands that follow established patterns from gh, az, and npm CLIs.

4. **Supporting silent/automated installation** via winget and npm for CI/CD scenarios with `--non-interactive` and `--auto` flags.

5. **Handling authentication as a distinct flow** — detect auth status, offer browser OAuth or token input, fall back to environment variables for headless use.

6. **Designing for cross-platform** — winget on Windows, Homebrew on macOS, npm as universal fallback.

The proposed architecture is fully specified with interfaces, concrete implementations, and CLI integration patterns. Implementation can proceed in phases:

- **Phase 1:** Core interfaces and Spectre.Console integration
- **Phase 2:** Detectors for Copilot CLI and GitHub auth
- **Phase 3:** WingetInstaller and NpmInstaller
- **Phase 4:** setup/doctor commands with UI flow
- **Phase 5:** Cross-platform installers (Brew) and polish

Estimated implementation effort: **3-5 days** for core functionality, **1-2 days** for cross-platform and polish.

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** 2026-02-05
**Status:** ✅ Complete
**Current Phase:** Complete (5 of 5)
**Document Location:** `/docs/research/011-hugh-prerequisite-detection-installation.md`
