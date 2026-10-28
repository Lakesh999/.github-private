# Research: GitHub Copilot SDK Integration for Hugh

## Document Information

| Field | Value |
|-------|-------|
| Document | 009-github-copilot-sdk-integration.md |
| Created | 2026-02-05 |
| Status | ✅ Complete |
| Current Phase | 5 of 5 (Complete) |

## Overview

This research document provides a comprehensive guide for integrating the GitHub.Copilot.SDK (v0.1.22) into the Hugh self-operating development loop to resolve all TODO stubs in CopilotService.cs, PhaseExecutor.cs, and Program.cs.

### Key Findings Summary

1. **SDK is publicly available** — GitHub.Copilot.SDK v0.1.22 on nuget.org (MIT license, .NET 8.0+)
2. **CopilotClient manages CLI lifecycle** — Auto-spawns, communicates via JSON-RPC/stdio, auto-restarts on crash
3. **SessionConfig enables agent customization** — SystemMessageConfig.Append loads PCH agent prompts; OnUserInputRequest handles questions
4. **MessageOptions.Attachments for documents** — Maps directly from existing FileAttachment record
5. **Event-based response handling** — AssistantMessageDeltaEvent for streaming; SessionIdleEvent for completion
6. **SignalClassifier integration works unchanged** — Pass accumulated response to Classify(); OnUserInputRequest intercepts questions
7. **Existing timeout pattern (CreateLinkedTokenSource + CancelAfter) integrates seamlessly** — No changes to PhaseExecutor needed
8. **Retry with exponential backoff for transient errors** — SessionErrorEvent classification determines recoverability

### TODO Resolution Map

| File | Line | TODO | Resolution |
|------|------|------|------------|
| CopilotService.cs | 65 | Integrate with SDK | Create CopilotClient + CopilotSession per phase |
| CopilotService.cs | 79 | Parse response | SdkResponseProcessor with SessionIdleEvent pattern |
| CopilotService.cs | 91 | Extract OutputDocumentPath | DocumentPathExtractor with 4 regex patterns |
| CopilotService.cs | 130 | Replace SimulateAgentExecutionAsync | Full SDK integration with event handling |
| PhaseExecutor.cs | 248 | Parse agent response | Use DocumentPathExtractor.ExtractDocumentPath() |
| Program.cs | 206, 236 | Apply timeout override | ApplyTimeoutOverride() with TimeSpan.TryParse |

### Architecture Summary

```
┌─────────────────────────────────────────────────────────────┐
│                     Hugh Application                        │
├─────────────────────────────────────────────────────────────┤
│  LoopOrchestrator                                           │
│    └── PhaseExecutor                                        │
│          ├── GetPhaseTimeout() → CreateLinkedTokenSource    │
│          └── ICopilotService.ExecutePhaseAsync()            │
│                                                             │
│  CopilotService (ICopilotService)                           │
│    ├── CopilotClient (SDK) → Manages CLI lifecycle          │
│    ├── CopilotSession (SDK) → Per-phase session             │
│    │     ├── SessionConfig.SystemMessage → Agent prompt     │
│    │     ├── SessionConfig.OnUserInputRequest → Questions   │
│    │     └── session.SendAsync(MessageOptions)              │
│    │                                                        │
│    ├── SdkResponseProcessor                                 │
│    │     ├── AssistantMessageDeltaEvent → Accumulate        │
│    │     ├── SessionIdleEvent → Classify + Extract path     │
│    │     └── SessionErrorEvent → Exception + retry logic    │
│    │                                                        │
│    ├── SignalClassifier → Unchanged from existing           │
│    └── DocumentPathExtractor → New, 4 regex patterns        │
├─────────────────────────────────────────────────────────────┤
│  GitHub.Copilot.SDK (v0.1.22)                               │
│    └── Copilot CLI (spawned process, auto-restart)          │
│          └── LLM + MCP Tools                                │
└─────────────────────────────────────────────────────────────┘
```

### Implementation Priority

1. **Add SDK package** — `dotnet add package GitHub.Copilot.SDK`
2. **Create SdkResponseProcessor** — Event handling + signal classification
3. **Create DocumentPathExtractor** — Path extraction with 4 patterns
4. **Update CopilotService.ExecutePhaseAsync** — Full SDK integration
5. **Add timeout override** — Program.cs ApplyTimeoutOverride()
6. **Add retry logic** — ExecuteWithRetryAsync with exponential backoff

### Actionable Conclusions

- **SDK is production-ready for internal use** — Technical preview, but stable for Hugh
- **Existing patterns preserved** — SignalClassifier, QuestionHandler, BlockerHandler unchanged
- **Timeout architecture remains intact** — PhaseExecutor patterns work with SDK
- **OnUserInputRequest is the autonomy key** — Intercept and auto-respond to agent questions
- **InfiniteSessionConfig recommended** — Auto-compaction prevents context overflow

### Open Questions

- SDK version stability — v0.1.22 is technical preview; monitor for breaking changes
- Custom tool support — Not needed for Hugh, but available via Microsoft.Extensions.AI if future phases require it

---

## Research Objectives

Investigate how to integrate the GitHub.Copilot.SDK prerelease package into the Hugh project to resolve the remaining TODO stubs in CopilotService.cs, PhaseExecutor.cs, and Program.cs.

### Key Questions to Answer

1. What is the current GitHub.Copilot.SDK package structure, version, and NuGet source?
2. What are the core SDK APIs for session creation, message sending, and document attachment?
3. How does the SDK handle user input requests (OnUserInputRequest callback)?
4. How do we extract output document paths from agent responses?
5. What is the proper cancellation and timeout handling pattern with the SDK?

### TODOs to Resolve

| File | Line | TODO Description |
|------|------|------------------|
| CopilotService.cs | 65 | Integrate with GitHub.Copilot.SDK when available |
| CopilotService.cs | 79 | Parse actual response from SDK to extract decisions/paths |
| CopilotService.cs | 91 | Extract OutputDocumentPath from SDK response |
| CopilotService.cs | 130 | Replace SimulateAgentExecutionAsync with actual SDK calls |
| PhaseExecutor.cs | 248 | Parse agent response to extract document path |
| Program.cs | 206, 236 | Apply timeout override to config |

---

## Research Phases

| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 1 | SDK Package Discovery | ✅ Complete | 2026-02-05 |
| 2 | Session & Client API | ✅ Complete | 2026-02-05 |
| 3 | Message & Attachment Handling | ✅ Complete | 2026-02-05 |
| 4 | Response Parsing & Signal Detection | ✅ Complete | 2026-02-05 |
| 5 | Cancellation, Timeouts & Error Handling | ✅ Complete | 2026-02-05 |

---

## Phase 1: SDK Package Discovery

**Status:** ✅ Complete
**Scope:** 
- Locate the GitHub.Copilot.SDK prerelease package (NuGet source, version)
- Identify any dependencies or prerequisites
- Determine package installation steps
- Review package structure and namespaces

### Findings

#### Package Information

| Property | Value |
|----------|-------|
| **Package Name** | `GitHub.Copilot.SDK` |
| **Current Version** | 0.1.22 |
| **NuGet Source** | nuget.org (public) |
| **Repository** | [github.com/github/copilot-sdk](https://github.com/github/copilot-sdk/tree/main/dotnet) |
| **License** | MIT |
| **Status** | Technical Preview (may have breaking changes) |
| **Target Framework** | .NET 8.0 or later |
| **Prefix Reserved** | Yes (official GitHub package) |

#### Installation

Add the package to the Hugh project:

```bash
dotnet add package GitHub.Copilot.SDK
```

Or add to `Huge.csproj`:

```xml
<PackageReference Include="GitHub.Copilot.SDK" Version="0.1.22" />
```

#### Prerequisites

1. **.NET 8.0 or later** — Hugh already targets `net8.0` ✅
2. **GitHub Copilot CLI installed and in PATH** — Required dependency
   - The SDK spawns and manages the Copilot CLI as a child process
   - Can be configured via `CopilotClientOptions.CliPath` if not in PATH
3. **GitHub Copilot subscription** — For authentication (or BYOK configuration)
4. **Authentication** — One of:
   - Logged-in GitHub user (`UseLoggedInUser = true`, default)
   - Explicit `GithubToken` in options
   - BYOK via `ProviderConfig`

#### SDK Architecture

The SDK acts as a wrapper around the Copilot CLI:

```
┌─────────────────────────────────────────────────┐
│           Hugh .NET Application                 │
│                                                 │
│   ┌─────────────────────────────────────────┐   │
│   │        GitHub.Copilot.SDK               │   │
│   │                                         │   │
│   │  CopilotClient                          │   │
│   │    ├── StartAsync()                     │   │
│   │    ├── CreateSessionAsync()             │   │
│   │    └── StopAsync()                      │   │
│   │                                         │   │
│   │  CopilotSession                         │   │
│   │    ├── SendAsync()                      │   │
│   │    ├── On() (event subscription)        │   │
│   │    ├── AbortAsync()                     │   │
│   │    └── DisposeAsync()                   │   │
│   └─────────────────────────────────────────┘   │
│                     │                           │
│              JSON-RPC / stdio                   │
│                     │                           │
│   ┌─────────────────────────────────────────┐   │
│   │     Copilot CLI (managed by SDK)        │   │
│   └─────────────────────────────────────────┘   │
│                     │                           │
│                 LLM + Tools                     │
└─────────────────────────────────────────────────┘
```

**Key architectural points:**
- SDK spawns Copilot CLI as a child process automatically
- Communication via JSON-RPC over stdio (default) or TCP
- Auto-restart on crash (`AutoRestart = true` by default)
- Session state persisted to `~/.copilot/session-state/{sessionId}/`

#### Primary Namespaces and Types

| Namespace/Type | Purpose |
|----------------|---------|
| `GitHub.Copilot.SDK` | Main namespace for all SDK types |
| `CopilotClient` | Main entry point, manages CLI lifecycle and sessions |
| `CopilotClientOptions` | Configuration for the client (paths, ports, auth) |
| `CopilotSession` | Represents a conversation session |
| `SessionConfig` | Configuration for creating sessions (model, hooks, etc.) |
| `MessageOptions` | Options for sending messages (prompt, attachments) |
| `SessionEvent` | Base class for all session events |
| `SystemMessageConfig` | System prompt customization |
| `SessionHooks` | Hook handlers for session lifecycle |
| `InfiniteSessionConfig` | Context compaction configuration |
| `UserInputResponse` | Response type for OnUserInputRequest callback |

#### Event Types

Sessions emit strongly-typed events via pattern matching:

| Event Type | Description |
|------------|-------------|
| `UserMessageEvent` | User message added to session |
| `AssistantMessageEvent` | Assistant response (final) |
| `AssistantMessageDeltaEvent` | Streaming response chunk |
| `ToolExecutionStartEvent` | Tool execution started |
| `ToolExecutionCompleteEvent` | Tool execution completed |
| `SessionStartEvent` | Session started |
| `SessionIdleEvent` | Session finished processing (completion marker) |
| `SessionErrorEvent` | Error occurred |
| `SessionCompactionStartEvent` | Context compaction started |
| `SessionCompactionCompleteEvent` | Compaction finished |

#### Optional Dependencies for Custom Tools

If using custom tools with `AIFunctionFactory.Create`:
- `Microsoft.Extensions.AI` — For type-safe tool definitions
- `System.ComponentModel` — For `[Description]` attributes

#### Current Huge.csproj Missing Packages

The SDK needs to be added to Huge.csproj. Current packages:
- Serilog (logging) ✅
- Microsoft.Extensions.Configuration ✅
- System.CommandLine ✅
- **GitHub.Copilot.SDK** ❌ (needs to be added)

### Key Discoveries

- **Package is publicly available on nuget.org** — Version 0.1.22 as of Feb 2026
- **Technical Preview status** — May have breaking changes; abstraction layer recommended
- **No special NuGet source required** — Standard nuget.org feed suffices
- **Prerequisites:** .NET 8.0 (satisfied) + Copilot CLI in PATH
- **MIT licensed** — No licensing concerns for internal use
- **SDK manages CLI lifecycle** — Automatic spawn, restart, and cleanup
- **Prefix reserved** — Official GitHub package with verified ownership

### Relevant Files/Resources

- [Huge.csproj](Hugh/Huge.csproj) — Project file to add SDK reference
- [CopilotService.cs](Hugh/Agents/CopilotService.cs) — Primary file for SDK integration
- [NuGet Package](https://www.nuget.org/packages/GitHub.Copilot.SDK) — Official package page
- [GitHub Repository](https://github.com/github/copilot-sdk/tree/main/dotnet) — SDK source and documentation
- [docs/research/008-self-operating-development-loop.md](docs/research/008-self-operating-development-loop.md) — Previous research with SDK details

---

## Phase 2: Session & Client API

**Status:** ✅ Complete
**Scope:**
- How to create a CopilotClient or equivalent
- How to configure sessions (SessionConfig, SystemMessage, Model selection)
- How to load agent prompts as system messages
- Session lifecycle management (async disposal)

### Findings

#### 1. CopilotClient Creation & Lifecycle

The `CopilotClient` is the main entry point for SDK interactions. It manages the Copilot CLI process lifecycle.

**Basic Client Creation:**

```csharp
using GitHub.Copilot.SDK;

// Default options - uses logged-in GitHub user, finds CLI in PATH
await using var client = new CopilotClient();
await client.StartAsync();

// ... use client ...

// Explicit shutdown (or let IAsyncDisposable handle it)
await client.StopAsync();
```

**With Custom Options:**

```csharp
var options = new CopilotClientOptions
{
    // Path to Copilot CLI if not in PATH
    CliPath = @"C:\Users\me\.local\bin\copilot",
    
    // Authentication options
    UseLoggedInUser = true,        // Default: use GitHub CLI's logged-in user
    // GithubToken = "ghp_...",    // Alternative: explicit token
    
    // Connection options  
    Port = 8080,                   // Custom port (default: auto-assigned)
    ConnectionMode = ConnectionMode.Stdio, // Stdio (default) or Tcp
    
    // Resilience
    AutoRestart = true,            // Auto-restart CLI on crash (default: true)
    
    // BYOK (Bring Your Own Key) for custom providers
    // ProviderConfig = new ProviderConfig { ... }
};

await using var client = new CopilotClient(options);
await client.StartAsync();
```

**CopilotClientOptions Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CliPath` | `string?` | null (PATH) | Custom path to Copilot CLI executable |
| `UseLoggedInUser` | `bool` | true | Use GitHub CLI's authenticated user |
| `GithubToken` | `string?` | null | Explicit GitHub token (alternative to logged-in user) |
| `Port` | `int?` | auto | Custom port for CLI communication |
| `ConnectionMode` | `ConnectionMode` | `Stdio` | `Stdio` (default) or `Tcp` |
| `AutoRestart` | `bool` | true | Auto-restart CLI if it crashes |
| `ProviderConfig` | `ProviderConfig?` | null | BYOK configuration for custom LLM providers |

#### 2. Session Creation with SessionConfig

Sessions represent isolated conversation contexts. Each `CreateSessionAsync()` call creates a fresh session with no prior history.

**Basic Session Creation:**

```csharp
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "claude-sonnet-4-5"
});
```

**Full SessionConfig Options:**

```csharp
var agentPrompt = await File.ReadAllTextAsync("agents/pch-coder.md");

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    // Model selection
    Model = "claude-sonnet-4-5",  // or "gpt-5", "claude-opus-4-5", etc.
    
    // System message (agent prompt)
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,  // Add to default Copilot prompt
        Content = agentPrompt             // PCH agent prompt content
    },
    
    // User input interception (for autonomous answering)
    OnUserInputRequest = async (request, invocation) =>
    {
        return new UserInputResponse
        {
            Answer = "Proceed with the recommended option",
            WasFreeform = true
        };
    },
    
    // Session lifecycle hooks
    Hooks = new SessionHooks
    {
        OnPreToolUse = async (input, invocation) =>
        {
            // Intercept before tool execution
            return new PreToolUseHookOutput
            {
                PermissionDecision = "allow"  // "allow", "deny", "ask"
            };
        },
        OnPostToolUse = async (input, invocation) =>
        {
            // Post-process tool results
            return new PostToolUseHookOutput();
        },
        OnSessionEnd = async (input, invocation) =>
        {
            // Cleanup on session end
            return null;
        }
    },
    
    // Infinite session (auto context compaction)
    InfiniteSession = new InfiniteSessionConfig
    {
        Enabled = true,
        CompactionThreshold = 0.8  // Compact at 80% context usage
    }
});
```

**SessionConfig Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Model` | `string` | Model to use (e.g., "claude-sonnet-4-5", "gpt-5") |
| `SystemMessage` | `SystemMessageConfig?` | Custom system message configuration |
| `OnUserInputRequest` | `Func<..., Task<UserInputResponse>>?` | Callback for agent questions |
| `Hooks` | `SessionHooks?` | Lifecycle hooks for tool use and session events |
| `InfiniteSession` | `InfiniteSessionConfig?` | Auto-compaction for long sessions |

#### 3. SystemMessageConfig — Loading Agent Prompts

The `SystemMessageConfig` allows loading PCH agent prompts as the session's system message.

**SystemMessageMode Options:**

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Append` | Add agent prompt to default Copilot instructions | **Recommended** — preserves guardrails |
| `Replace` | Fully replace system prompt with agent content | Use with caution — removes guardrails |

**Loading PCH Agent as System Message:**

```csharp
// Load agent prompt from file
var agentPrompt = await File.ReadAllTextAsync("agents/pch-coder.md");

var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = _config.Copilot.Model,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = agentPrompt
    }
});
```

**Integration with Existing AgentPromptLoader:**

```csharp
// Using the existing AgentPromptLoader pattern
var agentPrompt = await _promptLoader.LoadPromptAsync(agentDefinition, ct);

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = _config.Copilot.Model,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = agentPrompt  // Already loaded, potentially cached
    }
});
```

#### 4. Session Lifecycle & Async Disposal

Both `CopilotClient` and `CopilotSession` implement `IAsyncDisposable` for proper resource cleanup.

**Recommended Pattern (using declarations):**

```csharp
public async Task ExecutePhaseAsync(AgentDefinition agent, CancellationToken ct)
{
    // Client per operation (or reuse at higher scope)
    await using var client = new CopilotClient();
    await client.StartAsync();
    
    var agentPrompt = await _promptLoader.LoadPromptAsync(agent, ct);
    
    // Fresh session per phase (Ralph Wiggum pattern)
    await using var session = await client.CreateSessionAsync(new SessionConfig
    {
        Model = _config.Copilot.Model,
        SystemMessage = new SystemMessageConfig
        {
            Mode = SystemMessageMode.Append,
            Content = agentPrompt
        }
    });
    
    // ... send messages and handle responses ...
    
    // Session and client are automatically disposed
}
```

**Manual Disposal Pattern:**

```csharp
CopilotClient? client = null;
CopilotSession? session = null;

try
{
    client = new CopilotClient();
    await client.StartAsync();
    
    session = await client.CreateSessionAsync(config);
    // ... use session ...
}
finally
{
    if (session != null)
        await session.DisposeAsync();
    if (client != null)
        await client.DisposeAsync();
}
```

**Client Reuse Pattern (Recommended for Multiple Phases):**

```csharp
public class CopilotClientManager : IAsyncDisposable
{
    private CopilotClient? _client;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public async Task<CopilotSession> CreateSessionAsync(
        SessionConfig config, 
        CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_client == null)
            {
                _client = new CopilotClient();
                await _client.StartAsync();
            }
            
            return await _client.CreateSessionAsync(config);
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.StopAsync();
            await _client.DisposeAsync();
        }
        _lock.Dispose();
    }
}
```

#### 5. Integration with Existing Hugh Architecture

**Current SessionConfig in PhaseExecutor (to be replaced):**

The existing `SessionConfig` record in PhaseExecutor.cs should be replaced with SDK usage:

```csharp
// CURRENT: Custom SessionConfig record (placeholder)
private SessionConfig BuildSessionConfig(AgentDefinition agentDefinition, string agentPrompt)
{
    return new SessionConfig
    {
        AgentName = agentDefinition.Name,
        SystemMessage = agentPrompt,
        Model = _config.Copilot.Model,
        Phase = agentDefinition.Phase
    };
}

// PROPOSED: SDK SessionConfig usage
private async Task<CopilotSession> CreateSdkSessionAsync(
    CopilotClient client,
    string agentPrompt,
    CancellationToken ct)
{
    return await client.CreateSessionAsync(new GitHub.Copilot.SDK.SessionConfig
    {
        Model = _config.Copilot.Model,
        SystemMessage = new SystemMessageConfig
        {
            Mode = SystemMessageMode.Append,
            Content = agentPrompt
        },
        OnUserInputRequest = async (request, invocation) =>
        {
            // Route to existing CopilotService signal handling
            var classification = _signalClassifier.Classify(request.Prompt);
            var response = await _copilotService.HandleSignalAsync(
                classification, 
                "current-phase", 
                Guid.NewGuid(), 
                ct);
            
            return new UserInputResponse
            {
                Answer = response?.Response ?? "Continue with recommended option",
                WasFreeform = true
            };
        }
    });
}
```

**Proposed Changes to ICopilotService:**

```csharp
public interface ICopilotService
{
    // Existing method (keep for backward compatibility)
    Task<PhaseResult> ExecutePhaseAsync(
        string agentName,
        string prompt,
        IEnumerable<string> documentPaths,
        CancellationToken ct);
    
    // NEW: SDK-based execution
    Task<PhaseResult> ExecutePhaseWithSdkAsync(
        AgentDefinition agentDefinition,
        string prompt,
        IEnumerable<FileAttachment> attachments,
        CancellationToken ct);
}
```

#### 6. Model Selection

Available models depend on the user's Copilot subscription and can be specified in `SessionConfig.Model`:

| Model Identifier | Description |
|-----------------|-------------|
| `claude-sonnet-4-5` | Claude Sonnet 4.5 (recommended for coding tasks) |
| `claude-opus-4-5` | Claude Opus 4.5 (highest capability) |
| `gpt-5` | GPT-5 (OpenAI) |
| `gpt-4o` | GPT-4 Optimized |
| `o1` | OpenAI o1 reasoning model |

The model can be configured in `appsettings.json`:

```json
{
  "Huge": {
    "Copilot": {
      "Model": "claude-sonnet-4-5"
    }
  }
}
```

And accessed via `_config.Copilot.Model` in the existing configuration pattern.

### Key Discoveries

- **CopilotClient wraps CLI lifecycle** — Handles spawning, communication, and auto-restart of Copilot CLI process
- **IAsyncDisposable pattern** — Both client and session support async disposal via `await using`
- **Fresh context per CreateSessionAsync()** — Each call creates isolated session with no prior history (Ralph Wiggum pattern)
- **SystemMessageConfig.Append is recommended** — Adds agent prompt while preserving Copilot guardrails
- **OnUserInputRequest enables autonomy** — Callback intercepts agent questions for autonomous answering
- **Model selection via SessionConfig.Model** — Matches existing `_config.Copilot.Model` pattern
- **Client reuse is safe** — Single client can create multiple sessions; each session is independent
- **InfiniteSessionConfig for long contexts** — Auto-compaction prevents context window exhaustion

### Relevant Files/Resources

- [CopilotService.cs](../Hugh/Agents/CopilotService.cs) — Current stub implementation to be updated
- [ICopilotService.cs](../Hugh/Agents/ICopilotService.cs) — Interface to potentially extend
- [PhaseExecutor.cs](../Hugh/Orchestration/PhaseExecutor.cs) — Contains custom SessionConfig to replace
- [AgentPromptLoader.cs](../Hugh/Agents/AgentPromptLoader.cs) — Existing prompt loading logic to reuse
- [HugeConfig.cs](../Hugh/Configuration/HugeConfig.cs) — Configuration including CopilotConfig.Model
- [008-self-operating-development-loop.md](008-self-operating-development-loop.md) — Previous research with SDK examples

---

## Phase 3: Message & Attachment Handling

**Status:** ✅ Complete
**Scope:**
- How to attach documents to a session (FileAttachment API)
- How to send messages with MessageOptions
- How to handle streaming vs. complete responses
- Document path handling and context limits

### Findings

#### 1. MessageOptions and SendAsync API

The SDK's `session.SendAsync()` method accepts a `MessageOptions` object to send prompts with attachments:

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = "Continue implementing the plan — start Phase 3",
    Attachments = new List<UserMessageDataAttachmentsItem>
    {
        new UserMessageDataAttachmentsItem
        {
            Type = UserMessageDataAttachmentsItemType.File,
            Path = "/path/to/plan.md"
        }
    }
});
```

**MessageOptions Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Prompt` | `string` | The text prompt to send to the agent |
| `Attachments` | `List<UserMessageDataAttachmentsItem>` | File attachments for context |

#### 2. SDK FileAttachment API (UserMessageDataAttachmentsItem)

The SDK uses `UserMessageDataAttachmentsItem` to represent file attachments:

```csharp
new UserMessageDataAttachmentsItem
{
    Type = UserMessageDataAttachmentsItemType.File,
    Path = "/absolute/or/relative/path/to/document.md"
}
```

**Mapping Existing Hugh FileAttachment to SDK:**

The current `FileAttachment` record in PhaseExecutor.cs needs to be mapped to the SDK type:

```csharp
// Current Hugh FileAttachment (in PhaseExecutor.cs)
public record FileAttachment
{
    public string Path { get; init; } = "";
    public string FileName { get; init; } = "";
    public string MimeType { get; init; } = "text/plain";
}

// Conversion to SDK type
public static UserMessageDataAttachmentsItem ToSdkAttachment(FileAttachment attachment)
{
    return new UserMessageDataAttachmentsItem
    {
        Type = UserMessageDataAttachmentsItemType.File,
        Path = attachment.Path
    };
}

// Usage in ExecutePhaseAsync
var sdkAttachments = attachments
    .Select(ToSdkAttachment)
    .ToList();

await session.SendAsync(new MessageOptions
{
    Prompt = prompt,
    Attachments = sdkAttachments
});
```

#### 3. Multi-Document Attachment Pattern

For phases requiring multiple documents (vision + research + plan):

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = $"Continue implementing the plan — start Phase {phaseNumber}",
    Attachments = new List<UserMessageDataAttachmentsItem>
    {
        new() { Type = UserMessageDataAttachmentsItemType.File, Path = visionDocPath },
        new() { Type = UserMessageDataAttachmentsItemType.File, Path = researchDocPath },
        new() { Type = UserMessageDataAttachmentsItemType.File, Path = planDocPath }
    }
});
```

**Document Flow by Phase:**

| Phase | Agent | Documents Attached |
|-------|-------|--------------------|
| Vision | pch-visionary | (none — starts from feature request) |
| Research | pch-researcher | vision |
| Planning | pch-planner | vision, research |
| Review | pch-plan-reviewer | vision, research, plan |
| Coding | pch-coder | plan (vision/research embedded in plan) |

#### 4. Streaming vs Complete Responses

The SDK emits events via the `session.On()` pattern for handling responses:

**Event Types for Response Handling:**

| Event | Description | Use Case |
|-------|-------------|----------|
| `AssistantMessageDeltaEvent` | Streaming chunk of response | Real-time output display, early signal detection |
| `AssistantMessageEvent` | Final/complete response | Full response processing |
| `SessionIdleEvent` | Session finished processing | Completion detection |

**Streaming Response Pattern:**

```csharp
var responseBuilder = new StringBuilder();
var completionSource = new TaskCompletionSource();

session.On(evt =>
{
    switch (evt)
    {
        case AssistantMessageDeltaEvent delta:
            // Streaming chunk — accumulate for pattern matching
            responseBuilder.Append(delta.Content);
            
            // Early detection: check for phase completion markers
            if (responseBuilder.ToString().Contains("✅ **Phase"))
            {
                _logger.LogInformation("Phase completion detected in stream");
            }
            break;
            
        case AssistantMessageEvent complete:
            // Final response — full content available
            var fullResponse = complete.Content;
            _logger.LogDebug("Complete response: {Length} chars", fullResponse.Length);
            break;
            
        case SessionIdleEvent:
            // Session is done processing — safe to proceed
            completionSource.SetResult();
            break;
    }
});

await completionSource.Task;
var finalContent = responseBuilder.ToString();
```

**Non-Streaming (Await Complete Response):**

```csharp
string? responseContent = null;
var completionSource = new TaskCompletionSource();

session.On(evt =>
{
    if (evt is AssistantMessageEvent complete)
    {
        responseContent = complete.Content;
    }
    else if (evt is SessionIdleEvent)
    {
        completionSource.SetResult();
    }
});

await session.SendAsync(new MessageOptions { Prompt = prompt, Attachments = attachments });
await completionSource.Task;

// responseContent now contains full response
```

#### 5. Context Limits and InfiniteSessionConfig

**Context Window Limits by Model:**

| Model | Context Window | Practical Limit |
|-------|----------------|-----------------|
| Claude Opus 4.5 | 200,000 tokens | ~180,000 tokens |
| Claude Sonnet 4.5 | 200,000 tokens | ~180,000 tokens |
| GPT-4 / GPT-4o | 128,000 tokens | ~115,000 tokens |

**InfiniteSessionConfig for Auto-Compaction:**

Enable automatic context compaction for long-running sessions:

```csharp
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = _config.Copilot.Model,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = agentPrompt
    },
    InfiniteSession = new InfiniteSessionConfig
    {
        Enabled = true,
        CompactionThreshold = 0.8  // Compact at 80% context usage
    }
});
```

**Compaction Events:**

| Event | Description |
|-------|-------------|
| `SessionCompactionStartEvent` | Context compaction started |
| `SessionCompactionCompleteEvent` | Compaction finished |

```csharp
session.On(evt =>
{
    if (evt is SessionCompactionStartEvent)
        _logger.LogInformation("Context compaction started...");
    else if (evt is SessionCompactionCompleteEvent)
        _logger.LogInformation("Context compaction complete");
});
```

#### 6. Document Attachment Strategies

**Strategy A: Full Embedding (< 30K tokens)**

Embed all documents directly:

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = "Continue implementing Phase 3 of the plan",
    Attachments = new[]
    {
        new UserMessageDataAttachmentsItem { Type = UserMessageDataAttachmentsItemType.File, Path = visionPath },
        new UserMessageDataAttachmentsItem { Type = UserMessageDataAttachmentsItemType.File, Path = planPath }
    }
});
```

**Strategy B: Selective Embedding (30-80K tokens)**

Embed essential docs; reference large ones in prompt:

```csharp
var planSection = ExtractPhaseSection(planContent, currentPhase);

await session.SendAsync(new MessageOptions
{
    Prompt = $"Implement Phase {currentPhase}. Full plan at docs/plans/001.md",
    Attachments = new[]
    {
        new UserMessageDataAttachmentsItem { Type = UserMessageDataAttachmentsItemType.File, Path = visionPath }
        // Plan section embedded in prompt or partial attachment
    }
});
```

**Strategy C: Summary + Reference (> 80K tokens)**

Use LLM or extractive summarization for large documents:

```csharp
var researchSummary = await SummarizeDocument(researchContent, maxTokens: 3000);
// Embed summary, reference full document path for agent to read if needed
```

#### 7. Path Handling

**Absolute vs Relative Paths:**

- Both work with the SDK
- Absolute paths recommended for reliability
- Use `Path.GetFullPath()` for normalization

```csharp
private string NormalizePath(string docPath)
{
    if (Path.IsPathRooted(docPath))
        return docPath;
    
    return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), docPath));
}
```

**Alternative: @file Syntax in Prompt (Deferred Read):**

The Copilot CLI also supports `@path/to/file` syntax in prompts for lazy file loading:

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = "Review the plan at @docs/plans/001-feature.md and continue with Phase 3"
    // No Attachments — agent reads file on demand
});
```

| Approach | Token Timing | Best For |
|----------|--------------|----------|
| `Attachments` | Immediate (full content) | Essential documents |
| `@file` in prompt | Deferred (agent reads when needed) | Large or optional docs |

#### 8. Proposed CopilotService Integration

**Updated ExecutePhaseAsync Implementation:**

```csharp
public async Task<PhaseResult> ExecutePhaseAsync(
    string agentName,
    string agentPrompt,
    string prompt,
    IEnumerable<string> documentPaths,
    CancellationToken ct)
{
    var stopwatch = Stopwatch.StartNew();
    var decisions = new List<Decision>();
    string? responseContent = null;
    
    await using var client = new CopilotClient();
    await client.StartAsync();
    
    await using var session = await client.CreateSessionAsync(new SessionConfig
    {
        Model = _config.Copilot.Model,
        SystemMessage = new SystemMessageConfig
        {
            Mode = SystemMessageMode.Append,
            Content = agentPrompt
        },
        InfiniteSession = new InfiniteSessionConfig
        {
            Enabled = true,
            CompactionThreshold = 0.8
        },
        OnUserInputRequest = async (request, invocation) =>
        {
            // Route to signal handling
            var classification = _signalClassifier.Classify(request.Prompt);
            var response = await HandleSignalAsync(classification, agentName, runId, ct);
            return new UserInputResponse
            {
                Answer = response?.Response ?? "Continue with recommended option",
                WasFreeform = true
            };
        }
    });
    
    // Build attachments
    var attachments = documentPaths
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => new UserMessageDataAttachmentsItem
        {
            Type = UserMessageDataAttachmentsItemType.File,
            Path = NormalizePath(p)
        })
        .ToList();
    
    // Set up completion detection
    var completionSource = new TaskCompletionSource();
    var responseBuilder = new StringBuilder();
    
    session.On(evt =>
    {
        switch (evt)
        {
            case AssistantMessageDeltaEvent delta:
                responseBuilder.Append(delta.Content);
                break;
            case SessionIdleEvent:
                completionSource.TrySetResult();
                break;
        }
    });
    
    // Send prompt with attachments
    await session.SendAsync(new MessageOptions
    {
        Prompt = prompt,
        Attachments = attachments
    });
    
    // Wait for completion with cancellation support
    using (ct.Register(() => completionSource.TrySetCanceled()))
    {
        await completionSource.Task;
    }
    
    responseContent = responseBuilder.ToString();
    stopwatch.Stop();
    
    // Parse response for output path and signals
    var outputPath = ExtractOutputDocumentPath(responseContent);
    
    return new PhaseResult
    {
        Success = true,
        OutputDocumentPath = outputPath,
        DecisionsMade = decisions,
        Duration = stopwatch.Elapsed
    };
}
```

### Key Discoveries

- **MessageOptions.Attachments is the SDK's file attachment API** — Uses `UserMessageDataAttachmentsItem` with `Type` and `Path` properties
- **Existing FileAttachment maps to SDK type** — Simple conversion from Huge's custom record to SDK's attachment item
- **AssistantMessageDeltaEvent for streaming, SessionIdleEvent for completion** — Event-based pattern for response handling
- **InfiniteSessionConfig prevents context exhaustion** — Auto-compaction at configurable threshold (0.8 = 80% recommended)
- **Both absolute and relative paths work** — Normalize with `Path.GetFullPath()` for consistency
- **Two attachment approaches available** — Immediate embed via `Attachments` or deferred via `@file` syntax
- **Multi-document attachment supported** — Array of attachments for vision + research + plan patterns

### Relevant Files/Resources

- [PhaseExecutor.cs](../Hugh/Orchestration/PhaseExecutor.cs) — Contains existing `FileAttachment` record and `PrepareDocumentAttachments()` method
- [CopilotService.cs](../Hugh/Agents/CopilotService.cs) — Current stub with TODO for SDK integration
- [ICopilotService.cs](../Hugh/Agents/ICopilotService.cs) — Interface to extend for SDK-based execution
- [008-self-operating-development-loop.md](008-self-operating-development-loop.md) — Prior research with SDK attachment patterns
- [GitHub Copilot SDK Repository](https://github.com/github/copilot-sdk/tree/main/dotnet) — SDK source and documentation

---

## Phase 4: Response Parsing & Signal Detection

**Status:** ✅ Complete
**Scope:**
- How to receive and parse agent responses
- How to detect phase completion signals in responses
- How to extract output document paths from agent output
- How to integrate with existing SignalClassifier

### Findings

#### 1. Response Accumulation Pattern

The SDK emits responses via events. To integrate with the existing SignalClassifier, responses must be accumulated from streaming events and then classified:

```csharp
public async Task<PhaseResult> ExecuteWithSignalDetectionAsync(
    CopilotSession session,
    string prompt,
    List<UserMessageDataAttachmentsItem> attachments,
    CancellationToken ct)
{
    var responseBuilder = new StringBuilder();
    var completionSource = new TaskCompletionSource();
    string? finalResponse = null;
    
    // Subscribe to session events
    session.On(evt =>
    {
        switch (evt)
        {
            case AssistantMessageDeltaEvent delta:
                // Accumulate streaming chunks
                responseBuilder.Append(delta.Content);
                
                // Optional: Early signal detection for long responses
                var partialContent = responseBuilder.ToString();
                if (partialContent.Contains("✅ **Phase"))
                {
                    _logger.Debug("Phase completion signal detected in stream");
                }
                break;
                
            case AssistantMessageEvent complete:
                // Final aggregated content (alternative to delta accumulation)
                finalResponse = complete.Content;
                break;
                
            case SessionIdleEvent:
                // Session finished processing - safe to proceed
                completionSource.TrySetResult();
                break;
                
            case SessionErrorEvent error:
                completionSource.TrySetException(new Exception(error.Message));
                break;
        }
    });
    
    // Send the prompt with attachments
    await session.SendAsync(new MessageOptions
    {
        Prompt = prompt,
        Attachments = attachments
    });
    
    // Wait for completion with cancellation support
    using (ct.Register(() => completionSource.TrySetCanceled()))
    {
        await completionSource.Task;
    }
    
    // Use finalResponse if available, otherwise accumulated content
    var content = finalResponse ?? responseBuilder.ToString();
    
    // Classify the response
    var classification = _signalClassifier.Classify(content);
    
    // Extract document path
    var outputPath = ExtractDocumentPath(content);
    
    return new PhaseResult
    {
        Success = classification.Type != SignalType.Blocker,
        OutputDocumentPath = outputPath,
        DecisionsMade = new List<Decision>()
    };
}
```

#### 2. Integrating SignalClassifier with SDK Response Events

The existing `SignalClassifier` works perfectly with SDK responses. The key integration points:

**A. Post-Completion Classification (Recommended):**

Classify the full response after `SessionIdleEvent`:

```csharp
session.On(evt =>
{
    if (evt is SessionIdleEvent)
    {
        var fullResponse = responseBuilder.ToString();
        var classification = _signalClassifier.Classify(fullResponse);
        
        switch (classification.Type)
        {
            case SignalType.PhaseComplete:
                _logger.Information("Phase {Phase} completed", classification.PhaseNumber);
                break;
                
            case SignalType.WorkflowComplete:
                _logger.Information("Workflow complete: {Match}", classification.RawMatch);
                break;
                
            case SignalType.Blocker:
                _logger.Error("Blocker detected: {Match}", classification.RawMatch);
                break;
                
            case SignalType.QuestionWithRecommendation:
                // Should be handled by OnUserInputRequest callback instead
                _logger.Warning("Question in final response - may indicate callback miss");
                break;
        }
        
        completionSource.TrySetResult();
    }
});
```

**B. Early Detection (For Progress Reporting):**

Check for signals during streaming for real-time feedback:

```csharp
session.On(evt =>
{
    if (evt is AssistantMessageDeltaEvent delta)
    {
        responseBuilder.Append(delta.Content);
        var partial = responseBuilder.ToString();
        
        // Check for phase completion (non-blocking progress update)
        if (PhaseCompleteRegex().IsMatch(partial) && !_phaseCompletionReported)
        {
            _phaseCompletionReported = true;
            _logger.Information("Phase completion detected in stream");
        }
    }
});
```

#### 3. OnUserInputRequest for Interactive Signal Handling

The SDK's `OnUserInputRequest` callback is invoked when the agent needs user input (questions). This is where the existing signal handling should be integrated:

```csharp
var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = _config.Copilot.Model,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = agentPrompt
    },
    OnUserInputRequest = async (request, invocation) =>
    {
        // Classify the agent's question prompt
        var classification = _signalClassifier.Classify(request.Prompt);
        
        switch (classification.Type)
        {
            case SignalType.ModeSelection:
                // Auto-select Manual mode (option 1) per Hugh design
                _logger.Information("Mode selection detected - auto-selecting Manual (1)");
                return new UserInputResponse
                {
                    Answer = "1",
                    WasFreeform = true
                };
                
            case SignalType.QuestionWithRecommendation:
                // Auto-accept the recommendation
                var letter = classification.RecommendationLetter;
                _logger.Information("Auto-accepting recommendation: {Letter}", letter);
                
                // Log the decision
                var decision = new Decision
                {
                    Phase = _currentPhase,
                    Question = classification.RawMatch ?? request.Prompt,
                    Selected = letter,
                    Rationale = $"Auto-accepted recommendation: {classification.RecommendationOption}",
                    Autonomous = true,
                    Timestamp = DateTime.UtcNow
                };
                await _decisionLogger.LogDecisionAsync(decision, _runId, invocation.CancellationToken);
                
                return new UserInputResponse
                {
                    Answer = letter,
                    WasFreeform = true
                };
                
            case SignalType.QuestionOpen:
                // Open question requires human input - cannot auto-respond
                // Abort the session and return pause signal
                _logger.Warning("Open question requires human input - pausing");
                await invocation.Session.AbortAsync();
                
                return new UserInputResponse
                {
                    Answer = "", // Will not be sent due to abort
                    WasFreeform = true
                };
                
            default:
                // Generic question - try to continue
                _logger.Debug("Generic input request - responding with continue");
                return new UserInputResponse
                {
                    Answer = "Continue with the recommended approach",
                    WasFreeform = true
                };
        }
    }
});
```

#### 4. Document Path Extraction

The existing `DocumentPathRegex()` in PhaseExecutor.cs handles path extraction. Here's the complete pattern with additional patterns for robust extraction:

**Existing Pattern (PhaseExecutor.cs):**
```csharp
[GeneratedRegex(@"(?:created|saved|wrote|generated)\s+(?:to|at)?:?\s*[`""']?([^\s`""']+\.md)[`""']?", RegexOptions.IgnoreCase)]
private static partial Regex DocumentPathRegex();
```

**Enhanced Extraction with Multiple Patterns:**

```csharp
public partial class DocumentPathExtractor
{
    // Pattern 1: Explicit creation statements
    // "Created vision document at: docs/vision/001-feature.md"
    [GeneratedRegex(@"(?:created|saved|wrote|generated|updated)\s+(?:vision|research|plan|document)?:?\s*(?:to|at)?:?\s*[`""']?([^\s`""'\n]+\.md)[`""']?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExplicitCreationRegex();
    
    // Pattern 2: Document location in handoff messages
    // "**Document:** docs/vision/001-feature.md"
    // "**Document Location:** /docs/plans/001.md"
    [GeneratedRegex(@"\*\*Document(?:\s+Location)?:\*\*\s*[`""']?([^\s`""'\n]+\.md)[`""']?", RegexOptions.Compiled)]
    private static partial Regex HandoffDocumentRegex();
    
    // Pattern 3: Markdown link to created document
    // "[001-feature.md](docs/plans/001-feature.md)"
    [GeneratedRegex(@"\[([^\]]+\.md)\]\(([^)]+\.md)\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();
    
    // Pattern 4: Backtick-quoted paths
    // "The research document has been created at: `docs/research/001-auth.md`"
    [GeneratedRegex(@"(?:created|saved|updated|written)\s+(?:at|to)?:?\s*`([^`]+\.md)`", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BacktickPathRegex();
    
    /// <summary>
    /// Extracts document paths from agent response using multiple patterns.
    /// Returns the first matched path, prioritizing explicit creation statements.
    /// </summary>
    public string? ExtractDocumentPath(string agentResponse)
    {
        if (string.IsNullOrWhiteSpace(agentResponse))
            return null;
        
        // Priority 1: Handoff document patterns (most reliable)
        var handoffMatch = HandoffDocumentRegex().Match(agentResponse);
        if (handoffMatch.Success)
            return NormalizePath(handoffMatch.Groups[1].Value);
        
        // Priority 2: Backtick-quoted paths
        var backtickMatch = BacktickPathRegex().Match(agentResponse);
        if (backtickMatch.Success)
            return NormalizePath(backtickMatch.Groups[1].Value);
        
        // Priority 3: Explicit creation statements
        var creationMatch = ExplicitCreationRegex().Match(agentResponse);
        if (creationMatch.Success)
            return NormalizePath(creationMatch.Groups[1].Value);
        
        // Priority 4: Markdown links (extract the path, not display text)
        var linkMatch = MarkdownLinkRegex().Match(agentResponse);
        if (linkMatch.Success)
            return NormalizePath(linkMatch.Groups[2].Value);
        
        return null;
    }
    
    /// <summary>
    /// Extracts ALL document paths mentioned in response.
    /// Useful for phases that may create multiple documents.
    /// </summary>
    public IReadOnlyList<string> ExtractAllDocumentPaths(string agentResponse)
    {
        if (string.IsNullOrWhiteSpace(agentResponse))
            return Array.Empty<string>();
        
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (Match match in HandoffDocumentRegex().Matches(agentResponse))
            paths.Add(NormalizePath(match.Groups[1].Value));
            
        foreach (Match match in BacktickPathRegex().Matches(agentResponse))
            paths.Add(NormalizePath(match.Groups[1].Value));
            
        foreach (Match match in ExplicitCreationRegex().Matches(agentResponse))
            paths.Add(NormalizePath(match.Groups[1].Value));
            
        foreach (Match match in MarkdownLinkRegex().Matches(agentResponse))
            paths.Add(NormalizePath(match.Groups[2].Value));
        
        return paths.ToList();
    }
    
    private static string NormalizePath(string path)
    {
        // Remove leading/trailing quotes and backticks
        path = path.Trim('`', '"', '\'', ' ');
        
        // Convert forward slashes to platform-appropriate separators
        if (Path.DirectorySeparatorChar == '\\')
            path = path.Replace('/', '\\');
        
        // Remove leading slash if present and path is relative
        if (path.StartsWith("/") || path.StartsWith("\\"))
        {
            var withoutSlash = path.TrimStart('/', '\\');
            // Only remove if it looks like a relative path (starts with docs, src, etc.)
            if (withoutSlash.StartsWith("docs") || withoutSlash.StartsWith("src"))
                path = withoutSlash;
        }
        
        return path;
    }
}
```

#### 5. Complete Response Processing Pipeline

Bringing it all together — a complete response processing class that integrates SDK events with existing signal detection:

```csharp
using System.Text;
using System.Text.RegularExpressions;
using GitHub.Copilot.SDK;
using Huge.Detection;
using Huge.Models;
using Serilog;

namespace Huge.Agents;

/// <summary>
/// Processes SDK response events and integrates with SignalClassifier.
/// </summary>
public partial class SdkResponseProcessor
{
    private readonly SignalClassifier _signalClassifier;
    private readonly DocumentPathExtractor _pathExtractor;
    private readonly ILogger _logger;
    
    public SdkResponseProcessor(
        SignalClassifier signalClassifier,
        ILogger? logger = null)
    {
        _signalClassifier = signalClassifier;
        _pathExtractor = new DocumentPathExtractor();
        _logger = logger ?? Log.Logger;
    }
    
    /// <summary>
    /// Result of processing an SDK session response.
    /// </summary>
    public record ProcessedResponse
    {
        public string Content { get; init; } = "";
        public SignalClassification Signal { get; init; } = new() { Type = SignalType.None };
        public string? OutputDocumentPath { get; init; }
        public IReadOnlyList<string> AllDocumentPaths { get; init; } = Array.Empty<string>();
        public bool WasAborted { get; init; }
        public Exception? Error { get; init; }
    }
    
    /// <summary>
    /// Processes a session after sending a message, handling all events.
    /// </summary>
    public async Task<ProcessedResponse> ProcessResponseAsync(
        CopilotSession session,
        CancellationToken ct)
    {
        var responseBuilder = new StringBuilder();
        var completionSource = new TaskCompletionSource<ProcessedResponse>();
        Exception? sessionError = null;
        bool wasAborted = false;
        
        session.On(evt =>
        {
            try
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent delta:
                        responseBuilder.Append(delta.Content);
                        break;
                        
                    case SessionIdleEvent:
                        var content = responseBuilder.ToString();
                        var signal = _signalClassifier.Classify(content);
                        var primaryPath = _pathExtractor.ExtractDocumentPath(content);
                        var allPaths = _pathExtractor.ExtractAllDocumentPaths(content);
                        
                        completionSource.TrySetResult(new ProcessedResponse
                        {
                            Content = content,
                            Signal = signal,
                            OutputDocumentPath = primaryPath,
                            AllDocumentPaths = allPaths,
                            WasAborted = wasAborted,
                            Error = sessionError
                        });
                        break;
                        
                    case SessionErrorEvent error:
                        sessionError = new Exception(error.Message);
                        _logger.Error("Session error: {Message}", error.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error processing session event");
                completionSource.TrySetException(ex);
            }
        });
        
        // Wait for completion with cancellation support
        using (ct.Register(() =>
        {
            wasAborted = true;
            completionSource.TrySetCanceled();
        }))
        {
            return await completionSource.Task;
        }
    }
    
    /// <summary>
    /// Checks if a signal indicates the phase/workflow completed successfully.
    /// </summary>
    public static bool IsCompletionSignal(SignalClassification signal) =>
        signal.Type is SignalType.PhaseComplete or SignalType.WorkflowComplete;
    
    /// <summary>
    /// Checks if a signal indicates execution should pause.
    /// </summary>
    public static bool IsPauseSignal(SignalClassification signal) =>
        signal.Type is SignalType.Blocker or SignalType.QuestionOpen;
}
```

#### 6. Phase-Specific Response Expectations

Different phases produce different response patterns:

| Phase | Expected Signal | Document Pattern |
|-------|-----------------|------------------|
| Vision | `PhaseComplete` then `WorkflowComplete` | `docs/vision/{id}-*.md` |
| Research | `PhaseComplete` per phase | `docs/research/{id}-*.md` |
| Planning | `PhaseComplete` then `WorkflowComplete` | `docs/plans/{id}-*.md` |
| Review | `WorkflowComplete` (approval) or `Blocker` (issues) | Updates existing plan |
| Coding | `PhaseComplete` per sub-phase | No document (modifies source) |

**Phase-Specific Extraction Example:**

```csharp
public string? ExtractPhaseOutput(string response, LoopPhase phase)
{
    // First try general extraction
    var generalPath = ExtractDocumentPath(response);
    if (generalPath != null && ValidatePathForPhase(generalPath, phase))
        return generalPath;
    
    // Phase-specific patterns
    var pattern = phase switch
    {
        LoopPhase.Vision => @"docs[/\\]vision[/\\][^\s`""']+\.md",
        LoopPhase.Research => @"docs[/\\]research[/\\][^\s`""']+\.md",
        LoopPhase.Planning => @"docs[/\\]plans[/\\][^\s`""']+\.md",
        _ => null
    };
    
    if (pattern == null)
        return generalPath;
    
    var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
    return match.Success ? NormalizePath(match.Value) : generalPath;
}

private static bool ValidatePathForPhase(string path, LoopPhase phase)
{
    return phase switch
    {
        LoopPhase.Vision => path.Contains("vision"),
        LoopPhase.Research => path.Contains("research"),
        LoopPhase.Planning => path.Contains("plan"),
        _ => true
    };
}
```

#### 7. Updated CopilotService Integration

Complete integration example for `CopilotService.ExecutePhaseAsync`:

```csharp
public async Task<PhaseResult> ExecutePhaseAsync(
    string agentName,
    string prompt,
    IEnumerable<string> documentPaths,
    CancellationToken ct)
{
    var stopwatch = Stopwatch.StartNew();
    var decisions = new List<Decision>();
    var runId = Guid.NewGuid();
    
    _logger.Information("Executing phase with agent {AgentName}", agentName);
    
    try
    {
        // Create SDK client and session
        await using var client = new CopilotClient();
        await client.StartAsync();
        
        var agentPrompt = await _promptLoader.LoadPromptAsync(agentName, ct);
        
        await using var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = _config.Copilot.Model,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = agentPrompt
            },
            InfiniteSession = new InfiniteSessionConfig
            {
                Enabled = true,
                CompactionThreshold = 0.8
            },
            OnUserInputRequest = async (request, invocation) =>
            {
                // Integrate with existing signal handling
                var classification = _signalClassifier.Classify(request.Prompt);
                var response = await HandleSignalAsync(
                    classification, agentName, runId, invocation.CancellationToken);
                
                if (response?.ShouldPause == true)
                {
                    // Cannot pause in callback - abort session
                    await invocation.Session.AbortAsync();
                    return new UserInputResponse { Answer = "", WasFreeform = true };
                }
                
                return new UserInputResponse
                {
                    Answer = response?.Response ?? "Continue with recommended approach",
                    WasFreeform = true
                };
            }
        });
        
        // Build attachments
        var attachments = documentPaths
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Select(p => new UserMessageDataAttachmentsItem
            {
                Type = UserMessageDataAttachmentsItemType.File,
                Path = Path.GetFullPath(p)
            })
            .ToList();
        
        // Send prompt
        await session.SendAsync(new MessageOptions
        {
            Prompt = prompt,
            Attachments = attachments
        });
        
        // Process response with signal detection
        var processor = new SdkResponseProcessor(_signalClassifier, _logger);
        var result = await processor.ProcessResponseAsync(session, ct);
        
        stopwatch.Stop();
        
        // Handle blocker signals
        if (result.Signal.Type == SignalType.Blocker)
        {
            _logger.Error("Blocker detected: {Match}", result.Signal.RawMatch);
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Blocker: {result.Signal.RawMatch}",
                OutputDocumentPath = result.OutputDocumentPath,
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed
            };
        }
        
        return new PhaseResult
        {
            Success = true,
            OutputDocumentPath = result.OutputDocumentPath,
            DecisionsMade = decisions,
            Duration = stopwatch.Elapsed
        };
    }
    catch (OperationCanceledException)
    {
        stopwatch.Stop();
        _logger.Warning("Agent execution cancelled for {AgentName}", agentName);
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = "Operation cancelled",
            Duration = stopwatch.Elapsed
        };
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.Error(ex, "Agent execution failed for {AgentName}", agentName);
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = ex.Message,
            Duration = stopwatch.Elapsed
        };
    }
}
```

### Key Discoveries

- **Response accumulation via StringBuilder + AssistantMessageDeltaEvent** — Streaming chunks must be accumulated before classification
- **SessionIdleEvent is the completion signal** — Only classify after this event to ensure full response
- **Existing SignalClassifier.Classify() works unchanged** — Pass accumulated response text directly
- **OnUserInputRequest handles interactive signals** — Questions are intercepted before appearing in final response
- **DocumentPathRegex exists but needs enhancement** — Multiple patterns needed for reliable extraction
- **Handoff document pattern is most reliable** — `**Document:** path` format is consistent across agents
- **QuestionOpen cannot auto-respond** — Must abort session and surface pause to orchestrator
- **Phase-specific validation improves accuracy** — Match document paths to expected phase output folders

### Relevant Files/Resources

- [SignalClassifier.cs](../Hugh/Detection/SignalClassifier.cs) — Existing regex patterns for signal detection
- [SignalType.cs](../Hugh/Detection/SignalType.cs) — Signal type enum definitions
- [CopilotService.cs](../Hugh/Agents/CopilotService.cs) — Integration target with HandleSignalAsync method
- [PhaseExecutor.cs](../Hugh/Orchestration/PhaseExecutor.cs) — Contains DocumentPathRegex and ExtractOutputDocumentPath
- [QuestionHandler.cs](../Hugh/Handlers/QuestionHandler.cs) — Existing question handling logic to preserve
- [BlockerHandler.cs](../Hugh/Handlers/BlockerHandler.cs) — Blocker handling for pause signals

---

## Phase 5: Cancellation, Timeouts & Error Handling

**Status:** ✅ Complete
**Scope:**
- How SDK handles CancellationToken
- OnUserInputRequest callback pattern for intercepting questions
- SessionIdleEvent or equivalent for completion detection
- Error handling and retry patterns
- Timeout configuration and override implementation

### Findings

#### 1. CancellationToken Integration with SDK

The SDK supports cancellation through multiple mechanisms:

**A. Session AbortAsync() for Immediate Termination:**

```csharp
// Abort a running session
await session.AbortAsync();
```

**B. CancellationToken via TaskCompletionSource Pattern:**

The SDK's event-based pattern requires manual cancellation integration:

```csharp
public async Task<ProcessedResponse> ProcessWithCancellationAsync(
    CopilotSession session,
    string prompt,
    List<UserMessageDataAttachmentsItem> attachments,
    CancellationToken ct)
{
    var responseBuilder = new StringBuilder();
    var completionSource = new TaskCompletionSource<ProcessedResponse>();
    
    // Subscribe to events
    session.On(evt =>
    {
        switch (evt)
        {
            case AssistantMessageDeltaEvent delta:
                responseBuilder.Append(delta.Content);
                break;
                
            case SessionIdleEvent:
                var content = responseBuilder.ToString();
                completionSource.TrySetResult(new ProcessedResponse
                {
                    Content = content,
                    Success = true
                });
                break;
                
            case SessionErrorEvent error:
                completionSource.TrySetException(new CopilotSessionException(error.Message));
                break;
        }
    });
    
    // Send the prompt
    await session.SendAsync(new MessageOptions
    {
        Prompt = prompt,
        Attachments = attachments
    });
    
    // Wait with cancellation support
    using (ct.Register(() =>
    {
        // Abort the session on cancellation
        _ = session.AbortAsync();
        completionSource.TrySetCanceled(ct);
    }))
    {
        try
        {
            return await completionSource.Task;
        }
        catch (OperationCanceledException)
        {
            return new ProcessedResponse
            {
                Success = false,
                WasCancelled = true,
                Content = responseBuilder.ToString() // Partial content
            };
        }
    }
}
```

**C. InvocationContext.CancellationToken in Callbacks:**

The SDK provides a cancellation token in callback invocation contexts:

```csharp
OnUserInputRequest = async (request, invocation) =>
{
    // Access the SDK's cancellation token
    var ct = invocation.CancellationToken;
    
    // Use for async operations within the callback
    var response = await SomeAsyncOperation(ct);
    
    return new UserInputResponse { Answer = response, WasFreeform = true };
}
```

#### 2. Timeout Implementation Pattern

The existing PhaseExecutor uses `CreateLinkedTokenSource` with `CancelAfter()`. This pattern integrates seamlessly with SDK:

**Current Pattern (PhaseExecutor.cs lines 67-68):**
```csharp
using var phaseCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);
phaseCts.CancelAfter(phaseTimeout);
```

**Extended Pattern with SDK Integration:**

```csharp
public async Task<PhaseResult> ExecuteWithTimeoutAsync(
    AgentDefinition agent,
    string prompt,
    List<FileAttachment> attachments,
    TimeSpan timeout,
    CancellationToken globalCt)
{
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(globalCt);
    timeoutCts.CancelAfter(timeout);
    var ct = timeoutCts.Token;
    
    await using var client = new CopilotClient();
    await client.StartAsync();
    
    await using var session = await client.CreateSessionAsync(new SessionConfig
    {
        Model = _config.Copilot.Model,
        SystemMessage = new SystemMessageConfig
        {
            Mode = SystemMessageMode.Append,
            Content = await _promptLoader.LoadPromptAsync(agent, ct)
        }
    });
    
    try
    {
        // Process with timeout-linked token
        return await ProcessWithCancellationAsync(session, prompt, attachments, ct);
    }
    catch (OperationCanceledException) when (!globalCt.IsCancellationRequested)
    {
        // Timeout occurred (not user cancellation)
        _logger.LogWarning("Phase {Phase} timed out after {Timeout}", agent.Phase, timeout);
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = $"Phase timed out after {timeout}"
        };
    }
    catch (OperationCanceledException)
    {
        // User cancelled (Ctrl+C)
        _logger.LogWarning("Phase {Phase} cancelled by user", agent.Phase);
        throw; // Propagate for graceful shutdown
    }
}
```

#### 3. Timeout Override Implementation (Program.cs TODOs at lines 206, 236)

The TODO at Program.cs requires parsing the timeout string and applying it to config:

**Implementation for Timeout Override:**

```csharp
private static TimeSpan? ParseTimeoutOverride(string? timeout)
{
    if (string.IsNullOrEmpty(timeout))
        return null;
    
    // Support formats: "HH:mm:ss", "mm:ss", or just minutes "30"
    if (TimeSpan.TryParse(timeout, out var parsed))
        return parsed;
    
    if (int.TryParse(timeout, out var minutes))
        return TimeSpan.FromMinutes(minutes);
    
    Log.Warning("Invalid timeout format: {Timeout}. Expected HH:mm:ss or minutes.", timeout);
    return null;
}

private static void ApplyTimeoutOverride(HugeConfig config, TimeSpan timeout)
{
    // Apply to all phases uniformly
    config.Timeouts.VisionPhase = timeout;
    config.Timeouts.ResearchPhase = timeout;
    config.Timeouts.PlanningPhase = timeout;
    config.Timeouts.ReviewPhase = timeout;
    config.Timeouts.CodingPhasePerPhase = timeout;
    
    Log.Information("Timeout override applied: {Timeout} to all phases", timeout);
}

// Usage in Program.cs:
if (!string.IsNullOrEmpty(timeout))
{
    var parsed = ParseTimeoutOverride(timeout);
    if (parsed.HasValue)
    {
        ApplyTimeoutOverride(hugeConfig, parsed.Value);
    }
}
```

**Alternative: Per-Phase Override via Named Format:**

```csharp
// Support format: "Vision=10m,Research=30m" or just "30m" for all
private static void ApplyTimeoutOverride(HugeConfig config, string timeout)
{
    if (timeout.Contains('='))
    {
        // Parse per-phase format
        foreach (var part in timeout.Split(','))
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && TryParsePhaseTimeout(kv[0].Trim(), kv[1].Trim(), config))
                continue;
            Log.Warning("Invalid phase timeout: {Part}", part);
        }
    }
    else
    {
        // Apply uniform timeout
        if (TimeSpan.TryParse(timeout, out var ts))
            ApplyUniformTimeout(config, ts);
    }
}
```

#### 4. SessionErrorEvent Handling

The SDK emits `SessionErrorEvent` for errors. Handle in the event subscription:

```csharp
/// <summary>
/// Custom exception for SDK session errors.
/// </summary>
public class CopilotSessionException : Exception
{
    public string? SessionError { get; }
    public bool IsTransient { get; }
    
    public CopilotSessionException(string message, bool isTransient = false) 
        : base(message)
    {
        SessionError = message;
        IsTransient = isTransient;
    }
}

// In event handler:
session.On(evt =>
{
    if (evt is SessionErrorEvent error)
    {
        var isTransient = IsTransientError(error.Message);
        
        _logger.Error("Session error: {Message} (Transient: {IsTransient})", 
            error.Message, isTransient);
        
        completionSource.TrySetException(
            new CopilotSessionException(error.Message, isTransient));
    }
});

private static bool IsTransientError(string message)
{
    // Patterns indicating transient/recoverable errors
    var transientPatterns = new[]
    {
        "connection reset",
        "timeout",
        "temporarily unavailable",
        "rate limit",
        "503",
        "502",
        "network",
        "cli process",
        "spawn"
    };
    
    return transientPatterns.Any(p => 
        message.Contains(p, StringComparison.OrdinalIgnoreCase));
}
```

#### 5. Retry Pattern with Exponential Backoff

For transient failures (CLI crash, network issues), implement retry logic:

```csharp
public async Task<PhaseResult> ExecuteWithRetryAsync(
    AgentDefinition agent,
    string prompt,
    List<FileAttachment> attachments,
    int maxRetries = 3,
    CancellationToken ct = default)
{
    var baseDelay = TimeSpan.FromSeconds(2);
    Exception? lastException = null;
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            _logger.LogDebug("Execution attempt {Attempt}/{Max}", attempt, maxRetries);
            
            return await ExecutePhaseInternalAsync(agent, prompt, attachments, ct);
        }
        catch (CopilotSessionException ex) when (ex.IsTransient && attempt < maxRetries)
        {
            lastException = ex;
            var delay = baseDelay * Math.Pow(2, attempt - 1);
            
            _logger.LogWarning(
                "Transient error on attempt {Attempt}: {Error}. Retrying in {Delay}...",
                attempt, ex.Message, delay);
            
            await Task.Delay(delay, ct);
        }
        catch (OperationCanceledException)
        {
            throw; // Don't retry on cancellation
        }
    }
    
    _logger.LogError("All {MaxRetries} attempts failed", maxRetries);
    return new PhaseResult
    {
        Success = false,
        ErrorMessage = $"Failed after {maxRetries} attempts: {lastException?.Message}"
    };
}

private async Task<PhaseResult> ExecutePhaseInternalAsync(
    AgentDefinition agent,
    string prompt,
    List<FileAttachment> attachments,
    CancellationToken ct)
{
    // SDK's AutoRestart handles CLI crashes automatically
    // This is for session-level transient errors
    
    await using var client = new CopilotClient(new CopilotClientOptions
    {
        AutoRestart = true  // Default, ensures CLI restarts on crash
    });
    await client.StartAsync();
    
    // ... session creation and execution
}
```

#### 6. CLI AutoRestart Behavior

The SDK's `CopilotClientOptions.AutoRestart` (default: `true`) handles CLI process crashes:

```csharp
var options = new CopilotClientOptions
{
    AutoRestart = true,  // Default - CLI auto-restarts on crash
    // Alternatively, disable for manual control:
    // AutoRestart = false
};
```

**Behavior when AutoRestart is enabled:**
- SDK detects CLI process exit
- Automatically spawns new CLI process
- Existing sessions may need to be recreated
- Transparent to application code for new sessions

**Manual CLI restart handling (if AutoRestart = false):**

```csharp
try
{
    await ExecuteSessionAsync(session, ct);
}
catch (Exception ex) when (IsCliCrashError(ex))
{
    _logger.LogWarning("CLI process crashed, restarting...");
    await client.StopAsync();
    await client.StartAsync();
    
    // Recreate session and retry
    await using var newSession = await client.CreateSessionAsync(sessionConfig);
    await ExecuteSessionAsync(newSession, ct);
}
```

#### 7. Graceful Shutdown Pattern

Integration with existing Program.cs Ctrl+C handling:

```csharp
// Program.cs already sets up:
private static CancellationTokenSource _globalCts = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    _globalCts.Cancel();
    ConsoleOutput.WriteWarning("Shutdown requested. Completing current operation...");
};

// SDK integration for graceful shutdown:
public async Task<PhaseResult> ExecutePhaseAsync(
    AgentDefinition agent,
    string prompt,
    CancellationToken globalCt)
{
    CopilotSession? session = null;
    
    try
    {
        await using var client = new CopilotClient();
        await client.StartAsync();
        
        session = await client.CreateSessionAsync(sessionConfig);
        
        using (globalCt.Register(async () =>
        {
            if (session != null)
            {
                _logger.LogInformation("Aborting session due to shutdown...");
                try { await session.AbortAsync(); } catch { /* ignore */ }
            }
        }))
        {
            return await ProcessSessionAsync(session, prompt, globalCt);
        }
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Phase cancelled during shutdown");
        throw; // Allow orchestrator to handle
    }
    finally
    {
        // IAsyncDisposable handles cleanup
    }
}
```

#### 8. Complete Error Handling Strategy

**Error Classification Table:**

| Error Type | Source | Recovery Strategy |
|------------|--------|-------------------|
| `SessionErrorEvent` | SDK | Check IsTransient; retry if transient |
| `OperationCanceledException` (timeout) | CancelAfter | Return timeout result; mark phase failed |
| `OperationCanceledException` (user) | Ctrl+C | Propagate for shutdown |
| CLI crash | AutoRestart | SDK handles; session may need recreation |
| Network error | SDK → CLI | Retry with backoff |
| `SignalType.Blocker` | Agent response | Pause execution; require human input |
| `SignalType.QuestionOpen` | Agent response | Abort session; pause for user |

**Complete CopilotService.ExecutePhaseAsync with Error Handling:**

```csharp
public async Task<PhaseResult> ExecutePhaseAsync(
    string agentName,
    string prompt,
    IEnumerable<string> documentPaths,
    CancellationToken ct)
{
    var stopwatch = Stopwatch.StartNew();
    var decisions = new List<Decision>();
    var runId = _currentRunId ?? Guid.NewGuid();
    
    try
    {
        // Execute with retry for transient errors
        return await ExecuteWithRetryAsync(
            agentName, prompt, documentPaths, runId, maxRetries: 3, ct);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        stopwatch.Stop();
        _logger.Warning("Agent execution cancelled for {AgentName}", agentName);
        
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = "Operation was cancelled",
            Duration = stopwatch.Elapsed
        };
    }
    catch (CopilotSessionException ex)
    {
        stopwatch.Stop();
        _logger.Error(ex, "SDK session error for {AgentName}: {Error}", agentName, ex.Message);
        
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = $"Session error: {ex.Message}",
            Duration = stopwatch.Elapsed
        };
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.Error(ex, "Unexpected error for {AgentName}", agentName);
        
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = ex.Message,
            Duration = stopwatch.Elapsed
        };
    }
}

private async Task<PhaseResult> ExecuteWithRetryAsync(
    string agentName,
    string prompt,
    IEnumerable<string> documentPaths,
    Guid runId,
    int maxRetries,
    CancellationToken ct)
{
    Exception? lastException = null;
    var baseDelay = TimeSpan.FromSeconds(2);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await ExecuteCoreAsync(agentName, prompt, documentPaths, runId, ct);
        }
        catch (CopilotSessionException ex) when (ex.IsTransient && attempt < maxRetries)
        {
            lastException = ex;
            var delay = baseDelay * Math.Pow(2, attempt - 1);
            
            _logger.Warning(
                "Transient error (attempt {Attempt}): {Error}. Retrying in {Delay}...",
                attempt, ex.Message, delay);
            
            await Task.Delay(delay, ct);
        }
    }
    
    throw lastException ?? new Exception("Unknown failure");
}

private async Task<PhaseResult> ExecuteCoreAsync(
    string agentName,
    string prompt,
    IEnumerable<string> documentPaths,
    Guid runId,
    CancellationToken ct)
{
    var stopwatch = Stopwatch.StartNew();
    var decisions = new List<Decision>();
    
    await using var client = new CopilotClient();
    await client.StartAsync();
    
    var agentPrompt = await _promptLoader.LoadPromptAsync(agentName, ct);
    
    await using var session = await client.CreateSessionAsync(new SessionConfig
    {
        Model = _config.Copilot.Model,
        SystemMessage = new SystemMessageConfig
        {
            Mode = SystemMessageMode.Append,
            Content = agentPrompt
        },
        InfiniteSession = new InfiniteSessionConfig
        {
            Enabled = true,
            CompactionThreshold = 0.8
        },
        OnUserInputRequest = async (request, invocation) =>
        {
            var classification = _signalClassifier.Classify(request.Prompt);
            var response = await HandleSignalAsync(
                classification, agentName, runId, invocation.CancellationToken);
            
            if (response?.ShouldPause == true)
            {
                await invocation.Session.AbortAsync();
                return new UserInputResponse { Answer = "", WasFreeform = true };
            }
            
            if (response?.DecisionMade != null)
            {
                decisions.Add(response.DecisionMade);
            }
            
            return new UserInputResponse
            {
                Answer = response?.Response ?? "Continue with recommended approach",
                WasFreeform = true
            };
        }
    });
    
    // Build SDK attachments
    var attachments = documentPaths
        .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
        .Select(p => new UserMessageDataAttachmentsItem
        {
            Type = UserMessageDataAttachmentsItemType.File,
            Path = Path.GetFullPath(p)
        })
        .ToList();
    
    // Process with cancellation support
    var processor = new SdkResponseProcessor(_signalClassifier, _logger);
    
    // Subscribe and send
    await session.SendAsync(new MessageOptions
    {
        Prompt = prompt,
        Attachments = attachments
    });
    
    var result = await processor.ProcessResponseAsync(session, ct);
    
    stopwatch.Stop();
    
    // Handle blocker signals
    if (result.Signal.Type == SignalType.Blocker)
    {
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = $"Blocker: {result.Signal.RawMatch}",
            OutputDocumentPath = result.OutputDocumentPath,
            DecisionsMade = decisions,
            Duration = stopwatch.Elapsed
        };
    }
    
    return new PhaseResult
    {
        Success = !result.WasAborted && result.Error == null,
        OutputDocumentPath = result.OutputDocumentPath,
        ErrorMessage = result.Error?.Message,
        DecisionsMade = decisions,
        Duration = stopwatch.Elapsed
    };
}
```

### Key Discoveries

- **SDK uses event-based pattern, not direct async/await** — CancellationToken integration requires TaskCompletionSource wrapper with ct.Register()
- **AbortAsync() for immediate session termination** — Use in OnUserInputRequest when pause is needed
- **InvocationContext.CancellationToken available in callbacks** — SDK provides its own token for callback operations
- **Existing timeout pattern (CreateLinkedTokenSource + CancelAfter) integrates seamlessly** — No changes needed to PhaseExecutor timeout logic
- **AutoRestart handles CLI crashes transparently** — Default behavior; sessions may need recreation after restart
- **SessionErrorEvent for error detection** — Check message for transient vs permanent error classification
- **Retry with exponential backoff for transient errors** — Network issues, rate limits, temporary unavailability
- **Blocker = pause, QuestionOpen = abort + pause** — Different signal types require different abort strategies

### Relevant Files/Resources

- [Program.cs](../Hugh/Program.cs) — Lines 206, 236 for timeout override TODOs; global CTS at line 22
- [PhaseExecutor.cs](../Hugh/Orchestration/PhaseExecutor.cs) — Lines 67-68 for existing timeout pattern
- [CopilotService.cs](../Hugh/Agents/CopilotService.cs) — Integration target; existing HandleSignalAsync
- [HugeConfig.cs](../Hugh/Configuration/HugeConfig.cs) — TimeoutConfig class for phase-specific timeouts
- [appsettings.json](../Hugh/appsettings.json) — Timeout configuration values
- [QuestionHandler.cs](../Hugh/Handlers/QuestionHandler.cs) — Existing question handling patterns
- [BlockerHandler.cs](../Hugh/Handlers/BlockerHandler.cs) — Blocker pause pattern

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|

*No organizational standards applicable to this research.*

---

## Conclusions

The GitHub.Copilot.SDK provides a complete foundation for integrating AI agents into the Hugh self-operating development loop. All identified TODOs have clear resolution paths:

### Implementation Recommendations

1. **Start with SDK package installation and basic client/session lifecycle** — Verify CLI communication works before proceeding

2. **Implement SdkResponseProcessor first** — This is the core integration point that enables:
   - Event-based response accumulation
   - SessionIdleEvent completion detection
   - SignalClassifier integration
   - Error handling with SessionErrorEvent

3. **OnUserInputRequest is critical for autonomy** — The callback intercepts agent questions:
   - Route through existing SignalClassifier.Classify()
   - Auto-respond via QuestionHandler patterns
   - Use session.AbortAsync() when pause is required (QuestionOpen, Blocker)

4. **Preserve existing architecture** — No changes needed to:
   - SignalClassifier/SignalType
   - QuestionHandler/BlockerHandler
   - PhaseExecutor timeout logic (CreateLinkedTokenSource + CancelAfter)
   - HugeConfig/TimeoutConfig structure

5. **Add retry resilience** — Transient errors (network, rate limits, CLI restart) should retry with exponential backoff; permanent errors (Blocker signals) should pause immediately

6. **Timeout override** — Implement ParseTimeoutOverride() and ApplyTimeoutOverride() in Program.cs to support `--timeout` CLI argument

### Risk Mitigations

| Risk | Mitigation |
|------|------------|
| SDK breaking changes (technical preview) | Wrap SDK in thin abstraction; version-pin in .csproj |
| CLI process instability | AutoRestart = true (default); retry logic for transient errors |
| Context window exhaustion | InfiniteSessionConfig with CompactionThreshold = 0.8 |
| Question response failures | Abort session cleanly; surface pause to orchestrator |
| Timeout race conditions | Linked CancellationTokenSource ensures clean cancellation |

### Estimated Implementation Effort

| Component | Effort | Priority |
|-----------|--------|----------|
| Add SDK package + verify connection | 1 hour | P0 |
| SdkResponseProcessor class | 2 hours | P0 |
| DocumentPathExtractor class | 1 hour | P0 |
| CopilotService.ExecutePhaseAsync rewrite | 3 hours | P0 |
| Program.cs timeout override | 30 min | P1 |
| Retry logic with exponential backoff | 1 hour | P1 |
| Unit tests for new components | 2 hours | P1 |
| Integration testing | 2 hours | P2 |

**Total estimated effort:** ~12-13 hours of focused development

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** 2026-02-05
**Completed Date:** 2026-02-05
**Status:** ✅ Complete
**Document Location:** /docs/research/009-github-copilot-sdk-integration.md

### Next Steps

This research is ready for handoff to `@pch-planner` to create a detailed implementation plan. The plan should:

1. Define the phase structure for implementing SDK integration
2. Specify exact file changes for each phase
3. Include test requirements and acceptance criteria
4. Account for the dependency order (SdkResponseProcessor → DocumentPathExtractor → CopilotService)
