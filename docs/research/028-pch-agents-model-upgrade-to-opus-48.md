---
id: "028"
type: research
title: "PCH Agents Model Upgrade to Claude Opus 4.8"
status: ✅ Complete
created: "2026-08-19"
current_phase: "3 of 3"
---

## Introduction

This research investigates upgrading the PCH agent system's default AI model from Claude Opus 4.6 to Claude Opus 4.8. It confirms the correct model identifier string, documents all locations where the model is configured (agent YAML front matter, Hugh CLI config, and subagent invocation), and determines how to ensure subagents also use the updated model.

## Objectives

- Confirm the correct model name/identifier for Claude Opus 4.8 in VS Code Copilot agent YAML front matter
- Identify all locations where the model is currently set to Claude Opus 4.6
- Determine how subagent model selection works (inherited from parent agent, or independently configured)
- Document the complete change procedure to upgrade all agents and subagents to Claude Opus 4.8

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | Model Identifier Validation & Configuration Inventory | ✅ Complete | Validate correct Claude Opus 4.8 model string for VS Code agent YAML; inventory all config locations (7 agent files, appsettings.json, copilot-instructions.md) | 2026-08-19 |
| 2 | Subagent Model Inheritance & Override Mechanisms | ✅ Complete | Determine how subagent model is selected when invoked via `runSubagent`; check if parent model propagates or if each agent needs its own `model:` field; review Hugh SDK `SessionConfig.Model` behavior | 2026-08-19 |
| 3 | Change Procedure & Impact Analysis | ✅ Complete | Document exact steps to upgrade; identify risks (e.g., model availability, token limits, cost differences); list all files to modify | 2026-08-19 |

## Phase 1: Model Identifier Validation & Configuration Inventory

**Status:** ✅ Complete  
**Session:** 2026-08-19

### Model Identifier Format

Two distinct model identifier formats are used depending on context:

| Context | Current Value | Format Pattern |
|---------|--------------|----------------|
| VS Code agent YAML `model:` field | `Claude Opus 4.6` | Human-readable: `Claude {Family} {Version}` |
| Hugh SDK / `appsettings.json` | `"claude-opus-4.6"` | Kebab-case API identifier: `claude-opus-{version}` |

**For Claude Opus 4.8, the identifiers would be:**
- **YAML front matter:** `Claude Opus 4.8`
- **Hugh SDK / appsettings.json:** `"claude-opus-4.8"`

### Complete Configuration Inventory

#### Category 1: Agent YAML Front Matter (7 files)

All 7 agent files currently set `model: Claude Opus 4.6` on line 4:

| File | Current Value | Line |
|------|--------------|------|
| `agents/hugh.md` | `Claude Opus 4.6` | 4 |
| `agents/pch-coder.md` | `Claude Opus 4.6` | 4 |
| `agents/pch-helper.md` | `Claude Opus 4.6` | 4 |
| `agents/pch-plan-reviewer.md` | `Claude Opus 4.6` | 4 |
| `agents/pch-planner.md` | `Claude Opus 4.6` | 4 |
| `agents/pch-researcher.md` | `Claude Opus 4.6` | 4 |
| `agents/pch-visionary.md` | `Claude Opus 4.6` | 4 |

#### Category 2: Hugh CLI Configuration (3 files)

| File | Setting | Current Value | Line |
|------|---------|--------------|------|
| `Hugh/appsettings.json` | `Hugh.Copilot.Model` | `"claude-opus-4.6"` | 23 |
| `Hugh/Configuration/HughConfig.cs` | `CopilotConfig.Model` default | `"claude-opus-4.6"` | 188 |
| `Hugh/README.md` | Config example | `"claude-opus-4.6"` | 91 |
| `Hugh/README.md` | Config table | `claude-opus-4.6` | 106 |
| `Hugh/README.md` | Env var table (`HUGH_SDK_TEST_MODEL`) | `claude-opus-4.6` | 438 |

#### Category 3: Documentation References (3 files)

These contain **stale** "Claude Opus 4.5" values missed during the 4.5→4.6 upgrade:

| File | Context | Current Value | Lines |
|------|---------|--------------|-------|
| `.github/copilot-instructions.md` | YAML example | `Claude Opus 4.5` | 49 |
| `.github/copilot-instructions.md` | Field description | `Claude Opus 4.5` | 62 |
| `.github/copilot-instructions.md` | Hugh config example | `"claude-opus-4.6"` | 422 |
| `README.md` | YAML example | `Claude Opus 4.5` | 67 |
| `README.md` | Agent format example | `Claude Opus 4.5` | 298 |

#### Category 4: Agent Body Documentation (1 file)

`agents/hugh.md` has additional model references in its body content:

| Location in `agents/hugh.md` | Current Value | Line |
|------------------------------|--------------|------|
| Config table `Hugh.Copilot.Model` | `"claude-opus-4.6"` | 403 |
| Example JSON `"Model":` | `"claude-opus-4.6"` | 1115 |

#### Category 5: Historical Docs (NOT requiring update)

Plan and research documents under `docs/plans/` and `docs/research/` reference prior model versions. These are historical records and should **not** be updated.

**Key Discoveries:**
- All 7 agent YAML front matter files use `model: Claude Opus 4.6` consistently on line 4
- Two format patterns: human-readable (`Claude Opus 4.6`) for YAML vs kebab-case (`claude-opus-4.6`) for SDK
- `.github/copilot-instructions.md` and root `README.md` have stale "Claude Opus 4.5" references missed during prior upgrade
- `agents/hugh.md` has 2 additional body references beyond YAML front matter
- `Hugh/Configuration/HughConfig.cs` has a hardcoded default fallback value
- **Total files requiring update: 12 unique files**

| File | Relevance |
|------|-----------|
| `agents/hugh.md` | YAML model field + body config references |
| `agents/pch-coder.md` | YAML model field |
| `agents/pch-helper.md` | YAML model field |
| `agents/pch-plan-reviewer.md` | YAML model field |
| `agents/pch-planner.md` | YAML model field |
| `agents/pch-researcher.md` | YAML model field |
| `agents/pch-visionary.md` | YAML model field |
| `Hugh/appsettings.json` | Runtime config |
| `Hugh/Configuration/HughConfig.cs` | Hardcoded default |
| `Hugh/README.md` | Documentation examples |
| `.github/copilot-instructions.md` | YAML examples + Hugh config |
| `README.md` | YAML examples |

**Gaps:** Model identifier `claude-opus-4.8` is assumed based on established pattern. If Anthropic uses a different naming convention, the identifier may differ.  
**Assumptions:** Naming follows same pattern as 4.5 and 4.6 versions.

## Phase 2: Subagent Model Inheritance & Override Mechanisms

**Status:** ✅ Complete  
**Session:** 2026-08-19

### VS Code `runSubagent` — Model Selection Behavior

The `runSubagent` tool has exactly three parameters:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `prompt` | string | Yes | Detailed task description |
| `description` | string | Yes | Short (3-5 word) task description |
| `agentName` | string | No | Specific agent to invoke |

**There is no `model` parameter on `runSubagent`.** The tool does not accept a model override.

When `agentName` is specified (as PCH always does), the subagent loads the custom agent definition file. The subagent’s own YAML `model:` field governs model selection — not the parent agent’s model.

Both pch-coder and pch-researcher invoke subagents using themselves as the agent name, so the same `model:` field governs both parent and subagent execution. **Updating the agent file’s `model:` field automatically updates model for both parent and subagent contexts.**

### Hugh SDK — `SessionConfig.Model` Behavior

Hugh’s CLI orchestration is completely separate from VS Code’s `runSubagent`. Hugh creates SDK sessions directly via `CopilotClient.CreateSessionAsync()` using a single centralized model setting:

```csharp
var sessionConfig = new SdkSessionConfig
{
    Model = _config.Copilot.Model,
```

Hugh does NOT read agent YAML `model:` fields — it injects agent prompts as `SystemMessage.Content` and uses `HughConfig.Copilot.Model` for all phases.

The `HUGH_SDK_TEST_MODEL` env var allows CI to override the model without changing `appsettings.json`.

### Handoff Model Override

Handoffs support an optional `model:` field in YAML, but no PCH agent handoffs currently use it. Irrelevant to this upgrade.

### Summary: Two Independent Model Control Surfaces

| Context | Model Source | Config Location | Update Required |
|---------|-------------|-----------------|------------------|
| **VS Code — Agent as parent** | Agent YAML `model:` field | `agents/{name}.md` front matter | Yes — all 7 files |
| **VS Code — Agent as subagent** | Subagent’s own YAML `model:` field | Same agent file | Yes — same 7 files |
| **VS Code — Handoff target** | Target agent’s YAML `model:` field | Same agent file | Yes — same 7 files |
| **Hugh CLI — All phases** | `SessionConfig.Model` from config | `appsettings.json` + `HughConfig.cs` | Yes — 2 locations |
| **Hugh CLI — Test override** | `HUGH_SDK_TEST_MODEL` env var | Environment / CI config | Optional |

**Key Discoveries:**
- `runSubagent` has NO `model` parameter — model comes from the agent definition file
- Named subagents use their own `model:` field, not the parent’s model
- Updating agent YAML `model:` covers parent, subagent, and handoff contexts automatically
- Hugh SDK uses one centralized `HughConfig.Copilot.Model` for all phases — independent from agent YAML
- Handoff `model:` override exists but is unused by PCH agents

| File | Relevance |
|------|-----------|
| `agents/pch-coder.md` | Subagent orchestrator; invokes subagents with `agentName: "pch-coder"` |
| `agents/pch-researcher.md` | Subagent orchestrator; invokes subagents with `agentName: "pch-researcher"` |
| `Hugh/Agents/CopilotService.cs` | Creates SDK sessions with `Model = _config.Copilot.Model` |
| `Hugh/Configuration/HughConfig.cs` | `CopilotConfig.Model` default value |
| `docs/research/025-copilot-autopilot-and-agent-handoff-automation.md` | Documents subagent model selection |

**Gaps:** Exact priority resolution when custom agent has `model:` field vs parent session model has not been empirically tested — interpretation is based on prior research documentation.  
**Assumptions:** Named agents use their own `model:` field based on research 025 stating subagents receive "the subagent’s own model, tools, and instructions."

## Phase 3: Change Procedure & Impact Analysis

**Status:** ✅ Complete  
**Session:** 2026-08-19

### Verification of Phase 1 Inventory

All 12 files verified. No additional model references found in tests, CI workflows, scripts, or installer. Phase 1’s inventory is **complete and accurate**.

### Complete Upgrade Procedure

#### Step 1: Update Agent YAML Front Matter (7 files, line 4 each)

Change `model: Claude Opus 4.6` → `model: Claude Opus 4.8`

| File | Old | New |
|------|-----|-----|
| `agents/hugh.md` | `Claude Opus 4.6` | `Claude Opus 4.8` |
| `agents/pch-coder.md` | `Claude Opus 4.6` | `Claude Opus 4.8` |
| `agents/pch-helper.md` | `Claude Opus 4.6` | `Claude Opus 4.8` |
| `agents/pch-plan-reviewer.md` | `Claude Opus 4.6` | `Claude Opus 4.8` |
| `agents/pch-planner.md` | `Claude Opus 4.6` | `Claude Opus 4.8` |
| `agents/pch-researcher.md` | `Claude Opus 4.6` | `Claude Opus 4.8` |
| `agents/pch-visionary.md` | `Claude Opus 4.6` | `Claude Opus 4.8` |

#### Step 2: Update Hugh CLI Configuration (3 files, 5 locations)

| File | Line | Old | New |
|------|------|-----|-----|
| `Hugh/appsettings.json` | 23 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |
| `Hugh/Configuration/HughConfig.cs` | 188 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |
| `Hugh/README.md` | 91 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |
| `Hugh/README.md` | 106 | `claude-opus-4.6` | `claude-opus-4.8` |
| `Hugh/README.md` | 438 | `claude-opus-4.6` | `claude-opus-4.8` |

#### Step 3: Update Agent Body Documentation (1 file, 2 locations)

| File | Line | Old | New |
|------|------|-----|-----|
| `agents/hugh.md` | 403 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |
| `agents/hugh.md` | 1115 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |

#### Step 4: Fix Stale 4.5 References → 4.8 (2 files, 4 locations)

| File | Line | Old | New |
|------|------|-----|-----|
| `.github/copilot-instructions.md` | 49 | `Claude Opus 4.5` | `Claude Opus 4.8` |
| `.github/copilot-instructions.md` | 62 | `Claude Opus 4.5` | `Claude Opus 4.8` |
| `README.md` | 67 | `Claude Opus 4.5` | `Claude Opus 4.8` |
| `README.md` | 298 | `Claude Opus 4.5` | `Claude Opus 4.8` |

#### Step 5: Update copilot-instructions.md Hugh Config Example

| File | Line | Old | New |
|------|------|-----|-----|
| `.github/copilot-instructions.md` | 422 | `"claude-opus-4.6"` | `"claude-opus-4.8"` |

### Change Order & Rationale

All steps in a **single atomic commit**. Agent YAML and Hugh config are independent control surfaces — no phased rollout needed.

Suggested commit message: `Update all model references from Claude Opus 4.6 to Claude Opus 4.8`

### Risk Analysis

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| Model not yet available | High | Low | Verify in VS Code model picker before committing |
| Behavioral regression | Medium | Low | Smoke test one agent on 4.8 first |
| Token limit changes | Low | Low | Hugh’s compaction thresholds are ratio-based, adapt automatically |
| Cost increase | Low | Medium | Check Anthropic pricing |
| Kebab-case identifier assumption | Medium | Low | Verify via Copilot CLI before committing |

### Rollback Procedure

- **Full rollback:** `git revert <sha>` on the single upgrade commit
- **Hugh-only rollback:** Edit `appsettings.json` back to `claude-opus-4.6` (no recompilation needed)
- **Runtime override:** Set `HUGH_SDK_TEST_MODEL=claude-opus-4.6` env var
- **VS Code agent rollback:** Edit any agent’s YAML line 4 (takes effect on next chat session)

### Testing Approach

1. **Pre-upgrade:** Confirm `Claude Opus 4.8` appears in VS Code Copilot model picker
2. **VS Code smoke tests:** Invoke `@pch-helper`, test subagent orchestration, verify handoffs
3. **Hugh smoke tests:** Run `hugh --check`, invoke with trivial request
4. **No unit test changes needed** — tests don’t reference model strings

**Key Discoveries:**
- Phase 1’s inventory is 100% complete — no missed references
- 18 edit locations across 12 files, all verified with exact values
- Pure configuration/documentation changes — zero code logic changes, zero test changes
- Single atomic commit recommended
- Stale 4.5 references should go directly to 4.8 (skip 4.6)

**Gaps:** None  
**Assumptions:** Kebab-case identifier `claude-opus-4.8` follows established pattern. Model assumed available on org’s Copilot subscription.

## Overview

Upgrading from Claude Opus 4.6 to 4.8 requires updating **18 edit locations across 12 files**. The changes are purely configuration and documentation — no code logic, no tests, and no CI/CD modifications needed.

The model is configured through two independent control surfaces: **VS Code agent YAML** (`model: Claude Opus 4.8` in 7 agent files) and **Hugh CLI** (`"claude-opus-4.8"` in `appsettings.json` + `HughConfig.cs`). These are fully independent — VS Code never reads Hugh config and Hugh never reads agent YAML front matter.

For VS Code subagent orchestration, the `runSubagent` tool has no `model` parameter. Named subagents load their own agent definition file, so each agent’s YAML `model:` field controls model selection in parent, subagent, and handoff contexts. Updating the 7 agent files is sufficient to cover all VS Code model contexts.

The upgrade also corrects stale "Claude Opus 4.5" references in documentation files (`.github/copilot-instructions.md` and `README.md`) that were missed during the 4.5→4.6 upgrade.

## Key Findings

- Two model identifier formats: `Claude Opus 4.8` (YAML front matter) and `claude-opus-4.8` (Hugh SDK/config)
- 12 unique files require updating with 18 total edit locations
- `runSubagent` has no `model` parameter — subagents use their own agent file’s `model:` field
- Hugh SDK uses a single centralized `HughConfig.Copilot.Model` for all phases, independent from agent YAML
- No model references exist in tests, CI workflows, scripts, or installer
- Hugh’s compaction thresholds are ratio-based (0.8/0.95), adapting automatically to any model’s context window
- Stale "Claude Opus 4.5" references in docs should go directly to 4.8

## Actionable Conclusions

- Execute all 18 edits in a **single atomic commit** — no phased rollout needed
- **Pre-flight check:** Verify `Claude Opus 4.8` appears in VS Code Copilot model picker before committing
- **Smoke test one agent** (e.g., `@pch-helper`) on 4.8 before bulk upgrade to catch behavioral regression
- **Rollback is trivial** — `git revert` or edit `appsettings.json` at runtime for Hugh
- This is a straightforward single-phase plan for `pch-planner` / `pch-coder`

## Open Questions

- The kebab-case identifier `claude-opus-4.8` is assumed based on the `4.5`/`4.6` pattern — verify via Copilot CLI or API before implementation
- Exact priority resolution when a named agent’s `model:` field conflicts with the parent session’s model has not been empirically tested (prior research documents give consistent but unverified interpretation)

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2026-08-19 |
| Status | ✅ Complete |
| Current Phase | ✅ Complete |
| Path | /docs/research/028-pch-agents-model-upgrade-to-opus-48.md |
