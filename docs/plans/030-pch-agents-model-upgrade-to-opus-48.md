---
id: "030"
type: plan
title: "PCH Agents Model Upgrade to Claude Opus 4.8"
status: ✅ Complete
created: "2026-08-19"
updated: "2026-08-19"
owner: pch-planner
version: v1.2
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-08-19 | pch-planner | Initial plan creation || v1.1 | 2026-08-19 | pch-plan-reviewer | Review approved — corrected edit count, added review summary |
| v1.2 | 2026-08-19 | pch-coder | Implementation complete — all 19 edits applied |
## Introduction

This plan upgrades the PCH agent system's default AI model from Claude Opus 4.6 to Claude Opus 4.8 across all configuration surfaces. The change is purely configuration and documentation — zero code logic, zero test changes, zero CI/CD modifications. Based on research document `/docs/research/028-pch-agents-model-upgrade-to-opus-48.md`.

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|-----------|
| — | No decisions required | — | All technical choices are dictated by the existing architecture — this is a mechanical find-and-replace with no design alternatives |

## Holistic Review

**Decision Interactions:** None — this is a single atomic change with no interdependent choices.

**Architectural Considerations:** The upgrade touches two independent control surfaces (VS Code agent YAML and Hugh CLI config) that do not interact. Changing one does not affect the other.

**Trade-offs:** None accepted — the only trade-off is potential behavioral regression from the new model, mitigated by smoke testing before deployment.

**Risks Acknowledged:**
- Model identifier `claude-opus-4.8` is assumed based on established naming pattern — verify before implementation
- Model must be available on the org's Copilot subscription

## Overview

### Feature Summary

Update all model references from Claude Opus 4.6 (and stale 4.5 references) to Claude Opus 4.8 across the entire repository. This covers:
- 7 agent YAML front matter files
- Hugh CLI runtime configuration and hardcoded default
- Documentation examples in README, copilot-instructions, and agent body content

### Objectives

1. All PCH agents run on Claude Opus 4.8 in VS Code Copilot
2. Hugh CLI orchestration uses Claude Opus 4.8 for all phases
3. All documentation accurately reflects the current model version
4. Stale "Claude Opus 4.5" references corrected (missed during prior 4.5→4.6 upgrade)

## Requirements

### Functional

- All 7 agent files specify `model: Claude Opus 4.8` in YAML front matter
- `Hugh/appsettings.json` specifies `"claude-opus-4.8"`
- `Hugh/Configuration/HughConfig.cs` default fallback is `"claude-opus-4.8"`
- All documentation examples show the correct model string for their context

### Non-Functional

- Single atomic commit — all changes land together
- No compilation errors after change
- Existing unit tests continue to pass without modification

### Out of Scope

- Historical plan/research documents referencing prior model versions (these are records, not config)
- Performance benchmarking of Opus 4.8 vs 4.6
- Token limit or cost analysis
- CI/CD workflow changes

## Technical Design

### Architecture

No architectural changes. Two independent control surfaces updated:

1. **VS Code Agent YAML** — `model:` field in agent front matter controls model for parent agent, subagent, and handoff contexts
2. **Hugh CLI Config** — `HughConfig.Copilot.Model` controls model for all Hugh SDK sessions

### Data Contracts

No data entities in scope — data contracts not applicable.

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: Agent YAML Front Matter
    location: "agents/*.md (line 4)"
    usage: Human-readable model name "Claude Opus 4.8"
  - pattern: Hugh SDK Config
    location: "Hugh/appsettings.json, Hugh/Configuration/HughConfig.cs"
    usage: Kebab-case API identifier "claude-opus-4.8"
  - pattern: Documentation Examples
    location: ".github/copilot-instructions.md, README.md, agents/hugh.md"
    usage: Match format of context (YAML example vs JSON config)
```

### Complete Edit Inventory

#### Category 1: Agent YAML Front Matter (7 files)

| File | Line | Old | New |
|------|------|-----|-----|
| `agents/hugh.md` | 4 | `model: Claude Opus 4.6` | `model: Claude Opus 4.8` |
| `agents/pch-coder.md` | 4 | `model: Claude Opus 4.6` | `model: Claude Opus 4.8` |
| `agents/pch-helper.md` | 4 | `model: Claude Opus 4.6` | `model: Claude Opus 4.8` |
| `agents/pch-plan-reviewer.md` | 4 | `model: Claude Opus 4.6` | `model: Claude Opus 4.8` |
| `agents/pch-planner.md` | 4 | `model: Claude Opus 4.6` | `model: Claude Opus 4.8` |
| `agents/pch-researcher.md` | 4 | `model: Claude Opus 4.6` | `model: Claude Opus 4.8` |
| `agents/pch-visionary.md` | 4 | `model: Claude Opus 4.6` | `model: Claude Opus 4.8` |

#### Category 2: Hugh CLI Configuration (3 files, 5 locations)

| File | Line | Old | New |
|------|------|-----|-----|
| `Hugh/appsettings.json` | 23 | `"Model": "claude-opus-4.6"` | `"Model": "claude-opus-4.8"` |
| `Hugh/Configuration/HughConfig.cs` | 188 | `= "claude-opus-4.6"` | `= "claude-opus-4.8"` |
| `Hugh/README.md` | 91 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |
| `Hugh/README.md` | 106 | `claude-opus-4.6` | `claude-opus-4.8` |
| `Hugh/README.md` | 438 | `claude-opus-4.6` | `claude-opus-4.8` |

#### Category 3: Agent Body Documentation (1 file, 2 locations)

| File | Line | Old | New |
|------|------|-----|-----|
| `agents/hugh.md` | 403 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |
| `agents/hugh.md` | 1115 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |

#### Category 4: Stale 4.5 References → 4.8 (2 files, 4 locations)

| File | Line | Old | New |
|------|------|-----|-----|
| `.github/copilot-instructions.md` | 49 | `model: Claude Opus 4.5` | `model: Claude Opus 4.8` |
| `.github/copilot-instructions.md` | 62 | `typically Claude Opus 4.5` | `typically Claude Opus 4.8` |
| `README.md` | 67 | `model: Claude Opus 4.5` | `model: Claude Opus 4.8` |
| `README.md` | 298 | `model: Claude Opus 4.5` | `model: Claude Opus 4.8` |

#### Category 5: copilot-instructions.md Hugh Config Example (1 location)

| File | Line | Old | New |
|------|------|-----|-----|
| `.github/copilot-instructions.md` | 422 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |

**Total: 19 edit locations across 12 unique files**

## Dependencies

| Dependency | Type | Status |
|------------|------|--------|
| Claude Opus 4.8 available in VS Code Copilot model picker | External | Verify pre-implementation |
| Claude Opus 4.8 available via Copilot CLI SDK | External | Verify pre-implementation |
| Research 028 completed | Internal | ✅ Complete |

## Risks

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| Model identifier `claude-opus-4.8` doesn't match actual API name | High | Low | Verify in VS Code model picker and Copilot CLI before committing |
| Model not yet available on org subscription | High | Low | Check availability before implementation |
| Behavioral regression on new model | Medium | Low | Smoke test one agent before bulk commit |
| Stale references missed | Low | Low | Research grep verified no additional occurrences |

### Rollback Procedure

- **Full rollback:** `git revert <sha>` on the single upgrade commit
- **Hugh-only rollback:** Edit `appsettings.json` back to `"claude-opus-4.6"` (no recompilation needed)
- **Runtime override:** Set `HUGH_SDK_TEST_MODEL=claude-opus-4.6` environment variable
- **VS Code agent rollback:** Edit any agent's YAML line 4 (takes effect on next chat session)

## Execution Plan

### Phase 1: Model Upgrade (All Changes)

**Status:** ✅ Complete
**Completed:** 2026-08-19
**Size:** Medium
**Files to Modify:** 12
**Prerequisites:** Verify Claude Opus 4.8 is available in VS Code model picker
**Entry Point:** `agents/pch-planner.md`
**Verification:** `dotnet build` succeeds; `dotnet test` passes; grep confirms no remaining `4.6` or `4.5` model references

| Step | Task | Files | Acceptance Criteria | Status |
|------|------|-------|---------------------|--------|
| 1.1 | Update all 7 agent YAML `model:` fields from `Claude Opus 4.6` to `Claude Opus 4.8` | `agents/hugh.md`, `agents/pch-coder.md`, `agents/pch-helper.md`, `agents/pch-plan-reviewer.md`, `agents/pch-planner.md`, `agents/pch-researcher.md`, `agents/pch-visionary.md` | All 7 files have `model: Claude Opus 4.8` on line 4 | ✅ Complete |
| 1.2 | Update Hugh `appsettings.json` model value | `Hugh/appsettings.json` | `"Model": "claude-opus-4.8"` in Copilot section | ✅ Complete |
| 1.3 | Update `HughConfig.cs` hardcoded default | `Hugh/Configuration/HughConfig.cs` | `public string Model { get; set; } = "claude-opus-4.8";` | ✅ Complete |
| 1.4 | Update `Hugh/README.md` model references (3 locations) | `Hugh/README.md` | All 3 occurrences of `claude-opus-4.6` replaced with `claude-opus-4.8` | ✅ Complete |
| 1.5 | Update `agents/hugh.md` body model references (2 locations) | `agents/hugh.md` | Both `"claude-opus-4.6"` occurrences in body replaced with `"claude-opus-4.8"` | ✅ Complete |
| 1.6 | Fix stale `Claude Opus 4.5` references in `.github/copilot-instructions.md` (3 locations: 2 stale 4.5 + 1 Hugh config 4.6) | `.github/copilot-instructions.md` | No remaining `4.5` or `4.6` model references | ✅ Complete |
| 1.7 | Fix stale `Claude Opus 4.5` references in root `README.md` (2 locations) | `README.md` | No remaining `4.5` model references | ✅ Complete |
| 1.8 | Run `dotnet build` and `dotnet test` to confirm no compilation/test regressions | — | Build succeeds; all tests pass | ✅ Complete |
| 1.9 | Grep entire repo for remaining `opus-4.6`, `opus-4.5`, `Opus 4.6`, `Opus 4.5` (excluding `docs/plans/` and `docs/research/`) to confirm no missed references | — | Zero hits outside historical documents | ✅ Complete |

### Post-Implementation Verification

- Invoke `@pch-helper` in VS Code and confirm it responds using Opus 4.8
- Run `hugh --check` to verify Hugh CLI still passes prerequisite checks
- Verify subagent orchestration works (invoke `@pch-coder` with trivial task)

## Standards

No organizational standards applicable to this plan.

## Review Session Log

**Questions Pending:** 0
**Questions Resolved:** 0
**Last Updated:** 2026-08-19

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| — | No clarifying questions required | — | — | — |

## Review Summary

**Review Date:** 2026-08-19
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v1.0
**Reviewed Plan Version:** v1.1

### Review Metrics
- Issues Found: 1 (Critical: 0, Major: 0, Minor: 1)
- Clarifying Questions Asked: 0
- Sections Updated: Edit Inventory total count corrected

### Key Improvements Made
1. Corrected edit location total from 18 to 19 (7+5+2+4+1)

### Codebase Verification
- All 18 non-trivial file path + line number claims verified against live codebase ✅
- No test files assert on model string — zero test breakage risk confirmed ✅
- Comprehensive grep confirms no missed references outside historical documents ✅

### Remaining Considerations
- Verify Claude Opus 4.8 availability in VS Code model picker before implementing
- Verify `claude-opus-4.8` identifier works in Copilot CLI SDK before committing

### Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 2 | 12 files, all simple string replacements |
| New patterns introduced | 1 | No new patterns |
| External dependencies | 2 | Model availability in VS Code + CLI SDK |
| Migration complexity | 1 | Single commit, instant revert |
| Test coverage required | 1 | Existing tests pass unchanged |
| **Overall Complexity** | **7/25** | Low |

### Sign-off
This plan has been reviewed and is **Ready for Implementation**.

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2026-08-19 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2026-08-19 |
| Status | ✅ Complete |
| Implemented By | pch-coder |
| Implementation Date | 2026-08-19 |
| Plan Location | /docs/plans/030-pch-agents-model-upgrade-to-opus-48.md |

## Implementation Notes

### Phase 1 - Model Upgrade (All Changes)

**Completed:** 2026-08-19

**Files Modified:**

- `agents/hugh.md`
- `agents/pch-coder.md`
- `agents/pch-helper.md`
- `agents/pch-plan-reviewer.md`
- `agents/pch-planner.md`
- `agents/pch-researcher.md`
- `agents/pch-visionary.md`
- `Hugh/appsettings.json`
- `Hugh/Configuration/HughConfig.cs`
- `Hugh/README.md`
- `.github/copilot-instructions.md`
- `README.md`

**Deviations from Plan:** None

**Verification:**

- `dotnet build` — succeeded
- `dotnet test` — 953 tests passed, 0 failed
- Grep for remaining `opus-4.6`, `opus-4.5`, `Opus 4.6`, `Opus 4.5` — zero hits outside historical documents

### Plan Completion

**All phases completed:** 2026-08-19
**Total tasks completed:** 9
**Total files modified:** 12
