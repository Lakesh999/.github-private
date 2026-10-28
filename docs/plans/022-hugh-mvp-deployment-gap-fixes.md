---
id: "022"
type: plan
title: "Hugh MVP Deployment Gap Fixes"
status: "✅ Complete"
created: "2026-02-18"
updated: "2026-02-18"
completed: "2026-02-18"
owner: "pch-planner"
version: "v2.1"
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-18 | pch-planner | Initial plan creation |
| v1.1 | 2026-02-18 | pch-planner | Added decisions 1-4, requirements, technical design, execution plan |
| v2.0 | 2026-02-18 | pch-planner | Holistic review completed |
| v2.1 | 2026-02-18 | pch-plan-reviewer | Review: corrected line references, removed redundant step 2.2, marked Ready for Implementation |
| v3.0 | 2026-02-18 | pch-coder | Implementation complete: all 3 phases executed, 506 tests pass, 0 warnings |

## Introduction

This plan addresses the gaps identified in Research 020 ("Hugh MVP Deployment and Execution — Gap Analysis and Execution Plan") that block Hugh from reaching a working MVP. The research validated that Hugh's build infrastructure is solid (0 warnings, 506 tests pass, CI/CD functional) but identified several gaps preventing end-to-end runtime success across both the direct CLI flow and the agent-mediated flow.

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|-----------|
| 1 | Plan Scope | C — Comprehensive — All Implementable Gaps | Covers all concrete code fixes (Gaps 2-4), proactive Console.ReadLine() guard (Gap 6), plus structured validation phases with `hugh --doctor` and verbose runtime output capture. Maximizes risk reduction within what pch-coder can execute. |
| 2 | Console.ReadLine() Guard Approach | C — Combine Both Checks (Config + Environment) | Skip `Console.ReadLine()` if EITHER `Autonomous.Enabled` is true OR `Console.IsInputRedirected` is true. Provides defense-in-depth: config check handles intended autonomous case, environment check catches unexpected non-interactive contexts (CI/CD, piped). |
| 3 | Version Stamping Approach | B — Development Sentinel Value | Use `0.0.0-dev` as permanent default in `.csproj`. CI/CD already overrides via `-p:Version=`. Sentinel never goes stale, requires zero maintenance, and provides diagnostic clarity (release vs local build). |
| 4 | Runtime Validation Scope | C — Build + Doctor + Unit Test Verification | After all code changes, run full test suite to confirm nothing is broken, then run `hugh doctor` to verify setup detection changes. Deterministic validation without dependency on live Copilot SDK auth/network. |

## Holistic Review

### Decision Interactions

1. **Console.ReadLine() guard + Validation scope:** The guard (Decision 2) prevents hangs in autonomous mode, and the validation scope (Decision 4) uses `dotnet test` + `hugh doctor` rather than a live runtime prompt. These are complementary — the guard ensures safety if someone does run Hugh autonomously before the live runtime test is conducted separately, while the validation scope avoids depending on unpredictable SDK auth.

2. **Version stamping + CI/CD:** The sentinel value `0.0.0-dev` (Decision 3) works because CI/CD already overrides via `-p:Version=`. No conflict. `System.CommandLine` will automatically expose the assembly version via `--version` flag.

3. **CopilotCliDetector hints + doctor validation:** The hint text fix (Gap 2) is directly validated by Phase 3 step 3.3 (`hugh doctor --verbose`), creating a closed verification loop.

### Architectural Considerations

- **No new abstractions introduced**: All changes are surgical edits to existing files. No new interfaces, classes, or patterns. This minimizes integration risk.
- **Dual-environment compatibility preserved**: The `agents/hugh.md` model reference update is metadata-only. The `Console.ReadLine()` guard uses `Console.IsInputRedirected` which is already used elsewhere in `Program.cs` (doctor and resume commands), maintaining pattern consistency.
- **Test coverage gap for Console.ReadLine() guard**: The guard in `CopilotService` is not directly unit-tested because it sits deep in an async method that requires a live `CopilotSession`. This is acceptable for MVP — the guard is simple conditional logic with no side effects beyond logging and early return. Post-MVP, consider extracting the input decision into a testable helper method.

### Trade-offs Accepted

- **No live runtime validation in this plan**: The most critical gap (Gap 1: "Has anyone actually run Hugh?") remains addressable only outside this plan, as it requires live Copilot SDK authentication. This plan maximizes what can be deterministically validated.
- **Sentinel version for dev builds**: `0.0.0-dev` is slightly unconventional but clearly communicates intent. Alternative approaches (static version tracking) were rejected for zero-maintenance benefit.

### Risks Acknowledged

- The `Console.ReadLine()` guard returns `Success = false` with an `ErrorMessage` — the orchestrator's handling of this specific error message pattern should be verified during implementation to ensure it doesn't trigger unintended retry loops.
- Phase 3 step 3.3 (`hugh doctor`) depends on the workspace having the correct directory structure. Running via `dotnet run --project` should resolve paths correctly via `AppContext.BaseDirectory` fallback.

## Overview

This plan closes the gaps identified in Research 020 that block Hugh from reaching a working MVP. It covers four concrete code fixes and includes structured validation phases:

1. **Fix CopilotCliDetector installation hints** (Gap 2) — Replace incorrect `gh extension install` hints with standalone Copilot CLI installation guidance
2. **Update stale model reference** (Gap 3) — Change "Claude Opus 4.5" to "Claude Opus 4.6" in `agents/hugh.md`
3. **Add version stamping** (Gap 4) — Add `<Version>` property to `Hugh.csproj` with CI/CD dynamic override
4. **Guard Console.ReadLine() in autonomous mode** (Gap 6) — Prevent process hang when no interactive console is available
5. **Runtime validation** (Gap 1) — Build Hugh, run `hugh doctor`, and run Hugh with a test prompt in verbose mode to capture and triage output

**Out of scope:** Tier 3 graceful degradation in `agents/hugh.md` (Gap 5 — post-MVP), VS Code agent-mediated flow testing (requires interactive VS Code session).

## Requirements

### Functional

- **FR-1:** `CopilotCliDetector` must provide correct installation hints referencing the standalone Copilot CLI (`winget install GitHub.Copilot`), not the `gh copilot` extension
- **FR-2:** `agents/hugh.md` must reference the current model ("Claude Opus 4.6") in its YAML front matter
- **FR-3:** `Hugh.csproj` must include a `<Version>` property set to `0.0.0-dev` as a development sentinel; CI/CD overrides this at publish time
- **FR-4:** `CopilotService` must not call `Console.ReadLine()` when autonomous mode is enabled OR when the process has no interactive console (stdin redirected)
- **FR-5:** `hugh doctor` must execute successfully after all changes are applied
- **FR-6:** All existing unit tests (506+) must continue to pass after all changes

### Non-Functional

- **NFR-1:** All changes must be backward-compatible — no breaking changes to CLI interface, configuration schema, or agent prompt format
- **NFR-2:** Changes must preserve dual-environment compatibility (VS Code Copilot chat + Hugh CLI) for agent prompts

### Out of Scope

- Tier 3 graceful degradation in `agents/hugh.md` (Gap 5 — post-MVP)
- VS Code agent-mediated flow testing (requires interactive VS Code session)
- Live end-to-end runtime test with Copilot SDK (depends on auth/network outside plan scope)
- Refactoring `Console.ReadLine()` into a full `IUserInputProvider` abstraction (post-MVP if more input points emerge)

## Technical Design

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: Setup Detection
    location: "Hugh/Setup/Detection/*.cs"
    usage: CopilotCliDetector follows IPrerequisiteDetector interface with PrerequisiteStatus return
  - pattern: Agent Prompt YAML Front Matter
    location: "agents/*.md"
    usage: All agent prompts use YAML front matter with name, description, model, handoffs fields
  - pattern: Project Configuration
    location: "Hugh/Hugh.csproj"
    usage: Standard .NET 8 SDK project with PackageReference style
  - pattern: Autonomous Config Access
    location: "Hugh/Agents/CopilotService.cs"
    usage: Config accessed via _config field (HughConfig type) — _config.Autonomous.Enabled
  - pattern: Non-Interactive Detection
    location: "Hugh/Program.cs"
    usage: Existing pattern uses Console.IsInputRedirected in doctor command and resume handler
```

### Data Contracts

No data entities in scope — data contracts not applicable.

### Change 1: CopilotCliDetector Installation Hints

**File:** `Hugh/Setup/Detection/CopilotCliDetector.cs`
**Lines:** 60-63 (non-zero exit code branch) and 74-77 (exception branch)

Both `Hints` arrays currently contain:
```csharp
Hints = new[]
{
    "Install the Copilot CLI extension: gh extension install github/gh-copilot",
    "Ensure 'copilot' is accessible from your PATH"
}
```

Replace with:
```csharp
Hints = new[]
{
    "Install the standalone Copilot CLI: winget install GitHub.Copilot",
    "Or download from: https://github.com/github/copilot-cli/releases",
    "Ensure 'copilot' is accessible from your PATH"
}
```

Both occurrences must be updated identically. The third hint ("Ensure 'copilot' is accessible...") is retained as-is.

**Test impact:** `Hugh.Tests/Setup/CopilotCliDetectorTests.cs` — any tests asserting on hint text will need updating to match the new strings.

### Change 2: Agent Model Reference Update

**File:** `agents/hugh.md`
**Line:** 4 (YAML front matter `model:` field)

Change:
```yaml
model: Claude Opus 4.5
```
To:
```yaml
model: Claude Opus 4.6
```

No code or test impact — this is a prompt metadata field parsed only by the VS Code agent system.

### Change 3: Version Stamping

**File:** `Hugh/Hugh.csproj`

Add `<Version>0.0.0-dev</Version>` inside the existing `<PropertyGroup>`:

```xml
<PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>0.0.0-dev</Version>
</PropertyGroup>
```

The CI/CD workflow (`.github/workflows/release.yml`) already passes `-p:Version=${{ steps.version.outputs.VERSION }}` at publish time, which overrides this default. No CI/CD changes required.

### Change 4: Console.ReadLine() Guard in Autonomous Mode

**File:** `Hugh/Agents/CopilotService.cs`
**Lines:** ~205-213 (inside the `ShouldPause == true` branch, before the `Console.ReadLine()` loop)

Add a guard at line 210 (inside the `QuestionText != null` block, before `const int maxUserInputAttempts = 3;`):

```csharp
if (questionResponse?.ShouldPause == true)
{
    // NeedsUserInput: re-invocation loop (max 3 attempts)
    if (questionResponse.QuestionText != null)
    {
        // Guard: skip interactive input in autonomous mode or non-interactive environments
        if (_config.Autonomous.Enabled || Console.IsInputRedirected)
        {
            _logger.Warning(
                "Skipping interactive input — autonomous mode: {Autonomous}, stdin redirected: {Redirected}. " +
                "Question: {Question}",
                _config.Autonomous.Enabled, Console.IsInputRedirected, questionResponse.QuestionText);

            return new PhaseResult
            {
                Success = false,

                ErrorMessage = $"Phase paused — user input required: {questionResponse.QuestionText}",
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed
            };
        }

        const int maxUserInputAttempts = 3;
        for (var attempt = 0; attempt < maxUserInputAttempts; attempt++)
        {
            var userAnswer = Console.ReadLine()?.Trim();
```

**Key details:**
- The `_config` field is already available in scope (class field of type `HughConfig`)
- `Console.IsInputRedirected` is a static property — no injection needed
- The returned `PhaseResult` uses `Success = false` with descriptive `ErrorMessage` — `PhaseResult` does not have a `Status` string property
- Existing tests for `CopilotService` are integration-level and may not cover this path directly; the guard is testable through the existing `SdkQuestionHandlerTests` that validate the `ShouldPause` flow

**PhaseResult model note:** `PhaseResult` (at `Hugh/Models/PhaseResult.cs`) does not have a `Status` property. The guard should return with `Success = false` and set `ErrorMessage` to convey the pause reason. The orchestrator (`LoopOrchestrator`) already handles `Success = false` results by checking `ErrorMessage` content.

## Dependencies

| Dependency | Type | Impact |
|------------|------|--------|
| .NET 8 SDK | Build tool | Required to build and test Hugh |
| Existing test suite (506 tests) | Validation | Must pass after all changes |
| `CopilotCliDetectorTests.cs` | Test file | Contains hint assertion that must be updated (line 181) |
| CI/CD workflow (`release.yml`) | Pipeline | Already passes `-p:Version=` — no changes needed |
| Copilot CLI standalone binary | Runtime | Required for `hugh doctor` validation step |

## Risks

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| Console.ReadLine() guard breaks interactive mode | Medium | Low | Guard only activates when `Autonomous.Enabled` OR `Console.IsInputRedirected` — interactive CLI users are unaffected |
| CopilotCliDetector test assertions break | Low | High (expected) | Plan includes updating the hint assertion in `CopilotCliDetectorTests.cs` |
| Version sentinel `0.0.0-dev` causes System.CommandLine `--version` display confusion | Low | Low | Sentinel clearly indicates non-release build; CI/CD overrides for releases |
| `hugh doctor` fails during validation due to missing `copilot` binary | Medium | Medium | Validation phase documents expected output; missing binary is a known prerequisite, not a code bug |

## Execution Plan

### Phase 1: Code Fixes (Gaps 2, 3, 4)

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 4
**Prerequisites:** None
**Entry Point:** `Hugh/Setup/Detection/CopilotCliDetector.cs`
**Verification:** N/A (first phase)

| Step | Task | Files | Status |
|------|------|-------|--------|
| 1.1 | Update CopilotCliDetector hints (non-zero exit code branch) | `Hugh/Setup/Detection/CopilotCliDetector.cs` | ✅ Complete |
| 1.2 | Update CopilotCliDetector hints (exception branch) | `Hugh/Setup/Detection/CopilotCliDetector.cs` | ✅ Complete |
| 1.3 | Update CopilotCliDetector test assertion | `Hugh.Tests/Setup/CopilotCliDetectorTests.cs` | ✅ Complete |
| 1.4 | Update `agents/hugh.md` model reference | `agents/hugh.md` | ✅ Complete |
| 1.5 | Add version property to Hugh.csproj | `Hugh/Hugh.csproj` | ✅ Complete |

### Phase 2: Console.ReadLine() Guard (Gap 6)

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 1 complete
**Entry Point:** `Hugh/Agents/CopilotService.cs`
**Verification:** Phase 1 changes applied; `CopilotCliDetector.cs` contains new hints

| Step | Task | Files | Status |
|------|------|-------|--------|
| 2.1 | Add autonomous/non-interactive guard before Console.ReadLine() loop | `Hugh/Agents/CopilotService.cs` | ✅ Complete |

### Phase 3: Validation

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 0
**Prerequisites:** Phase 2 complete
**Entry Point:** Run `dotnet test` from repo root
**Verification:** Phase 2 code changes applied in `CopilotService.cs`

| Step | Task | Files | Status |
|------|------|-------|--------|
| 3.1 | Run full test suite | N/A | ✅ Complete — 506 passed, 0 failures |
| 3.2 | Build Hugh in Release configuration | N/A | ✅ Complete — 0 errors, 0 warnings |
| 3.3 | Run `hugh doctor` | N/A | ✅ Complete — all 5 prerequisites satisfied |

## Implementation Notes

### Phase 1 - Code Fixes (Gaps 2, 3, 4)

**Completed:** 2026-02-18
**Execution Mode:** Automatic (Subagent)

**Files Modified:**

- `Hugh/Setup/Detection/CopilotCliDetector.cs` — Updated both hint arrays (non-zero exit code and exception branches) to reference standalone Copilot CLI installation
- `Hugh.Tests/Setup/CopilotCliDetectorTests.cs` — Updated hint assertion from `"gh extension install"` to `"winget install"`
- `agents/hugh.md` — Changed model from "Claude Opus 4.5" to "Claude Opus 4.6"
- `Hugh/Hugh.csproj` — Added `<Version>0.0.0-dev</Version>` to main PropertyGroup

**Deviations from Plan:** None

### Phase 2 - Console.ReadLine() Guard (Gap 6)

**Completed:** 2026-02-18
**Execution Mode:** Automatic (Subagent)

**Files Modified:**

- `Hugh/Agents/CopilotService.cs` — Inserted guard before `Console.ReadLine()` loop checking `_config.Autonomous.Enabled || Console.IsInputRedirected`; returns `PhaseResult` with `Success = false` and descriptive `ErrorMessage` when triggered

**Deviations from Plan:** None

### Phase 3 - Validation

**Completed:** 2026-02-18
**Execution Mode:** Automatic (Subagent)

**Results:**

- `dotnet test`: 506 passed, 0 failures, 0 warnings
- `dotnet build -c Release`: 0 errors, 0 warnings
- `hugh doctor`: All 5 prerequisites satisfied (agents-folder, workspace, github-cli, copilot-cli, github-auth)

**Note:** `--verbose` flag is not supported on the `doctor` subcommand; ran without it. All prerequisites detected successfully.

### Plan Completion

**All phases completed:** 2026-02-18
**Total tasks completed:** 9
**Total files modified:** 5
**Code review:** Clean — no issues found

## Standards

No organizational standards applicable to this plan (pch-standards-space unavailable).

## Review Session Log

**Questions Pending:** 0
**Questions Resolved:** 0
**Last Updated:** 2026-02-18

No clarifying questions were required — all issues found were objective corrections applied directly.

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 1 | 4 files across 3 directories |
| New patterns introduced | 1 | No new patterns; uses existing `Console.IsInputRedirected` convention |
| External dependencies | 1 | No new dependencies |
| Migration complexity | 1 | No migrations; all changes are additive or in-place edits |
| Test coverage required | 1 | 1 existing test assertion updated; no new test files |
| **Overall Complexity** | **5/25** | **Low** |

## Review Summary

**Review Date:** 2026-02-18
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 3 (Critical: 0, Major: 1, Minor: 2)
- Clarifying Questions Asked: 0
- Sections Updated: Change 1 (line refs), Change 4 (insertion point), Phase 1 Steps 1.1/1.2 (line refs), Phase 2 (merged Step 2.2 into 2.1)

### Key Improvements Made
1. Corrected line references for CopilotCliDetector hints from 57-59/71-73 to 60-63/74-77 (verified against actual source)
2. Tightened Console.ReadLine() guard insertion point from "~line 211" to exact "line 210, before `const int maxUserInputAttempts`"
3. Merged redundant Step 2.2 (tautological verification of guard's else path) into Step 2.1 acceptance criteria

### Remaining Considerations
- The Console.ReadLine() guard is not directly unit-tested (acknowledged in the plan as acceptable for MVP). Post-MVP, consider extracting input decision into a testable helper.
- Phase 3 Step 3.3 (`hugh doctor`) depends on workspace directory structure and may report missing `copilot` binary — this is expected, not a code bug.

### Sign-off
This plan has been reviewed and is **Ready for Implementation**.

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2026-02-18 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2026-02-18 |
| Status | ✅ Complete |
| Implemented By | pch-coder |
| Implementation Date | 2026-02-18 |
| Plan Location | /docs/plans/022-hugh-mvp-deployment-gap-fixes.md |
