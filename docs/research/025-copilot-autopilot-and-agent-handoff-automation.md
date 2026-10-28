---
id: "025"
type: research
title: "GitHub Copilot Autopilot Feature & PCH Agent Handoff Automation"
status: ✅ Complete
created: "2026-04-26"
current_phase: "5 of 5"
---

## Introduction

This research investigates GitHub Copilot's newly released "autopilot" feature and analyzes how the PCH agent system (visionary, researcher, planner, plan-reviewer, coder) can be improved to execute handoffs automatically while generating fresh context windows. The goal is to understand the autopilot capability, map it against the current agent handoff mechanisms, and document concrete improvement strategies to enable long-running, multi-agent workflows that don't exhaust context windows.

## Objectives

- Understand the capabilities and design of GitHub Copilot's new "autopilot" feature
- Document how the autopilot feature handles context windows, session management, and agent continuity
- Analyze the current handoff mechanisms across all PCH agents (visionary → researcher → planner → reviewer → coder)
- Identify gaps between current manual handoff patterns and what autopilot enables
- Propose specific improvements to each agent's prompt and YAML front matter to support automatic handoffs with fresh context windows
- Evaluate how autopilot interacts with VS Code's subagent orchestration (`chat.customAgentInSubagent.enabled`)
- Document migration path from current handoff patterns to autopilot-enabled automatic workflows

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | GitHub Copilot Autopilot Feature Deep Dive | ✅ Complete | Research autopilot feature capabilities, API surface, configuration, context window management, session boundaries, and agent orchestration support | 2026-04-26 |
| 2 | Current PCH Agent Handoff Analysis | ✅ Complete | Analyze all 5 core agent prompts (visionary, researcher, planner, reviewer, coder) — document current handoff YAML, handoff prompts, session boundary patterns, context window strategies, and subagent delegation patterns | 2026-04-26 |
| 3 | Context Window Management Patterns | ✅ Complete | Research how autopilot manages context across multi-agent chains; compare with current PCH strategies (subagent isolation, document-based persistence, phase-per-session); identify fresh context window generation techniques | 2026-04-26 |
| 4 | Agent Improvement Recommendations | ✅ Complete | For each of the 5 agents, document specific YAML front matter changes, prompt modifications, handoff automation strategies, and context window preservation techniques; include before/after examples | 2026-04-26 |
| 5 | Migration & Integration Strategy | ✅ Complete | Document migration path from current manual handoffs to autopilot-enabled automatic workflows; address backward compatibility with Hugh CLI dual-environment architecture; identify prerequisites and VS Code settings | 2026-04-26 |

## Phase 1: GitHub Copilot Autopilot Feature Deep Dive

**Status:** ✅ Complete  
**Session:** 2026-04-26

### 1. What "Autopilot" Actually Is

Autopilot is **not a separate feature or API** — it is a **permission level** within the VS Code Copilot agent system. It sits alongside two other permission levels in a hierarchy of increasing agent autonomy:

| Permission Level | Tool Approvals | Question Handling | Agent Behavior |
|-----------------|----------------|-------------------|----------------|
| **Default Approvals** | Manual approval for tools requiring confirmation | Agent asks clarifying questions and waits | Interactive, user-controlled |
| **Bypass Approvals** | Auto-approves all tool calls | Agent may still ask clarifying questions | Semi-autonomous, user may need to respond |
| **Autopilot (Preview)** | Auto-approves all tool calls | Auto-responds to questions; agent continues autonomously | Fully autonomous until task completion |

Autopilot is selected from the **permissions picker dropdown** in the Chat view input area.

### 2. Autopilot Capabilities

When Autopilot is enabled, the agent has four key behavioral changes:

1. **Continuous iteration** — The agent keeps working autonomously until it determines the task is complete. No pause for user review between steps.
2. **Auto-approve all tools** — All tool calls (file edits, terminal commands, MCP tools, extension tools) are approved automatically, identical to Bypass Approvals.
3. **Auto-retry on errors** — The agent automatically retries when it encounters errors during tool execution or code changes.
4. **Auto-respond to questions** — Tools that normally block and ask for user input (clarifying questions, confirmation dialogs) auto-respond so the agent does not stall. This is the **key differentiator** from Bypass Approvals.

### 3. Configuration & Settings

| Setting | Purpose | Default |
|---------|---------|--------|
| `chat.autopilot.enabled` | Master toggle to enable/disable Autopilot availability | `true` (on by default) |
| `chat.permissions.default` | Persist preferred permission level across sessions | `"defaultApprovals"` |

**Activation methods:**
- Select "Autopilot (Preview)" from the permissions picker dropdown in any chat session
- Set `chat.permissions.default` to persist Autopilot as the default across all new sessions

**Related auto-approval settings (relevant but separate from Autopilot):**
- `chat.tools.global.autoApprove` — Auto-approve all tools globally (toggle via `/yolo` or `/autoApprove` slash commands)
- `chat.tools.terminal.autoApprove` — Granular terminal command auto-approval patterns
- `chat.tools.urls.autoApprove` — URL access auto-approval patterns
- `chat.tools.eligibleForAutoApproval` — Per-tool auto-approval eligibility

### 4. Autopilot and Session/Context Window Management

**Context window behavior under Autopilot is identical to non-Autopilot sessions:**
- Each chat session has its own independent context window
- Context accumulates as the agent reads files, makes tool calls, and receives results
- A visual context window control in the chat input shows usage (e.g., "15K/128K")
- **Automatic compaction** summarizes earlier messages when the window fills up (controlled by `github.copilot.chat.summarizeAgentConversationHistory.enabled`)
- **Manual compaction** via `/compact` command or the context window control
- Starting a **new session** clears the context entirely

**Critical finding for PCH workflow:** Autopilot does NOT create fresh context windows between phases of work. It continues in the same session, accumulating context. For long multi-step workflows, the context will eventually be compacted (summarized), which may lose important details from earlier phases.

### 5. Handoff Mechanism in VS Code Custom Agents

VS Code's custom agent handoff system is the primary mechanism for chaining agents together, defined in agent files via YAML frontmatter:

```yaml
handoffs:
  - label: Start Implementation     # Button text shown to user
    agent: implementation            # Target agent name
    prompt: Now implement the plan.  # Prompt sent to target agent
    send: false                      # false = prefill only; true = auto-send
    model: GPT-5.2 (copilot)        # Optional: override model for handoff
```

**How handoffs work:**
1. After a chat response completes, handoff **buttons** appear at the bottom of the response
2. When the user selects a handoff button, they switch to the target agent with the prompt pre-filled
3. If `send: true`, the prompt auto-submits to start the next workflow step
4. The handoff creates a **new session** — conversation history from the prior agent carries over, but the target agent's instructions replace the prior agent's

**Key architectural detail:** Handoffs with `send: true` combined with Autopilot could enable a partially automatic workflow, but **conversation history carries over** — the new session inherits the full conversation, which means context is NOT reset.

### 6. Subagent Infrastructure

VS Code's subagent system provides the **only mechanism for fresh context isolation**:

| Property | Subagents | Handoffs |
|----------|-----------|----------|
| Context isolation | ✅ Separate context window | ❌ Carries over conversation |
| Execution model | Synchronous (parent waits) or parallel | Sequential (new session) |
| Result handling | Only summary returned to parent | Full conversation visible |
| Session creation | Invisible to user (tool call) | Creates new visible session |
| Custom agent support | ✅ Can use custom agents | ✅ Targets custom agents |
| Nesting | Up to 5 levels (if `chat.subagents.allowInvocationsFromSubagents` enabled) | No nesting concept |
| Model override | ✅ Explicit, agent config, or parent | ✅ Via `model` in handoff |

**Relevant subagent capabilities:**
- `runSubagent` tool must be enabled for the agent
- Custom agents can be run as subagents (e.g., invoke `pch-coder` as a subagent)
- `user-invocable: false` creates agents only accessible as subagents
- `disable-model-invocation: true` prevents an agent from being invoked as a subagent
- `agents` frontmatter property restricts which subagents a coordinator can use
- `chat.subagents.allowInvocationsFromSubagents` enables recursive nesting (up to 5 levels)

**Orchestration patterns documented by VS Code:**
1. **Coordinator and worker pattern** — A coordinator manages the task and delegates to specialized subagents
2. **Multi-perspective code review** — Parallel subagents for correctness, quality, security, architecture
3. **Recursive processing** — Self-referential agents that divide-and-conquer

### 7. Autopilot + Handoffs vs. Autopilot + Subagents

**Autopilot + Handoffs (with `send: true`):**
- When a handoff has `send: true`, the prompt auto-submits — but the USER still needs to select the handoff button
- **Handoff buttons are UI-level user interactions, not tool calls** — Autopilot cannot automatically trigger them
- This is a significant limitation for fully automated multi-agent workflows

**Autopilot + Subagents:**
- Subagents are invoked via the `runSubagent` tool, which IS a tool call
- Under Autopilot, `runSubagent` is auto-approved like all other tool calls
- Combined with the coordinator/worker pattern, this enables **fully autonomous multi-agent orchestration** within a single session

### 8. VS Code Settings Inventory (Autopilot-Related)

| Setting | Type | Default | Purpose |
|---------|------|---------|--------|
| `chat.autopilot.enabled` | boolean | `true` | Enable/disable Autopilot availability |
| `chat.permissions.default` | string | `"defaultApprovals"` | Default permission level for new sessions |
| `chat.agent.enabled` | boolean | varies | Enable/disable agent capabilities |
| `chat.subagents.allowInvocationsFromSubagents` | boolean | `false` | Enable nested subagent invocation |
| `chat.customAgentInSubagent.enabled` | boolean | — | Enable custom agents as subagents (used by PCH) |
| `github.copilot.chat.summarizeAgentConversationHistory.enabled` | boolean | `true` | Enable automatic context compaction |

### 9. Implications for PCH Agent Workflow

**Current PCH workflow architecture:**
```
Visionary → Researcher → Planner → Reviewer → Coder
(separate sessions, manual handoff buttons, subagent delegation within phases)
```

**What Autopilot enables:**
- ✅ Auto-approval of all tool calls within a session (no user clicking "Allow")
- ✅ Auto-response to clarifying questions (no stalling on user input)
- ✅ Auto-retry on errors (resilience)
- ✅ Combined with subagents: fully autonomous phase execution within an agent

**What Autopilot does NOT solve:**
- ❌ Cross-agent handoffs still require user interaction (handoff buttons are UI, not tool calls)
- ❌ Fresh context windows between agents — handoffs carry over conversation history
- ❌ Context window exhaustion in long multi-agent chains
- ❌ Automatic progression from one agent to the next in the pipeline

**The gap:** The PCH workflow needs **both** Autopilot (for intra-agent autonomy) AND a coordinator pattern using subagents (for inter-agent orchestration with fresh context). A single "Feature Builder" coordinator agent could use subagents to invoke each PCH agent sequentially, each with isolated context, while running under Autopilot for zero-interruption execution.

**Key Discoveries:**
- Autopilot is a **permission level**, not a separate feature or API
- The key differentiator from Bypass Approvals is **auto-responding to clarifying questions**
- Autopilot does **NOT** create fresh context windows — context accumulates and is eventually compacted
- **Handoff buttons are UI interactions, NOT tool calls** — Autopilot cannot auto-trigger handoffs (critical gap)
- **Subagents provide the only mechanism for fresh context isolation**
- The **coordinator/worker pattern** is directly applicable to the PCH workflow
- VS Code supports up to **5 levels of nested subagents** with the right settings
- The `agents` frontmatter property can restrict which subagents a coordinator may use

| File | Relevance |
|------|----------|
| `agents/pch-visionary.md` | 3 handoffs (self-continue, researcher, planner) all with `send: false` |
| `agents/pch-researcher.md` | Single handoff to self with `send: false`; relies on subagent orchestration |
| `agents/pch-planner.md` | Single handoff to pch-plan-reviewer with `send: true` (auto-submits) |
| `agents/pch-plan-reviewer.md` | Single handoff to pch-coder with `send: true` (auto-submits) |
| `agents/pch-coder.md` | Single handoff to pch-planner with `send: false` |
| `agents/hugh.md` | Hugh CLI operator agent, single handoff to pch-helper |

**External Sources:**
- [Agents overview](https://code.visualstudio.com/docs/copilot/agents/overview) — Permission levels table including Autopilot
- [Agent tools](https://code.visualstudio.com/docs/copilot/agents/agent-tools) — Autopilot behavior details
- [Custom agents](https://code.visualstudio.com/docs/copilot/customization/custom-agents) — YAML frontmatter, handoffs, orchestration patterns
- [Subagents](https://code.visualstudio.com/docs/copilot/agents/subagents) — Context isolation, coordinator/worker pattern
- [Agent concepts](https://code.visualstudio.com/docs/copilot/concepts/agents) — Agent loop, memory systems
- [Chat sessions](https://code.visualstudio.com/docs/copilot/chat/chat-sessions) — Session management, handoffs
- [Chat context](https://code.visualstudio.com/docs/copilot/chat/copilot-chat-context) — Context window monitoring, compaction
- [VS Code 1.117 release notes](https://code.visualstudio.com/updates) — April 22, 2026

**Gaps:**
- No dedicated Autopilot documentation page exists (it's a subsection of agent-tools)
- Unclear whether Autopilot behavior can be scoped per-agent — appears session-level only
- No confirmation that `send: true` handoffs can be made to auto-trigger under Autopilot
- Context compaction reliability for structured data (plan documents, status tables) is undocumented

**Assumptions:**
- Handoff buttons are UI-level interactions based on documentation describing them as "interactive suggestions after a chat response completes" — not explicitly confirmed as non-automatable via tools
- Context compaction under Autopilot uses the same summarization mechanism as manual `/compact`

## Phase 2: Current PCH Agent Handoff Analysis

**Status:** ✅ Complete  
**Session:** 2026-04-26

### Agent-by-Agent Handoff Summary

| Agent | Forward Handoff | send:true | Backward Handoff | Uses Subagents | Creates Artifacts | Session Pattern |
|-------|----------------|-----------|-------------------|---------------|-------------------|----------------|
| pch-visionary | researcher or planner | No | self (continue) | No | /docs/vision/ | Interactive Q&A |
| pch-researcher | **NONE** | No | self (continue) | **Yes** | /docs/research/ | Subagent phases |
| pch-planner | plan-reviewer | **Yes** | — | No | /docs/plans/ | Interactive Q&A |
| pch-plan-reviewer | coder | **Yes** | (text-based to planner) | No | Updates plans/ | Single session |
| pch-coder | **NONE (terminal)** | No | planner | **Yes** | Updates plans/ | Subagent phases |
| pch-helper | any agent (5 handoffs) | No | — | No | None | Routing hub |
| hugh | pch-helper | No | — | No | None | CLI operator |

### Handoff Chain Flow

```
pch-visionary ──[send:false]──► pch-researcher ──[NO HANDOFF]──► pch-planner ──[send:true]──► pch-plan-reviewer ──[send:true]──► pch-coder ──[TERMINAL]
```

**Two automated segments** exist (planner→reviewer and reviewer→coder via `send: true`), separated by **fully manual gaps**:
- vision→research: `send: false` — user must click handoff button
- research→plan: **NO handoff defined** — user must manually invoke planner

### Agent Detail: pch-visionary

**Handoffs (3):**

| Label | Target | Prompt | Send |
|-------|--------|--------|------|
| Continue Vision | pch-visionary (self) | "Continue vision capture from the attached document" | false |
| Start Research | pch-researcher | "Begin research based on the completed vision document..." | false |
| Skip Research - Start Planning | pch-planner | "Create an implementation plan based on the completed vision document..." | false |

- Interactive Q&A (6 stages), one question at a time
- Incremental document building — "Never generate more than one major ## section"
- Creates `/docs/vision/{NNN}-{description}.md` with Handoff table containing Created By, Status, Next Agent, Path
- Also creates `/docs/data-contracts/` files (optional)

### Agent Detail: pch-researcher

**Handoffs (1):**

| Label | Target | Prompt | Send |
|-------|--------|--------|------|
| Continue researching | pch-researcher (self) | "Continue phased research" | false |

- **CRITICAL GAP: No forward handoff to pch-planner** — only a self-loop. After research completes, completion message says "Hand off to `@pch-planner`" but no handoff button exists.
- Two modes: outline creation (Session 1) and subagent-orchestrated phases (Session 2+)
- "STOP IMMEDIATELY" after outline creation — must not start Phase 1 in same session
- Uses `runSubagent` with `agentName: "pch-researcher"` for each phase
- Creates `/docs/research/{NNN}-{description}.md`
- Updates vision document's Release Overview when research completes

### Agent Detail: pch-planner

**Handoffs (1):**

| Label | Target | Prompt | Send |
|-------|--------|--------|------|
| Start Plan Review | pch-plan-reviewer | "Review the implementation plan for correctness, clarity, and specificity" | **true** |

- Interactive Q&A, one question at a time
- Creates document skeleton first, populates through Q&A
- Creates `/docs/plans/{NNN}-{description}.md` with Handoff table
- `send: true` → auto-submits to reviewer (part of the "fast lane")

### Agent Detail: pch-plan-reviewer

**Handoffs (1):**

| Label | Target | Prompt | Send |
|-------|--------|--------|------|
| Start Implementation | pch-coder | "Now implement the plan outlined above" | **true** |

- Single-session review: Initial Assessment → Critical → Major → Minor → Final Summary
- One question at a time
- Modifies the existing plan document in `/docs/plans/`
- `send: true` → auto-submits to coder (fast lane continuation)
- **No handoff button for backward escalation to planner** — text-based recommendation only

### Agent Detail: pch-coder

**Handoffs (1):**

| Label | Target | Prompt | Send |
|-------|--------|--------|------|
| Request Plan Update | pch-planner | "Update the implementation plan based on issues discovered during implementation" | false |

- Subagent orchestration — each phase in isolated subagent
- Uses `runSubagent` with `agentName: "pch-coder"` for each phase
- Terminal agent — no forward handoff after implementation
- Offers commit/push on completion
- Post-implementation code review via additional subagent

### Identified Gaps in Current Handoff Architecture

**Gap 1: Missing researcher→planner handoff** — The biggest chain break. No handoff button exists; user must manually invoke planner.

**Gap 2: Handoff prompts are context-free** — Prompts don't include document paths (e.g., "Review the implementation plan" without specifying which plan). User must manually attach/reference documents.

**Gap 3: Handoff buttons are UI-only** — Even `send: true` requires user to click the button. Autopilot cannot auto-trigger handoffs (per Phase 1 findings).

**Gap 4: No reviewer→planner backward handoff button** — Text-based escalation only, no handoff defined.

**Gap 5: Self-handoffs don't carry session state** — Self-loops (visionary, researcher) require user to provide the document path.

**Gap 6: pch-coder has no forward handoff** — Terminal agent with no progression to testing, documentation, or deployment.

**Gap 7: pch-helper handoffs are context-free** — Generic prompts without capturing conversation context.

### Autopilot + Coordinator Opportunities

1. **Coordinator agent for full pipeline** — Invoke visionary→researcher→planner→reviewer→coder as subagents, passing document paths automatically
2. **Researcher→planner gap fix** — Most impactful quick fix: add handoff with `send: true` and document path
3. **Document path auto-injection** — Coordinator tracks current document path, injects into each agent's prompt
4. **Eliminate manual self-handoff loops** — Coordinator manages multi-phase lifecycle without user-initiated new sessions
5. **Bidirectional handoff automation** — Coordinator detects escalation signals and auto-re-invokes planner

### Cross-Cutting Patterns

- **Document-centric communication** — All agents pass data via shared `/docs/` documents
- **Two agent categories** — Interactive (visionary, planner, reviewer) vs. Subagent Orchestrators (researcher, coder)
- **Shared "Never Self-Answer" anti-pattern** — Three agents have nearly identical large sections preventing self-answering (could be a shared instruction)
- **Structured return formats** — Both orchestrators (researcher, coder) use identical structured text returns parsed by the parent
- **send:true only in middle of chain** — Creates partial automation (planner→reviewer→coder) with manual gaps at start and end

| File | Relevance |
|------|----------|
| `agents/pch-visionary.md` | 3 handoffs (all send:false), interactive Q&A, creates vision docs |
| `agents/pch-researcher.md` | 1 self-handoff, subagent orchestration, MISSING forward handoff |
| `agents/pch-planner.md` | 1 handoff to reviewer (send:true), interactive Q&A, creates plans |
| `agents/pch-plan-reviewer.md` | 1 handoff to coder (send:true), single-session review |
| `agents/pch-coder.md` | 1 backward handoff (send:false), subagent orchestration, terminal |
| `agents/pch-helper.md` | 5 handoffs to all agents (all send:false), routing hub |
| `agents/hugh.md` | 1 handoff to helper, CLI operator |

**Gaps:** Exact user experience of `send: true` under Autopilot not tested. Hugh CLI's CopilotService agent transition handling not analyzed (code-level).

**Assumptions:** `send: true` still requires user button click (per Phase 1). All agents use same YAML frontmatter schema.

## Phase 3: Context Window Management Patterns

**Status:** ✅ Complete  
**Session:** 2026-04-26

### Autopilot Context Lifecycle

Autopilot operates within a **single cumulative context window** per chat session. Context grows from seven assembled layers: system instructions, customizations, user message, conversation history, implicit context, explicit references, and tool outputs.

**Compaction triggers:**
- **Automatic:** When context window fills up, controlled by `github.copilot.chat.summarizeAgentConversationHistory.enabled` (default: true)
- **Manual:** `/compact` command or context window control, with optional focus instructions

**Survives compaction:** System instructions, custom instructions from `.github/copilot-instructions.md` ("included in every request"), agent definition/persona, summarized conversation history.

**Lost or degraded:** Exact text of prior messages, structured data formats (tables, JSON, code snippets), intermediate tool outputs, precise code references, PCH structured return formats.

**Impact:** A long Autopilot session accumulates ALL tool call results, rapidly consuming context. Compaction is lossy — summaries cannot preserve structured data fidelity. This is why document-based persistence is critical: the document IS the memory, not the conversation.

### Subagent Context Isolation Mechanics

**What the subagent receives:**
- Task prompt ONLY — "It doesn’t inherit the main agent’s conversation history or instructions"
- Custom agent definition (if specified) — the subagent's own model, tools, and instructions
- Does NOT receive: parent conversation history, accumulated tool results, implicit context

**What the parent receives back:**
- Only the final result — "only the final result is returned to the main agent"
- Does NOT receive: intermediate tool calls, file reads, search results, or reasoning steps

**Execution model:** Synchronous (parent waits), parallel (multiple simultaneous), nesting up to 5 levels with `chat.subagents.allowInvocationsFromSubagents`.

### Comparison of 4 Context Strategies

| Strategy | Context Isolation | Automatable | Fresh Context | Context Efficiency | Autopilot Compatible |
|----------|------------------|-------------|---------------|-------------------|---------------------|
| **A: Handoff chains** (current cross-agent) | ❌ None — history carries over | ❌ Manual (button click) | ❌ No | ❌ Poor — bloated | ❌ No |
| **B: Subagent delegation** (current internal) | ✅ Complete | ✅ Yes | ✅ Yes | ✅ Good | ✅ Yes |
| **C: Coordinator/worker** (proposed) | ✅ Per-worker | ✅ Yes | ✅ For workers; ⚠️ coordinator grows | ✅ Good for workers | ✅ Yes |
| **D: Autopilot + subagent hybrid** (proposed) | ✅ Per-phase | ✅✅ Maximum | ✅ For subagents | Same as C | ✅ Native |

**Strategy D (Autopilot + subagent hybrid)** is the strongest option: fully autonomous, no user intervention for tool approvals or questions, fresh context per subagent. Primary risk: coordinator context growth from accumulated subagent results.

### Document-Based Context Recovery

**Current PCH approach:** Both researcher and coder embed the **entire document** in each subagent prompt. The subagent is told: "You have no memory of prior phases — all context comes from the document below."

**Hugh CLI approach:** Creates a brand new SDK session per phase via `CreateSessionAsync()`. Document paths are appended to the user prompt (the agent then reads them via tools). Each session starts with zero conversation history.

**Reliability comparison:**

| Recovery Method | Fidelity | Completeness | Resilience |
|----------------|----------|--------------|------------|
| Document embed in prompt | ✅ High — exact text | ✅ Full document | ✅ Survives session breaks |
| Document path + read | ✅ High — reads from disk | ✅ Full document | ✅ Survives session breaks |
| Conversation history | ⚠️ Degrades over time | ⚠️ Compaction loses detail | ❌ Lost on new session |
| Compacted summary | ❌ Low — lossy | ❌ Partial | ❌ Lost on new session |
| Memory tool | ✅ High — persisted | ⚠️ Manual | ✅ Cross-session |

**Key insight:** Document-based persistence is fundamentally more reliable than conversation-based context — documents don't degrade, can be re-read at full fidelity, and survive all session boundaries.

### Context Window Budgets

**Estimated per-phase consumption:**

| PCH Activity | Estimated Context Per Phase | Notes |
|-------------|---------------------------|-------|
| Research subagent (1 phase) | 30-60K tokens | Agent prompt + full doc + 5-10 file reads + searches |
| Coder subagent (1 phase) | 40-80K tokens | Agent prompt + full plan + file reads + edits + tests |
| Planner (interactive) | 20-40K per turn | Agent prompt + growing conversation + file reads |
| Reviewer (interactive) | 15-30K per turn | Agent prompt + plan + review analysis |
| Orchestrator (coordinator) | Grows 5-10K per subagent result | Accumulates returned summaries |

**Capacity per model:** Claude Opus/Sonnet: 200K tokens. GPT-4o: 128K tokens.

**Coordinator risk:** Managing 25+ phases (5 releases × 5 phases) would accumulate 50-125K of returned results, approaching limits even on 200K models. Hugh CLI addresses this with `CompactionThreshold: 0.8` and per-phase fresh sessions.

### Fresh Context Generation — Complete Catalog

| Mechanism | Fresh Context? | Automatable? | What's Preserved | What's Lost |
|-----------|---------------|-------------|-----------------|------------|
| **New chat session** | ✅ Completely | ❌ Manual | Nothing (unless docs saved) | Everything |
| **Subagent invocation** | ✅ Completely | ✅ Yes | Parent context unchanged | Subagent intermediate work |
| **Hugh SDK new session** | ✅ Completely | ✅ Yes | State in `.orchestrator/` | Prior session conversation |
| **Handoff to agent** | ❌ History carries over | ❌ Manual | Full conversation | Nothing lost (but bloated) |
| **`/compact` command** | ❌ Summarizes only | ❌ Manual | Summarized history | Exact messages, structured data |
| **Auto-compaction** | ❌ Summarizes only | ✅ Automatic | Same as `/compact` | Same as `/compact` |
| **Memory tool** | N/A (persistence) | ✅ Yes | Written notes persist | Must be explicitly written/read |
| **Document persistence** | N/A (persistence) | ✅ Yes | Full document fidelity | Reasoning not written to doc |

**Conclusion:** Only **three mechanisms** produce truly fresh context:
1. New chat session (manual)
2. Subagent invocation (automatable) ← **only automatable option in VS Code**
3. Hugh SDK session creation (programmatic)

**Key Discoveries:**
- Subagent invocation is the ONLY automatable mechanism for fresh context in VS Code
- Custom instructions survive compaction but conversation history and tool outputs are lossy-compressed
- Subagents receive ONLY the task prompt — complete isolation from parent
- A coordinator managing 25+ phases would accumulate 50-125K tokens — approaching model limits
- Hugh CLI achieves fresh context via `CreateSessionAsync` per phase with `CompactionThreshold: 0.8`
- VS Code’s context engineering guide recommends: "Keep different types of work in separate chat sessions" — validating PCH’s isolation approach

| File | Relevance |
|------|----------|
| `Hugh/Agents/CopilotService.cs` | CreateSessionAsync, InfiniteSession, BuildPromptWithDocuments — per-phase fresh sessions |
| `Hugh/Agents/SdkResponseProcessor.cs` | SessionCompaction event handling — compaction tracking |
| `Hugh/Configuration/HughConfig.cs` | CompactionThreshold property (0.8) |
| `Hugh/Models/PhaseResult.cs` | CompactionOccurred property |
| `agents/pch-researcher.md` | Subagent prompt template, ">5000 lines" context note |
| `agents/pch-coder.md` | Subagent prompt template, context window consideration |

**External Sources:**
- [Subagents](https://code.visualstudio.com/docs/copilot/agents/subagents) — Context isolation details
- [Chat context](https://code.visualstudio.com/docs/copilot/chat/copilot-chat-context) — Compaction behavior
- [Agent concepts](https://code.visualstudio.com/docs/copilot/concepts/agents) — 7-layer context assembly
- [Context engineering guide](https://code.visualstudio.com/docs/copilot/guides/context-engineering-guide) — Session isolation recommendation
- [Chat sessions](https://code.visualstudio.com/docs/copilot/chat/chat-sessions) — Session independence

**Gaps:** Exact token overhead for VS Code system instructions undocumented. Compaction algorithm fidelity is opaque. Whether subagent results are eligible for parent compaction is unknown.

## Phase 4: Agent Improvement Recommendations

**Status:** ✅ Complete  
**Session:** 2026-04-26

### Per-Agent Recommendation Summary

| Agent | Key YAML Changes | Key Prompt Changes | Priority |
|-------|------------------|-------------------|----------|
| pch-visionary | `disable-model-invocation: true`, `tools` list | Structured completion signal, coordinator-aware mode | Medium |
| pch-researcher | **Add forward handoff to planner** (`send: true`), `agents: ['pch-researcher']` | Completion signal, improved handoff message | **Highest** |
| pch-planner | `disable-model-invocation: true`, backward handoff to researcher | Completion signal, coordinator-aware mode | Medium |
| pch-plan-reviewer | `disable-model-invocation: true`, backward handoff to planner ("Revise Plan") | Completion signal, coordinator-aware mode | Medium |
| pch-coder | `agents: ['pch-coder']`, "Start New Feature" handoff to visionary | Completion signal, coordinator-aware mode | Medium |
| **pch-pipeline** (NEW) | Full coordinator: `agents: [all 5]`, `tools: ['agent', 'read', 'search']` | Pipeline state machine, document path passing, error handling | Full Migration |

### Quick Wins (Implement Immediately — No Breaking Changes)

| # | Change | Impact | Risk | Effort |
|---|--------|--------|------|--------|
| QW-1 | Add forward handoff from researcher to planner (`send: true`) | Closes biggest chain break | None | 5 min |
| QW-2 | Add backward handoff from reviewer to planner | Formalizes text-based escalation | None | 5 min |
| QW-3 | Add `agents: ['pch-researcher']` to researcher YAML | Formalizes self-delegation | None | 2 min |
| QW-4 | Add `agents: ['pch-coder']` to coder YAML | Formalizes self-delegation | None | 2 min |
| QW-5 | Enrich handoff prompts with document path instructions | Better context passing | None | 15 min |
| QW-6 | Add `tools` arrays to all agents | Principle of least privilege | Low | 15 min |
| QW-7 | Add "Start New Feature" handoff to coder | Closes pipeline loop | None | 5 min |

**Total quick win effort:** ~1 hour. Zero backward compatibility risk.

### Medium-Term (Requires Coordinator Agent)

| # | Change | Prerequisites | Effort |
|---|--------|---------------|--------|
| MT-1 | Create `pch-pipeline` coordinator agent | Quick wins complete | 2-4 hours |
| MT-2 | Add `disable-model-invocation: true` to interactive agents | Coordinator lists them in `agents` | 10 min |
| MT-3 | Add structured completion signals to all agents | Prompt modifications | 1-2 hours |
| MT-4 | Add coordinator-aware autonomous mode to interactive agents | Significant prompt changes; testing | 3-5 hours |

### pch-researcher: Highest Priority Fix

**Before (current YAML):**
```yaml
handoffs:
  - label: Continue researching
    agent: pch-researcher
    prompt: Continue phased research
    send: false
```

**After (proposed YAML):**
```yaml
tools: ['read', 'search', 'editFiles', 'web', 'agent']
agents: ['pch-researcher']
handoffs:
  - label: Continue Researching
    agent: pch-researcher
    prompt: Continue phased research from the attached research document
    send: false
  - label: Start Planning
    agent: pch-planner
    prompt: |
      Create an implementation plan based on the completed research.
      The research document and its referenced vision document contain 
      all findings needed for planning. Start with the first phase 
      identified in the research.
    send: true
```

### Access Control Model with `disable-model-invocation`

| Agent | User-Invocable | Subagent by Coordinator | Subagent by Others |
|-------|---------------|------------------------|-------------------|
| pch-visionary | ✅ Yes | ✅ Yes (listed in coordinator `agents`) | ❌ No (`disable-model-invocation`) |
| pch-researcher | ✅ Yes | ✅ Yes (listed in coordinator `agents`) | ❌ No |
| pch-planner | ✅ Yes | ✅ Yes (listed in coordinator `agents`) | ❌ No |
| pch-plan-reviewer | ✅ Yes | ✅ Yes (listed in coordinator `agents`) | ❌ No |
| pch-coder | ✅ Yes | ✅ Yes (listed in coordinator `agents`) | ❌ No |
| pch-pipeline | ✅ Yes | N/A (is coordinator) | ❌ No |

**Key mechanism:** VS Code’s `agents` array overrides `disable-model-invocation: true` for listed agents. The coordinator CAN invoke protected agents; other agents CANNOT.

### Proposed pch-pipeline Coordinator Agent

```yaml
---
name: pch-pipeline
description: Orchestrates the full PCH development pipeline using subagent delegation
model: Claude Opus 4.6
tools: ['agent', 'read', 'search']
agents: ['pch-visionary', 'pch-researcher', 'pch-planner', 'pch-plan-reviewer', 'pch-coder']
handoffs:
  - label: Get Workflow Help
    agent: pch-helper
    prompt: Help the user understand the PCH pipeline workflow
    send: false
---
```

**Coordinator orchestration loop:**

```
Vision ──[vision_doc]──► Research ──[research_doc]──► Planning ──[plan_doc]──► Review ──[approved]──► Coding
                                                                     ▲                          │
                                                                     └── needs-revision ────────┘
```

- Coordinator invokes each agent as a subagent with document paths
- Parses structured completion signals from results
- Tracks minimal state: document path + 3-5 sentence summary per stage
- Maximum 3 review cycles before escalating to user
- Multi-release: completes full pipeline per release, compacts between releases

**Context growth estimate:**

| Stages | Estimated Coordinator Context | Assessment |
|--------|------------------------------|------------|
| Single feature (5 stages) | 25-50K tokens | ✅ Comfortable on 200K |
| Multi-release (15 stages) | 75-150K tokens | ⚠️ Needs aggressive summarization |
| Large project (25+ stages) | 125-250K tokens | ❌ Exceeds limits — per-release compaction required |

### Structured Completion Signals

Each agent would output a machine-parseable completion block:

```
VISION_COMPLETE / RESEARCH_COMPLETE / PLAN_COMPLETE / REVIEW_COMPLETE / IMPLEMENTATION_COMPLETE
DOCUMENT_PATH: /docs/{type}/{NNN}-{description}.md
STATUS: complete | partial | blocked | needs-revision
```

These are additive to existing text-based signals — they don’t replace them. Hugh’s `SignalClassifier` could adopt these as additional regex patterns.

### Backward Compatibility with Hugh CLI

**All proposed changes are backward-compatible:**
- Hugh doesn’t parse YAML frontmatter for `tools`, `agents`, or `disable-model-invocation` — it uses `SessionConfig` directly
- Structured completion signals are additive to existing text/tool-based signals
- Coordinator-aware mode activates only when running as a subagent (Hugh invokes as top-level)
- `pch-pipeline` is a new agent file coexisting alongside existing agents; Hugh uses its own `LoopOrchestrator`

| File | Relevance |
|------|----------|
| `agents/pch-visionary.md` | Needs `disable-model-invocation`, `tools`, improved handoffs |
| `agents/pch-researcher.md` | **Highest priority**: needs forward handoff to planner |
| `agents/pch-planner.md` | Needs `disable-model-invocation`, backward handoff to researcher |
| `agents/pch-plan-reviewer.md` | Needs `disable-model-invocation`, backward handoff to planner |
| `agents/pch-coder.md` | Needs `agents: ['pch-coder']`, forward handoff to visionary |
| `Hugh/Agents/CopilotService.cs` | Doesn't parse YAML frontmatter (compatibility confirmed) |
| `Hugh/Detection/SignalClassifier.cs` | Could adopt new structured signal patterns |

**External Sources:**
- [Custom agents](https://code.visualstudio.com/docs/copilot/customization/custom-agents) — `agents`, `disable-model-invocation`, `tools` properties
- [Subagents](https://code.visualstudio.com/docs/copilot/agents/subagents) — Coordinator/worker pattern, `agents` override behavior

**Gaps:** Whether `disable-model-invocation` + coordinator `agents` override works exactly as documented needs testing. Token overhead of `tools`/`agents` properties unknown. Coordinator-aware autonomous mode for interactive agents needs significant prototyping.

## Phase 5: Migration & Integration Strategy

**Status:** ✅ Complete  
**Session:** 2026-04-26

### Migration Phases (Incremental Rollout)

Four phases, each independently valuable with clean rollback paths:

| Phase | Goal | Prerequisites | Effort | Risk |
|-------|------|---------------|--------|------|
| **M1: Quick Wins** | Fix handoff chain, add `tools`/`agents` arrays | Current agents | ~1 hour | Zero |
| **M2: Coordinator MVP** | Create `pch-pipeline` agent, structured completion signals | M1 complete, VS Code settings | 3-6 hours | Low |
| **M3: Autonomous Mode** | Coordinator-aware mode for interactive agents | M2 working | 3-5 hours | Medium |
| **M4: Full Autopilot** | Multi-release pipeline, error recovery, progress reporting | M3 tested | 4-8 hours | Medium |

#### M1: Quick Wins (YAML Changes Only)

7 changes, ~1 hour, zero backward compatibility risk:
- QW-1: Add researcher→planner handoff (`send: true`) — closes biggest gap
- QW-2: Add reviewer→planner backward handoff
- QW-3: Add `agents: ['pch-researcher']` to researcher
- QW-4: Add `agents: ['pch-coder']` to coder
- QW-5: Enrich handoff prompts with document path instructions
- QW-6: Add `tools` arrays to all agents
- QW-7: Add coder→visionary "Start New Feature" handoff

**Rollback:** `git checkout agents/`

#### M2: Coordinator MVP

Create `pch-pipeline.md` coordinator agent:
```yaml
name: pch-pipeline
tools: ['agent', 'read', 'search']
agents: ['pch-visionary', 'pch-researcher', 'pch-planner', 'pch-plan-reviewer', 'pch-coder']
```

Add structured completion signals to all agents (`VISION_COMPLETE`, `RESEARCH_COMPLETE`, etc.) and `disable-model-invocation: true` to interactive agents.

**Pipeline state machine:** Vision → Research → Planning → Review (→ Revise loop, max 3) → Coding

**Rollback:** Delete `pch-pipeline.md`. Remove `disable-model-invocation`. Existing agents work unchanged.

#### M3: Autonomous Mode

Add `[COORDINATOR_MODE]` detection to visionary, planner, and reviewer:
- Visionary: auto-completes Q&A with reasonable defaults
- Planner: auto-selects approaches from research findings
- Reviewer: completes full review in single pass

**When invoked directly by user:** unchanged interactive Q&A behavior.

**Rollback:** Remove coordinator-aware sections from prompts.

#### M4: Full Autopilot Pipeline

Multi-release support, error recovery, progress reporting. User invokes `@pch-pipeline`, selects Autopilot, walks away.

### VS Code Settings (Copy-Paste Ready)

**M2+ settings (recommended for coordinator):**
```json
{
  "chat.customAgentInSubagent.enabled": true,
  "chat.subagents.allowInvocationsFromSubagents": true,
  "chat.autopilot.enabled": true,
  "chat.permissions.default": "autopilot",
  "github.copilot.chat.summarizeAgentConversationHistory.enabled": true
}
```

| Setting | M1 | M2 | M3 | M4 |
|---------|----|----|----|----|  
| `chat.customAgentInSubagent.enabled` | Required¹ | Required | Required | Required |
| `chat.subagents.allowInvocationsFromSubagents` | Optional | **Required** | **Required** | **Required** |
| `chat.autopilot.enabled` | Optional | Recommended | Recommended | **Required** |
| `chat.permissions.default` | — | Optional | Optional | `"autopilot"` |

¹ Already required by current pch-researcher and pch-coder.

### Testing Strategy

| Test Area | M1 | M2 | M3 | M4 |
|-----------|----|----|----|----|  
| Handoff buttons | ✅ | ✅ | ✅ | ✅ |
| Subagent invocation | ✅ | ✅ | ✅ | ✅ |
| Context isolation | — | ✅ | ✅ | ✅ |
| Coordinator pipeline | — | ✅ | ✅ | ✅ |
| Autonomous agent mode | — | — | ✅ | ✅ |
| Hugh CLI regression | ✅ | ✅ | ✅ | ✅ |

**Critical edge cases:** coordinator context exhaustion (multi-release), review loop convergence (max 3 cycles), subagent nesting depth (pipeline→researcher→researcher-subagent = 3 levels, within 5-level limit).

### Dual-Environment Integration (Hugh CLI)

**Hugh requires ZERO code changes across all migration phases.**

| Migration Phase | Hugh Impact | Why |
|-----------------|-------------|-----|
| M1: Quick wins | None | Hugh doesn’t parse YAML `tools`/`agents`/handoffs |
| M2: Coordinator | None | `pch-pipeline` is VS Code-only; Hugh uses `LoopOrchestrator` |
| M3: Autonomous mode | None | `[COORDINATOR_MODE]` only triggers under subagent context |
| M4: Full pipeline | None | Hugh has its own `SolutionOrchestrator` for multi-release |

**Optional Hugh improvements:**
- Add `VISION_COMPLETE`, `RESEARCH_COMPLETE`, etc. as new `SignalClassifier` regex patterns (additive, non-breaking)
- These align with existing dual-detection architecture (tool events primary, text regex fallback)

**Version compatibility:** Updated agent files ship automatically in Hugh’s portable ZIP via the release workflow. No release pipeline changes needed.

### Risk Assessment

| Risk | Phase | Probability | Impact | Mitigation |
|------|-------|-------------|--------|------------|
| `tools` array blocks needed tools | M1 | Low | Medium | Test thoroughly; start with broad lists |
| `disable-model-invocation` prevents legitimate use | M2 | Low | High | Test coordinator `agents` override first |
| **Coordinator context exhaustion** | M2-M4 | Medium | **High** | Store only doc paths + summaries; per-release compaction |
| **Autonomous mode quality degradation** | M3 | Medium | Medium | A/B test vs. interactive; keep coordinator-mode gated |
| Autopilot auto-responds incorrectly | M4 | Medium | Medium | Use `[COORDINATOR_MODE]` defaults instead of Autopilot’s generic responses |
| Hugh CLI regression | M1-M3 | Low | High | Run `dotnet test` after every prompt change |

### User Experience Progression

**M1:** More handoff buttons, smoother transitions. Same manual workflow.

**M2:** Single `@pch-pipeline` invocation replaces manual agent sequencing. User monitors progress.

**M3:** Pipeline runs without pauses for Q&A. Individual agent usage unchanged.

**M4 (end state):** User types feature request → selects Autopilot → invokes `@pch-pipeline` → walks away → returns to find vision, research, plan, review, and code all completed. Estimated end-to-end: ~55 minutes for a single feature.

**Key Discoveries:**
- All four migration phases are independently valuable with clean rollback paths
- Hugh CLI requires ZERO code changes across all phases
- `[COORDINATOR_MODE]` marker is preferable to Autopilot’s generic auto-response for interactive agents
- Subagent nesting is safe at 3 levels (pipeline → researcher → researcher-subagent)
- pch-helper should gain a "Run Full Pipeline" handoff to pch-pipeline in M2
- Release workflow needs no changes — updated agents ship automatically

| File | Relevance |
|------|----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | Phase pipeline that pch-pipeline mirrors |
| `Hugh/Detection/SignalClassifier.cs` | Regex patterns for structured signal alignment |
| `Hugh/Agents/CopilotService.cs` | Per-phase fresh sessions, document context injection |
| `.github/workflows/release.yml` | Confirms updated agents deploy automatically |
| `agents/pch-helper.md` | Should gain pch-pipeline handoff in M2 |

**External Sources:** None (synthesis of prior phases)

**Gaps:** `disable-model-invocation` + `agents` override needs live testing. Autopilot auto-response quality for domain-specific questions is unpredictable. Multi-release compaction strategy needs prototyping.

## Overview

This research investigated GitHub Copilot's Autopilot feature and analyzed how the PCH agent workflow (visionary → researcher → planner → reviewer → coder) can be transformed to execute handoffs automatically with fresh context windows at each transition. Five phases of research covered the Autopilot capability itself, the current agent handoff architecture, context window management patterns, per-agent improvement recommendations, and a complete migration strategy.

### The Core Problem

The PCH agent pipeline currently relies on **handoff buttons** (YAML `handoffs:` in agent frontmatter) to chain agents together. This mechanism has three fundamental limitations:

1. **Handoff buttons are UI interactions, not tool calls** — even with `send: true`, a user must click the button. Autopilot cannot trigger them automatically.
2. **Handoffs carry conversation history** — the new agent session inherits the full prior conversation, causing context accumulation rather than isolation.
3. **The chain is broken** — pch-researcher has NO forward handoff to pch-planner (only a self-loop), creating a fully manual gap in the middle of the pipeline.

Additionally, handoff prompts are context-free (they don't include document paths), self-handoffs don't carry session state, and the pipeline has no forward handoff after pch-coder (terminal dead end).

### The Solution: Autopilot + Coordinator/Worker Pattern

The central discovery is that **Autopilot and subagent orchestration solve complementary problems**:

| Capability | Autopilot | Subagents | Combined |
|-----------|-----------|-----------|----------|
| Auto-approve tool calls | ✅ | N/A | ✅ |
| Auto-respond to questions | ✅ | N/A | ✅ |
| Fresh context per phase | ❌ | ✅ | ✅ |
| Automate cross-agent transitions | ❌ | ✅ | ✅ |
| Zero user interaction | ✅ | Partial | ✅ |

Neither alone achieves fully automated, context-efficient pipeline execution. Combined via a **coordinator/worker pattern** — a new `pch-pipeline` agent that invokes each PCH agent as a subagent — they enable zero-click orchestration with fresh context per stage.

### Architecture: Current vs. Proposed

**Current architecture (handoff-based):**
```
User clicks → Visionary ──[button]──► Researcher ──[NO HANDOFF]──► Planner ──[button]──► Reviewer ──[button]──► Coder ──[dead end]
                                                     ↑ MANUAL GAP
```
- 5 manual user actions required
- Context accumulates across handoffs
- No isolation between agents

**Proposed architecture (coordinator + subagent):**
```
User invokes @pch-pipeline (Autopilot mode)
  └── Pipeline coordinator (single orchestrator session)
        ├── runSubagent → pch-visionary    [fresh context] → vision_doc
        ├── runSubagent → pch-researcher   [fresh context] → research_doc
        ├── runSubagent → pch-planner      [fresh context] → plan_doc
        ├── runSubagent → pch-plan-reviewer [fresh context] → approved plan
        │     └── (review loop: max 3 cycles back to planner)
        └── runSubagent → pch-coder        [fresh context] → implemented code
```
- 0 manual user actions required
- Fresh context for each agent
- Coordinator holds only document paths + brief summaries (~25-50K tokens)

### Agent Categories and Their Automation Paths

The five core agents fall into two categories with different automation characteristics:

**Subagent Orchestrators (researcher, coder):** Already use the coordinator/worker pattern internally — they invoke themselves as subagents for phase isolation. These agents are **immediately compatible** with the pipeline coordinator. No prompt changes needed for basic operation.

**Interactive Agents (visionary, planner, reviewer):** Currently require human Q&A interaction. For fully autonomous operation under a coordinator, these need a **`[COORDINATOR_MODE]`** detection mechanism that switches from interactive Q&A to autonomous decision-making with documented assumptions. This is the most complex prompt engineering task in the migration.

### Access Control Model

A clean access control model prevents arbitrary agents from invoking interactive agents as subagents while allowing the coordinator to do so:

| Agent | User-Invocable | Coordinator Subagent | Other Subagent |
|-------|:---:|:---:|:---:|
| pch-visionary | ✅ | ✅ | ❌ |
| pch-researcher | ✅ | ✅ | ❌ |
| pch-planner | ✅ | ✅ | ❌ |
| pch-plan-reviewer | ✅ | ✅ | ❌ |
| pch-coder | ✅ | ✅ | ❌ |
| pch-pipeline | ✅ | N/A | ❌ |

**Mechanism:** `disable-model-invocation: true` on each agent blocks arbitrary subagent use. The coordinator's `agents: [...]` array explicitly overrides this for listed agents (documented VS Code behavior).

### Context Window Analysis

Only **three mechanisms** produce truly fresh context windows:

| Mechanism | Fresh Context | Automatable | Used By |
|-----------|:---:|:---:|---------|
| New chat session | ✅ | ❌ (manual) | Current PCH inter-agent |
| Subagent invocation | ✅ | ✅ | Current PCH intra-agent; proposed coordinator |
| Hugh SDK session | ✅ | ✅ | Hugh CLI |

Handoffs, compaction, and forks do NOT provide fresh context — they carry or compress existing history. **Subagent invocation is the only automatable fresh-context mechanism in VS Code**, making it the required foundation for any automated pipeline.

Context budget estimates show the coordinator is comfortable for single features (~25-50K) but needs per-release compaction for multi-release workflows (~75-150K).

### Migration Strategy

The migration is organized into four incremental phases, each independently valuable:

| Phase | What Changes | Effort | Risk | Key Capability Gained |
|-------|-------------|--------|------|----------------------|
| **M1: Quick Wins** | 7 YAML-only changes to existing agents | ~1 hour | Zero | Complete handoff chain; `tools`/`agents` arrays |
| **M2: Coordinator MVP** | New `pch-pipeline` agent + completion signals | 3-6 hours | Low | Automated pipeline via subagents |
| **M3: Autonomous Mode** | `[COORDINATOR_MODE]` in interactive agents | 3-5 hours | Medium | No-pause Q&A under coordinator |
| **M4: Full Autopilot** | Multi-release, error recovery, progress reporting | 4-8 hours | Medium | Walk-away end-to-end automation |

Every phase has a clean rollback path. M1 requires only `git checkout agents/`. M2-M4 require deleting the coordinator file and reverting prompt additions.

### Backward Compatibility

**Hugh CLI requires ZERO code changes across all four migration phases.** This is because:
- Hugh doesn't parse YAML frontmatter for `tools`, `agents`, or `disable-model-invocation` — it uses `SessionConfig` directly
- Structured completion signals (`VISION_COMPLETE`, `RESEARCH_COMPLETE`, etc.) are additive to existing text/tool-based dual-detection
- `[COORDINATOR_MODE]` activates only under subagent context — Hugh invokes agents as top-level sessions
- `pch-pipeline` is a new file that coexists alongside existing agents; Hugh uses its own `LoopOrchestrator`
- Updated agent files ship automatically via the release workflow's portable ZIP

### End-State User Experience

**Before (current):** User manually invokes 5+ agents across separate sessions, clicks handoff buttons, attaches documents, and manages the pipeline (~2-3 hours of active involvement for a feature).

**After (M4):** User types a feature request → selects Autopilot → invokes `@pch-pipeline` → walks away → returns to find vision document, research, implementation plan, review, and code changes all completed (~55 minutes autonomous execution).

Individual agents remain fully usable for users who prefer interactive control.

## Key Findings

### Autopilot Feature (Phase 1)

1. **Autopilot is a permission level, not a feature or API** — it sits alongside "Default Approvals" and "Bypass Approvals" in the VS Code permissions picker. The key differentiator from Bypass Approvals is **auto-responding to clarifying questions** — the agent never stalls waiting for user input.

2. **Autopilot does NOT create fresh context windows** — context accumulates within a session and is eventually compacted (summarized). Compaction is lossy — structured data, tables, code references, and specific return formats may be degraded.

3. **Configuration is minimal:** `chat.autopilot.enabled` (default: `true`) and `chat.permissions.default` to persist the preference. No per-agent Autopilot scoping exists — it's session-level.

### Handoff Architecture (Phase 2)

4. **Handoff buttons are UI interactions, not tool calls** — Autopilot cannot automatically trigger handoffs between agents. This is the fundamental limitation that makes the current handoff chain incompatible with full automation.

5. **The researcher→planner gap is the biggest chain break** — pch-researcher has no forward handoff to pch-planner. After research completes, the text says "Hand off to `@pch-planner`" but no button exists. Adding one with `send: true` is the single highest-impact quick win.

6. **Only 2 of 13 total handoffs use `send: true`** — planner→reviewer and reviewer→coder form a partial "fast lane." All other transitions require explicit user action.

7. **Handoff prompts are context-free** — they don't include document paths, forcing users to manually attach or reference documents at each transition.

8. **Two distinct agent categories exist** — Interactive (visionary, planner, reviewer) vs. Subagent Orchestrators (researcher, coder). The orchestrators already use the coordinator/worker pattern internally but lack inter-agent coordination.

### Context Management (Phase 3)

9. **Subagent invocation is the ONLY automatable mechanism for fresh context in VS Code** — handoffs carry conversation history, compaction is lossy, new sessions are manual. `runSubagent` is the only tool-call-based mechanism that creates a truly isolated context window.

10. **Document-based persistence is fundamentally more reliable than conversation history** — documents don't degrade, can be re-read at full fidelity, and survive all session boundaries. This validates PCH's existing architecture.

11. **Custom instructions survive compaction** but conversation history, tool outputs, and structured data are lossy-compressed. The structured return formats PCH agents rely on (STATUS, FINDINGS, etc.) may not survive compaction reliably.

12. **A coordinator managing 25+ phases** (e.g., 5 releases × 5 stages) would accumulate 50-125K tokens of returned results — approaching limits even on 200K models. Per-release compaction and aggressive summarization are required.

### Agent Improvements (Phase 4)

13. **`disable-model-invocation: true` + coordinator `agents` array creates clean access control** — interactive agents are protected from arbitrary subagent invocation while the coordinator can override via its explicit `agents` list. VS Code documentation confirms this override behavior.

14. **7 quick wins are implementable in ~1 hour with zero risk** — researcher→planner handoff, reviewer→planner backward handoff, `agents` arrays for orchestrators, `tools` arrays for all agents, enriched handoff prompts, and coder→visionary cycle closure.

15. **`[COORDINATOR_MODE]` is preferable to Autopilot auto-response** — for interactive agents, providing their own intelligent defaults when running as subagents is more reliable than relying on Autopilot's generic auto-response behavior.

16. **Structured completion signals** (`VISION_COMPLETE`, `RESEARCH_COMPLETE`, `PLAN_COMPLETE`, `REVIEW_COMPLETE`, `IMPLEMENTATION_COMPLETE`) provide machine-parseable handoff data. These are additive to existing text-based signals and compatible with Hugh's `SignalClassifier`.

### Migration & Integration (Phase 5)

17. **All changes are backward-compatible with Hugh CLI** — Hugh doesn't parse YAML frontmatter for `tools`/`agents`/`disable-model-invocation`. The `pch-pipeline` coordinator is VS Code-only. Hugh uses its own `LoopOrchestrator` and `SolutionOrchestrator` for the same orchestration function.

18. **The release workflow needs no changes** — updated agent files ship automatically in Hugh's portable ZIP. `dotnet test` validates Hugh CLI regression at every migration phase.

19. **Subagent nesting depth is safe** — pch-pipeline (level 1) → pch-researcher (level 2) → researcher-subagent (level 3) is well within the 5-level maximum. Requires `chat.subagents.allowInvocationsFromSubagents: true`.

20. **pch-helper should gain a "Run Full Pipeline" handoff** to pch-pipeline in M2, becoming the natural entry point for users choosing between full automation and individual agent control.

## Actionable Conclusions

### Immediate Actions (M1 — This Week)

1. **Add forward handoff from pch-researcher to pch-planner** with `send: true` — this alone closes the biggest gap in the chain and dramatically improves the user experience.

2. **Add backward handoff from pch-plan-reviewer to pch-planner** ("Revise Plan") — formalizes the currently text-only escalation path.

3. **Add `agents: ['pch-researcher']` and `agents: ['pch-coder']`** — formalizes the existing self-delegation pattern in both subagent orchestrators.

4. **Add `tools` arrays to all 5 core agents** — enforces principle of least privilege (e.g., researcher shouldn't modify code files).

5. **Enrich all handoff prompts** with document path instructions — "Attach the document at the path shown above" instead of generic "Review the plan."

6. **Add "Start New Feature" handoff from pch-coder to pch-visionary** — closes the pipeline loop.

### Medium-Term Actions (M2-M3 — Next Sprint)

7. **Create `pch-pipeline` coordinator agent** — single entry point for full pipeline automation. YAML: `agents: [all 5 PCH agents]`, `tools: ['agent', 'read', 'search']`.

8. **Add structured completion signals** to all agents — `{PHASE}_COMPLETE\nDOCUMENT_PATH: ...\nSTATUS: ...` enables machine-parseable handoff.

9. **Add `disable-model-invocation: true`** to visionary, planner, and reviewer — protects interactive agents from arbitrary subagent invocation.

10. **Design `[COORDINATOR_MODE]` autonomous behavior** for interactive agents — most complex prompt engineering task. Start with planner (most structured), then reviewer, then visionary.

### Long-Term Actions (M4 — Next Month)

11. **Multi-release coordinator orchestration** — process releases sequentially with per-release compaction to manage context growth.

12. **Add pch-pipeline handoff to pch-helper** — helper becomes the guidance hub for choosing between automated and manual workflows.

13. **Optional Hugh CLI enhancement** — add `VISION_COMPLETE`/`RESEARCH_COMPLETE` regex patterns to `SignalClassifier` for improved phase detection reliability.

14. **A/B quality testing** — compare coordinator-generated outputs against manually-guided outputs to validate autonomous mode quality.

## Open Questions

### Needs Live Testing

1. **Does `agents` array correctly override `disable-model-invocation: true`?** — VS Code documentation says "A coordinator agent can reference subagents using the agents property, which overrides disable-model-invocation." However, this has not been tested with PCH's specific agent configuration and may have edge cases.

2. **Do `send: true` handoffs auto-execute under Autopilot?** — Documentation describes handoff buttons as "interactive suggestions after a chat response completes," implying they are UI-level. However, it's unclear whether Autopilot changes this behavior. Testing would confirm whether the planner→reviewer→coder "fast lane" becomes fully automatic under Autopilot.

3. **Does `tools` restriction work as expected?** — Adding `tools: ['read', 'search', 'editFiles']` to agents may block tools they currently use implicitly. Each agent's tool usage must be audited before deploying restrictions.

### Needs A/B Testing

4. **What is the quality impact of autonomous mode?** — Will coordinator-generated vision documents and plans be comparable to manually-guided ones? Interactive Q&A allows the human to steer decisions; autonomous mode relies on the agent's judgment. Complex features with ambiguous requirements may suffer quality degradation.

5. **How reliable is the coordinator's structured signal parsing?** — The coordinator parses freeform text from subagent results to extract `DOCUMENT_PATH` and `STATUS`. This is inherently fragile. If signals are buried in verbose output or formatted inconsistently, the coordinator may fail to extract them.

### Undocumented Behavior

6. **How does compaction affect structured data?** — When the coordinator's context is compacted, do document paths and structured pipeline state survive? The fidelity of compacted summaries for structured/tabular data is not documented by VS Code.

7. **Can Autopilot behavior be scoped per-agent?** — Currently appears session-level only. Per-agent scoping would allow Autopilot for simple stages (research, review) but interactive mode for complex decisions (visionary, planner).

8. **What is the actual token overhead of VS Code system instructions?** — Affects coordinator context budget estimates. The ~2-4K estimate is based on general LLM prompt engineering knowledge, not VS Code-specific measurement.

9. **Are subagent results treated differently in parent compaction?** — When the coordinator's context is compacted, are subagent return values eligible for summarization, or are they pinned? This affects how quickly the coordinator's context degrades.

10. **What is the premium request consumption under Autopilot?** — Long multi-stage pipelines with nested subagents may consume significant API quota. Cost implications for multi-release workflows are unknown.

## Standards Applied

| Standard | Relevance | Guidance |
|----------|-----------|----------|
| No organizational standards applicable to this research. | | |

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2026-04-26 |
| Status | ✅ Complete |
| Current Phase | ✅ Complete |
| Path | /docs/research/025-copilot-autopilot-and-agent-handoff-automation.md |
