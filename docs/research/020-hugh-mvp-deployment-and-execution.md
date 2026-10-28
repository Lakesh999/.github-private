---
id: "020"
type: research
title: "Hugh MVP Deployment and Execution — Gap Analysis and Execution Plan"
status: "✅ Complete"
created: "2026-02-18"
current_phase: "3 of 3"
vision_source: null
---

## Introduction

Hugh is a .NET 8 self-contained CLI that orchestrates 5 PCH agents (pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer, pch-coder) through the GitHub Copilot SDK. It also has a corresponding VS Code Copilot Chat agent prompt (`agents/hugh.md`) that acts as its CLI operator — detecting, installing, and invoking `hugh.exe` on behalf of the user.

For Hugh to deliver value, two flows must work end-to-end:

1. **Direct CLI flow**: User runs `hugh "build X"` from a terminal and Hugh orchestrates all 5 agents through the pipeline to completion.
2. **Agent-mediated flow**: User asks `@hugh` in VS Code Copilot Chat to build something, and the agent installs (if needed) and invokes `hugh.exe`, interprets output, and relays results.

This research systematically validates the current state of Hugh's build, test, release, and runtime infrastructure; identifies every gap blocking MVP deployment; and produces a prioritized execution plan to reach a working MVP.

## Objectives

- Validate that Hugh builds, tests pass, and release artifacts exist for deployment
- Identify all gaps between the current state and a working MVP where both flows execute successfully
- Assess prior research findings (016: UX Failure Analysis) for unresolved issues
- Produce a prioritized execution plan to close all MVP gaps

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | Current State Validation | ✅ Complete | Build, test, CI/CD, release artifacts, prerequisites on dev machine | 2026-02-18 |
| 2 | Gap Analysis | ✅ Complete | Identify all issues blocking MVP across both execution flows | 2026-02-18 |
| 3 | MVP Execution Plan | ✅ Complete | Prioritized action items to close all gaps and reach working MVP | 2026-02-18 |

## Phase 1: Current State Validation

**Status:** ✅ Complete
**Session:** 2026-02-18

### Build

- **Command:** `dotnet build Hugh/Hugh.csproj -c Release`
- **Result:** Build succeeded, 0 warnings, 0 errors
- **Framework:** .NET 8, self-contained win-x64 single-file publish
- **Key dependencies:** `GitHub.Copilot.SDK v0.1.22`, `System.CommandLine v2.0.0-beta4.22272.1`, `Serilog`

### Tests

- **Command:** `dotnet test Hugh.Tests/Hugh.Tests.csproj`
- **Result:** 506 tests passed, 0 failed, 0 skipped
- **Coverage:** All major subsystems have test coverage including signal classification, tool metrics, agent registry, path resolution, and setup detection

### CI/CD Pipeline

- **Workflow:** `.github/workflows/release.yml`
- **Trigger:** Tag push matching `v*.*.*` or manual `workflow_dispatch`
- **Process:** Builds self-contained win-x64 → creates portable ZIP (includes agents folder) → builds Inno Setup installer → publishes GitHub Release with both assets
- **Status:** Pipeline is functional

### Release Artifacts

- **Releases exist:** v1.0.0, v1.0.1, v1.0.2, v1.0.3, v1.1.0
- **Assets per release:** `Hugh-Setup.exe` (installer) and `Hugh-Portable.zip` (portable)
- **Repository:** `PointCHealth/.github-private` (INTERNAL visibility)

### Prerequisites on Dev Machine

| Prerequisite | Status | Details |
|-------------|--------|---------|
| Copilot CLI (`copilot --version`) | ✅ Available | v0.0.406 standalone binary |
| GitHub CLI (`gh --version`) | ✅ Available | Authenticated |
| Hugh CLI (`hugh.exe`) | ❌ Not installed | Not present at `%LOCALAPPDATA%/Programs/Hugh/Hugh.exe` |
| .NET 8 runtime | ✅ Available | Self-contained publish bundles it anyway |

### Configuration (`Hugh/appsettings.json`)

- Model: `claude-sonnet-4-5` (agent execution model)
- Autonomous mode: enabled, accept recommendations only
- Phase timeouts: Vision 10m, Research 30m, Planning 20m, Review 15m, Coding 20m per sub-phase
- Compaction threshold: 0.8

### Summary

The build infrastructure is solid. Code compiles cleanly, all 506 tests pass, CI/CD produces release artifacts, and the dev machine has the core prerequisites (copilot CLI, gh CLI). The critical gap is that **Hugh has never been run end-to-end at runtime** — all validation so far is at the build/test/CI level.

## Phase 2: Gap Analysis

**Status:** ✅ Complete
**Session:** 2026-02-18

### Gap 1: No End-to-End Runtime Validation

**Severity:** Critical
**Affects:** Both flows

Hugh has never been executed with a real prompt against the Copilot SDK. The entire pipeline — from CLI argument parsing through `CopilotService` session creation to agent prompt loading to signal detection to phase transitions — is untested at runtime. Unit tests cover individual components but do not validate the integrated system.

**Specific risks:**
- `CopilotClient`/`CopilotSession` creation may fail with SDK v0.1.22 configuration
- Agent prompt files may not resolve correctly from the installed path (see `PathResolver` resolution order: ENV → `AppContext.BaseDirectory` → CWD)
- `InfiniteSession` reflection-based enablement (`TryEnableInfiniteSession` in `CopilotService.cs:389-436`) may fail silently on newer SDK versions
- Phase timeout cancellation may not propagate correctly through the SDK event model
- `Console.ReadLine()` call in `CopilotService.cs:211` will block in non-interactive contexts

**Action:** Run `hugh "Create a simple hello world HTML page" --verbose` and observe what breaks.

### Gap 2: `CopilotCliDetector` Provides Wrong Installation Hints

**Severity:** Medium
**Affects:** Direct CLI flow (setup/doctor command)
**File:** `Hugh/Setup/Detection/CopilotCliDetector.cs:57-59, 71-73`

The detector's failure hints say:
```
"Install the Copilot CLI extension: gh extension install github/gh-copilot"
"Ensure 'copilot' is accessible from your PATH"
```

This is wrong. Hugh requires the **standalone `copilot` binary**, not the `gh copilot` extension. The correct installation command is `winget install GitHub.Copilot` (or download from GitHub releases). The `gh copilot` extension installs a different binary that is invoked as `gh copilot`, not `copilot`.

**Action:** Update hints to reference the correct standalone binary installation.

### Gap 3: `agents/hugh.md` Model Reference is Stale

**Severity:** Low
**Affects:** Agent-mediated flow (cosmetic)
**File:** `agents/hugh.md`

The agent prompt references "Claude Opus 4.5" but the current model family is Claude 4.6. All other agent prompts were updated in commit `4af80f6` ("Update all agent model references from Claude Opus 4.5 to Claude Opus 4.6") but `agents/hugh.md` may have been missed or intentionally left.

**Action:** Update model reference to "Claude Opus 4.6".

### Gap 4: No Version Stamping in Build

**Severity:** Low
**Affects:** Direct CLI flow (diagnostics)
**File:** `Hugh/Hugh.csproj`

The `.csproj` has no `<Version>` property. This means `hugh --version` (if implemented) and diagnostic logs cannot report the build version. Release tags exist (v1.0.0–v1.1.0) but are not embedded in the binary.

**Action:** Add `<Version>` property to `.csproj`, ideally set dynamically from the git tag in CI/CD.

### Gap 5: Graceful Degradation Not Implemented (Research 016 Finding)

**Severity:** Medium
**Affects:** Agent-mediated flow
**File:** `agents/hugh.md`

Research 016 ("Hugh Agent UX Failure Analysis") identified that when Hugh cannot be installed, the agent reaches a dead-end with no fallback. The recommended fix was a 3-tier degradation ladder:

1. **Tier 1:** Installed CLI → run `hugh` directly
2. **Tier 2:** Install from GitHub Releases → attempt installation
3. **Tier 3:** Direct Agent Routing → fall back to running PCH agents directly without Hugh orchestration

The dynamic org detection fix (replacing hardcoded `pch-development/hugh` with `gh api user/orgs`) was implemented, which fixes the Tier 2 dead-end. However, **Tier 3 (direct agent routing fallback)** has not been implemented. If Hugh truly cannot install, the agent still has no way to help the user.

**Action:** For MVP, this is acceptable as a known limitation. Post-MVP, implement Tier 3 fallback in `agents/hugh.md`.

### Gap 6: `Console.ReadLine()` in Service Layer

**Severity:** Medium
**Affects:** Direct CLI flow
**File:** `Hugh/Agents/CopilotService.cs:211`

The `CopilotService` contains a direct `Console.ReadLine()` call. This couples the service layer to interactive console input. In autonomous mode (the default configuration), this may cause the process to hang waiting for input that never comes, or it may be guarded by a condition — but its presence in a service class is a design concern that needs runtime validation.

**Action:** Verify during end-to-end test whether this blocks execution. If it does, it must be guarded or removed for autonomous mode.

## Phase 3: MVP Execution Plan

**Status:** ✅ Complete
**Session:** 2026-02-18

### Priority-Ranked Action Items

| # | Action | Priority | Effort | Gaps Addressed |
|---|--------|----------|--------|----------------|
| 1 | **Run Hugh end-to-end with a real prompt** | 🔴 Critical | 30 min | Gap 1 |
| 2 | **Fix CopilotCliDetector installation hints** | 🟡 Medium | 10 min | Gap 2 |
| 3 | **Update `agents/hugh.md` model reference** | 🟢 Low | 5 min | Gap 3 |
| 4 | **Triage and fix runtime failures from #1** | 🔴 Critical | Unknown | Gap 1, Gap 6 |
| 5 | **Add version stamping to `.csproj` and CI/CD** | 🟢 Low | 20 min | Gap 4 |
| 6 | **Test agent-mediated flow in VS Code** | 🟡 Medium | 30 min | Gap 1 (agent side) |
| 7 | **Validate installed path resolution** | 🟡 Medium | 15 min | Gap 1 (PathResolver) |

### Execution Details

#### Action 1: Run Hugh End-to-End (Critical)

This is the single most important MVP action. Everything else is secondary until we know what actually happens at runtime.

**Steps:**
1. Install Hugh from the latest release (`v1.1.0`) — either via installer or portable ZIP
2. Run `hugh --doctor` to validate prerequisites
3. Run `hugh "Create a simple hello world HTML page" --verbose`
4. Observe: Does the Copilot SDK session start? Does the Vision phase agent prompt load? Does the first signal get detected? Where does it break?

**Expected failure points (in order of likelihood):**
1. Copilot SDK session creation (authentication, model access)
2. Agent prompt file resolution (PathResolver can't find agents from installed location)
3. Signal detection (agent doesn't emit signals in expected format)
4. Phase transition (orchestrator doesn't advance correctly)
5. `Console.ReadLine()` blocking in autonomous mode

#### Action 2: Fix CopilotCliDetector Hints

**File:** `Hugh/Setup/Detection/CopilotCliDetector.cs`

**Current (wrong):**
```csharp
Hints = new[]
{
    "Install the Copilot CLI extension: gh extension install github/gh-copilot",
    "Ensure 'copilot' is accessible from your PATH"
}
```

**Correct:**
```csharp
Hints = new[]
{
    "Install the standalone Copilot CLI: winget install GitHub.Copilot",
    "Or download from: https://github.com/github/copilot-cli/releases",
    "Ensure 'copilot' is accessible from your PATH"
}
```

#### Action 3: Update Model Reference

**File:** `agents/hugh.md`

Find all occurrences of "Claude Opus 4.5" and replace with "Claude Opus 4.6".

#### Action 4: Triage Runtime Failures

This cannot be planned in advance — it depends entirely on what breaks in Action 1. Allocate time for debugging and fixing whatever surfaces. Common categories:
- Configuration issues → fix `appsettings.json` or `PathResolver`
- SDK issues → check SDK version compatibility, authentication
- Prompt issues → fix agent prompt format or signal patterns
- Blocking I/O → guard or remove `Console.ReadLine()` in autonomous mode

#### Action 5: Version Stamping

**File:** `Hugh/Hugh.csproj`

Add `<Version>1.1.0</Version>` as a default, then update CI/CD to pass `/p:Version=${{ github.ref_name }}` (stripping the `v` prefix) during `dotnet publish`.

#### Action 6: Test Agent-Mediated Flow

After Hugh is installed and working from CLI:
1. Open VS Code with the workspace containing `agents/hugh.md`
2. Ask `@hugh` to build something simple
3. Verify: Does the agent detect Hugh? Does it invoke the correct command? Does it interpret the output?

#### Action 7: Validate Path Resolution

After installation, verify that `PathResolver.GetInstallationPath()` returns the correct directory and that `AgentPromptLoader` can find all 5 agent prompt files from the installed location. This is a known fragile point — the resolution order (ENV → `AppContext.BaseDirectory` → CWD) may not work if agents are not bundled with the installer.

**Check:** Does the installer/portable ZIP include the `agents/` folder? The CI/CD workflow copies it:
```yaml
# From release.yml — portable ZIP includes agents
Copy-Item -Path "agents" -Destination "$publishDir/agents" -Recurse
```
But the Inno Setup installer may not include it — verify `installer/hugh-setup.iss` source paths.

## Key Findings

1. **Build infrastructure is solid** — 0 warnings, 506/506 tests pass, CI/CD works, releases exist. Hugh is well-engineered at the component level.

2. **Runtime is completely unvalidated** — The single biggest risk. No one has run `hugh.exe` with a real prompt against the live Copilot SDK. This is the #1 blocker.

3. **CopilotCliDetector gives wrong installation guidance** — Hints reference the `gh copilot` extension instead of the standalone `copilot` binary. This will confuse users running `hugh --doctor`.

4. **Prior UX failures (Research 016) are partially addressed** — Dynamic org detection fixed the dead-end installation path. Tier 3 graceful degradation (direct agent routing) remains unimplemented but is acceptable for MVP.

5. **Path resolution is the most likely integration failure** — `PathResolver` and `AgentPromptLoader` have a multi-step resolution order that depends on how Hugh is installed. If agents aren't bundled correctly with the installer, the very first phase will fail to load its prompt.

6. **`Console.ReadLine()` in CopilotService is a runtime risk** — In autonomous mode, this could hang the process. Needs runtime validation.

## Actionable Conclusions

### For MVP (Do Now)
- **Install and run Hugh end-to-end** — this single action will reveal the actual state of readiness and unblock all downstream work
- **Fix CopilotCliDetector hints** — 10-minute fix, eliminates user confusion during setup
- **Update stale model reference** in `agents/hugh.md`

### Post-MVP (Do After Runtime Works)
- Add version stamping to binary and CI/CD
- Implement Tier 3 graceful degradation in agent prompt
- Consider extracting `Console.ReadLine()` from service layer into a proper user-input abstraction

## Handoff

This research provides the complete gap analysis and prioritized execution plan for reaching Hugh MVP. The next step is **Action 1: install Hugh and run it end-to-end**. All other actions are secondary until runtime behavior is observed. The pch-planner should use this document to create an implementation plan for closing the identified gaps.

**Related documents:**
- Research 009: GitHub Copilot SDK Integration
- Research 010: Hugh Global CLI Installation
- Research 013: Hugh Copilot Agent Prompt
- Research 016: Hugh Agent UX Failure Analysis
