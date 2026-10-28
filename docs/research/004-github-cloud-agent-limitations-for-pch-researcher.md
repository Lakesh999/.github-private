# Research: GitHub Cloud Agent Limitations for pch-researcher

## Document Information

| Field | Value |
|-------|-------|
| Document | 004-github-cloud-agent-limitations-for-pch-researcher.md |
| Created | January 17, 2026 |
| Status | ✅ Complete |
| Current Phase | Single-Phase Research |

## Overview

This research investigates why the pch-researcher agent prompt may not work properly as a GitHub cloud agent (Copilot Coding Agent on GitHub.com) and what changes could be made to enable automatic/subagent mode in the cloud environment.

### Key Findings

1. **Prompt Size Limit:** GitHub custom agents have a **30,000 character limit** for the prompt content. The pch-researcher.md file is approximately **44,339 characters**, which **exceeds the limit by ~48%**.

2. **Subagent Support Not Available:** The `runSubagent` tool used in VS Code for automatic mode **is not available** in GitHub Copilot Coding Agent (cloud). The `handoffs` YAML property is **not supported on GitHub.com** (only VS Code and other IDEs).

3. **Feature Parity Gap:** GitHub Copilot Coding Agent and VS Code agent mode have different feature sets:
   - VS Code supports `model`, `argument-hint`, and `handoffs` properties
   - GitHub.com currently ignores these properties for compatibility
   - The "agent" tool alias (`agent`, `custom-agent`, `Task`) allows invoking different custom agents, but not the same recursive subagent orchestration pattern

4. **Environment Differences:** The automatic mode workflow was designed for VS Code's `chat.customAgentInSubagent.enabled` setting, which is a VS Code-specific feature.

### Root Causes

| Issue | Cause | Impact |
|-------|-------|--------|
| Prompt too large | 44,339 chars > 30,000 char limit | Agent cannot be loaded on GitHub.com |
| No subagent invocation | `runSubagent` not available in cloud | Automatic mode workflow doesn't work |
| Handoffs not supported | VS Code-specific feature | Cannot hand off to self for continuation |
| Different tool ecosystem | Cloud uses different tool abstraction | Tool names/patterns may differ |

### Recommendations

1. **Create a slimmed-down cloud variant** (< 30,000 chars) that focuses on manual mode only
2. **Split the agent** into smaller, specialized agents for cloud use
3. **Remove automatic mode sections** for the cloud version (save ~50% content)
4. **Wait for feature parity** if automatic mode is essential (GitHub may add subagent support)

---

## Research Objectives

Understand why pch-researcher doesn't work as a GitHub cloud agent and identify changes needed for cloud compatibility.

### Key Questions to Answer

1. What are the specific limitations of GitHub Copilot Coding Agent (cloud) compared to VS Code?
2. Why doesn't the subagent orchestration pattern work in the cloud?
3. What changes would make pch-researcher compatible with GitHub cloud agents?

---

## Detailed Findings

### 1. Prompt Size Limit (30,000 Characters)

From the official GitHub documentation:

> "Write the agent's prompt. Define the agent's behavior, expertise, and instructions in the Markdown content below the YAML frontmatter. **The prompt can be a maximum of 30,000 characters.**"
> — [Custom agents configuration](https://docs.github.com/en/copilot/reference/custom-agents-configuration)

**Current pch-researcher.md Size:**
- Character count: **44,339 characters**
- Line count: **1,317 lines**
- Exceeds limit by: **~14,339 characters (48% over)**

**Breakdown of Content Size:**

| Section | Approximate Size | Notes |
|---------|------------------|-------|
| YAML frontmatter | ~200 chars | Minimal |
| Critical Rules | ~1,500 chars | Essential |
| Core Responsibilities | ~800 chars | Essential |
| Mode Selection + Prerequisites | ~2,500 chars | Automatic mode specific |
| Automatic Mode Workflow | ~15,000 chars | Largest section, cloud-incompatible |
| Manual Mode Workflow | ~5,000 chars | Essential |
| Document Numbering | ~1,000 chars | Useful |
| Phase Breakdown Strategies | ~5,000 chars | Reference material |
| Standards Integration | ~1,200 chars | Optional |
| Document Structure Template | ~3,000 chars | Reference material |
| Handoff Messages | ~2,500 chars | Essential |
| Best Practices | ~4,000 chars | Reference material |
| Quality Standards | ~2,500 chars | Reference material |

**The Automatic Mode Workflow section alone is ~15,000 characters** — removing or significantly condensing it would bring the agent under the limit.

### 2. Subagent/Handoff Support Not Available on GitHub.com

From the official documentation:

> "The `model`, `argument-hint`, and `handoffs` properties from VS Code and other IDE custom agents are **currently not supported for Copilot coding agent on GitHub.com**. They are ignored to ensure compatibility."
> — [Custom agents configuration](https://docs.github.com/en/copilot/reference/custom-agents-configuration)

**Implications:**
- The `handoffs` YAML property (used for "Continue researching" → `pch-researcher` self-handoff) is ignored
- The `runSubagent` tool call that powers automatic mode in VS Code doesn't exist on GitHub.com
- There's no mechanism for an agent to invoke itself or another custom agent as a subagent

**Tool Alias Analysis:**

The `agent` tool alias (`agent`, `custom-agent`, `Task`) in GitHub.com documentation says:
> "Allows a different custom agent to be invoked to accomplish a task."

However, this is different from the subagent orchestration pattern used in automatic mode:
- It's for **delegating** work to a different specialized agent
- It's not for **recursive self-invocation** with phase-specific prompts
- It doesn't provide the same isolation and result capture as `runSubagent`

### 3. VS Code vs GitHub.com Feature Parity

| Feature | VS Code | GitHub.com |
|---------|---------|------------|
| Custom agents | ✅ Supported | ✅ Supported |
| `name` property | ✅ Supported | ✅ Supported |
| `description` property | ✅ Supported | ✅ Supported |
| `tools` property | ✅ Supported | ✅ Supported |
| `model` property | ✅ Supported | ❌ Ignored |
| `handoffs` property | ✅ Supported | ❌ Ignored |
| `mcp-servers` property | ❌ Not supported at repo level | ✅ Supported at org/enterprise level |
| Subagent invocation (`runSubagent`) | ✅ Supported | ❌ Not available |
| `chat.customAgentInSubagent.enabled` | ✅ Configurable | N/A |
| Prompt size limit | Larger (unclear exact limit) | **30,000 characters** |

### 4. What Would Work on GitHub.com

For pch-researcher to work on GitHub.com cloud agents, it needs to:

**A. Fit within 30,000 characters**

Options to reduce size:
1. Remove automatic mode sections entirely (~15,000 chars saved)
2. Condense reference material (phase strategies, templates) into brief summaries
3. Move detailed examples to external documentation (link to them instead)
4. Use more concise language throughout

**B. Use Manual Mode Only**

Since subagent orchestration isn't available:
- Focus on the one-phase-per-session workflow
- Rely on human intervention to continue between phases
- Use the cloud agent's native session management

**C. Adapt Tool References**

The pch-researcher references VS Code-specific concepts:
- `chat.customAgentInSubagent.enabled` — Remove or mark as VS Code only
- `runSubagent` tool — Not available, remove references
- Browser MCP / fetch_webpage — May have different availability

### 5. Recommended Changes for Cloud Compatibility

#### Option A: Create a Separate Cloud-Optimized Agent (~Recommended)

Create `pch-researcher-cloud.agent.md` with:
- Manual mode workflow only
- Condensed reference sections
- No automatic mode content
- Explicit notes about VS Code features not available
- Target size: < 25,000 characters (leave buffer)

**Estimated content:**
```
YAML frontmatter: ~200 chars
Critical rules: ~1,500 chars
Core responsibilities: ~800 chars
Manual mode workflow: ~5,000 chars
Condensed phase strategies: ~2,000 chars
Condensed document structure: ~1,500 chars
Handoff messages: ~2,500 chars
Essential best practices: ~2,000 chars
```
**Total: ~15,500 characters** (comfortably under limit)

#### Option B: Modularize into Specialized Agents

Split pch-researcher into smaller agents:
- `pch-researcher-outline.agent.md` — Creates research outlines
- `pch-researcher-phase.agent.md` — Executes a single research phase
- `pch-researcher-synthesis.agent.md` — Creates final overview

Each would be under 10,000 characters and could be manually invoked in sequence.

#### Option C: Simplify the Existing Agent

Trim pch-researcher.md to fit by:
- Removing automatic mode sections
- Condensing strategy sections into bullet points
- Moving templates to separate documentation
- Using terse language

This risks losing valuable context and guidance.

### 6. Automatic Mode Alternatives for Cloud

While true subagent orchestration isn't available, consider these patterns:

**A. Multi-Turn Session Pattern**
- User invokes agent for Phase 1
- Agent completes phase, outputs findings, requests continuation
- User says "Continue with Phase 2"
- Same agent continues in same session with accumulated context

**Limitation:** Context window fills up over many phases

**B. Document-Driven Continuation**
- Each invocation reads the research document
- Finds next incomplete phase
- Completes that phase, updates document
- Ends session
- User starts new session, invokes agent again

**This is essentially manual mode** but with clear document-based state management.

**C. GitHub Actions Integration**
- Research document changes trigger a GitHub Action
- Action invokes Copilot Coding Agent for next phase
- Could create a semi-automated pipeline

**Experimental, not well-supported currently**

---

## Comparison: pch-researcher vs pch-coder

For reference, the pch-coder.md is 32,536 characters, which also exceeds the 30,000 limit. Both agents would need similar treatment for cloud compatibility.

| Agent | Characters | Over Limit | Primary Issue |
|-------|------------|------------|---------------|
| pch-researcher.md | 44,339 | +48% | Automatic mode content |
| pch-coder.md | 32,536 | +8% | Automatic mode content |

---

## Conclusions

### Why pch-researcher Doesn't Work as a Cloud Agent

1. **Too Large:** At 44,339 characters, it exceeds the 30,000 character limit by 48%
2. **Unsupported Features:** Automatic mode relies on VS Code-specific subagent orchestration
3. **Ignored Properties:** The `handoffs` property is ignored on GitHub.com
4. **No Subagent Tool:** The `runSubagent` tool doesn't exist in the cloud environment

### What Would Make It Work

1. **Reduce to < 30,000 characters** by removing automatic mode and condensing reference material
2. **Focus on manual mode** workflow which is compatible with cloud agents
3. **Create a cloud-specific variant** that's optimized for GitHub.com limitations
4. **Accept manual phase progression** until GitHub adds subagent support

### Next Steps

1. Create `pch-researcher-cloud.agent.md` — A slimmed-down manual-mode-only version
2. Update the main pch-researcher.md with a note about cloud limitations
3. Consider the modular agent approach for complex research needs
4. Monitor GitHub's roadmap for subagent/handoff support on GitHub.com

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|
| GitHub Custom Agents Configuration | Direct | 30,000 character limit, property support |
| VS Code AI Extensibility | Comparison | Subagent and handoff capabilities |

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** January 17, 2026
**Status:** ✅ Complete
**Document Location:** /docs/research/004-github-cloud-agent-limitations-for-pch-researcher.md

---

✅ **This research is now fully complete.** The findings explain why pch-researcher doesn't work as a GitHub cloud agent and provide actionable recommendations for creating a cloud-compatible version.
