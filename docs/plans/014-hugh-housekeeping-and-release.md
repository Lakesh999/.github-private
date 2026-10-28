# 014 — Hugh Housekeeping and Release Workflow

## Document Information

```yaml
id: "014"
type: plan
title: "Hugh Housekeeping and Release Workflow"
status: ready-for-implementation
version: "v1.1"
created: "2026-02-10"
owner: pch-planner

versions:
  - version: "v1.0"
    date: "2026-02-10"
    author: pch-planner
    changes: Initial plan creation — 3 phases covering housekeeping, release workflow, and prompt validation
  - version: "v1.1"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Review complete — Phase 2 replaced with verification (workflow already exists), Phase 1 expanded to include Phase 5 status update, Phase 3 steps 3.9/3.10 clarified"
```

## Review Session Log

**Questions Pending:** 0 | **Questions Resolved:** 2 | **Last Updated:** 2026-02-10

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | Phase 2 (GitHub Actions Release Workflow) already exists in `.github/workflows/release.yml` | Correctness | Option A: Replace Phase 2 with verification | Phase 2 converted to verification task |
| 2 | Plan 010 Phase 5 also shows "Not Started" but workflow exists | Completeness | Option A: Add Phase 5 update to Phase 1 scope | Task 1.4 added to Phase 1 |

## Overview

This plan addresses three housekeeping and quality items for Hugh:

1. **Update Plan 010 Status** — Phases 1–3 of Plan 010 are implemented in code but the plan document shows them as "Not Started". Update the document to reflect reality.
2. **GitHub Actions Release Workflow** — Implement Plan 010 Phase 5 to enable automated releases via GitHub Releases when tags are pushed.
3. **Prompt Validation Checklist** — Complete the 10-item manual validation checklist from Plan 013 to verify subagent question passthrough works correctly.

### Objectives

1. Accurate documentation — Plan 010 reflects actual implementation status
2. Distribution capability — Tag-triggered CI/CD publishes Hugh to GitHub Releases
3. Quality assurance — Verified prompt behavior across all 6 agents

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR1 | Plan 010 document shows Phases 1–3 as complete | P0 |
| FR2 | GitHub Actions workflow triggers on `v*.*.*` tags | P0 |
| FR3 | Workflow runs tests before publishing | P0 |
| FR4 | Workflow compiles Inno Setup installer | P0 |
| FR5 | Workflow publishes installer and portable ZIP to GitHub Releases | P0 |
| FR6 | All 10 prompt validation checklist items verified | P1 |

### Out of Scope

- ARM64 builds (deferred per Plan 010)
- Code signing (future enhancement)
- macOS/Linux installers (future work)

## Dependencies

| Dependency | Type | Impact |
|------------|------|--------|
| Plan 010 Phases 1–3 code | Already complete | Referenced in housekeeping |
| Plan 010 Phase 4 (Inno Setup) | Already complete | Required for Phase 2 |
| Plan 013 agent prompts | Already complete | Required for Phase 3 validation |
| Inno Setup GitHub Action | External | `Minionguyjpro/Inno-Setup-Action` |
| GitHub Release Action | External | `softprops/action-gh-release@v2` |

## Progress Summary

| Phase | Description | Status | Tasks |
|-------|-------------|--------|-------|
| 1 | Plan 010 Status Update | ✅ Complete | 4/4 |
| 2 | Verify Release Workflow | ✅ Complete | 2/2 |
| 3 | Prompt Validation Checklist | ⚠️ Partial | 8/10 |

---

## Execution Plan

### Phase 1: Plan 010 Status Update

**Estimated Session Size:** Small (documentation only)
**Files to Modify:** 1
**Prerequisites:** None
**Entry Point:** Read `docs/plans/010-hugh-global-cli-installation.md`
**Verification:** Document shows accurate phase completion status

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Update Phase 1 status to Complete | `docs/plans/010-hugh-global-cli-installation.md` | Progress Summary shows Phase 1 ✅ Complete |
| 1.2 | Update Phase 2 status to Complete | `docs/plans/010-hugh-global-cli-installation.md` | Progress Summary shows Phase 2 ✅ Complete |
| 1.3 | Update Phase 3 status to Complete | `docs/plans/010-hugh-global-cli-installation.md` | Progress Summary shows Phase 3 ✅ Complete |
| 1.4 | Update Phase 5 status to Complete | `docs/plans/010-hugh-global-cli-installation.md` | Progress Summary shows Phase 5 ✅ Complete (release workflow exists) |

### Phase 2: Verify Release Workflow

**Estimated Session Size:** Small (verification only)
**Files to Modify:** 0 (read-only verification)
**Prerequisites:** Phase 1 complete
**Entry Point:** Read `.github/workflows/release.yml`
**Verification:** Existing workflow matches expected capabilities

> **Note:** The release workflow already exists at `.github/workflows/release.yml`. This phase verifies its completeness rather than creating it.

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Verify workflow configuration | `.github/workflows/release.yml` | Workflow triggers on `v*.*.*` tags and `workflow_dispatch`; runs on `windows-latest`; tests execute before publishing |
| 2.2 | Verify artifact generation | `.github/workflows/release.yml` | Workflow creates `hugh-setup-{version}.exe` and `hugh-portable-{version}-win-x64.zip` and uploads to GitHub Release |

**Existing Workflow Details:**

The workflow at `.github/workflows/release.yml` already implements:
- Tag trigger: `v*.*.*` pattern
- Manual trigger: `workflow_dispatch` with version input
- .NET 8 setup with `dotnet test` execution
- Single-file self-contained x64 publish
- Portable ZIP: `hugh-portable-{version}-win-x64.zip`
- Inno Setup: `Minionguyjpro/Inno-Setup-Action@v1.2.4`
- GitHub Release: `softprops/action-gh-release@v2`

### Phase 3: Prompt Validation Checklist

**Estimated Session Size:** Medium (manual verification)
**Files to Modify:** 1 (Plan 013 to record results)
**Prerequisites:** Plan 013 implementation complete (it is)
**Entry Point:** Read all 6 agent files in `agents/` folder
**Verification:** All 10 checklist items marked Pass

| Step | Checklist Item | Verification Method | Acceptance Criteria |
|------|----------------|---------------------|---------------------|
| 3.1 | Subagent Section Presence | Read all 6 agent `.md` files | Each contains "When Running as a Subagent" section with `needs_user_input` return format |
| 3.2 | pch-coder Invocation Template | Read `agents/pch-coder.md` | STATUS includes `needs_user_input`; 4 question fields in return format |
| 3.3 | pch-researcher Invocation Template | Read `agents/pch-researcher.md` | Same as 3.2 |
| 3.4 | pch-coder Processing | Read `agents/pch-coder.md` | "Handling `needs_user_input` Status" section with 5-step workflow |
| 3.5 | pch-researcher Processing | Read `agents/pch-researcher.md` | Same handling section adapted for research |
| 3.6 | pch-coder Mode Removal | Read `agents/pch-coder.md` | No mode selection UI; always Automatic Mode |
| 3.7 | pch-researcher Pre-Flight | Read `agents/pch-researcher.md` | Ambiguity Triggers table has 10 rows (5 existing + 5 new) |
| 3.8 | Never Answer Rule | Read both orchestrator agents | 🚨 CRITICAL warning present in handling sections |
| 3.9 | VS Code Test | Manual test in VS Code | In VS Code Copilot chat, invoke `@pch-coder` with an ambiguous task (e.g., "implement a new feature"). Verify the agent returns `needs_user_input` and the question is displayed to the user |
| 3.10 | Backward Compat | Run Hugh with existing plan | Execute `hugh` on a workspace with a completed plan at `/docs/plans/`. Verify `success`, `partial`, and `blocked` STATUS values are processed correctly without regression |

**Phase 3 Outcome:** Update Plan 013's Prompt Validation Checklist section with Pass/Fail results.

---

## Implementation Notes for AI Agents

### Phase 2 Verification Notes

Phase 2 is now verification-only since the release workflow already exists. When verifying:

1. **Confirm trigger configuration** — Check that `on.push.tags` includes `v*.*.*` pattern
2. **Confirm test execution** — Verify `dotnet test` step runs before `dotnet publish`
3. **Confirm artifact names** — Outputs are `hugh-setup-{version}.exe` and `hugh-portable-{version}-win-x64.zip`
4. **Confirm Inno Setup version** — Uses `Minionguyjpro/Inno-Setup-Action@v1.2.4`

### Phase 3 Execution Notes

- Steps 3.1–3.8 are file reads; can be parallelized
- Step 3.9 requires VS Code with Copilot; manually invoke a test question scenario
- Step 3.10 requires running Hugh or VS Code with existing plan/research to verify no regression

---

## Review Summary

**Review Date:** 2026-02-10
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v1.0
**Reviewed Plan Version:** v1.1

### Review Metrics
- Issues Found: 5 (Critical: 1, Major: 2, Minor: 2)
- Clarifying Questions Asked: 2
- Sections Updated: Progress Summary, Phase 1, Phase 2, Phase 3, Implementation Notes, Handoff

### Key Improvements Made
1. Replaced Phase 2 (create workflow) with verification tasks — workflow already exists at `.github/workflows/release.yml`
2. Added task 1.4 to Phase 1 to update Plan 010 Phase 5 status (also already complete)
3. Clarified Phase 3 steps 3.9 and 3.10 with specific test scenarios and acceptance criteria
4. Corrected artifact naming to match actual workflow outputs (`hugh-setup-*`, `hugh-portable-*`)
5. Updated Inno Setup action version reference to v1.2.4

### Remaining Considerations
- Phase 2 verification is read-only; if verification fails, create a new task to fix the workflow
- Phase 3 step 3.9 (VS Code Test) requires Copilot with subagent support enabled

### Sign-off
This plan has been reviewed and is **Ready for Implementation**

---

## Handoff Information

```yaml
handoff:
  created_by: pch-planner
  created_date: "2026-02-10"
  reviewed_by: pch-plan-reviewer
  review_date: "2026-02-10"
  status: ready-for-implementation
  next_agent: pch-coder
  plan_location: "/docs/plans/014-hugh-housekeeping-and-release.md"
```
