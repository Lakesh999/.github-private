# Research: Self-Operating Development Loop

## Document Information

| Field | Value |
|-------|-------|
| Document | 008-self-operating-development-loop.md |
| Created | 2026-02-05 |
| Status | ✅ Complete |
| Current Phase | 6 of 6 (All Complete) |
| Vision Source | /docs/vision/002-self-operating-development-loop.md |

## Overview

This research confirms that the Self-Operating Development Loop is **technically feasible** using existing GitHub Copilot CLI capabilities. The investigation covered six phases, from SDK exploration through document attachment patterns, yielding a clear implementation path.

### Executive Summary

| Research Question | Answer | Confidence |
|-------------------|--------|------------|
| Can agents be invoked headlessly? | ✅ Yes — via Copilot CLI `--prompt` and ACP | High |
| How to achieve fresh context per phase? | ✅ `session/new` in ACP; each `--prompt` invocation | High |
| How to detect phase completion? | ✅ Pattern matching on emoji markers + ACP `stopReason` | High |
| How to answer questions autonomously? | ✅ Meta-prompting with vision/standards context | Medium |
| How to attach documents between phases? | ✅ `ContentBlock::Resource` or `@file` references | High |

### Key Architectural Decision: GitHub.Copilot.SDK for .NET

**⚠️ CORRECTION (Post-Research Discovery):** The official **GitHub.Copilot.SDK** for .NET **does exist** at [github.com/github/copilot-sdk](https://github.com/github/copilot-sdk/tree/main/dotnet). This provides a first-class .NET API that wraps the Copilot CLI automatically.

**Installation:**
```bash
dotnet add package GitHub.Copilot.SDK
```

**Recommended Architecture:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     .NET Core CLI Orchestrator                          │
│                                                                         │
│   await using var client = new CopilotClient();                        │
│   await client.StartAsync();                                            │
│                        │                                                │
│   ┌────────────────────┼────────────────────────────────────────┐      │
│   │           GitHub.Copilot.SDK (.NET)                         │      │
│   │                                                              │      │
│   │   CreateSessionAsync() ──▶ Fresh context per phase          │      │
│   │   SendAsync() ──▶ Send prompts with attachments             │      │
│   │   OnUserInputRequest ──▶ Intercept agent questions          │      │
│   │   SessionIdleEvent ──▶ Detect completion                    │      │
│   └────────────────────┼────────────────────────────────────────┘      │
│                        │                                                │
│                   Copilot CLI (managed by SDK)                          │
│                        │                                                │
│               ┌────────┴────────┐                                       │
│               │  PCH Agents     │                                       │
│               │  (/agents/*.md) │                                       │
│               └─────────────────┘                                       │
└─────────────────────────────────────────────────────────────────────────┘
```

### SDK Key Features for Self-Operating Loop

| Feature | SDK API | Purpose |
|---------|---------|---------|
| Fresh context per phase | `CreateSessionAsync()` | Each call creates new isolated session |
| Question interception | `OnUserInputRequest` callback | Enables autonomous answering |
| Completion detection | `SessionIdleEvent` | Know when agent phase is done |
| File attachments | `MessageOptions.Attachments` | Pass documents to agents |
| System prompt customization | `SessionConfig.SystemMessage` | Load PCH agent prompts |
| Tool hooks | `Hooks.OnPreToolUse/OnPostToolUse` | Intercept tool execution |
| Streaming responses | `Streaming = true` | Real-time output monitoring |
| Infinite sessions | `InfiniteSessionConfig` | Automatic context compaction |

### Top 10 Research Discoveries

1. **Official .NET SDK exists** — `GitHub.Copilot.SDK` NuGet package wraps Copilot CLI with native .NET API
2. **CopilotClient manages CLI lifecycle** — SDK automatically spawns and manages Copilot CLI process
3. **Fresh context via `CreateSessionAsync()`** — Each call creates isolated session with zero conversation history (Ralph Wiggum pattern)
4. **`OnUserInputRequest` enables autonomous answering** — Callback intercepts agent questions before they reach user
5. **Four output categories** — Phase complete, final complete, question prompts, blockers — detected via events
6. **Agent recommendations enable autonomy** — PCH agents provide recommended answers; orchestrator can accept when confidence is high
7. **Meta-prompting for decisions** — Use LLM to evaluate agent recommendations against vision/standards
8. **Documents persist state** — PCH already uses documents for state; orchestrator extends with metadata in `.orchestrator/`
9. **Two attachment mechanisms** — `ContentBlock::Resource` (immediate embed) or `@file` (deferred read)
10. **Selective embedding optimal** — Embed vision + current phase section; summarize or reference large research docs

### Implementation Approach

| Component | Recommended Technology | Rationale |
|-----------|------------------------|-----------|
| **Orchestrator** | .NET Core Console App | User requirement; excellent async/await support |
| **Agent Invocation** | `GitHub.Copilot.SDK` | Official SDK with native .NET API |
| **Session Management** | `CopilotClient.CreateSessionAsync()` | Fresh context per phase |
| **Completion Detection** | `SessionIdleEvent` + content parsing | SDK events + emoji markers for redundancy |
| **Question Interception** | `OnUserInputRequest` callback | Built-in SDK support |
| **Document Attachment** | `MessageOptions.Attachments` | SDK file attachment API |

### Risk Summary

| Risk | Mitigation |
|------|-----------|
| SDK is in Technical Preview | Design abstraction layer; monitor for breaking changes |
| Large documents exceed context | Selective embedding; use `InfiniteSessions` for auto-compaction |
| Autonomous answers may be wrong | Confidence thresholds; decision audit log |
| Copilot CLI dependency | SDK manages CLI lifecycle automatically |

### Next Steps

1. **Create implementation plan** — Hand off to pch-planner
2. **Build orchestrator with SDK** — Use `CopilotClient` for session management
3. **Implement autonomous answering** — Via `OnUserInputRequest` callback
4. **Design phase executor** — Fresh `CreateSessionAsync()` per phase
5. **Add logging and metrics** — Track decisions and confidence scores

---

## Research Objectives

Investigate the technical feasibility and implementation approaches for building a .NET Core CLI that orchestrates PCH agents in an autonomous development loop. This research directly supports the vision of enabling "fire-and-forget" feature development.

### Key Questions to Answer

1. Can the GitHub Copilot SDK (Technical Preview) invoke custom agents headlessly outside VS Code?
2. How can agent sessions be spawned programmatically with fresh context?
3. What mechanisms exist to detect phase completion and intercept agent questions?
4. How can documents be attached to agent sessions via API?
5. What approaches enable autonomous question answering (model self-response)?

---

## Research Phases

| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 1 | Copilot SDK .NET Capabilities & Custom Agents | ✅ Complete | 2026-02-05 |
| 2 | Headless Agent Invocation Feasibility | ✅ Complete | 2026-02-05 |
| 3 | Session Management & Fresh Context | ✅ Complete | 2026-02-05 |
| 4 | Completion Detection & Signal Parsing | ✅ Complete | 2026-02-05 |
| 5 | Autonomous Question Answering Patterns | ✅ Complete | 2026-02-05 |
| 6 | Document Attachment & State Passing | ✅ Complete | 2026-02-05 |

---

## Phase 1: Copilot SDK .NET Capabilities & Custom Agents

**Status:** ✅ Complete
**Scope:**
- Explore the GitHub Copilot SDK (Technical Preview) for .NET
- Understand how to define and load custom agents programmatically
- Investigate how PCH agent prompts (.md files) can be loaded as SDK agents
- Document SDK limitations and capabilities
- Assess stability and API maturity

### Research Tasks

- [x] Locate official Copilot SDK documentation for .NET
- [x] Identify SDK installation and setup requirements
- [x] Research custom agent definition APIs
- [x] Determine how system prompts are loaded/configured
- [x] Investigate tool/capability restrictions in SDK agents
- [x] Document SDK version and preview status implications

### Findings

#### ⚠️ POST-RESEARCH CORRECTION

The original Phase 1 research incorrectly concluded that no .NET SDK exists. **This was wrong.**

**The official GitHub.Copilot.SDK for .NET exists:** [github.com/github/copilot-sdk/tree/main/dotnet](https://github.com/github/copilot-sdk/tree/main/dotnet)

---

#### 1. GitHub.Copilot.SDK for .NET — Official SDK

**Installation:**
```bash
dotnet add package GitHub.Copilot.SDK
```

**Requirements:**
- .NET 8.0 or later
- GitHub Copilot CLI installed and in PATH (SDK manages the CLI process automatically)
- GitHub Copilot subscription (or BYOK)

**Key APIs:**

```csharp
using GitHub.Copilot.SDK;

// Create and start client
await using var client = new CopilotClient();
await client.StartAsync();

// Create a fresh session (Ralph Wiggum pattern!)
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5",
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = pchAgentPromptContent // Load PCH agent prompt
    },
    OnUserInputRequest = async (request, invocation) =>
    {
        // CRITICAL: Intercept agent questions for autonomous answering
        return new UserInputResponse
        {
            Answer = await AnswerAutonomously(request, visionDoc),
            WasFreeform = true
        };
    }
});

// Send prompt with document attachments
await session.SendAsync(new MessageOptions
{
    Prompt = "Continue implementing the plan at Phase 3",
    Attachments = new List<UserMessageDataAttachmentsItem>
    {
        new UserMessageDataAttachmentsItem
        {
            Type = UserMessageDataAttachmentsItemType.File,
            Path = "/path/to/plan.md"
        }
    }
});

// Wait for completion via events
var done = new TaskCompletionSource();
session.On(evt =>
{
    if (evt is SessionIdleEvent)
        done.SetResult();
});
await done.Task;
```

**SDK Feature Mapping to Vision Requirements:**

| Vision Requirement | SDK Feature | API |
|-------------------|-------------|-----|
| FR-1: Invoke phases headlessly | Session management | `CopilotClient`, `CreateSessionAsync()` |
| FR-3: Fresh context per phase | Session isolation | Each `CreateSessionAsync()` is fresh |
| FR-4: Autonomous question answering | User input interception | `OnUserInputRequest` callback |
| FR-2: Pass documents between phases | File attachments | `MessageOptions.Attachments` |
| NFR-1: .NET Core CLI | Native .NET SDK | NuGet package |

---

#### 2. SDK Architecture

The SDK wraps the Copilot CLI automatically:

```
Your .NET Application
       ↓
GitHub.Copilot.SDK (NuGet)
       ↓ JSON-RPC
Copilot CLI (managed by SDK)
       ↓
LLM + Tools
```

**Key Architectural Points:**
- SDK spawns and manages Copilot CLI as a child process
- Communication via JSON-RPC over stdio
- Automatic restart on crash (`AutoRestart = true` by default)
- Session state persisted to `~/.copilot/session-state/{sessionId}/`

---

#### 3. Custom Agent Loading via SystemMessage

PCH agent prompts can be loaded via the `SystemMessage` configuration:

```csharp
var agentPrompt = await File.ReadAllTextAsync("agents/pch-coder.md");

var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "claude-sonnet-4.5",
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append, // Add to default system prompt
        Content = agentPrompt
    }
});
```

**Modes:**
- `Append` — Add PCH agent instructions to default Copilot system prompt (recommended)
- `Replace` — Full override (removes guardrails; use with caution)

---

#### 4. Session Hooks for Advanced Control

The SDK provides hooks for intercepting the agent lifecycle:

```csharp
var session = await client.CreateSessionAsync(new SessionConfig
{
    Hooks = new SessionHooks
    {
        OnPreToolUse = async (input, invocation) =>
        {
            // Intercept tool calls before execution
            return new PreToolUseHookOutput
            {
                PermissionDecision = "allow"
            };
        },
        OnPostToolUse = async (input, invocation) =>
        {
            // Log or modify tool results
            return new PostToolUseHookOutput();
        },
        OnSessionEnd = async (input, invocation) =>
        {
            // Cleanup or logging
            return null;
        }
    }
});
```

---

#### 5. SDK Status and Stability

| Aspect | Status |
|--------|--------|
| **Preview Status** | Technical Preview — may have breaking changes |
| **NuGet Package** | `GitHub.Copilot.SDK` |
| **Version** | v0.1.21 (as of Feb 2026) |
| **Repository** | [github/copilot-sdk](https://github.com/github/copilot-sdk) |
| **License** | MIT |

**Recommendations:**
- Design abstraction layer around SDK to isolate breaking changes
- Monitor [GitHub Issues](https://github.com/github/copilot-sdk/issues) for known issues
- Consider BYOK for production if GitHub auth is a concern

---

#### Original Research Below (Partially Obsolete)

*The following research on Copilot CLI is still relevant as background, but the SDK approach above is preferred.*

---

#### 6. GitHub Copilot CLI: Underlying Technology

The **GitHub Copilot CLI** is what the SDK wraps. Understanding it helps debug issues:

**Key Capabilities:**

| Capability | CLI Support | Notes |
|------------|-------------|-------|
| **Custom agent invocation** | ✅ Yes | `copilot --agent=agent-name --prompt "..."` |
| **Non-interactive prompting** | ✅ Yes | `--prompt` flag bypasses interactive mode |
| **PCH agent loading** | ✅ Yes | Loads from `.github/agents/`, `~/.copilot/agents/`, org-level `/agents/` |
| **MCP server integration** | ✅ Yes | `--mcp-server` flag or `/mcp add` |
| **Session persistence** | ✅ Yes | `--resume`, `--continue` flags |
| **Trusted directories** | ✅ Yes | `--allow-all-paths` for automation |
| **Permission bypass** | ✅ Yes | `--allow-all` or `--yolo` for full automation |
| **File attachment** | ✅ Yes | `@path/to/file` syntax in prompts |

**Example Invocation for Automation:**
```bash
copilot --agent=pch-researcher \
        --prompt "Continue research at @docs/research/001-example.md — start Phase 1" \
        --allow-all \
        --allow-all-paths
```

**Session Resumption:**
```bash
copilot --resume  # Resume previous session
copilot --continue  # Continue most recent local session
```

---

#### 3. Custom Agent File Format (Already Compatible)

The PCH agents in `/agents/` already use the correct format for Copilot CLI:

**YAML Frontmatter Structure:**
```yaml
---
name: agent-name
description: Agent purpose description
model: Claude Opus 4.5
tools: ["read", "edit", "search", "execute"]  # Optional
handoffs:
  - label: Button Label
    agent: target-agent
    prompt: Handoff context
    send: true/false
mcp-servers:  # For org-level agents
  - config: |
      {...}
---

# Agent Instructions
Markdown content with agent behavior...
```

**Agent Loading Hierarchy:**
1. System-level agents (built-in)
2. User-level: `~/.copilot/agents/`
3. Repository-level: `.github/agents/`
4. Organization-level: `/agents/` in `.github-private` repo

**Current PCH agents are fully compatible** — they can be loaded directly by Copilot CLI.

---

#### 4. Tool Configuration for Agents

Agents can specify which tools they have access to:

| Tool Alias | Actual Tools | Purpose |
|------------|--------------|---------|
| `execute` | shell, bash, powershell | Run terminal commands |
| `read` | Read, NotebookRead | Read file contents |
| `edit` | Edit, MultiEdit, Write | Modify files |
| `search` | Grep, Glob | Search codebase |
| `agent` | custom-agent, Task | Invoke other agents |
| `web` | WebSearch, WebFetch | External URL access |

**No tools specified = all tools enabled.** Use `tools: []` to disable all tools.

---

#### 5. Copilot Extensions (Server-Based Alternative)

For completeness, **Copilot Extensions** provide an alternative architecture:

**Architecture:** Server-side webhook that receives requests from GitHub.com Copilot Chat

**SDK:** `@copilot-extensions/preview-sdk` (JavaScript only)

**Key Methods:**
- `verifyRequest()` — Verify GitHub signatures
- `parseRequestBody()` — Parse incoming payload
- `createTextEvent()`, `createAckEvent()`, `createDoneEvent()` — Response streaming
- `prompt()` — Send prompts to Copilot models

**Challenges for Self-Operating Loop:**
- Requires running a web server
- User interaction required via GitHub.com chat
- Not designed for local autonomous operation
- No .NET SDK available

**Verdict:** Not suitable for the headless CLI automation envisioned.

---

#### 6. VS Code Language Model API

The **VS Code Language Model API** allows extensions to use Copilot models:

**Code Example:**
```typescript
const models = await vscode.lm.selectChatModels({ vendor: 'copilot' });
const [model] = models;
const messages = [
  vscode.LanguageModelChatMessage.User('Your prompt here')
];
const response = await model.sendRequest(messages, {}, token);
```

**Limitations:**
- Requires running inside VS Code extension host
- User consent required for model access
- Rate limiting applies
- Not designed for headless automation

**Verdict:** Could work as a fallback if VS Code can be scripted, but not ideal.

---

#### 7. MCP (Model Context Protocol) Integration

MCP provides a standardized protocol for connecting AI to external tools:

**Key Concepts:**
- **MCP Servers**: Provide tools, prompts, and resources to AI
- **Toolsets**: Groups of related capabilities (e.g., GitHub MCP server toolsets)
- **Remote/Local**: Can run locally or connect to remote servers

**GitHub MCP Server Toolsets:**
- `repos`, `issues`, `pull_requests`, `code_search`
- `copilot_spaces` (used by PCH agents for standards)

**Integration with Copilot CLI:**
```bash
# Configure in ~/.copilot/mcp-config.json
{
  "servers": {
    "github": {
      "type": "copilot",
      "toolsets": ["repos", "issues", "copilot_spaces"]
    }
  }
}
```

---

#### 8. API Maturity Assessment

| Component | Status | Stability | Risk Level |
|-----------|--------|-----------|------------|
| Copilot CLI | Public Preview | Moderate — active development | Medium |
| Custom Agents | GA for VS Code, Preview for CLI | Good | Low |
| MCP Protocol | GA | Stable | Low |
| Copilot Extensions SDK | Preview | Active development | Medium |
| VS Code LM API | GA | Stable | Low |

**Key Risk:** The Copilot CLI is in public preview and subject to change. Design should include abstraction layers to accommodate API evolution.

---

#### 9. Implications for .NET CLI Architecture

Given the absence of a .NET Copilot SDK, the recommended approach shifts:

**Option A: Process Wrapper (Recommended)**
- .NET CLI wraps Copilot CLI as a subprocess
- Capture stdout/stderr for response parsing
- Pass prompts via `--prompt` argument
- Use `--agent` to specify PCH agent

**Option B: API Integration**
- Use GitHub REST API for some operations
- Potentially call Copilot models via Azure OpenAI Service (requires separate setup)
- More complex but more control

**Option C: VS Code Automation**
- Script VS Code via command-line or extension
- Limited headless capability
- Not recommended for autonomous operation

### Key Discoveries

- **No .NET Copilot SDK exists** — The vision's assumption about a "GitHub Copilot SDK for .NET" is incorrect. Must pivot to alternative approaches.
- **Copilot CLI is the primary integration path** — Supports custom agents, non-interactive prompts, and automation flags.
- **PCH agents are already compatible** — The existing `.agent.md` format works with Copilot CLI out of the box.
- **Process wrapping is the recommended .NET approach** — The CLI orchestrator should invoke Copilot CLI as a subprocess.
- **MCP enhances capabilities** — Can extend agent capabilities through MCP server integration.
- **Extension SDK is JavaScript-only** — `@copilot-extensions/preview-sdk` doesn't support .NET.

### Relevant Files/Resources

**Internal:**
- [agents/pch-researcher.md](agents/pch-researcher.md) — Example of compatible agent format
- [agents/pch-coder.md](agents/pch-coder.md) — Shows handoff and tool configuration
- [.github/copilot-instructions.md](.github/copilot-instructions.md) — Repository instructions format

**External Documentation:**
- [GitHub Copilot CLI Documentation](https://docs.github.com/en/copilot/how-tos/copilot-cli/use-copilot-cli)
- [Custom Agents Configuration](https://docs.github.com/en/copilot/reference/custom-agents-configuration)
- [Creating Custom Agents](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/create-custom-agents)
- [Model Context Protocol (MCP)](https://docs.github.com/en/copilot/concepts/context/mcp)
- [VS Code Language Model API](https://code.visualstudio.com/api/extension-guides/language-model)
- [Copilot Extensions Preview SDK (JS)](https://github.com/copilot-extensions/preview-sdk.js)

### Gaps & Limitations

- **No direct .NET integration** — Must use process invocation; no native SDK
- **Copilot CLI output parsing** — Need to determine how to reliably parse responses and detect completion (deferred to Phase 4)
- **Session state management** — CLI session persistence mechanics need deeper investigation (Phase 3)
- **Question interception** — How to detect when agent asks a question vs provides output (Phase 4)

### Assumptions

- **Copilot CLI remains available** — Assuming the public preview continues toward GA
- **Custom agent format stability** — Assuming YAML frontmatter format is stable
- **Subprocess communication is viable** — Assuming Copilot CLI outputs can be reliably captured

---

## Phase 2: Headless Agent Invocation Feasibility

**Status:** ✅ Complete
**Scope:**
- Determine if PCH agents can be invoked outside VS Code
- Research Copilot CLI capabilities and JSON-RPC interface
- Evaluate alternative approaches if headless invocation isn't directly supported
- Assess impact on architecture if headless mode is infeasible

### Research Tasks

- [x] Investigate Copilot CLI for headless operations
- [x] Research VS Code extension host APIs for programmatic access
- [x] Explore Language Server Protocol (LSP) integration options
- [x] Check for REST/gRPC APIs for Copilot communication
- [x] Evaluate GitHub Copilot Workspace API (if applicable)
- [x] Document any authentication/authorization requirements

### Findings

#### 1. Critical Discovery: Headless Invocation IS Fully Supported

**YES — PCH agents can be invoked headlessly via Copilot CLI.** This is the most important finding for the self-operating development loop architecture.

GitHub Copilot CLI supports **three distinct modes of operation:**

| Mode | Entry Point | Use Case | Headless Compatible |
|------|-------------|----------|---------------------|
| **Interactive Mode** | `copilot` | Human-in-the-loop chat | ❌ No |
| **Plan Mode** | `Shift+Tab` in interactive | Implementation planning | ❌ No |
| **Programmatic Mode** | `copilot --prompt "..."` | Scripted automation | ✅ **YES** |

---

#### 2. Programmatic Mode: Full Headless Capability

The `--prompt` (or `-p`) flag enables non-interactive execution that exits after completion:

```bash
# Basic headless invocation
copilot --prompt "Your task here" --allow-all-tools

# With custom agent (PCH agents)
copilot --agent=pch-researcher \
        --prompt "Continue research at @docs/research/001-example.md — start Phase 1" \
        --allow-all \
        --silent
```

**Key Command-Line Flags for Automation:**

| Flag | Purpose | Automation Relevance |
|------|---------|---------------------|
| `-p PROMPT`, `--prompt PROMPT` | Execute prompt non-interactively, exits after completion | **Critical** — Primary headless entry point |
| `--agent AGENT` | Specify custom agent (e.g., `pch-coder`) | **Critical** — Enables PCH agent invocation |
| `--allow-all`, `--yolo` | Enable all permissions (tools + paths + URLs) | **Critical** — Full automation without prompts |
| `--allow-all-tools` | Allow all tools without confirmation | Required for tool usage |
| `--allow-all-paths` | Disable file path verification | Required for file operations |
| `--allow-all-urls` | Allow all URL access | Required for web fetching |
| `-s`, `--silent` | Output only agent response (no stats) | **Critical** — Scripting-friendly output |
| `--no-ask-user` | Disable ask_user tool (fully autonomous) | **Critical** — Prevents question blocking |
| `--resume [SESSION-ID]` | Resume previous session | Enables session continuity |
| `--continue` | Resume most recent local session | Quick session resume |
| `--model MODEL` | Select AI model | Model control |
| `--share [PATH]` | Save session to markdown file | Session logging |

---

#### 3. Major Discovery: ACP (Agent Client Protocol) Server

**The most significant architectural discovery:** Copilot CLI can run as an **ACP server** providing structured JSON-RPC communication — far superior to text parsing.

**Starting the ACP Server:**

```bash
# stdio mode (recommended for subprocess communication)
copilot --acp --stdio

# TCP mode (for network communication)
copilot --acp --port 3000
```

**ACP Protocol Characteristics:**

| Aspect | Detail |
|--------|--------|
| **Protocol** | JSON-RPC 2.0 |
| **Transport** | stdio (stdin/stdout) or TCP |
| **Format** | NDJSON (Newline Delimited JSON) |
| **Session Model** | `session/new` → `session/prompt` → `session/update` |
| **Tool Handling** | Structured permissions via `session/request_permission` |

**ACP Message Flow:**

```
1. Client → Agent: `initialize` (version & capability negotiation)
2. Client → Agent: `authenticate` (if required)
3. Client → Agent: `session/new` (create session with cwd, MCP servers)
4. Client → Agent: `session/prompt` (send prompts)
   Agent → Client: `session/update` (streaming notifications for progress)
   Agent → Client: Tool calls, file operations, permission requests
5. Client → Agent: `session/cancel` (optional interruption)
```

**TypeScript SDK Example (ACP Integration):**

```typescript
import * as acp from "@agentclientprotocol/sdk";
import { spawn } from "node:child_process";

async function main() {
  const copilotProcess = spawn("copilot", ["--acp", "--stdio"], {
    stdio: ["pipe", "pipe", "inherit"],
  });

  const output = Writable.toWeb(copilotProcess.stdin);
  const input = Readable.toWeb(copilotProcess.stdout);
  const stream = acp.ndJsonStream(output, input);

  const client = {
    async requestPermission(params) {
      // Auto-approve or implement custom logic
      return { outcome: { outcome: "approved" } };
    },
    async sessionUpdate(params) {
      // Handle streaming updates (agent messages, tool calls, etc.)
      if (params.update.sessionUpdate === "agent_message_chunk") {
        process.stdout.write(params.update.content.text);
      }
    },
  };

  const connection = new acp.ClientSideConnection((_agent) => client, stream);
  await connection.initialize({ protocolVersion: acp.PROTOCOL_VERSION, clientCapabilities: {} });

  const session = await connection.newSession({
    cwd: process.cwd(),
    mcpServers: [],
  });

  const result = await connection.prompt({
    sessionId: session.sessionId,
    prompt: [{ type: "text", text: "Your prompt here" }],
  });

  console.log("Stop reason:", result.stopReason);
}
```

**ACP Implications for .NET Architecture:**

| Approach | Complexity | Reliability | Recommendation |
|----------|------------|-------------|----------------|
| **Text Parsing** (--prompt + --silent) | Low | Medium | MVP — simple but brittle |
| **ACP over stdio** | Medium | High | **Recommended** — structured communication |
| **ACP over TCP** | Medium | High | Alternative for distributed scenarios |

---

#### 4. Authentication for Headless Operation

**No OAuth flow required for automation.** Authentication can be fully automated:

**Method 1: Personal Access Token (Recommended for Automation)**

```bash
# Create fine-grained PAT with "Copilot Requests" permission
# Set environment variable
export GH_TOKEN="ghp_your_token_here"

# Or
export GITHUB_TOKEN="ghp_your_token_here"
```

**Token Requirements:**
- Fine-grained Personal Access Token (PAT)
- Permission: "Copilot Requests" enabled
- No OAuth browser flow needed
- Token precedence: `GH_TOKEN` > `GITHUB_TOKEN`

**Method 2: GitHub CLI Auth (For Interactive Setup)**

```bash
# One-time setup (interactive)
gh auth login

# Copilot CLI inherits GitHub CLI authentication
copilot --prompt "..." --allow-all
```

---

#### 5. VS Code Extension Host APIs — Not Viable for Headless

The VS Code Language Model API (`vscode.lm`) is **not suitable** for headless automation:

| Limitation | Impact |
|------------|--------|
| Requires VS Code runtime | Cannot run as standalone process |
| User consent required | Blocks automation |
| Rate limiting | Unpredictable throughput |
| Extension context required | Complex setup |

**Verdict:** VS Code APIs are designed for interactive use within the editor, not headless automation.

---

#### 6. LSP Integration — Not Applicable

Language Server Protocol (LSP) is for language intelligence (completions, diagnostics), not agent orchestration. Copilot uses a proprietary protocol on top of LSP for code suggestions, but:

- No public LSP interface for agent invocation
- Agent mode is separate from LSP completions
- No documented way to invoke custom agents via LSP

**Verdict:** LSP is not a viable path for PCH agent invocation.

---

#### 7. REST/gRPC APIs — No Direct API

GitHub does not expose a public REST or gRPC API for Copilot agent invocation:

| API Type | Availability | Notes |
|----------|--------------|-------|
| REST API for Copilot | ❌ None | No public endpoints for agent chat |
| gRPC API | ❌ None | Internal only |
| GraphQL | ❌ None | Copilot not exposed in GraphQL |

**The Copilot CLI (via --prompt or ACP) is the only supported programmatic interface.**

---

#### 8. GitHub Copilot Workspace — Separate Product

GitHub Copilot Workspace is a **cloud-based** environment for multi-file development, **not an API**:

- Accessed via github.com, not programmatically
- Has a "delegate" feature from CLI (`/delegate` or `&` prefix)
- Can hand off work to cloud-based Copilot coding agent
- Creates draft PRs automatically
- Not suitable for local autonomous orchestration

**Delegation Example:**
```bash
# Delegate to cloud agent (creates PR)
copilot --prompt "& complete the API integration tests"
```

**Verdict:** Copilot Workspace is complementary but not a replacement for local headless invocation. The `--prompt` and ACP approaches are the correct paths.

---

#### 9. Impact Assessment: Architecture Viability

**✅ HEADLESS MODE IS FULLY FEASIBLE**

The self-operating development loop architecture is viable with the following approach:

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET Core CLI Orchestrator                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌───────────────────────────────────────────────────────┐    │
│   │              Recommended: ACP over stdio               │    │
│   │                                                        │    │
│   │  .NET Process.Start("copilot", "--acp", "--stdio")    │    │
│   │                          │                             │    │
│   │  ┌────────────────────────┼────────────────────────┐  │    │
│   │  │     JSON-RPC 2.0 over stdin/stdout              │  │    │
│   │  │  • session/new (create session)                 │  │    │
│   │  │  • session/prompt (send prompts)                │  │    │
│   │  │  • session/update (receive streaming)           │  │    │
│   │  │  • session/request_permission (handle tools)    │  │    │
│   │  └────────────────────────┼────────────────────────┘  │    │
│   │                          │                             │    │
│   │                     Copilot CLI                        │    │
│   │                    (ACP Server)                        │    │
│   └───────────────────────────────────────────────────────┘    │
│                                                                  │
│   Alternative (MVP): Text-based subprocess                       │
│   copilot --agent=pch-coder --prompt "..." --allow-all -s        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Recommended Architecture Path:**

| Phase | Approach | Rationale |
|-------|----------|-----------|
| MVP | `--prompt` with text parsing | Fast implementation, validate concept |
| Production | ACP over stdio | Structured communication, reliable parsing |

### Key Discoveries

- **Headless invocation is fully supported** via Copilot CLI's programmatic mode (`--prompt` flag)
- **ACP server mode** (`--acp`) provides JSON-RPC 2.0 interface for structured communication — superior to text parsing
- **Custom agents work headlessly** — `--agent=pch-researcher` with `--prompt` invokes PCH agents without VS Code
- **Autonomous operation is possible** — `--no-ask-user` flag disables question-asking, enabling autonomous mode
- **Token-based auth** — Fine-grained PAT with "Copilot Requests" permission enables fully automated authentication
- **Session continuity supported** — `--resume` and `--continue` flags enable multi-turn conversations across invocations
- **No REST/gRPC/LSP alternatives** — Copilot CLI is the only supported programmatic interface
- **VS Code extension host not viable** — Requires running VS Code, not suitable for headless automation

### Relevant Files/Resources

**Internal:**
- [agents/pch-researcher.md](agents/pch-researcher.md) — Compatible with `--agent` flag
- [agents/pch-coder.md](agents/pch-coder.md) — Compatible with `--agent` flag
- Phase 1 findings in this document — Copilot CLI as primary integration path

**External Documentation:**
- [GitHub Copilot CLI Command Reference](https://docs.github.com/en/copilot/reference/cli-command-reference)
- [About GitHub Copilot CLI](https://docs.github.com/en/copilot/concepts/agents/about-copilot-cli)
- [Using GitHub Copilot CLI](https://docs.github.com/en/copilot/how-tos/copilot-cli/use-copilot-cli)
- [Copilot CLI ACP Server](https://docs.github.com/en/copilot/reference/acp-server)
- [Installing Copilot CLI](https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli)
- [Agent Client Protocol Overview](https://agentclientprotocol.com/protocol/overview)
- [ACP Introduction](https://agentclientprotocol.com/get-started/introduction)

### Gaps & Limitations

- **No .NET ACP SDK** — TypeScript SDK exists (`@agentclientprotocol/sdk`), but no .NET version. Must implement JSON-RPC manually or port the TypeScript implementation.
- **ACP is preview** — Subject to change; abstraction layer recommended
- **Output parsing complexity** — For text-based `--prompt` mode, need to develop robust response parsing (addressed in Phase 4)
- **Session state persistence** — How session IDs are managed across CLI invocations needs deeper investigation (Phase 3)

### Assumptions

- **Copilot CLI continues availability** — Assuming public preview progresses to GA
- **ACP stability** — Assuming ACP protocol remains backward-compatible
- **Token permissions stable** — Assuming "Copilot Requests" PAT permission remains the correct scope

---

## Phase 3: Session Management & Fresh Context

**Status:** ✅ Complete
**Scope:**
- Research how to spawn new agent sessions programmatically
- Understand the "Ralph Wiggum pattern" — fresh context per phase
- Investigate context window management across sessions
- Determine session lifecycle and cleanup requirements

### Research Tasks

- [x] Research SDK session creation and management APIs
- [x] Understand how context accumulates within a session
- [x] Document methods to achieve fresh (empty) context
- [x] Investigate session isolation guarantees
- [x] Research conversation history management
- [x] Determine how to pass initial context without polluting the window

### Findings

#### 1. Critical Discovery: ACP `session/new` Provides Guaranteed Fresh Context

The ACP protocol's `session/new` method is the **primary mechanism for achieving fresh context** — exactly what the "Ralph Wiggum pattern" requires.

**Key Protocol Behavior:**

| Aspect | Behavior | Implication for PCH |
|--------|----------|---------------------|
| **Session Creation** | Each `session/new` creates a completely new conversation context | ✅ Fresh context per phase guaranteed |
| **Session ID** | Returns unique `sessionId` (e.g., `sess_abc123def456`) | Can track sessions for logging |
| **Context Isolation** | Sessions maintain their own context, history, and state | ✅ No pollution between phases |
| **Session Independence** | "Multiple independent interactions with the same Agent" | ✅ Phases don't inherit prior context |

**ACP `session/new` Request:**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "session/new",
  "params": {
    "cwd": "/path/to/workspace",
    "mcpServers": [
      {
        "name": "github",
        "command": "/path/to/mcp-server",
        "args": ["--stdio"],
        "env": []
      }
    ]
  }
}
```

**Response:**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "sessionId": "sess_abc123def456"
  }
}
```

**Critical Insight:** Each `session/new` call creates a session with ZERO conversation history. The agent starts completely fresh, with no knowledge of previous prompts or responses.

---

#### 2. The "Ralph Wiggum Pattern" Explained

The vision document references "Ralph Wiggum pattern" — this is a PCH-specific term for the session management approach used by PCH agents.

**Pattern Definition:**

> **Ralph Wiggum Pattern:** Spawn a completely fresh agent session for each phase of work, passing only the necessary document context (vision/research/plan) without carrying forward conversation history.

**Why This Pattern?**

| Challenge | Solution |
|-----------|----------|
| Context windows have token limits | Fresh session = full capacity per phase |
| Accumulated history reduces available tokens | No history = maximum working space |
| LLM performance degrades with bloated context | Clean slate = optimal performance |
| Phase-specific focus needed | Fresh context enables focused work |

**Implementation in Self-Operating Loop:**

```
┌─────────────────────────────────────────────────────────────────┐
│                    Phase Execution Pattern                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Phase 1: Vision                                                 │
│  ┌────────────────────────────────────────────────────────┐     │
│  │  session/new → Fresh context                           │     │
│  │  session/prompt (feature request + system prompt)      │     │
│  │  ... agent work ...                                    │     │
│  │  session ends → vision doc created                     │     │
│  └────────────────────────────────────────────────────────┘     │
│                          ↓                                       │
│  Phase 2: Research                                               │
│  ┌────────────────────────────────────────────────────────┐     │
│  │  session/new → FRESH context (no Phase 1 history!)     │     │
│  │  session/prompt (vision doc + research instruction)    │     │
│  │  ... agent work ...                                    │     │
│  │  session ends → research doc created                   │     │
│  └────────────────────────────────────────────────────────┘     │
│                          ↓                                       │
│  Phase 3: Planning  (and so on...)                               │
│  ┌────────────────────────────────────────────────────────┐     │
│  │  session/new → FRESH context (no Phase 1-2 history!)   │     │
│  │  session/prompt (vision + research + plan instruction) │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

#### 3. Session Lifecycle Management

**Session States:**

```
┌──────────┐     session/new      ┌──────────┐
│   NONE   │ ─────────────────────▶│  ACTIVE  │
└──────────┘                       └──────────┘
                                        │
                    ┌───────────────────┼───────────────────┐
                    │                   │                   │
                    ▼                   ▼                   ▼
            session/prompt      session/cancel      Process exit
                    │                   │                   │
                    ▼                   ▼                   ▼
             ┌──────────┐       ┌──────────┐       ┌──────────┐
             │PROCESSING│       │CANCELLED │       │ CLEANUP  │
             └──────────┘       └──────────┘       └──────────┘
                    │
                    ▼ (stopReason returned)
             ┌──────────┐
             │  READY   │ ← Can accept more prompts
             └──────────┘
```

**Stop Reasons:**

| StopReason | Meaning | PCH Handling |
|------------|---------|--------------|
| `end_turn` | Agent completed successfully | Phase complete, proceed to next |
| `max_tokens` | Token limit reached | May need to compact or retry |
| `max_turn_requests` | Too many LLM calls | Phase may be too large |
| `refusal` | Agent refused to continue | Log and investigate |
| `cancelled` | Client cancelled | User intervention |

---

#### 4. Methods to Achieve Fresh Context

There are **three approaches** to achieve fresh context, depending on the integration method:

**Approach A: ACP `session/new` (Recommended)**

```python
# .NET pseudo-code for ACP approach
async def execute_phase(agent_name, prompt, documents):
    # Create fresh session
    session = await acp_client.new_session(
        cwd=workspace_path,
        mcp_servers=mcp_config
    )
    
    # Build prompt with context
    content_blocks = [
        {"type": "text", "text": prompt}
    ]
    
    # Add documents as embedded resources
    for doc in documents:
        content_blocks.append({
            "type": "resource",
            "resource": {
                "uri": f"file://{doc.path}",
                "mimeType": "text/markdown",
                "text": doc.content
            }
        })
    
    # Execute (fresh context guaranteed by new session)
    result = await acp_client.prompt(
        session_id=session.session_id,
        prompt=content_blocks
    )
    
    return result
```

**Approach B: CLI `--prompt` Mode (Simpler, Text-Based)**

```bash
# Each invocation is a fresh session
copilot --agent=pch-researcher \
        --prompt "Research the authentication patterns in this codebase. \
                  Vision document: @docs/vision/001-auth.md" \
        --allow-all \
        --silent
```

Key behavior: **`--prompt` mode exits after completion** — this means each invocation naturally creates a fresh session.

**Approach C: CLI with Explicit Session Management**

```bash
# DO NOT use --resume or --continue for Ralph Wiggum pattern
# These PRESERVE context, which is the opposite of what we want

# CORRECT: Fresh session per phase
copilot --prompt "Phase 1 work..." --allow-all -s
copilot --prompt "Phase 2 work..." --allow-all -s  # Fresh!
copilot --prompt "Phase 3 work..." --allow-all -s  # Fresh!

# WRONG: Continued session (polluted context)
copilot --prompt "Phase 1 work..." --allow-all -s
copilot --continue  # Carries Phase 1 history forward!
```

---

#### 5. Session Isolation Guarantees

**What IS isolated between sessions:**

| Aspect | Isolated? | Notes |
|--------|-----------|-------|
| Conversation history | ✅ Yes | Each session starts empty |
| Token usage | ✅ Yes | Full context window available |
| Agent internal state | ✅ Yes | No memory of previous sessions |
| Tool call history | ✅ Yes | Tools don't carry over |
| Permission grants | ❌ No | CLI flags persist (--allow-all) |
| File system state | ❌ No | Files written by Phase 1 visible to Phase 2 |
| MCP server state | ⚠️ Depends | New connections per session, but server state may persist |

**Critical Insight:** Sessions provide **LLM context isolation**, not filesystem or MCP isolation. This is correct for PCH workflow — phases communicate via **documents on disk**, not via shared agent memory.

---

#### 6. Context Window Management Within a Session

**Automatic Context Management:**

- Copilot CLI automatically compresses conversation history when approaching 95% of token limit
- `/compact` slash command forces manual compression
- `/context` shows current token usage

**Context Budget Considerations:**

| Component | Typical Token Usage | Notes |
|-----------|---------------------|-------|
| System prompt (agent) | 2,000 - 5,000 | PCH agent instructions |
| Custom instructions | 500 - 2,000 | `.github/copilot-instructions.md` |
| User prompt | Variable | Feature request + documents |
| Tool outputs | Variable | Search results, file contents |
| Conversation history | Accumulates | Grows with each turn |

**For PCH Phases:**

Since each phase uses a fresh session, context budget is:
- Full capacity at start of each phase
- No accumulated history from previous phases
- Documents must be re-attached (they provide continuity, not history)

---

#### 7. Passing Documents Without Polluting Context

**Challenge:** How to give an agent phase the context it needs (vision doc, research doc, plan) without wasting context on irrelevant history?

**Solution: Embed as Resources in Initial Prompt**

ACP supports `ContentBlock::Resource` for embedding document content:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "session/prompt",
  "params": {
    "sessionId": "sess_xyz789",
    "prompt": [
      {
        "type": "text",
        "text": "Continue implementing the plan. Start with Phase 3."
      },
      {
        "type": "resource",
        "resource": {
          "uri": "file:///docs/plans/001-feature.md",
          "mimeType": "text/markdown",
          "text": "# Implementation Plan\n\n## Phase 3: API Layer\n..."
        }
      }
    ]
  }
}
```

**CLI Alternative: `@` File Reference**

```bash
copilot --prompt "Implement Phase 3 of @docs/plans/001-feature.md" --allow-all
```

**Token Efficiency:**

| Approach | Token Overhead | Recommended |
|----------|----------------|-------------|
| Full document embedded | Document size | ✅ For essential docs |
| File reference (@path) | Minimal (agent reads as needed) | ✅ For large docs |
| Previous conversation | Cumulative (wasteful) | ❌ Avoid |

---

#### 8. Session Cleanup and Resource Management

**ACP Cleanup Pattern:**

```typescript
// After phase completes
copilotProcess.stdin.end();         // Signal end of input
copilotProcess.kill("SIGTERM");     // Request graceful shutdown
await waitForExit(copilotProcess);  // Wait for cleanup
```

**Resources to Clean Up:**

| Resource | Cleanup Method | Notes |
|----------|----------------|-------|
| Copilot process | `SIGTERM` → wait for exit | Graceful shutdown |
| Session state | Automatic on process exit | ACP handles internally |
| MCP connections | Automatic | Closed with session |
| Terminal instances | `terminal/release` | If terminals were created |
| File locks | Automatic | Released on process exit |

**Recommended Cleanup Sequence:**

```
1. End stdin (signals no more input)
2. Wait for final session/update notifications
3. Check stopReason in session/prompt response
4. Kill process with SIGTERM
5. Wait for exit (with timeout)
6. Force kill if necessary (SIGKILL)
```

---

#### 9. Implementation Pattern for Self-Operating Loop

**Recommended Session Pattern for .NET Orchestrator:**

```csharp
public class PhaseExecutor
{
    private readonly AcpClient _acp;
    
    public async Task<PhaseResult> ExecutePhaseAsync(
        string agentName,
        PhaseContext context,
        CancellationToken ct)
    {
        // 1. Start fresh Copilot process with ACP
        await using var copilot = await StartCopilotProcess();
        
        // 2. Create fresh session (Ralph Wiggum pattern)
        var session = await _acp.NewSessionAsync(
            cwd: context.WorkspacePath,
            mcpServers: context.McpConfig
        );
        
        // 3. Build prompt with embedded documents
        var prompt = BuildPromptWithDocuments(context);
        
        // 4. Execute phase
        var result = await _acp.PromptAsync(
            sessionId: session.SessionId,
            prompt: prompt,
            cancellationToken: ct
        );
        
        // 5. Process result
        return new PhaseResult
        {
            Success = result.StopReason == "end_turn",
            StopReason = result.StopReason,
            OutputDocument = ExtractOutputDocument(result)
        };
        
        // 6. Cleanup happens in using block
    }
}
```

---

#### 10. Comparison: Fresh Session vs. Session Continuity

| Aspect | Fresh Session (Ralph Wiggum) | Continued Session |
|--------|------------------------------|-------------------|
| Context capacity | 100% available | Decreases with each turn |
| Phase isolation | ✅ Complete | ❌ Phases can interfere |
| Document passing | Explicit (embedded in prompt) | Implicit (in history) |
| Error isolation | ✅ Phase failure doesn't corrupt next | ❌ Errors may carry forward |
| Performance | ✅ Optimal LLM performance | ⚠️ Degrades over time |
| Process overhead | Higher (new process per phase) | Lower (single process) |
| **Recommended for PCH** | ✅ YES | ❌ NO |

### Key Discoveries

- **`session/new` guarantees fresh context** — Each ACP `session/new` call creates a completely isolated session with zero conversation history, exactly what the Ralph Wiggum pattern requires
- **CLI `--prompt` mode is inherently fresh** — Each `copilot --prompt` invocation exits after completion, naturally creating a fresh session per invocation
- **Session ID enables tracking** — The unique `sessionId` returned by `session/new` can be used for logging and correlation
- **DO NOT use `--resume` or `--continue`** — These flags preserve context across sessions, which defeats the fresh context pattern
- **Documents provide continuity without history** — Pass vision/research/plan docs as embedded resources or `@file` references to maintain phase-to-phase continuity without polluting the context window
- **Context isolation is LLM-level only** — Sessions don't isolate filesystem or MCP state; phases communicate via persisted documents (correct for PCH workflow)
- **Automatic context compression at 95%** — Copilot CLI compresses history when approaching limits, but fresh sessions avoid this entirely
- **Kill process for cleanup** — After phase completion, send `SIGTERM` and wait for graceful exit to clean up all resources

### Relevant Files/Resources

**Internal:**
- [agents/pch-researcher.md](agents/pch-researcher.md) — Shows phase-per-session pattern in Manual Mode
- [agents/pch-coder.md](agents/pch-coder.md) — Shows "execute one phase per session" pattern
- [docs/vision/002-self-operating-development-loop.md](docs/vision/002-self-operating-development-loop.md) — Defines Ralph Wiggum pattern requirement (FR-3)

**External Documentation:**
- [ACP Session Setup](https://agentclientprotocol.com/protocol/session-setup) — `session/new` and `session/load` documentation
- [ACP Prompt Turn](https://agentclientprotocol.com/protocol/prompt-turn) — Session lifecycle and stop reasons
- [ACP Schema](https://agentclientprotocol.com/protocol/schema) — `NewSessionRequest`, `SessionId`, `PromptRequest` types
- [Copilot CLI Command Reference](https://docs.github.com/en/copilot/reference/cli-command-reference) — `--prompt`, `--resume`, `--continue` flags
- [Copilot CLI ACP Server](https://docs.github.com/en/copilot/reference/acp-server) — Integration example

### Gaps & Limitations

- **No .NET ACP SDK** — Must implement JSON-RPC client manually or port TypeScript SDK
- **Session persistence unexplored** — `session/load` capability exists but wasn't deeply investigated (may be useful for recovery scenarios)
- **MCP state persistence** — How MCP servers handle state between sessions needs investigation for Phase 6

### Assumptions

- **Fresh process per phase is acceptable** — Performance overhead of starting new Copilot process per phase is acceptable for autonomous operation
- **Documents fit in context** — Vision/research/plan documents are small enough to embed without exceeding limits

---

## Phase 4: Completion Detection & Signal Parsing

**Status:** ✅ Complete
**Scope:**
- Identify signals that indicate an agent has completed a phase
- Research how to distinguish between agent asking a question vs completing
- Investigate structured output parsing for handoff detection
- Design completion validation approach

### Research Tasks

- [x] Analyze PCH agent output patterns for completion indicators
- [x] Research SDK response streaming and parsing capabilities
- [x] Identify explicit markers in agent outputs (e.g., "Phase X Complete")
- [x] Investigate how to detect question prompts requiring user input
- [x] Research interrupt/callback mechanisms in SDK
- [x] Design signal classification approach

### Findings

#### 1. Multi-Layer Signal Detection Architecture

Signal detection must operate at **two levels** depending on the integration approach:

| Level | Interface | Signal Type | Primary Use Case |
|-------|-----------|-------------|------------------|
| **Protocol Level** | ACP JSON-RPC | `stopReason` field in response | Definitive completion detection |
| **Content Level** | Agent text output | Text pattern matching | Intent classification (question vs completion) |

**Recommended Approach:** Use **ACP protocol-level** signals for completion detection, then **parse content** to classify the output type.

---

#### 2. ACP Protocol-Level Completion Signals

The ACP `session/prompt` response includes a `stopReason` field that provides **definitive completion information**:

**ACP Prompt Response Schema:**

```typescript
interface PromptResult {
  stopReason: StopReason;
  endState?: string;  // Optional end state description
}

type StopReason = 
  | "end_turn"           // Agent completed successfully
  | "max_tokens"         // Token limit reached
  | "max_turn_requests"  // Too many LLM calls in turn
  | "refusal"            // Agent refused to continue
  | "cancelled";         // Client cancelled the request
```

**Stop Reason Interpretation for Self-Operating Loop:**

| StopReason | Meaning | Orchestrator Action |
|------------|---------|---------------------|
| `end_turn` | Agent finished its turn successfully | Parse output to classify (completion vs question) |
| `max_tokens` | Context exhausted mid-generation | Retry with compacted context or smaller scope |
| `max_turn_requests` | Phase too complex (too many tool calls) | Break phase into smaller chunks |
| `refusal` | Agent declined the request | Log and escalate to human |
| `cancelled` | Client cancelled | Handle gracefully; may retry |

**Critical Insight:** `end_turn` indicates the agent **stopped generating** — it does NOT mean the phase is complete. The agent may have stopped to ask a question. **Content parsing is required** to determine the actual intent.

---

#### 3. ACP Streaming Signals (`session/update`)

During execution, ACP sends `session/update` notifications that can be monitored for real-time progress:

**Session Update Types:**

```typescript
type SessionUpdate = 
  | { sessionUpdate: "agent_message_chunk"; content: ContentBlock }
  | { sessionUpdate: "tool_request"; tool: string; params: any }
  | { sessionUpdate: "tool_result"; toolId: string; result: any }
  | { sessionUpdate: "done"; stopReason: StopReason };
```

**Streaming Detection Opportunities:**

| Update Type | Detection Opportunity |
|-------------|----------------------|
| `agent_message_chunk` | Accumulate for pattern matching; detect headers like "✅ **Phase" early |
| `tool_request` | Monitor for tools like `ask_user` (question indicator) |
| `done` | Final completion signal |

**Early Termination Detection:**

If the orchestrator detects an `ask_user` tool request, it knows the agent is asking a question **before** the turn ends. This enables:
1. **Pre-emptive classification** — Know it's a question before full output
2. **Autonomous answering** — Intercept and auto-respond (Phase 5 topic)

---

#### 4. PCH Agent Output Pattern Analysis

Comprehensive analysis of PCH agent files reveals **four distinct output categories** with clear textual markers:

##### Category A: Phase Completion (Manual Mode)

**Pattern:** Agent successfully completed a phase and requests new session

**Markers (pch-researcher):**
```
✅ **Phase [N] Complete**

**Phase:** [Phase Name]
**Key Findings:**
...

**Progress:** [N] of [Total] phases complete

---

🔄 **Ready for Phase [N+1]:** [Phase Name]

🚨 **IMPORTANT:** I will NOT start Phase [N+1] now...

⚠️ **Please start a new chat session**...
```

**Markers (pch-coder):**
```
✅ **Phase [N] Complete — Session End**

All tasks in Phase [N]: [Phase Name] have been implemented successfully.

**Completed This Session:**
...

---

🔄 **Next Phase Requires a New Session**
...

⚠️ **Please start a new chat session**...
```

**Detection Regex:**
```regex
✅\s*\*\*Phase\s+\[?\d+\]?\s+(Complete|Completed)
```

**Secondary Indicators:**
- Contains "**Progress:**" or "**Completed This Session:**"
- Contains "🔄 **Ready for Phase" or "🔄 **Next Phase"
- Contains "Please start a new chat session"

---

##### Category B: Final Completion (All Phases Done)

**Pattern:** Agent completed all phases; no new session needed

**Markers (pch-researcher):**
```
✅ **Research Complete**

All [N] research phases have been completed and documented.

**Document:** `[document path]`

**Research Summary:**
...

---

✅ **This research is now fully complete.** No further sessions needed.
```

**Markers (pch-coder):**
```
🎉 **All Phases Complete — Implementation Finished**

**Plan:** {plan_path}
**Total Phases:** {N}
...

Would you like me to commit and push all changes?
```

**Markers (pch-planner):**
```
📋 **Plan Complete — Ready for Review**

The implementation plan has been created at: `[plan path]`

**Plan Summary:**
...

**Next Step:** Use the **"Start Plan Review"**...
```

**Detection Regex:**
```regex
(✅\s*\*\*Research Complete|🎉\s*\*\*All Phases Complete|📋\s*\*\*Plan Complete)
```

**Secondary Indicators:**
- "No further sessions needed"
- "fully complete"
- "Ready for Review"

---

##### Category C: Mode Selection / Options Prompt

**Pattern:** Agent offering choices; requires user input to proceed

**Example (pch-researcher/pch-coder):**
```
🔄 **Execution Mode Selection**

I'm ready to [conduct research/implement the plan] at `[path]`.

**How would you like to proceed?**

1️⃣ **Manual Mode** (Recommended for first-time...)
   ...

2️⃣ **Automatic Mode** (For well-defined...)
   ...

**Enter 1 or 2** (or say "manual" / "automatic"):
```

**Detection Regex:**
```regex
\*\*Enter\s+(\d+\s+or\s+\d+|\d+,?\s*\d*,?\s*(or)?\s*\d+)\*\*
```

Or simpler emoji-based detection:
```regex
(1️⃣|2️⃣|3️⃣).*\n.*(1️⃣|2️⃣|3️⃣)
```

**Secondary Indicators:**
- "**How would you like to proceed?**"
- Numbered options with emoji prefixes (1️⃣, 2️⃣, 3️⃣)
- "Enter 1 or 2"

---

##### Category D: Clarifying Questions

**Pattern:** Agent needs information to proceed; structured question format

**Example (pch-planner):**
```
### [Question Topic]

**Question:** [Clear, specific question...]

**Options:**

A) **[Option Name]**
   [Description...]

B) **[Option Name]**
   [Description...]

C) **[Option Name]**
   [Description...]

---

**Recommendation:** [Letter] — [Option Name]

**Rationale:** [Explanation...]
```

**Example (pch-visionary open-ended):**
```
**[Question text ending with ?]**

Here's an example to help clarify what I'm asking:
[Example content]
```

**Detection Regex (structured question):**
```regex
\*\*Question:\*\*.*\n\n\*\*Options:\*\*
```

**Detection Regex (multiple choice):**
```regex
^[A-E]\)\s+\*\*[^*]+\*\*
```

**Secondary Indicators:**
- "**Recommendation:**" following options
- "**Rationale:**" explaining choice
- Question mark at end of bold text: `**...?**`

---

##### Category E: Blocker / Partial Completion

**Pattern:** Agent encountered issues; presenting recovery options

**Example:**
```
⚠️ **Phase [N] Completed with Issues**

**Status:** {partial | blocked}
**Tasks Completed:** [list]
**Tasks Blocked:** [list with reasons]

**Blocker Details:**
...

---

**How would you like to proceed?**

1️⃣ **Continue to Next Phase**
   ...

2️⃣ **Skip to Phase {M}**
   ...

3️⃣ **Stop Automatic Execution**
   ...
```

**Detection Regex:**
```regex
⚠️\s*\*\*Phase\s+\[?\d+\]?\s+(Completed with Issues|Partially Complete)
```

**Secondary Indicators:**
- "**Status:** partial" or "**Status:** blocked"
- "**Blocker Details:**"
- "**How would you like to proceed?**" after blocker content

---

#### 5. Subagent Structured Return Format

For automatic mode, subagents return a **structured format** that is easily parseable:

**pch-coder Subagent Return:**
```
PHASE: {N}
STATUS: success | partial | blocked
TASKS_COMPLETED: [list of task IDs]
TASKS_BLOCKED: [list of task IDs with reasons]
FILES_MODIFIED: [list of file paths]
FILES_CREATED: [list of file paths]
DEVIATIONS: [changes from plan or "none"]
NOTES: [implementation details]
VERIFICATION: [verification steps]
```

**pch-researcher Subagent Return:**
```
PHASE: {N}
PHASE_NAME: {Phase Name}
STATUS: success | partial | blocked
SCOPE_COMPLETED: [description]
FINDINGS: [markdown content]
KEY_DISCOVERIES: [bulleted list]
RELEVANT_FILES: [file paths with notes]
EXTERNAL_SOURCES: [URLs or "none"]
GAPS_FOUND: [list or "none"]
ASSUMPTIONS_MADE: [list or "none"]
FOLLOW_UP_NEEDED: [suggestions or "none"]
NOTES: [orchestrator context]
```

**Parsing Strategy:**
```python
def parse_subagent_response(output: str) -> dict:
    result = {}
    current_key = None
    current_value = []
    
    for line in output.split('\n'):
        # Match key-value pattern
        match = re.match(r'^([A-Z_]+):\s*(.*)', line)
        if match:
            # Save previous key-value
            if current_key:
                result[current_key] = '\n'.join(current_value).strip()
            current_key = match.group(1)
            current_value = [match.group(2)]
        elif current_key:
            # Continue multi-line value
            current_value.append(line)
    
    # Save last key-value
    if current_key:
        result[current_key] = '\n'.join(current_value).strip()
    
    return result
```

---

#### 6. Signal Classification Decision Tree

```
Agent Output Received
        │
        ▼
┌───────────────────────────────────────┐
│ Check ACP stopReason                   │
├───────────────────────────────────────┤
│ • end_turn → Continue to content      │
│ • max_tokens → Retry/Compact          │
│ • cancelled → Handle cancellation     │
│ • refusal → Escalate to human         │
└───────────────────────────────────────┘
        │ (end_turn)
        ▼
┌───────────────────────────────────────┐
│ Is this subagent mode?                 │
├───────────────────────────────────────┤
│ YES → Parse structured return format   │
│       • Extract STATUS field           │
│       • Route by success/partial/blocked│
│                                        │
│ NO → Continue to text pattern matching │
└───────────────────────────────────────┘
        │ (not subagent)
        ▼
┌───────────────────────────────────────┐
│ Check for FINAL completion markers     │
├───────────────────────────────────────┤
│ Regex: (Research Complete|All Phases   │
│        Complete|Plan Complete)         │
│                                        │
│ MATCH → WORKFLOW_COMPLETE              │
│         Hand off to next agent         │
└───────────────────────────────────────┘
        │ (no match)
        ▼
┌───────────────────────────────────────┐
│ Check for PHASE completion markers     │
├───────────────────────────────────────┤
│ Regex: Phase \[?\d+\]? Complete        │
│ + "Please start a new chat session"    │
│                                        │
│ MATCH → PHASE_COMPLETE                 │
│         Spawn new session for next     │
└───────────────────────────────────────┘
        │ (no match)
        ▼
┌───────────────────────────────────────┐
│ Check for QUESTION markers             │
├───────────────────────────────────────┤
│ • "Enter 1 or 2" → MODE_SELECTION      │
│ • "**Question:**" + "**Options:**"     │
│   → CLARIFYING_QUESTION                │
│ • "How would you like to proceed?"     │
│   + numbered options → OPTIONS_PROMPT  │
│ • Bold text ending in "?"              │
│   → OPEN_QUESTION                      │
│                                        │
│ Any MATCH → AWAITING_INPUT             │
│              Route to question handler │
└───────────────────────────────────────┘
        │ (no match)
        ▼
┌───────────────────────────────────────┐
│ Check for BLOCKER markers              │
├───────────────────────────────────────┤
│ • "Completed with Issues"              │
│ • "Status:** partial/blocked"          │
│ • "❌ Blocked"                         │
│                                        │
│ MATCH → BLOCKER_DETECTED               │
│         Present options to user        │
└───────────────────────────────────────┘
        │ (no match)
        ▼
┌───────────────────────────────────────┐
│ DEFAULT: IN_PROGRESS                   │
├───────────────────────────────────────┤
│ Agent output doesn't match known       │
│ patterns. May be:                      │
│ • Informational update                 │
│ • Unexpected format                    │
│                                        │
│ Action: Log and continue monitoring    │
└───────────────────────────────────────┘
```

---

#### 7. Regex Pattern Library for .NET Implementation

```csharp
public static class AgentOutputPatterns
{
    // Completion Patterns
    public static readonly Regex PhaseComplete = new Regex(
        @"✅\s*\*\*Phase\s+\[?\d+\]?\s+(Complete|Completed)",
        RegexOptions.Compiled);
    
    public static readonly Regex ResearchComplete = new Regex(
        @"✅\s*\*\*Research Complete\*\*",
        RegexOptions.Compiled);
    
    public static readonly Regex AllPhasesComplete = new Regex(
        @"🎉\s*\*\*All Phases Complete",
        RegexOptions.Compiled);
    
    public static readonly Regex PlanComplete = new Regex(
        @"📋\s*\*\*Plan Complete\s*—\s*Ready for Review",
        RegexOptions.Compiled);
    
    public static readonly Regex NewSessionRequest = new Regex(
        @"Please start a new chat session",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Question Patterns
    public static readonly Regex ModeSelection = new Regex(
        @"\*\*Enter\s+\d+\s+or\s+\d+\*\*",
        RegexOptions.Compiled);
    
    public static readonly Regex StructuredQuestion = new Regex(
        @"\*\*Question:\*\*.*?\*\*Options:\*\*",
        RegexOptions.Compiled | RegexOptions.Singleline);
    
    public static readonly Regex MultipleChoice = new Regex(
        @"^[A-E]\)\s+\*\*[^*]+\*\*",
        RegexOptions.Compiled | RegexOptions.Multiline);
    
    public static readonly Regex NumberedOptions = new Regex(
        @"[1-5]️⃣\s*\*\*[^*]+\*\*",
        RegexOptions.Compiled);
    
    public static readonly Regex HowToProceed = new Regex(
        @"\*\*How would you like to proceed\?\*\*",
        RegexOptions.Compiled);
    
    // Blocker Patterns
    public static readonly Regex PartialCompletion = new Regex(
        @"⚠️\s*\*\*Phase\s+\[?\d+\]?\s+(Completed with Issues|Partially Complete)",
        RegexOptions.Compiled);
    
    public static readonly Regex StatusBlocked = new Regex(
        @"\*\*Status:\*\*\s*(partial|blocked)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public static readonly Regex TaskBlocked = new Regex(
        @"❌\s*(Blocked|Failed)",
        RegexOptions.Compiled);
    
    // Subagent Return Format
    public static readonly Regex SubagentStatus = new Regex(
        @"^STATUS:\s*(success|partial|blocked)",
        RegexOptions.Compiled | RegexOptions.Multiline);
    
    public static readonly Regex SubagentPhase = new Regex(
        @"^PHASE:\s*(\d+)",
        RegexOptions.Compiled | RegexOptions.Multiline);
}
```

---

#### 8. Output Classification Implementation

```csharp
public enum OutputClassification
{
    WorkflowComplete,       // All phases done, hand off to next agent
    PhaseComplete,          // Phase done, needs new session
    ModeSelection,          // Asking for manual vs automatic
    ClarifyingQuestion,     // Structured question with options
    OptionsPrompt,          // Multiple choice with numbered options
    OpenQuestion,           // Open-ended question
    BlockerDetected,        // Partial/blocked with recovery options  
    InProgress,             // Informational, continue monitoring
    Unknown                 // Unrecognized format
}

public class OutputClassifier
{
    public OutputClassification Classify(string output)
    {
        // Check final completions first (most specific)
        if (AgentOutputPatterns.ResearchComplete.IsMatch(output) ||
            AgentOutputPatterns.AllPhasesComplete.IsMatch(output))
        {
            return OutputClassification.WorkflowComplete;
        }
        
        if (AgentOutputPatterns.PlanComplete.IsMatch(output))
        {
            return OutputClassification.WorkflowComplete;
        }
        
        // Check phase completion (needs new session)
        if (AgentOutputPatterns.PhaseComplete.IsMatch(output) &&
            AgentOutputPatterns.NewSessionRequest.IsMatch(output))
        {
            return OutputClassification.PhaseComplete;
        }
        
        // Check for mode selection
        if (AgentOutputPatterns.ModeSelection.IsMatch(output))
        {
            return OutputClassification.ModeSelection;
        }
        
        // Check for structured question
        if (AgentOutputPatterns.StructuredQuestion.IsMatch(output))
        {
            return OutputClassification.ClarifyingQuestion;
        }
        
        // Check for numbered options prompt
        if (AgentOutputPatterns.HowToProceed.IsMatch(output) &&
            AgentOutputPatterns.NumberedOptions.IsMatch(output))
        {
            return OutputClassification.OptionsPrompt;
        }
        
        // Check for blocker
        if (AgentOutputPatterns.PartialCompletion.IsMatch(output) ||
            AgentOutputPatterns.StatusBlocked.IsMatch(output))
        {
            return OutputClassification.BlockerDetected;
        }
        
        // Check for open questions (bold text ending with ?)
        if (Regex.IsMatch(output, @"\*\*[^*]+\?\*\*"))
        {
            return OutputClassification.OpenQuestion;
        }
        
        // Default
        return OutputClassification.InProgress;
    }
}
```

---

#### 9. ACP Tool Request Interception

For more sophisticated detection, monitor ACP `tool_request` messages during execution:

**Key Tools That Indicate Questions:**

| Tool Name | Significance | Action |
|-----------|--------------|--------|
| `ask_user` | Agent explicitly asking user | Intercept for autonomous answering |
| `confirm` | Agent requesting confirmation | Auto-approve or escalate |

**Interception Pattern:**

```typescript
// In ACP client handler
async sessionUpdate(params: SessionUpdateParams) {
  const update = params.update;
  
  if (update.sessionUpdate === "tool_request") {
    if (update.tool === "ask_user") {
      // Agent is asking a question
      const question = update.params.question;
      
      // Option 1: Route to autonomous answering
      const answer = await this.autoAnswer(question);
      if (answer) {
        await this.respondToTool(update.toolId, answer);
        return;
      }
      
      // Option 2: Escalate to human
      await this.escalateQuestion(question);
    }
  }
}
```

**Note:** The `--no-ask-user` CLI flag disables this tool entirely, forcing the agent to make autonomous decisions. This is useful for fully autonomous mode but means the agent might make suboptimal choices.

---

#### 10. Validation and Confidence Scoring

For robust detection, implement confidence scoring:

```csharp
public class ClassificationResult
{
    public OutputClassification Classification { get; set; }
    public float Confidence { get; set; }  // 0.0 - 1.0
    public List<string> MatchedPatterns { get; set; }
    public string RawOutput { get; set; }
}

public ClassificationResult ClassifyWithConfidence(string output)
{
    var result = new ClassificationResult
    {
        RawOutput = output,
        MatchedPatterns = new List<string>()
    };
    
    // Score based on number of matching indicators
    var completionScore = 0f;
    var questionScore = 0f;
    
    // Completion indicators
    if (AgentOutputPatterns.PhaseComplete.IsMatch(output))
    {
        completionScore += 0.5f;
        result.MatchedPatterns.Add("PhaseComplete");
    }
    if (AgentOutputPatterns.NewSessionRequest.IsMatch(output))
    {
        completionScore += 0.3f;
        result.MatchedPatterns.Add("NewSessionRequest");
    }
    if (output.Contains("**Progress:**"))
    {
        completionScore += 0.2f;
        result.MatchedPatterns.Add("ProgressSection");
    }
    
    // Question indicators
    if (AgentOutputPatterns.ModeSelection.IsMatch(output))
    {
        questionScore += 0.6f;
        result.MatchedPatterns.Add("ModeSelection");
    }
    if (AgentOutputPatterns.HowToProceed.IsMatch(output))
    {
        questionScore += 0.4f;
        result.MatchedPatterns.Add("HowToProceed");
    }
    
    // Determine classification based on highest score
    if (completionScore > questionScore && completionScore >= 0.5f)
    {
        result.Classification = OutputClassification.PhaseComplete;
        result.Confidence = completionScore;
    }
    else if (questionScore > completionScore && questionScore >= 0.5f)
    {
        result.Classification = OutputClassification.ModeSelection;
        result.Confidence = questionScore;
    }
    else
    {
        result.Classification = OutputClassification.Unknown;
        result.Confidence = Math.Max(completionScore, questionScore);
    }
    
    return result;
}
```

**Confidence Thresholds:**

| Confidence | Action |
|------------|--------|
| ≥ 0.8 | High confidence, proceed automatically |
| 0.5 - 0.8 | Medium confidence, proceed with logging |
| < 0.5 | Low confidence, may need human review |

### Key Discoveries

- **ACP `stopReason` is not sufficient alone** — `end_turn` only means the agent stopped generating, not that the phase is complete. Content parsing is required to classify the output type.
- **PCH agents use consistent, parseable patterns** — All agents use emoji prefixes (✅, 🔄, ⚠️, 📋, 🎉) and bold markdown headers that are reliable detection signals.
- **Four distinct output categories exist** — Phase completion, final completion, question prompts, and blocker states each have unique textual markers.
- **Subagent mode uses structured key-value format** — `STATUS: success | partial | blocked` provides unambiguous parsing for automatic mode.
- **Tool interception enables early detection** — Monitoring `ask_user` tool requests in ACP allows detecting questions before the turn ends.
- **"Please start a new chat session" is the definitive phase boundary marker** — This phrase distinguishes phase completion (needs new session) from final completion (no new session needed).
- **Confidence scoring improves robustness** — Matching multiple indicators increases classification confidence and reduces false positives.

### Relevant Files/Resources

**Internal (Analyzed for Patterns):**
- [agents/pch-researcher.md](agents/pch-researcher.md) — Session handoff messages at lines 1205-1283
- [agents/pch-coder.md](agents/pch-coder.md) — Phase completion and blocker handling patterns
- [agents/pch-planner.md](agents/pch-planner.md) — Question format and plan completion markers
- [agents/pch-visionary.md](agents/pch-visionary.md) — Open-ended and multiple choice question patterns

**External Documentation:**
- [ACP Prompt Turn](https://agentclientprotocol.com/protocol/prompt-turn) — `stopReason` values and session lifecycle
- [ACP Schema](https://agentclientprotocol.com/protocol/schema) — `SessionUpdate`, `StopReason` type definitions
- [ACP Session Setup](https://agentclientprotocol.com/protocol/session-setup) — Tool permission handling

### Gaps & Limitations

- **No testing with actual ACP output** — Patterns derived from agent prompt files, not observed runtime output. Real outputs may vary slightly.
- **Emoji encoding considerations** — Unicode emoji handling in .NET regex may require attention (`\u2705` vs `✅`).
- **Edge cases with custom agents** — Non-PCH agents won't follow these patterns; detection would fail.

### Assumptions

- **PCH agent output format is stable** — Assuming handoff message templates remain consistent across versions.
- **Markdown formatting is preserved** — Assuming Copilot CLI output preserves `**bold**` markdown syntax.
- **ACP `stopReason` is always present** — Assuming the protocol guarantees this field in all responses.

---

## Phase 5: Autonomous Question Answering Patterns

**Status:** ✅ Complete
**Scope:**
- Research methods for intercepting agent questions
- Investigate routing questions back to the model for self-answering
- Explore prompt engineering for autonomous decision-making
- Define boundaries between autonomous and human-required decisions

### Research Tasks

- [x] Analyze question patterns in pch-planner and pch-plan-reviewer
- [x] Research multi-turn conversation handling in SDK
- [x] Investigate meta-prompting for self-answering capability
- [x] Design question classification (autonomous vs human-required)
- [x] Research confidence thresholds for autonomous responses
- [x] Document edge cases and failure modes

### Findings

#### 1. PCH Agent Question Pattern Analysis

Comprehensive analysis of PCH agent prompts reveals **three distinct question types** with different structures and autonomous answerability:

##### Question Type A: Structured Multiple Choice (pch-planner, pch-plan-reviewer)

**Format:**
```markdown
### [Question Topic]

**Question:** [Clear, specific question...]

**Options:**

A) **[Option Name]**
   [Description, trade-offs, when appropriate]

B) **[Option Name]**
   [Description, trade-offs, when appropriate]

C) **[Option Name]**
   [Description, trade-offs, when appropriate]

---

**Recommendation:** [Letter] — [Option Name]

**Rationale:** [Why this option fits...]
```

**Autonomous Answerability:** **HIGH** — The agent already provides a recommendation with rationale. The orchestrator can accept the recommendation when confidence is high.

**Detection Regex:**
```regex
\*\*Question:\*\*.*?\n\n\*\*Options:\*\*.*?\*\*Recommendation:\*\*\s*([A-E])\s*[—-]\s*([^\n]+)
```

**Key Insight:** The recommendation letter and name can be extracted and used as the autonomous answer.

---

##### Question Type B: Open-Ended Elicitation (pch-visionary)

**Format:**
```markdown
### What Problem Are We Solving?

**Question:** In a few sentences, describe the problem or opportunity 
you're trying to address. What's not working today, or what new 
capability do you need?
```

**Autonomous Answerability:** **LOW** — These questions require genuine user input as they're capturing the user's intent, not making technical decisions.

**Detection Pattern:**
- No options provided
- Question ends with open-ended prompt
- Often appears in Stage 1-2 of vision capture

---

##### Question Type C: Validation/Confirmation (pch-visionary, pch-plan-reviewer)

**Format:**
```markdown
### Confirming [Topic]

Based on what you've shared, here's my understanding:

> [Synthesized summary]

**Is this accurate?** If not, what should I adjust?

1. ✅ Yes, that's correct — proceed
2. ✏️ Partially correct — [specify what to change]
3. ❌ No, let me clarify — [provide correction]
```

**Autonomous Answerability:** **HIGH** — In autonomous mode, the orchestrator can assume the agent's synthesis is correct and respond with "1" (proceed).

---

##### Question Type D: Mode Selection (pch-coder, pch-researcher)

**Format:**
```markdown
🔄 **Execution Mode Selection**

**How would you like to proceed?**

1️⃣ **Manual Mode** (Recommended for first-time...)
2️⃣ **Automatic Mode** (For well-defined...)

**Enter 1 or 2** (or say "manual" / "automatic"):
```

**Autonomous Answerability:** **HIGH** — The orchestrator controls execution mode and can programmatically respond with "2" for automatic mode.

---

##### Question Type E: Blocker Recovery (pch-coder, pch-researcher)

**Format:**
```markdown
⚠️ **Phase [N] Completed with Issues**

**How would you like to proceed?**

1️⃣ **Continue to Next Phase**
2️⃣ **Skip to Phase {M}**
3️⃣ **Stop Automatic Execution**
4️⃣ **Retry Phase {N}**
5️⃣ **Provide Clarification**
```

**Autonomous Answerability:** **MEDIUM** — The orchestrator can apply policy-based decisions (e.g., always continue on non-critical blockers) but may need escalation for critical failures.

---

#### 2. Question Interception Architecture

Building on Phase 4's ACP `ask_user` tool discovery, here's the question interception architecture:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Question Interception Flow                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Agent generates output                                                  │
│         │                                                                │
│         ▼                                                                │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │               ACP Tool Layer (Early Detection)                   │   │
│  │                                                                   │   │
│  │  session/update → tool_request "ask_user"                        │   │
│  │         │                                                         │   │
│  │         ▼                                                         │   │
│  │  Intercept BEFORE turn ends → Route to AnswerEngine              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                │                                         │
│                       (if no tool interception)                         │
│                                │                                         │
│                                ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              Content Layer (Post-Completion)                     │   │
│  │                                                                   │   │
│  │  Agent output text → Pattern matching → Classify question type   │   │
│  │         │                                                         │   │
│  │         ▼                                                         │   │
│  │  Extract question + options + recommendation                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                │                                         │
│                                ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                   Answer Engine                                   │   │
│  │                                                                   │   │
│  │  ┌─────────────┐   ┌─────────────┐   ┌─────────────────────┐   │   │
│  │  │ Classifier  │ → │ Answer Gen  │ → │ Confidence Check    │   │   │
│  │  │             │   │             │   │                     │   │   │
│  │  │ - Type      │   │ - Vision    │   │ - Threshold met?    │   │   │
│  │  │ - Context   │   │ - Standards │   │ - Escalate if low   │   │   │
│  │  │ - Risk      │   │ - Codebase  │   │                     │   │   │
│  │  └─────────────┘   └─────────────┘   └─────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                │                                         │
│                                ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                   Response Injection                             │   │
│  │                                                                   │   │
│  │  High Confidence → session/prompt with answer                    │   │
│  │  Low Confidence  → Escalate to human (queue for notification)    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

#### 3. Meta-Prompting for Self-Answering

The key innovation for autonomous question answering is **meta-prompting** — using an LLM to answer questions on behalf of the user.

##### Meta-Prompt Architecture

```
┌───────────────────────────────────────────────────────────────────────┐
│                      Meta-Prompting Strategy                           │
├───────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  QUESTION INTERCEPTED                                                  │
│  "How should user authentication be implemented?"                      │
│         │                                                              │
│         ▼                                                              │
│  ┌─────────────────────────────────────────────────────────────┐      │
│  │                  Context Aggregation                         │      │
│  │                                                               │      │
│  │  1. Vision Document → Requirements, constraints, goals       │      │
│  │  2. Previous Phase Docs → Research findings, decisions       │      │
│  │  3. Codebase Patterns → Existing implementations             │      │
│  │  4. Standards Space → Organizational guidelines              │      │
│  │  5. Question Context → Options, recommendation, rationale    │      │
│  └─────────────────────────────────────────────────────────────┘      │
│         │                                                              │
│         ▼                                                              │
│  ┌─────────────────────────────────────────────────────────────┐      │
│  │                  Meta-Prompt Generation                      │      │
│  │                                                               │      │
│  │  "You are an autonomous decision-maker for a development     │      │
│  │   orchestration system. A planning agent is asking:          │      │
│  │                                                               │      │
│  │   [ORIGINAL QUESTION WITH OPTIONS]                           │      │
│  │                                                               │      │
│  │   Based on the following context, select the best option:    │      │
│  │                                                               │      │
│  │   Vision Requirements:                                        │      │
│  │   [Extracted from vision doc]                                 │      │
│  │                                                               │      │
│  │   Existing Patterns:                                          │      │
│  │   [Extracted from codebase analysis]                          │      │
│  │                                                               │      │
│  │   Organizational Standards:                                   │      │
│  │   [Extracted from pch-standards-space]                        │      │
│  │                                                               │      │
│  │   Respond with ONLY the option letter (A, B, C, etc.)        │      │
│  │   followed by a one-sentence justification."                  │      │
│  └─────────────────────────────────────────────────────────────┘      │
│         │                                                              │
│         ▼                                                              │
│  ┌─────────────────────────────────────────────────────────────┐      │
│  │                  LLM Response                                │      │
│  │                                                               │      │
│  │  "A — JWT with HTTP-only Cookies aligns with the existing   │      │
│  │   auth pattern in src/auth/JwtAuthService.ts and meets      │      │
│  │   the security requirements from the vision document."       │      │
│  └─────────────────────────────────────────────────────────────┘      │
│         │                                                              │
│         ▼                                                              │
│  Inject answer into next session/prompt → Agent continues              │
│                                                                        │
└───────────────────────────────────────────────────────────────────────┘
```

##### Meta-Prompt Template

```csharp
public string BuildMetaPrompt(
    QuestionContext question,
    VisionDocument vision,
    ResearchDocument research,
    CodebasePatterns patterns,
    StandardsContext standards)
{
    return $"""
    You are an autonomous decision-maker for a software development orchestration system.
    A {question.AgentName} agent has asked the following question:

    ---
    {question.FullQuestionText}
    ---

    ## Decision Context

    ### Vision Requirements (Primary Source)
    {vision.ExtractRelevantRequirements(question.Topic)}

    ### Existing Codebase Patterns
    {patterns.GetRelevantPatterns(question.Topic)}

    ### Organizational Standards
    {standards.GetRelevantGuidelines(question.Topic)}

    ### Previous Decisions in This Workflow
    {research.GetRelatedDecisions(question.Topic)}

    ## Your Task

    1. If the agent provided a **Recommendation**, evaluate whether it aligns with the context above.
    2. If it aligns, respond with the recommended option.
    3. If it conflicts, choose the option that best fits the vision and standards.
    4. If no option fits well, respond with "ESCALATE" and explain why.

    ## Response Format

    Respond in this exact format:
    ```
    ANSWER: [A|B|C|D|E|ESCALATE]
    CONFIDENCE: [HIGH|MEDIUM|LOW]
    JUSTIFICATION: [One sentence explaining the choice]
    ```
    """;
}
```

---

#### 4. Question Classification: Autonomous vs Human-Required

Not all questions should be answered autonomously. Here's the classification framework:

##### Classification Matrix

| Question Type | Characteristics | Autonomous? | Confidence Threshold |
|---------------|-----------------|-------------|---------------------|
| **Technical Choice** | Architecture, patterns, libraries | ✅ Yes | Medium (0.6+) |
| **Scope Decision** | What to include/exclude | ⚠️ Conditional | High (0.8+) |
| **Requirements Capture** | User intent, problem definition | ❌ No | N/A — Always escalate |
| **Validation/Confirmation** | "Is this correct?" | ✅ Yes | Low (0.4+) |
| **Mode Selection** | Manual vs Automatic | ✅ Yes | Always proceed |
| **Blocker Recovery** | How to handle failures | ⚠️ Policy-based | Depends on policy |
| **Security/Compliance** | Auth, data handling | ⚠️ Conditional | Very High (0.9+) |
| **Cost/Resource** | Infrastructure sizing | ❌ Usually No | Escalate by default |

##### Classification Decision Tree

```csharp
public class QuestionClassifier
{
    public AnswerStrategy ClassifyQuestion(ParsedQuestion question)
    {
        // Stage 1: Check if question type is always human-required
        if (IsRequirementsCapture(question))
            return AnswerStrategy.AlwaysEscalate;
        
        if (IsCostOrResourceDecision(question))
            return AnswerStrategy.EscalateUnlessExplicitPolicy;
        
        // Stage 2: Check if recommendation exists
        if (question.HasRecommendation)
        {
            // High confidence in agent's recommendation
            if (question.Type == QuestionType.TechnicalChoice)
                return AnswerStrategy.AcceptRecommendation(threshold: 0.6);
            
            if (question.Type == QuestionType.ValidationConfirmation)
                return AnswerStrategy.AcceptRecommendation(threshold: 0.4);
            
            if (question.Type == QuestionType.ScopeDecision)
                return AnswerStrategy.AcceptRecommendation(threshold: 0.8);
        }
        
        // Stage 3: Check if context provides clear answer
        if (CanDeriveFromVision(question))
            return AnswerStrategy.DeriveFromContext(source: "vision");
        
        if (CanDeriveFromStandards(question))
            return AnswerStrategy.DeriveFromContext(source: "standards");
        
        // Stage 4: Meta-prompt for complex decisions
        if (question.Type == QuestionType.TechnicalChoice)
            return AnswerStrategy.MetaPrompt(threshold: 0.6);
        
        // Stage 5: Default to escalation for safety
        return AnswerStrategy.Escalate;
    }
    
    private bool IsRequirementsCapture(ParsedQuestion q)
    {
        // Open-ended questions about what the user wants
        return q.Type == QuestionType.OpenEnded &&
               q.Stage <= 2 && // Vision capture stages 1-2
               !q.HasOptions;
    }
    
    private bool IsCostOrResourceDecision(ParsedQuestion q)
    {
        var costKeywords = new[] { "cost", "pricing", "budget", "resource", 
                                    "infrastructure", "scaling", "capacity" };
        return costKeywords.Any(k => q.Text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

#### 5. Confidence Threshold Framework

Autonomous answers require confidence thresholds to prevent incorrect decisions:

##### Confidence Scoring Model

```csharp
public class ConfidenceCalculator
{
    public float CalculateConfidence(
        ParsedQuestion question,
        AnswerContext context,
        MetaPromptResult metaResult)
    {
        var score = 0.0f;
        
        // Factor 1: Agent recommendation alignment (0.0 - 0.3)
        if (question.HasRecommendation)
        {
            if (metaResult.ChosenOption == question.RecommendedOption)
                score += 0.3f;  // Full alignment
            else
                score += 0.1f;  // Divergence reduces confidence
        }
        
        // Factor 2: Vision document coverage (0.0 - 0.25)
        var visionRelevance = context.Vision.GetTopicRelevance(question.Topic);
        score += visionRelevance * 0.25f;
        
        // Factor 3: Standards match (0.0 - 0.20)
        if (context.Standards.HasGuidance(question.Topic))
            score += 0.20f;
        
        // Factor 4: Codebase pattern exists (0.0 - 0.15)
        if (context.Patterns.HasExistingExample(question.Topic))
            score += 0.15f;
        
        // Factor 5: Meta-prompt confidence (0.0 - 0.10)
        if (metaResult.SelfReportedConfidence == "HIGH")
            score += 0.10f;
        else if (metaResult.SelfReportedConfidence == "MEDIUM")
            score += 0.05f;
        
        return Math.Min(score, 1.0f);
    }
}
```

##### Threshold Policies

| Threshold | Decision | Logging |
|-----------|----------|---------|
| **≥ 0.8** | Proceed automatically | Log decision for audit |
| **0.6 - 0.8** | Proceed with warning | Log + flag for review |
| **0.4 - 0.6** | Defer to async review | Queue for human review, continue if no response |
| **< 0.4** | Escalate immediately | Block until human responds |

---

#### 6. Context Sources for Autonomous Answers

The orchestrator can draw from multiple context sources to inform autonomous decisions:

##### Context Priority Hierarchy

| Priority | Source | Purpose | Token Budget |
|----------|--------|---------|--------------|
| 1 | Vision Document | User requirements, constraints, goals | ~2000 tokens |
| 2 | Previous Phase Outputs | Decisions already made | ~1500 tokens |
| 3 | Organizational Standards | Company guidelines (pch-standards-space) | ~1000 tokens |
| 4 | Codebase Patterns | Existing implementations | ~1000 tokens |
| 5 | Question Context | Options, recommendation, rationale | ~500 tokens |

##### Context Extraction Strategies

```csharp
public class ContextExtractor
{
    public async Task<AnswerContext> ExtractContext(
        ParsedQuestion question,
        string workspacePath)
    {
        var context = new AnswerContext();
        
        // 1. Vision document - always included
        context.Vision = await LoadVisionDocument(workspacePath);
        
        // 2. Previous phase outputs
        context.PreviousDecisions = await LoadDecisionLog(workspacePath);
        
        // 3. Standards - query semantically
        context.Standards = await QueryStandardsSpace(
            query: question.Topic,
            tokenBudget: 1000);
        
        // 4. Codebase patterns - search for similar implementations
        context.Patterns = await SearchCodebase(
            query: question.Topic,
            patternType: "implementation");
        
        // 5. Question context - already parsed
        context.Question = question;
        
        return context;
    }
    
    private async Task<StandardsContext> QueryStandardsSpace(
        string query, int tokenBudget)
    {
        try
        {
            // Use GitHub MCP server to query pch-standards-space
            var result = await _mcpClient.InvokeAsync(
                server: "github",
                tool: "get_copilot_space",
                args: new { 
                    space: "pch-standards-space",
                    query: query
                });
            
            return TruncateToTokenBudget(result, tokenBudget);
        }
        catch
        {
            // Fallback: proceed without standards
            return StandardsContext.Empty;
        }
    }
}
```

---

#### 7. Response Injection Patterns

After generating an autonomous answer, inject it back into the agent session:

##### Pattern A: Next Turn Injection (ACP)

```csharp
// Agent asked question, turn ended with end_turn
// Inject answer as next prompt

var answer = await _answerEngine.GenerateAnswer(question);

if (answer.Confidence >= Thresholds.AutoProceed)
{
    await _acp.PromptAsync(new PromptRequest
    {
        SessionId = session.SessionId,
        Prompt = new[]
        {
            new ContentBlock
            {
                Type = "text",
                Text = answer.FormattedResponse  // e.g., "A"
            }
        }
    });
}
```

##### Pattern B: Tool Response Injection (ACP ask_user)

```csharp
// Intercept ask_user tool request before turn ends

_acp.OnToolRequest += async (sender, e) =>
{
    if (e.Tool == "ask_user")
    {
        var question = ParseQuestion(e.Params);
        var answer = await _answerEngine.GenerateAnswer(question);
        
        if (answer.Confidence >= Thresholds.AutoProceed)
        {
            // Respond directly to tool
            await _acp.RespondToToolAsync(
                toolId: e.ToolId,
                result: answer.FormattedResponse);
            e.Handled = true;  // Don't bubble up
        }
    }
};
```

##### Pattern C: CLI Mode Chaining

```bash
# Question detected in output; chain with answer
copilot --prompt "A" --allow-all --silent

# Or use session resume with answer
copilot --resume $SESSION_ID --prompt "A" --allow-all --silent
```

---

#### 8. Edge Cases and Failure Modes

##### Failure Mode: Ambiguous Options

**Scenario:** All options seem equally valid or none perfectly fit the vision requirements.

**Detection:**
- Meta-prompt returns "ESCALATE"
- Confidence score < 0.4
- Multiple options score within 0.1 of each other

**Handling:**
```csharp
if (answer.IsAmbiguous)
{
    // Don't guess - escalate with context
    await _notificationService.EscalateAsync(new Escalation
    {
        Question = question,
        Analysis = answer.Justification,
        TopOptions = answer.GetTopOptions(2),
        RecommendedAction = "Human review required - options are equally viable"
    });
    
    // Block or defer based on policy
    return await _policy.HandleAmbiguousQuestion(question);
}
```

##### Failure Mode: Context Window Exhaustion

**Scenario:** Context for meta-prompting exceeds token limits.

**Detection:**
- Token count estimate exceeds model limit
- Context aggregation fails

**Handling:**
```csharp
if (context.EstimatedTokens > MaxContextTokens)
{
    // Prioritize and truncate
    context = context
        .KeepTop("vision", 1500)    // Highest priority
        .KeepTop("standards", 800)
        .KeepTop("patterns", 500)
        .DropLeast();
    
    // Log truncation for debugging
    _logger.LogWarning("Context truncated for question {QId}", question.Id);
}
```

##### Failure Mode: Cascading Decisions

**Scenario:** Early autonomous decision has downstream impact that wasn't considered.

**Detection:** Difficult to detect in real-time; requires post-hoc analysis.

**Mitigation:**
1. Log all autonomous decisions with full context
2. Allow human review before deployment phases
3. Implement decision rollback capability in plans

```csharp
public class DecisionAuditLog
{
    public void LogAutonomousDecision(AutonomousAnswer answer)
    {
        var entry = new DecisionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Phase = _currentPhase,
            Question = answer.Question.FullText,
            ChosenOption = answer.FormattedResponse,
            Confidence = answer.Confidence,
            Justification = answer.Justification,
            ContextSnapshot = answer.Context.Serialize(),
            ReversibilityScore = CalculateReversibility(answer)
        };
        
        _auditLog.Append(entry);
    }
}
```

##### Failure Mode: Agent Ignores Answer

**Scenario:** Agent doesn't accept the provided answer and asks again or differently.

**Detection:**
- Same question type detected in next turn
- Loop counter exceeds threshold

**Handling:**
```csharp
if (IsRepeatedQuestion(question, lastQuestion))
{
    _loopCounter++;
    
    if (_loopCounter > MaxLoopAttempts)
    {
        // Break loop - escalate to human
        await _notificationService.EscalateAsync(new Escalation
        {
            Type = EscalationType.AgentLoop,
            Question = question,
            AttemptHistory = _questionHistory,
            Message = "Agent not accepting autonomous answers"
        });
        
        return await WaitForHumanIntervention();
    }
    
    // Try reformulating answer
    return await ReformulateAnswer(question, lastAnswer);
}
```

##### Failure Mode: Security/Compliance Questions

**Scenario:** Question involves security, privacy, or compliance decisions.

**Detection:**
```csharp
private static readonly string[] SecurityKeywords = 
{
    "authentication", "authorization", "password", "secret", 
    "encryption", "token", "permission", "role", "access control",
    "compliance", "GDPR", "HIPAA", "PCI", "audit", "privacy"
};

public bool IsSecuritySensitive(ParsedQuestion question)
{
    return SecurityKeywords.Any(k => 
        question.Text.Contains(k, StringComparison.OrdinalIgnoreCase));
}
```

**Handling:**
```csharp
if (IsSecuritySensitive(question))
{
    // Apply stricter threshold
    if (answer.Confidence < Thresholds.SecurityMinimum) // 0.9
    {
        await EscalateWithPriority(question, Priority.High);
        return await WaitForHumanWithTimeout(TimeSpan.FromHours(1));
    }
    
    // Even if confident, log with security flag
    _securityLog.LogDecision(answer);
}
```

---

#### 9. Implementation Recommendations

##### Recommended Architecture

```csharp
public interface IAutonomousAnswerEngine
{
    Task<AnswerResult> TryAnswerAsync(ParsedQuestion question, CancellationToken ct);
}

public class AutonomousAnswerEngine : IAutonomousAnswerEngine
{
    private readonly IQuestionClassifier _classifier;
    private readonly IContextExtractor _contextExtractor;
    private readonly IMetaPromptService _metaPrompt;
    private readonly IConfidenceCalculator _confidence;
    private readonly IEscalationService _escalation;
    private readonly IDecisionAuditLog _auditLog;
    
    public async Task<AnswerResult> TryAnswerAsync(
        ParsedQuestion question, 
        CancellationToken ct)
    {
        // Step 1: Classify question
        var strategy = _classifier.ClassifyQuestion(question);
        
        if (strategy == AnswerStrategy.AlwaysEscalate)
            return AnswerResult.Escalate(reason: "Question type requires human input");
        
        // Step 2: Extract context
        var context = await _contextExtractor.ExtractContext(question, ct);
        
        // Step 3: Generate answer
        AnswerResult answer;
        
        if (strategy == AnswerStrategy.AcceptRecommendation)
        {
            answer = AnswerResult.FromRecommendation(question);
        }
        else if (strategy == AnswerStrategy.DeriveFromContext)
        {
            answer = DeriveFromContext(question, context);
        }
        else
        {
            answer = await _metaPrompt.GenerateAnswer(question, context, ct);
        }
        
        // Step 4: Calculate confidence
        answer.Confidence = _confidence.Calculate(question, context, answer);
        
        // Step 5: Apply threshold policy
        if (answer.Confidence < GetThreshold(question))
        {
            await _escalation.EscalateAsync(question, answer);
            return AnswerResult.Escalated(answer);
        }
        
        // Step 6: Log and return
        _auditLog.LogAutonomousDecision(answer);
        return answer;
    }
}
```

##### Phased Rollout Strategy

| Phase | Capability | Risk Level |
|-------|------------|------------|
| **1** | Accept agent recommendations (Types A, D) | Low |
| **2** | Accept validation confirmations (Type C) | Low |
| **3** | Meta-prompt for technical choices | Medium |
| **4** | Policy-based blocker recovery | Medium |
| **5** | Full autonomous mode (all types except requirements) | High |

### Key Discoveries

- **Agent recommendations are the primary autonomous answer source** — PCH agents already provide recommended options with rationale; the orchestrator's job is to evaluate and accept when appropriate, not generate novel answers.
- **Three-tier confidence system works best** — HIGH (proceed), MEDIUM (proceed with logging), LOW (escalate) provides practical balance between autonomy and safety.
- **Context hierarchy determines answer quality** — Vision document is the primary source of truth; standards and patterns provide supporting evidence.
- **ACP `ask_user` tool enables early interception** — Questions can be answered before the turn ends, enabling smoother multi-turn flow.
- **Meta-prompting adds a decision layer** — Using an LLM to evaluate agent recommendations against vision/standards provides intelligent filtering without human intervention.
- **Certain question types should never be autonomous** — Requirements capture, cost decisions, and high-risk security choices require human input regardless of confidence.
- **Loop detection is critical** — Agents may not accept autonomous answers; loop breakers prevent infinite retry cycles.

### Relevant Files/Resources

**Internal (Analyzed for Patterns):**
- [agents/pch-planner.md](agents/pch-planner.md) — Structured question format with A/B/C options
- [agents/pch-plan-reviewer.md](agents/pch-plan-reviewer.md) — Review question format with recommendations
- [agents/pch-visionary.md](agents/pch-visionary.md) — Elicitation, refinement, and validation question types
- [docs/vision/002-self-operating-development-loop.md](docs/vision/002-self-operating-development-loop.md) — FR-4 autonomous question answering requirement

**External Documentation:**
- [ACP Schema — Tool Requests](https://agentclientprotocol.com/protocol/schema) — `ask_user` tool handling
- [Copilot Spaces API](https://docs.github.com/en/copilot/how-tos/use-copilot-spaces) — Standards retrieval via MCP

### Gaps & Limitations

- **No runtime testing of meta-prompting** — Approach is theoretically sound but untested with actual LLM responses.
- **Token limits for context aggregation unknown** — Exact limits depend on chosen model for meta-prompting.
- **Standards space query performance** — Unknown latency for pch-standards-space queries in automated loop.
- **Human escalation UX undefined** — Phase 2 (external notifications) needed for complete escalation path.

### Assumptions

- **Agent recommendation quality is high** — Assuming PCH agents provide sound recommendations based on context.
- **Vision document is comprehensive** — Assuming user completed vision capture thoroughly before automation.
- **Standards space is accessible** — Assuming pch-standards-space is configured and available via MCP.
- **Single LLM provider for meta-prompting** — Assuming same Copilot/Claude model is used for meta-prompting.

---

## Phase 6: Document Attachment & State Passing

**Status:** ✅ Complete
**Scope:**
- Research how to attach documents to agent sessions via API
- Investigate SDK file/context attachment mechanisms
- Understand state persistence between phases via documents
- Document format and size limitations

### Research Tasks

- [x] Research SDK document/file attachment APIs
- [x] Investigate context window usage for attached documents
- [x] Explore file reference vs inline content approaches
- [x] Research multi-document attachment capabilities
- [x] Document size limits and chunking strategies
- [x] Design state serialization approach for phase handoffs

### Findings

#### 1. Two Document Attachment Mechanisms Available

Building on Phase 3's foundation, there are **two primary approaches** for attaching documents to agent sessions:

| Mechanism | Interface | Use Case | Token Impact |
|-----------|-----------|----------|--------------|
| **ACP Embedded Resources** | `ContentBlock::Resource` in `session/prompt` | Full document content in prompt | Full document tokens consumed |
| **CLI @file Reference** | `@path/to/file` syntax in prompt text | File path reference for agent to read | Minimal initial tokens; file read later |

---

#### 2. ACP Embedded Resources (ContentBlock::Resource)

The ACP protocol supports embedding document content directly in the prompt via `ContentBlock` objects:

**ContentBlock Types:**

```typescript
type ContentBlock = 
  | { type: "text"; text: string }
  | { type: "resource"; resource: EmbeddedResource };

interface EmbeddedResource {
  uri: string;          // e.g., "file:///docs/vision/001-feature.md"
  mimeType?: string;    // e.g., "text/markdown"
  text?: string;        // Inline content (for text resources)
  blob?: string;        // Base64 for binary (images, etc.)
}
```

**ACP Prompt with Embedded Document:**

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "session/prompt",
  "params": {
    "sessionId": "sess_abc123",
    "prompt": [
      {
        "type": "text",
        "text": "Continue implementing the plan. Start with Phase 3."
      },
      {
        "type": "resource",
        "resource": {
          "uri": "file:///docs/plans/001-feature.md",
          "mimeType": "text/markdown",
          "text": "# Implementation Plan\n\n## Overview\n...[full document content]..."
        }
      }
    ]
  }
}
```

**Key Behaviors:**

| Aspect | Behavior | Implication |
|--------|----------|-------------|
| Inline content | Full document text in `resource.text` | Tokens consumed immediately in context |
| URI purpose | Informational (agent knows source) | Enables agent to reference file path in responses |
| MIME type | Hints content type | Agent may format/interpret accordingly |
| Multiple resources | Array of ContentBlocks supported | Can attach vision + research + plan together |

---

#### 3. CLI @file Reference Syntax

The Copilot CLI supports `@path/to/file` references within prompts for lazy file loading:

**Syntax:**

```bash
# Single file reference
copilot --prompt "Implement the plan at @docs/plans/001-feature.md" --allow-all

# Multiple file references
copilot --prompt "Review @docs/vision/001-feature.md and create plan" --allow-all

# Relative and absolute paths work
copilot --prompt "Use @./docs/vision/001.md as reference" --allow-all
copilot --prompt "Read @C:/repos/project/docs/plan.md" --allow-all
```

**How @file Works:**

1. Agent receives prompt with `@path/to/file` syntax
2. Agent recognizes this as a file reference
3. Agent uses `read_file` tool to load content when needed
4. File content enters context at read time (not upfront)

**Comparison:**

| Approach | Token Timing | Control | Best For |
|----------|--------------|---------|----------|
| Embedded Resource | Immediate (full content upfront) | Orchestrator controls exactly what agent sees | Essential documents that MUST be read |
| @file Reference | Deferred (agent reads when needed) | Agent decides what/when to read | Large docs or optional references |

---

#### 4. PCH Document Size Analysis

**Measured Document Sizes (Current Repository):**

| Document Type | Size Range | Token Estimate* | Context Impact |
|---------------|------------|-----------------|----------------|
| **Vision docs** | 16-19 KB | 4,000-5,000 tokens | Moderate — essential context |
| **Research docs** | 12-115 KB | 3,000-29,000 tokens | Heavy — may need summarization |
| **Plans** | 7-47 KB | 1,750-12,000 tokens | Moderate to heavy |
| **Agent prompts** | 14-52 KB | 3,500-13,000 tokens | System prompt overhead |

*Estimate: ~4 characters = 1 token (rough heuristic for English text)

**Largest Documents:**

| Document | Size | Lines | Token Estimate |
|----------|------|-------|----------------|
| 005-pch-visionary-agent-design.md | 115.5 KB | 3,131 | ~29,000 tokens |
| 008-self-operating-development-loop.md | 111.2 KB | 2,838 | ~28,000 tokens |
| 002-beta-pch-researcher-subagent-integration.md | 80.5 KB | 1,935 | ~20,000 tokens |
| 006-pch-agent-presentation-research.md | 74.3 KB | 2,338 | ~18,500 tokens |

**Context Budget Analysis:**

| Component | Typical Tokens | Notes |
|-----------|----------------|-------|
| Agent system prompt | 8,000-13,000 | PCH agents are detailed |
| Vision document | 4,000-5,000 | Always needed |
| Research document | 3,000-29,000 | Often large; may need summarization |
| Plan document | 1,750-12,000 | Varies by plan complexity |
| Working space for agent | 10,000-30,000 | Tool outputs, reasoning, generation |
| **Total available** | ~128,000-200,000 | Depends on model (Claude 200K, GPT-4 128K) |

---

#### 5. Context Window Limits and Strategies

**Model Context Limits (As of Feb 2026):**

| Model | Context Window | Practical Limit* |
|-------|----------------|------------------|
| Claude 3.5 / Opus 4.5 | 200,000 tokens | ~180,000 (leave headroom) |
| GPT-4 Turbo | 128,000 tokens | ~115,000 |
| GPT-4o | 128,000 tokens | ~115,000 |

*Practical limit = leave 10-15% for tool outputs and generation

**Document Attachment Strategies:**

##### Strategy A: Full Embedding (Small Documents)

```
Suitable when: Total document size < 30,000 tokens
Approach: Embed all documents as ContentBlock::Resource
Pros: Agent has full context immediately
Cons: Consumes context upfront
```

```csharp
// .NET Implementation Pattern
var prompt = new List<ContentBlock>
{
    new TextBlock { Text = "Continue implementing Phase 3 of the plan" },
    new ResourceBlock 
    { 
        Uri = "file:///docs/vision/001-feature.md",
        MimeType = "text/markdown",
        Text = File.ReadAllText(visionPath)
    },
    new ResourceBlock 
    { 
        Uri = "file:///docs/plans/001-feature.md",
        MimeType = "text/markdown",
        Text = File.ReadAllText(planPath)
    }
};
```

##### Strategy B: Selective Embedding (Medium Documents)

```
Suitable when: Some documents are large (30-80K tokens total)
Approach: Embed essential docs; reference others with @file
Pros: Balance between context and flexibility
Cons: Agent may not read referenced files
```

```csharp
// Embed vision + current phase section; reference full plan
var planSection = ExtractPhaseSection(planContent, currentPhase);

var prompt = new List<ContentBlock>
{
    new TextBlock { Text = $"Implement Phase {currentPhase}. Full plan at @docs/plans/001-feature.md" },
    new ResourceBlock { Text = visionContent },  // Full vision (4-5K tokens)
    new ResourceBlock { Text = planSection }     // Just current phase (1-3K tokens)
};
```

##### Strategy C: Summary + Reference (Large Documents)

```
Suitable when: Documents exceed 80,000 tokens
Approach: Embed summaries; use @file for details
Pros: Fits in context window
Cons: Agent may miss nuances; requires summarization logic
```

```csharp
// Summarize large research doc, embed summary
var researchSummary = SummarizeDocument(researchContent, maxTokens: 3000);

var prompt = new List<ContentBlock>
{
    new TextBlock { Text = "Implement Phase 3. Full research at @docs/research/001.md" },
    new ResourceBlock { Text = visionContent },     // Full (small)
    new ResourceBlock { Text = researchSummary },   // Summary
    new ResourceBlock { Text = planContent }        // Full plan
};
```

---

#### 6. Multi-Document Attachment Patterns

**Phase-to-Document Mapping:**

| Workflow Phase | Documents Needed | Attachment Strategy |
|----------------|------------------|---------------------|
| **pch-visionary** | Feature request only | Text prompt, no documents |
| **pch-researcher** | Vision document | Embed vision (~5K tokens) |
| **pch-planner** | Vision + Research | Embed vision + research summary |
| **pch-plan-reviewer** | Vision + Research + Plan | Selective embed (may need summarization) |
| **pch-coder** | Vision + Plan (research optional) | Embed vision + current phase section |

**Multi-Document ACP Example:**

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "session/prompt",
  "params": {
    "sessionId": "sess_xyz789",
    "prompt": [
      {
        "type": "text",
        "text": "You are pch-planner. Create an implementation plan based on the vision and research."
      },
      {
        "type": "resource",
        "resource": {
          "uri": "file:///docs/vision/001-feature.md",
          "mimeType": "text/markdown",
          "text": "[Full vision document content ~5K tokens]"
        }
      },
      {
        "type": "resource",
        "resource": {
          "uri": "file:///docs/research/001-feature.md",
          "mimeType": "text/markdown",
          "text": "[Research summary or full content ~15K tokens]"
        }
      }
    ]
  }
}
```

---

#### 7. State Persistence via Documents (Existing PCH Pattern)

**Critical Insight:** PCH agents already use documents as the state persistence mechanism. This is the established pattern the orchestrator should follow.

**Current PCH State Transfer Pattern:**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Document-Based State Persistence                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Phase 1: pch-visionary                                                      │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │  Input: User's feature request (text)                          │         │
│  │  Output: /docs/vision/001-feature.md                           │         │
│  │                                                                 │         │
│  │  State persisted in document:                                   │         │
│  │  • Requirements, goals, constraints                             │         │
│  │  • Non-goals, out of scope items                                │         │
│  │  • Research topics identified                                   │         │
│  │  • Session log of Q&A decisions                                 │         │
│  └────────────────────────────────────────────────────────────────┘         │
│                              │                                               │
│                              ▼ (vision doc passed)                           │
│  Phase 2: pch-researcher                                                     │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │  Input: Vision document                                         │         │
│  │  Output: /docs/research/001-feature.md                          │         │
│  │                                                                 │         │
│  │  State persisted in document:                                   │         │
│  │  • Phased findings (each phase's discoveries)                   │         │
│  │  • Key discoveries synthesized                                  │         │
│  │  • Files analyzed, external sources                             │         │
│  │  • Progress status per phase                                    │         │
│  └────────────────────────────────────────────────────────────────┘         │
│                              │                                               │
│                              ▼ (vision + research docs passed)               │
│  Phase 3: pch-planner                                                        │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │  Input: Vision + Research documents                             │         │
│  │  Output: /docs/plans/001-feature.md                             │         │
│  │                                                                 │         │
│  │  State persisted in document:                                   │         │
│  │  • Phased implementation plan (tasks per phase)                 │         │
│  │  • Technical decisions made (with rationale)                    │         │
│  │  • Session log of planning Q&A                                  │         │
│  │  • Dependencies and prerequisites                               │         │
│  └────────────────────────────────────────────────────────────────┘         │
│                              │                                               │
│                              ▼ (all docs passed for review)                  │
│  [Continues through pch-plan-reviewer → pch-coder]                           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Why Documents Work as State:**

| Benefit | Explanation |
|---------|-------------|
| **Persistence** | Survives session boundaries, process restarts |
| **Visibility** | Human-readable, can be reviewed/edited |
| **Version control** | Git tracks changes, enables rollback |
| **Tool compatibility** | All agents have file read/write tools |
| **Size flexibility** | No message size limits; unlimited detail |

---

#### 8. State Serialization Format Design

For the self-operating loop, the orchestrator needs to track additional state beyond what's in documents:

**Orchestrator State Schema:**

```json
{
  "workflowId": "feature-001-auth-system",
  "startedAt": "2026-02-05T10:00:00Z",
  "currentPhase": {
    "agent": "pch-planner",
    "stage": "planning",
    "startedAt": "2026-02-05T14:30:00Z",
    "sessionId": "sess_abc123"
  },
  "documents": {
    "vision": "/docs/vision/001-auth-system.md",
    "research": "/docs/research/001-auth-system.md",
    "plan": null
  },
  "phaseHistory": [
    {
      "agent": "pch-visionary",
      "status": "complete",
      "startedAt": "2026-02-05T10:00:00Z",
      "completedAt": "2026-02-05T11:30:00Z",
      "outputDocument": "/docs/vision/001-auth-system.md",
      "questionsAnswered": 12,
      "autonomousAnswers": 8,
      "escalatedAnswers": 4
    },
    {
      "agent": "pch-researcher",
      "status": "complete",
      "phases": [
        { "phase": 1, "status": "complete" },
        { "phase": 2, "status": "complete" },
        { "phase": 3, "status": "complete" }
      ]
    }
  ],
  "decisionLog": [
    {
      "timestamp": "2026-02-05T11:15:00Z",
      "question": "Which auth pattern?",
      "answer": "A — JWT with HTTP-only cookies",
      "confidence": 0.85,
      "method": "accept_recommendation"
    }
  ],
  "metrics": {
    "totalAutonomousTime": "PT4H30M",
    "totalQuestions": 47,
    "autonomouslyAnswered": 39,
    "humanAnswered": 8
  }
}
```

**State File Location:**

```
/docs/
  vision/001-auth-system.md       # Agent output
  research/001-auth-system.md     # Agent output
  plans/001-auth-system.md        # Agent output
  
.orchestrator/
  feature-001-auth-system.state.json    # Orchestrator state
  feature-001-auth-system.log           # Detailed execution log
```

---

#### 9. Chunking Strategies for Large Documents

When documents exceed practical context limits, apply chunking:

**Chunking Decision Flow:**

```
Document loaded
       │
       ▼
┌──────────────────────────────────┐
│ Estimate tokens (size / 4)       │
└──────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────┐
│ tokens < 10,000?                 │────YES───▶ Embed full document
└──────────────────────────────────┘
       │ NO
       ▼
┌──────────────────────────────────┐
│ tokens < 30,000?                 │────YES───▶ Embed, but monitor usage
└──────────────────────────────────┘
       │ NO
       ▼
┌──────────────────────────────────┐
│ Is it a research/plan document?  │
└──────────────────────────────────┘
       │ YES                │ NO
       ▼                    ▼
┌────────────────┐  ┌────────────────────────────────┐
│ Extract current│  │ Summarize using LLM or         │
│ phase section  │  │ extractive summarization       │
└────────────────┘  └────────────────────────────────┘
```

**Phase-Section Extraction Pattern:**

```csharp
public string ExtractPhaseSection(string planContent, int phaseNumber)
{
    // Extract just the section for the current phase
    var pattern = $@"## Phase {phaseNumber}:.*?(?=## Phase \d+:|## Handoff|$)";
    var match = Regex.Match(planContent, pattern, RegexOptions.Singleline);
    
    if (match.Success)
    {
        // Include header context
        var overview = ExtractOverviewSection(planContent);
        return $"{overview}\n\n{match.Value}";
    }
    
    return planContent; // Fallback to full document
}
```

**Summarization Pattern:**

```csharp
public async Task<string> SummarizeForContext(
    string document, 
    int maxTokens, 
    string focusArea)
{
    // Use LLM to create focused summary
    var summaryPrompt = $"""
        Summarize the following document for use as context in a {focusArea} task.
        Keep the summary under {maxTokens} tokens.
        Focus on: key decisions, requirements, constraints, and actionable information.
        
        Document:
        {document}
        """;
    
    return await _llm.Complete(summaryPrompt);
}
```

---

#### 10. Implementation Recommendations

**Recommended Document Attachment Strategy for Self-Operating Loop:**

| Phase Transition | Source Agent | Target Agent | Attachment Strategy |
|------------------|--------------|--------------|---------------------|
| Initiation → Vision | User | pch-visionary | Text prompt only |
| Vision → Research | pch-visionary | pch-researcher | Embed vision (full) |
| Research → Planning | pch-researcher | pch-planner | Embed vision + research overview + key discoveries |
| Planning → Review | pch-planner | pch-plan-reviewer | Embed vision + plan + research summary |
| Review → Coding | pch-plan-reviewer | pch-coder | Embed vision + plan (phase sections) |

**.NET Implementation Pattern:**

```csharp
public class DocumentAttachmentService
{
    private readonly string _workspacePath;
    
    public async Task<List<ContentBlock>> BuildPhasePrompt(
        WorkflowState state,
        string targetAgent,
        string instruction)
    {
        var blocks = new List<ContentBlock>();
        
        // Always start with instruction
        blocks.Add(new TextBlock { Text = instruction });
        
        // Add documents based on target agent needs
        switch (targetAgent)
        {
            case "pch-researcher":
                // Just vision
                blocks.Add(await EmbedDocument(state.Documents.Vision));
                break;
                
            case "pch-planner":
                // Vision + research (summarized if large)
                blocks.Add(await EmbedDocument(state.Documents.Vision));
                blocks.Add(await EmbedOrSummarize(state.Documents.Research, 15000));
                break;
                
            case "pch-coder":
                // Vision + current phase of plan
                blocks.Add(await EmbedDocument(state.Documents.Vision));
                blocks.Add(await EmbedPhaseSection(state.Documents.Plan, state.CurrentPhase));
                break;
        }
        
        return blocks;
    }
    
    private async Task<ContentBlock> EmbedDocument(string path)
    {
        var content = await File.ReadAllTextAsync(Path.Combine(_workspacePath, path));
        return new ResourceBlock
        {
            Uri = $"file:///{path}",
            MimeType = "text/markdown",
            Text = content
        };
    }
    
    private async Task<ContentBlock> EmbedOrSummarize(string path, int maxTokens)
    {
        var content = await File.ReadAllTextAsync(Path.Combine(_workspacePath, path));
        var estimatedTokens = content.Length / 4;
        
        if (estimatedTokens <= maxTokens)
        {
            return new ResourceBlock { Text = content };
        }
        
        // Summarize
        var summary = await _summarizer.Summarize(content, maxTokens);
        return new ResourceBlock 
        { 
            Text = $"[Summary of {path}]\n\n{summary}\n\n[Full document available at @{path}]"
        };
    }
    
    private async Task<ContentBlock> EmbedPhaseSection(string planPath, int phase)
    {
        var content = await File.ReadAllTextAsync(Path.Combine(_workspacePath, planPath));
        var section = ExtractPhaseSection(content, phase);
        
        return new ResourceBlock
        {
            Uri = $"file:///{planPath}#phase-{phase}",
            MimeType = "text/markdown",
            Text = section
        };
    }
}
```

### Key Discoveries

- **Two attachment mechanisms available** — ACP `ContentBlock::Resource` for immediate embedding; CLI `@file` syntax for deferred reading. Both work; choose based on control needs.
- **PCH documents are moderately sized** — Vision docs ~5K tokens, plans ~2-12K tokens, research docs can exceed 25K tokens. Most fit comfortably in modern context windows.
- **Documents ARE the state** — PCH agents already use documents as state persistence. Orchestrator extends this with metadata state file.
- **Selective embedding is optimal** — Embed essential documents (vision, current phase); reference or summarize large documents (full research).
- **Phase-section extraction reduces context** — For pch-coder, extract only the current phase section from plans rather than embedding full document.
- **Multi-document attachment works via ContentBlock array** — ACP supports multiple `ContentBlock::Resource` entries in a single prompt.
- **Token estimation: ~4 chars per token** — Use this heuristic for budgeting; leaves headroom for agent reasoning and tool outputs.
- **Orchestrator state is separate from document state** — Track workflow progress, decision log, and metrics in `.orchestrator/` directory; agent outputs remain in `/docs/`.

### Relevant Files/Resources

**Internal (Analyzed for Patterns):**
- [agents/pch-researcher.md](agents/pch-researcher.md) — Document attachment detection patterns (lines 84-130)
- [agents/pch-visionary.md](agents/pch-visionary.md) — Vision document structure and state
- [.github/copilot-instructions.md](.github/copilot-instructions.md) — Document-based persistence principle
- [docs/vision/002-self-operating-development-loop.md](docs/vision/002-self-operating-development-loop.md) — FR-2 pass documents requirement
- Phase 3 findings in this document — `ContentBlock::Resource` discovery

**Document Size Analysis:**
- Vision docs: 16-19 KB (387-400 lines)
- Research docs: 12-115 KB (283-3131 lines)
- Plans: 7-47 KB (205-1156 lines)
- Agent prompts: 14-52 KB (335-1584 lines)

**External Documentation:**
- [ACP Schema — ContentBlock](https://agentclientprotocol.com/protocol/schema) — Prompt content types
- [ACP Protocol Overview](https://agentclientprotocol.com/protocol/overview) — Resource embedding
- [Copilot CLI Command Reference](https://docs.github.com/en/copilot/reference/cli-command-reference) — @file syntax

### Gaps & Limitations

- **No explicit ACP size limits documented** — Assuming standard LLM context limits apply; no protocol-level restrictions found
- **Summarization quality untested** — LLM summarization for large documents is theoretical; may lose critical details
- **Binary resource handling unexplored** — ContentBlock supports `blob` for images/binary; not investigated for this use case
- **@file reliability unknown** — Agent may not always read @file references; embedded resources are more guaranteed

### Assumptions

- **ACP ContentBlock format is stable** — Assuming the `type: resource` format remains consistent
- **Token estimation is approximately correct** — Using 4 chars/token; actual ratio varies by content
- **Most workflows fit in context** — Assuming typical vision+plan+working space stays under 100K tokens
- **File paths are stable** — Documents remain at predictable paths during workflow execution

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|

*Standards will be queried from pch-standards-space during research phases.*

---

## Conclusions

### Technical Feasibility: CONFIRMED ✅

The Self-Operating Development Loop is technically feasible using existing GitHub Copilot infrastructure. While the original vision assumed a .NET Copilot SDK that doesn't exist, the Copilot CLI with ACP protocol provides an excellent alternative that supports all required capabilities.

### Recommended Architecture

A .NET Core console application that:
1. **Wraps Copilot CLI** as a subprocess using ACP over stdio
2. **Implements JSON-RPC 2.0** client for structured communication
3. **Creates fresh sessions** via `session/new` for each phase (Ralph Wiggum pattern)
4. **Embeds documents** via `ContentBlock::Resource` for state transfer
5. **Detects completion** using emoji pattern matching + ACP `stopReason`
6. **Answers questions autonomously** by accepting agent recommendations when confident

### Implementation Recommendations

1. **Start with MVP using text parsing** — Simpler implementation for proof of concept
2. **Graduate to ACP** — More reliable for production use
3. **Implement confidence thresholds** — Accept agent recommendations ≥0.8 confidence automatically
4. **Log all decisions** — Create audit trail for autonomous answers
5. **Design for recovery** — Persist state frequently; support resume from any phase

### Open Questions for Planning Phase

1. How to handle multi-phase plans within pch-coder (phases within phases)?
2. What's the optimal retry strategy for failed phases?
3. Should the orchestrator support parallel research phases?
4. How to implement external notifications (Phase 2 feature)?

### Research Success Criteria Met

| Original Question | Answered? | Summary |
|-------------------|-----------|---------|
| Can custom agents be invoked headlessly? | ✅ Yes | Copilot CLI `--prompt` + `--agent` |
| How to spawn fresh sessions? | ✅ Yes | ACP `session/new`; each CLI invocation |
| How to detect phase completion? | ✅ Yes | Emoji patterns + stopReason |
| How to attach documents? | ✅ Yes | ContentBlock::Resource or @file |
| How to answer questions autonomously? | ✅ Yes | Accept recommendations + meta-prompting |

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** 2026-02-05
**Status:** ✅ Complete — All Phases Done
**Total Phases:** 6 of 6
**Document Location:** `/docs/research/008-self-operating-development-loop.md`

**Research Duration:** 1 day (all 6 phases)

**Key Deliverables:**
- Architecture pivot from SDK to CLI wrapping documented
- ACP protocol integration patterns defined
- Completion detection regex library provided
- Autonomous question answering strategy designed
- Document attachment patterns with sizing analysis

**Ready for:** pch-planner to create implementation plan
