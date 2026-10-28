# Implementation Plan: Standards Integration for pch-planner, pch-plan-reviewer, pch-researcher, and beta-pch-researcher

## Document Information

| Field | Value |
|-------|-------|
| Document | 003-pch-agents-standards-integration.md |
| Created | January 16, 2026 |
| Status | ✅ Implementation Complete |
| Owner | pch-planner |
| Version | v2.4 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | January 16, 2026 | pch-planner | Initial plan creation |
| v2.0 | January 16, 2026 | pch-planner | Completed planning; holistic review done |
| v2.1 | January 16, 2026 | pch-plan-reviewer | Review complete; clarified section placements and acceptance criteria |
| v2.2 | January 16, 2026 | pch-planner | Added pch-plan-reviewer to scope for same MCP/standards context as pch-planner |
| v2.3 | January 16, 2026 | pch-plan-reviewer | Re-review: clarified pch-plan-reviewer placement; fixed agent count references |
| v2.4 | January 16, 2026 | pch-coder | All phases complete; implementation finished |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | January 16, 2026 | Initial Context | Research document 003 provided | Comprehensive research on MCP/Copilot Spaces integration completed |
| 1 | January 16, 2026 | Copilot Space Name | A: "pch-standards-space" | Maintains pch-* naming convention; clear agent-standards relationship |
| 1 | January 16, 2026 | Standards Placement (planner) | B: New separate "Standards Integration" section | Clearer separation; dedicated visibility for standards workflow |
| 1 | January 16, 2026 | Standards Placement (researchers) | C: Standalone "Standards Integration" section | Consistency with planner; clear visibility without interleaving |
| 1 | January 16, 2026 | Fallback Behavior | B: Warn and Continue | Balances visibility with workflow continuity; supports rollout period |
| 1 | January 16, 2026 | Standards Citation | A: "Referenced Standards" section | Clear traceability; consistent with existing pattern documentation |
| 1 | January 16, 2026 | Section Placement | B: Before Workflow Process | Logical flow: context → standards → workflow execution |
| 1 | January 16, 2026 | Query Approach | A: Semantic Query | Leverages built-in Spaces semantic search; natural integration |

---

## Review Session Log

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | Where to place Standards Integration in pch-planner? | Correctness | A: Subsection within Context Gathering (Step 0) | Updated Section Placement table; revised Phase 1 tasks |
| 2 | Should researcher agents match pch-planner's subsection approach? | Clarity | B: Keep as top-level section | No change needed; structural difference justified by agent organization |
| 3 | Where to place Standards Integration in pch-plan-reviewer? | Clarity | A: New section before Review Checklist | Updated Section Placement table; removed ambiguous OR option |

---

## Holistic Review Summary

### Decision Interactions Analysis

All planning decisions are complementary with no conflicts identified:

| Decision 1 | Decision 2 | Interaction |
|------------|------------|-------------|
| Separate "Standards Integration" section | Placement before Workflow | ✅ Clean separation — standards loaded just before execution |
| Semantic Query approach | Warn and Continue fallback | ✅ Compatible — query may fail gracefully if Space unavailable |
| "Referenced Standards" section output | Consistent section across agents | ✅ Uniform documentation pattern aids review and compliance |

### Architectural Considerations

1. **MCP Dependency is Optional**
   The design ensures agents function normally without MCP/Spaces. Standards are additive guidance, not a hard dependency. This supports gradual rollout.

2. **Token Budget Awareness**
   Instructions limit standards to ~3000-4000 tokens. This leaves sufficient context for the agent's primary work (planning/research).

3. **Subagent Inheritance (beta-pch-researcher)**
   Subagents invoked via `runSubagent` inherit MCP configuration from the parent agent context. Standards access should work in automatic mode.

### Trade-offs Accepted

| Trade-off | Decision | Rationale |
|-----------|----------|-----------|
| Additional section increases agent file size | Accept | ~35 lines per agent is minimal; clarity benefits outweigh size |
| Users must configure MCP server | Accept | One-time setup; documented in prerequisites |
| No automatic Space creation | Accept | Org admin responsibility; outside agent scope |

### Risks Acknowledged

| Risk | Mitigation |
|------|------------|
| MCP server not installed by user | Fallback warning; agent continues without standards |
| Copilot Space doesn't exist yet | Fallback warning; agent continues without standards |
| Standards overload context | Token budget guidance; agent should summarize if needed |

### No Follow-up Questions Required

All architectural concerns have been addressed. The plan is ready for execution.

Update four agents (`pch-planner`, `pch-plan-reviewer`, `pch-researcher`, `beta-pch-researcher`) to integrate organizational standards from GitHub Copilot Spaces via MCP. This enables agents to automatically discover and apply PCH organizational standards during planning and research activities.

### Background

Research document [003-standards-integration-for-pch-agents.md](../research/003-standards-integration-for-pch-agents.md) identified that:

1. **GitHub Copilot Spaces** provides organization-wide document sharing that stays automatically synced
2. Standards can be accessed via the **GitHub MCP server's `copilot_spaces` toolset**
3. Agent YAML front matter supports `mcp-servers` field for deploying MCP configuration
4. The `tools` property should be **omitted** to inherit all available tools (including MCP tools)

### Objectives

1. **Enable standards access** — Configure agents to access organizational standards from Copilot Spaces via MCP
2. **Integrate standards discovery** — Add explicit steps for agents to discover and reference applicable standards
3. **Document standards usage** — Agents should cite referenced standards in their outputs
4. **Maintain backward compatibility** — Agents must function correctly even if MCP/Spaces is unavailable

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Add `mcp-servers` configuration to agent YAML front matter | High |
| FR-2 | Remove/omit `tools` property to inherit all available tools | High |
| FR-3 | Add "Standards Discovery" step to pch-planner's Context Gathering | High |
| FR-4 | Add "Standards Discovery" section to researcher agents | High |
| FR-5 | Document how to cite standards in plans and research | Medium |
| FR-6 | Include fallback behavior when MCP/Spaces unavailable | Medium |

### Non-Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR-1 | Minimal changes to existing agent structure | Medium |
| NFR-2 | Clear documentation for maintainability | Medium |
| NFR-3 | Token-efficient standards loading (~3000-4000 token budget) | Low |

### Out of Scope

- Creating the actual Copilot Space (organizational setup task)
- Creating the standards documents themselves
- Changes to pch-coder or pch-helper agents
- Creating `.github/instructions/` folder structure

---

## Technical Design

### Target Files

| File | Current Lines | Estimated New Lines | Purpose |
|------|---------------|---------------------|---------|
| `agents/pch-planner.md` | 364 | ~400 | Add MCP config + Standards Integration section || `agents/pch-plan-reviewer.md` | 337 | ~375 | Add MCP config + Standards Integration section || `agents/pch-researcher.md` | 675 | ~710 | Add MCP config + Standards Integration section |
| `agents/beta-pch-researcher.md` | 1259 | ~1295 | Add MCP config + Standards Integration section |

### YAML Front Matter Changes

All four agents will receive the same YAML front matter pattern. Add `mcp-servers` configuration and ensure `tools` property is **omitted** to inherit all available tools.

**pch-planner.md:**
```yaml
---
name: pch-planner
description: Creates highly detailed implementation plans and technical specifications for new features
model: Claude Opus 4.5
mcp-servers:
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
handoffs:
  - label: Start Plan Review
    agent: pch-plan-reviewer
    prompt: Review the implementation plan for correctness, clarity, and specificity before implementation begins
    send: true
---
```

**pch-plan-reviewer.md:**
```yaml
---
name: pch-plan-reviewer
description: Reviews implementation plans for correctness, clarity, and specificity before implementation
model: Claude Opus 4.5
mcp-servers:
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
handoffs:
  - label: Start Implementation
    agent: pch-coder
    prompt: Now implement the plan outlined above
    send: true
---
```

**pch-researcher.md:**
```yaml
---
name: pch-researcher
description: Researches and outlines multi-step plans
model: Claude Opus 4.5
mcp-servers:
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
handoffs:
  - label: Continue researching
    agent: pch-researcher
    prompt: Researches and outlines multi-step plans
    send: false
---
```

**beta-pch-researcher.md:**
```yaml
---
name: beta-pch-researcher
description: Conducts comprehensive, phased research with optional automatic subagent orchestration
model: Claude Opus 4.5
mcp-servers:
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
handoffs:
  - label: Continue researching
    agent: beta-pch-researcher
    prompt: Continue phased research
    send: false
---
```

### Standards Integration Section Content

Each agent will receive a new "Standards Integration" section with the following structure:

```markdown
## Standards Integration

Before beginning [planning/research], query organizational standards from the PCH Standards Copilot Space.

### Querying Standards

1. **Access the Standards Space**
   Use the GitHub MCP server's `copilot_spaces` toolset to query the organization's "pch-standards-space":
   
   - List available spaces: `list_copilot_spaces`
   - Query relevant standards: `get_copilot_space` with the feature/topic description

2. **Semantic Query**
   Describe the feature or research topic when querying. The Space uses semantic search to return relevant standards.
   
   Example query: "Azure Function implementation with user authentication and REST API design"

3. **Token Budget**
   Limit standards context to ~3000-4000 tokens. If multiple standards apply, prioritize by relevance.

### Fallback Behavior

If the MCP server or Copilot Space is unavailable:

⚠️ **Could not access organizational standards from pch-standards-space. Proceeding without standards context.**

Continue with [planning/research] normally. Standards are supplementary guidance, not a blocking requirement.

### Documenting Standards

Include a "Referenced Standards" section in [plan/research] documents:

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|
| [Standard Name] | [Why it applies] | [Specific guidance used] |

If no standards were found or applicable, note: "No organizational standards applicable to this [plan/research]."
```

### Section Placement

| Agent | Placement Strategy |
|-------|--------------------|
| pch-planner | Add as **step 5 within "Context Gathering (Step 0)"** section |
| pch-plan-reviewer | Insert as **new section before "Review Checklist"** |
| pch-researcher | Insert as new top-level section after "Decision Framework", before "Workflow Process" |
| beta-pch-researcher | Insert as new top-level section after "Decision Framework", before "Workflow Process" |

---

## Dependencies and Prerequisites

| Dependency | Type | Description | Status |
|------------|------|-------------|--------|
| Research 003 | Document | Standards integration research completed | ✅ Complete |
| GitHub MCP Server | Extension | Users must have GitHub MCP server extension installed | External |
| Copilot Space | Org Setup | Organization must create "pch-standards-space" | External |
| VS Code Setting | Config | `chat.customAgentInSubagent.enabled: true` for beta agent | External |

---

## Risk Assessment and Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| MCP server not installed | Medium | Medium | Graceful fallback; agents work without standards |
| Copilot Space doesn't exist | Medium | Low | Clear error message; continue without standards |
| Standards consume too many tokens | Low | Medium | Summary loading; token budget guidance |
| MCP config syntax errors | Low | High | Tested, validated configuration in plan |

---

## Progress Summary

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | pch-planner YAML + Standards Integration | ✅ Complete |
| Phase 1.5 | pch-plan-reviewer YAML + Standards Integration | ✅ Complete |
| Phase 2 | pch-researcher YAML + Standards Integration | ✅ Complete |
| Phase 3 | beta-pch-researcher YAML + Standards Integration | ✅ Complete |
| Phase 4 | Documentation and Testing | ✅ Complete |

---

## Execution Plan

### Phase 1: pch-planner Updates

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** None
**Entry Point:** Read `agents/pch-planner.md`
**Verification:** Agent file parses correctly; YAML front matter valid

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Update YAML front matter | `agents/pch-planner.md` | Add `mcp-servers` config; verify `tools` property is absent; preserve existing `handoffs` |
| 1.2 | Add Standards Integration as step 5 within Context Gathering | `agents/pch-planner.md` | Add as numbered step 5 inside "Context Gathering (Step 0)" section; include querying instructions for pch-standards-space, fallback behavior, documentation guidance for "Referenced Standards" section |
| 1.3 | Update "Plan Structure" section | `agents/pch-planner.md` | Add "- Referenced Standards (table of standards cited)" bullet after "Implementation Notes for AI agents" in the Plan Structure list |
| 1.4 | Validate agent syntax | `agents/pch-planner.md` | YAML front matter parses without errors; markdown renders correctly |

### Phase 1.5: pch-plan-reviewer Updates

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 1 complete (pch-planner pattern established)
**Entry Point:** Read `agents/pch-plan-reviewer.md`
**Verification:** Agent file parses correctly; YAML front matter valid

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.5.1 | Update YAML front matter | `agents/pch-plan-reviewer.md` | Add `mcp-servers` config; verify `tools` property is absent; preserve existing `handoffs` |
| 1.5.2 | Add Standards Integration section | `agents/pch-plan-reviewer.md` | Insert as new section before "Review Checklist"; include querying instructions for pch-standards-space, fallback behavior, guidance on verifying plan references standards correctly |
| 1.5.3 | Add "Standards Verification" to Review Checklist | `agents/pch-plan-reviewer.md` | Add subsection under Completeness: "- [ ] Referenced Standards section included (if standards were queried)" |
| 1.5.4 | Validate agent syntax | `agents/pch-plan-reviewer.md` | YAML front matter parses without errors; markdown renders correctly |

### Phase 2: pch-researcher Updates

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 1.5 complete (planner/reviewer pattern established)
**Entry Point:** Read `agents/pch-researcher.md`
**Verification:** Agent file parses correctly; YAML front matter valid

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Update YAML front matter | `agents/pch-researcher.md` | Add `mcp-servers` config; verify `tools` property is absent; preserve existing `handoffs` |
| 2.2 | Add "Standards Integration" section | `agents/pch-researcher.md` | Insert after "Decision Framework", before "Workflow Process"; adapt content for research context |
| 2.3 | Update "Research Document Structure" | `agents/pch-researcher.md` | Add "Referenced Standards" section template to the document structure |
| 2.4 | Validate agent syntax | `agents/pch-researcher.md` | YAML front matter parses without errors; markdown renders correctly |

### Phase 3: beta-pch-researcher Updates

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 2 complete (pch-researcher pattern)
**Entry Point:** Read `agents/beta-pch-researcher.md`
**Verification:** Agent file parses correctly; YAML front matter valid

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Update YAML front matter | `agents/beta-pch-researcher.md` | Add `mcp-servers` config; verify `tools` property is absent; preserve existing `handoffs` |
| 3.2 | Add "Standards Integration" section | `agents/beta-pch-researcher.md` | Insert after "Decision Framework", before "Workflow Process"; adapt for research context |
| 3.3 | Update "Research Document Structure" | `agents/beta-pch-researcher.md` | Add "Referenced Standards" section template (match pch-researcher) |
| 3.4 | Update subagent invocation prompt | `agents/beta-pch-researcher.md` | Add note in Automatic Mode subagent prompt section stating: "MCP-based standards access is inherited from the parent agent context; subagents can query pch-standards-space." |
| 3.5 | Validate agent syntax | `agents/beta-pch-researcher.md` | YAML front matter parses without errors; markdown renders correctly |

### Phase 4: Documentation and Testing

**Estimated Session Size:** Small
**Files to Modify:** 2
**Prerequisites:** Phases 1-3 complete (including Phase 1.5)
**Entry Point:** Read updated agent files
**Verification:** Documentation accurate; test scenario passes

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 4.1 | Update copilot-instructions.md | `.github/copilot-instructions.md` | Add note about Standards Integration capability in agent descriptions |
| 4.2 | Update README.md | `README.md` | Document prerequisites: MCP server, Copilot Space setup |
| 4.3 | Manual test: pch-planner | N/A | Invoke pch-planner; verify standards query attempt; verify fallback message if Space unavailable |
| 4.4 | Manual test: pch-plan-reviewer | N/A | Invoke pch-plan-reviewer; verify standards query attempt; verify fallback message if Space unavailable |
| 4.5 | Manual test: pch-researcher | N/A | Invoke pch-researcher; verify standards query attempt; verify fallback message if Space unavailable |

---

## Implementation Notes for AI Agents

### Key Patterns

1. **YAML Front Matter Format**
   - Use `mcp-servers` array with `config` containing JSON string
   - JSON must be valid and properly escaped within YAML
   - Do NOT add a `tools` property — omitting it allows inheritance of all tools

2. **Standards Integration Section**
   - Consistent structure across all four agents
   - Adapt language: "planning" for pch-planner, "review" for pch-plan-reviewer, "research" for researchers
   - Include exact Space name: "pch-standards-space"

3. **Fallback Warning Format**
   ```
   ⚠️ **Could not access organizational standards from pch-standards-space. Proceeding without standards context.**
   ```

4. **Referenced Standards Table Format**
   ```markdown
   ## Referenced Standards

   | Standard | Relevance | Key Guidance Applied |
   |----------|-----------|---------------------|
   | Azure Standards | Feature uses Azure Functions | Use managed identities; follow naming conventions |
   ```

### Testing Checklist

- [ ] YAML front matter parses correctly (no syntax errors)
- [ ] Agent can be invoked via `@agent-name`
- [ ] Standards query is attempted during context gathering
- [ ] Fallback message appears when Space unavailable
- [ ] No regression in existing agent functionality

### External Dependencies

| Dependency | Owner | Notes |
|------------|-------|-------|
| GitHub MCP Server | User | Must be installed in VS Code |
| pch-standards-space | Org Admin | Must be created and shared org-wide |
| VS Code 1.101+ | User | Required for remote MCP support |

---

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 2 | 4 agent files + 2 documentation files |
| New patterns introduced | 1 | MCP config is documented; follows existing YAML pattern |
| External dependencies | 2 | MCP server + Copilot Space (both optional) |
| Migration complexity | 1 | Additive only; no breaking changes |
| Test coverage required | 1 | Manual smoke tests only |
| **Overall Complexity** | 8/25 | **Low** — straightforward config/documentation updates |

---

## Implementation Notes

### Phase 1 — pch-planner Updates

**Completed:** January 16, 2026
**Files Modified:**

- `agents/pch-planner.md`

**Tasks Completed:**

| Step | Task | Status | Notes |
|------|------|--------|-------|
| 1.1 | Update YAML front matter | ✅ Complete | Added `mcp-servers` config with copilot_spaces toolset; `tools` property already absent; preserved existing `handoffs` |
| 1.2 | Add Standards Integration as step 5 within Context Gathering | ✅ Complete | Added as numbered step 5 with querying instructions, fallback behavior, and documentation guidance |
| 1.3 | Update "Plan Structure" section | ✅ Complete | Added "Referenced Standards (table of standards cited)" bullet after "Implementation Notes for AI agents" |
| 1.4 | Validate agent syntax | ✅ Complete | YAML front matter parses without errors; no markdown lint issues |

**Deviations from Plan:** None

**Discoveries:**

- The `tools` property was already absent from the original YAML front matter (only had `handoffs`), so no removal was necessary

---

### Phase 2 — pch-researcher Updates

**Completed:** January 16, 2026
**Files Modified:**

- `agents/pch-researcher.md`

**Tasks Completed:**

| Step | Task | Status | Notes |
|------|------|--------|-------|
| 2.1 | Update YAML front matter | ✅ Complete | Added `mcp-servers` config with copilot_spaces toolset; `tools` property already absent; preserved existing `handoffs` |
| 2.2 | Add "Standards Integration" section | ✅ Complete | Inserted as new top-level section after "Example Research Outlines", before "Workflow Process" (line 262); includes querying instructions, fallback behavior, documenting standards guidance |
| 2.3 | Update "Research Document Structure" | ✅ Complete | Added "Referenced Standards" section template (line 462) with table format and fallback note |
| 2.4 | Validate agent syntax | ✅ Complete | YAML front matter parses without errors; no markdown lint issues |

**Deviations from Plan:** None

**Discoveries:**

- The `tools` property was already absent from the original YAML front matter (only had `handoffs`), so no removal was necessary
- The "Decision Framework" is a subsection (###) within the larger "How to Break Down Research into Phases" section; Standards Integration was placed at the top-level (##) after that entire section block, immediately before "Workflow Process"

---

### Phase 3 — beta-pch-researcher Updates

**Completed:** January 16, 2026
**Files Modified:**

- `agents/beta-pch-researcher.md`

**Tasks Completed:**

| Step | Task | Status | Notes |
|------|------|--------|-------|
| 3.1 | Update YAML front matter | ✅ Complete | Added `mcp-servers` config with copilot_spaces toolset; `tools` property already absent; preserved existing `handoffs` |
| 3.2 | Add "Standards Integration" section | ✅ Complete | Inserted as new top-level section after "Example Research Outlines", before "Workflow Process"; includes querying instructions, fallback behavior, documenting standards guidance |
| 3.3 | Update "Research Document Structure" | ✅ Complete | Added "Referenced Standards" section template with table format and fallback note |
| 3.4 | Update subagent invocation prompt | ✅ Complete | Added note in Automatic Mode subagent prompt about MCP-based standards inheritance |
| 3.5 | Validate agent syntax | ✅ Complete | YAML front matter parses without errors; no markdown lint issues |

**Deviations from Plan:** None

**Discoveries:**

- Matched pch-researcher pattern exactly for consistency
- Subagent MCP inheritance note added to clarify that standards access works in automatic mode

---

### Phase 4 — Documentation and Testing

**Completed:** January 16, 2026
**Files Modified:**

- `.github/copilot-instructions.md`
- `README.md`

**Tasks Completed:**

| Step | Task | Status | Notes |
|------|------|--------|-------|
| 4.1 | Update copilot-instructions.md | ✅ Complete | Added "Standards Integration" subsection after "Automatic Mode (Beta Agents)" section |
| 4.2 | Update README.md | ✅ Complete | Added "Prerequisites for Standards Integration (Optional)" section before "Key Features" |
| 4.3 | Manual test: pch-planner | ⏳ Manual verification required | User should invoke pch-planner and verify standards query attempt/fallback message |
| 4.4 | Manual test: pch-plan-reviewer | ⏳ Manual verification required | User should invoke pch-plan-reviewer and verify standards query attempt/fallback message |
| 4.5 | Manual test: pch-researcher | ⏳ Manual verification required | User should invoke pch-researcher and verify standards query attempt/fallback message |

**Deviations from Plan:** None

**Testing Notes:**

Tasks 4.3-4.5 require manual verification:

- Agent should attempt to query pch-standards-space during context gathering
- If Space is unavailable, agent should display: `⚠️ **Could not access organizational standards from pch-standards-space. Proceeding without standards context.**`
- Agent should continue with normal workflow after the fallback

---

## Review Summary

**Review Date:** January 16, 2026
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.2
**Reviewed Plan Version:** v2.3

### Review Metrics
- Issues Found: 5 (Critical: 0, Major: 2, Minor: 3)
- Clarifying Questions Asked: 3
- Sections Updated: Section Placement table, Holistic Review Summary, YAML section intro, Review Session Log

### Key Improvements Made
1. Clarified pch-planner placement: Standards Integration as step 5 within Context Gathering (Step 0)
2. Confirmed researcher agents use top-level section (justified by different agent structure)
3. Added specific acceptance criteria for Step 1.3 (exact bullet location)
4. Added specific acceptance criteria for Step 3.4 (subagent MCP inheritance note)
5. Clarified pch-plan-reviewer placement: new section before Review Checklist (removed ambiguous OR)
6. Updated agent count references from "three" to "four" after v2.2 scope expansion

### Remaining Considerations
- None; all agents now in scope

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** January 16, 2026
**Reviewed By:** pch-plan-reviewer
**Review Date:** January 16, 2026
**Status:** ✅ Ready for Implementation
**Next Agent:** pch-coder
**Plan Location:** `/docs/plans/003-pch-agents-standards-integration.md`
