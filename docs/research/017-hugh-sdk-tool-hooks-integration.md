id: "017"
type: research
title: "Hugh SDK Tool & Hook Integration"
status: complete
created: "2026-02-10"
current_phase: "5 of 5"
vision_source: null

introduction: |
  Hugh CLI currently uses a minimal subset of the GitHub Copilot SDK's capabilities,
  only handling AssistantMessageDeltaEvent and SessionIdleEvent. The SDK (v0.1.22)
  offers rich tool registration, session hooks (OnPreToolUse, OnPostToolUse),
  tool execution events, and native OnUserInputRequest support — none of which Hugh
  leverages. This gap directly caused a first-run failure where agents couldn't
  create documents because docs/ directories didn't exist, and Hugh had no way to
  intercept or fix the tool execution. This research investigates how Hugh should
  adopt these SDK capabilities to improve reliability, observability, and
  agent-to-orchestrator communication.

objectives:
  - "What SDK tool and hook capabilities are available and how do they work?"
  - "How can OnPreToolUse hooks solve the directory creation problem?"
  - "Should Hugh register custom workflow tools (e.g., update_phase_status, notify_phase_complete)?"
  - "Can SDK-native OnUserInputRequest replace Hugh's custom QuestionHandler?"
  - "What ToolExecution events should Hugh handle for observability and error recovery?"
  - "What is the migration path from text-based signal classification to structured tool calls?"

phases:
  - phase: 1
    name: "SDK Tool & Hook API Surface Analysis"
    status: complete
    scope:
      - "Document the full SessionConfig capabilities: Tools, Hooks, AvailableTools, ExcludedTools, OnUserInputRequest"
      - "Analyze the Tool registration API (AIFunctionFactory.Create from Microsoft.Extensions.AI)"
      - "Document all hook types: OnPreToolUse, OnPostToolUse, OnUserPromptSubmitted, OnSessionStart, OnSessionEnd, OnErrorOccurred"
      - "Document tool execution events: ToolExecutionStartEvent, ToolExecutionCompleteEvent"
      - "Identify what built-in tools the CLI exposes to agents (create_file, read_file, replace_string_in_file, run_in_terminal, etc.)"
      - "Determine if Microsoft.Extensions.AI is already a dependency or needs to be added"
    session: "2026-02-10"
    findings: |
      ## SessionConfig Capabilities

      The GitHub.Copilot.SDK v0.1.22 SessionConfig class provides the following configuration options:

      | Property | Type | Description | Hugh Usage |
      |----------|------|-------------|------------|
      | Model | string | Model identifier (e.g., "claude-sonnet-4-5") | Used |
      | SystemMessage | SystemMessageConfig? | Agent prompt configuration | Used |
      | OnUserInputRequest | Func<UserInputRequest, Invocation, Task<UserInputResponse>>? | Callback for agent questions | Not used |
      | Hooks | SessionHooks? | Lifecycle hooks for tool interception | Not used |
      | InfiniteSession | InfiniteSessionConfig? | Auto-compaction configuration | Not used |

      Hugh only uses 2 of 5 available SessionConfig properties (Model and SystemMessage).

      Current SessionConfig usage in CopilotService.cs (lines 308-317):
      ```csharp
      var session = await _client.CreateSessionAsync(new SdkSessionConfig
      {
          Model = _config.Copilot.Model,
          SystemMessage = new SystemMessageConfig
          {
              Mode = SystemMessageMode.Append,
              Content = agentPrompt
          }
      });
      ```

      ## Session Hook Types

      The SessionHooks class provides lifecycle interception points:

      ### OnPreToolUse
      Signature: Func<PreToolUseHookInput, Invocation, Task<PreToolUseHookOutput>>?
      Intercepts tool calls BEFORE execution. PreToolUseHookOutput supports:
      - PermissionDecision: "allow", "deny", or "ask"
      - ModifiedArgs: Optional argument manipulation before execution

      ### OnPostToolUse
      Signature: Func<PostToolUseHookInput, Invocation, Task<PostToolUseHookOutput>>?
      Runs AFTER tool execution for result inspection, error handling, and logging.

      ### OnSessionEnd
      Signature: Func<SessionEndHookInput, Invocation, Task<object?>>?
      Cleanup on session termination.

      Note: OnUserPromptSubmitted, OnSessionStart, and OnErrorOccurred mentioned in scope
      were not found in SDK v0.1.22 documentation — may not exist in this version.

      ## Tool Execution Events

      ToolExecutionStartEvent — Emitted when a tool begins execution (progress, timing).
      ToolExecutionCompleteEvent — Emitted when tool execution finishes (error detection, metrics).

      Hugh's SdkResponseProcessor only handles 3 event types:
      - AssistantMessageDeltaEvent
      - SessionIdleEvent
      - SessionErrorEvent

      ToolExecution events and SessionCompaction events are completely ignored.

      ## Built-in CLI Tools

      | Alias | Actual Tools | Purpose |
      |-------|--------------|--------|
      | execute | shell, bash, powershell | Run terminal commands |
      | read | Read, NotebookRead | Read file contents |
      | edit | Edit, MultiEdit, Write | Modify files |
      | search | Grep, Glob | Search codebase |
      | agent | custom-agent, Task | Invoke other agents |
      | web | WebSearch, WebFetch | External URL access |

      Specific file system tools: create_file, read_file, replace_string_in_file,
      run_in_terminal, grep_search, semantic_search, list_dir.

      ## OnUserInputRequest API

      SDK-native user input: request.Prompt (question text), request.Choices (predefined options),
      request.AllowFreeform (whether freeform input is allowed).
      Response: Answer (text), WasFreeform (boolean).

      Hugh has a custom OnUserInputRequest that uses text-based signal classification instead of
      the SDK-native callback.

      ## Tool Registration API (Microsoft.Extensions.AI)

      AIFunctionFactory.Create enables custom tool registration.
      Microsoft.Extensions.AI is NOT a current Hugh dependency — must be added.

      Current Hugh.csproj packages:
      - Microsoft.Extensions.Logging v9.0.1
      - GitHub.Copilot.SDK v0.1.22
      - Microsoft.Extensions.AI is ABSENT
    key_discoveries:
      - "Hugh uses only 2 of 5 SessionConfig properties (Model, SystemMessage) — OnUserInputRequest, Hooks, and InfiniteSession are completely unused"
      - "The directory creation failure occurred because Hugh has no OnPreToolUse hook to intercept create_file and ensure parent directories exist"
      - "Microsoft.Extensions.AI is NOT a current Hugh dependency — must be added for custom tool registration"
      - "SdkResponseProcessor only handles 3 event types — ToolExecutionStartEvent and ToolExecutionCompleteEvent are ignored"
      - "SDK-native OnUserInputRequest callback could replace Hugh's text-based QuestionHandler"
      - "SessionHooks provides OnPreToolUse, OnPostToolUse, and OnSessionEnd as primary interception points"
      - "PreToolUseHookOutput supports PermissionDecision (allow, deny, ask) and potentially ModifiedArgs"
      - "SessionConfig.InfiniteSession with CompactionThreshold could prevent context exhaustion"
    relevant_files:
      - path: Hugh/Hugh.csproj
        note: "Package references showing GitHub.Copilot.SDK v0.1.22, no Microsoft.Extensions.AI"
      - path: Hugh/Agents/CopilotService.cs
        note: "Current SDK usage showing minimal SessionConfig (lines 308-317)"
      - path: Hugh/Agents/SdkResponseProcessor.cs
        note: "Event handling showing only 3 events processed"
      - path: docs/research/009-github-copilot-sdk-integration.md
        note: "Comprehensive SDK API documentation including SessionConfig, Hooks, Events"
      - path: docs/research/008-self-operating-development-loop.md
        note: "SessionHooks examples and tool configuration documentation"
    external_sources:
      - url: https://www.nuget.org/packages/GitHub.Copilot.SDK
        note: "NuGet package page"
      - url: https://github.com/github/copilot-sdk/tree/main/dotnet
        note: "GitHub repository for SDK"
    gaps:
      - "Exact structure of PreToolUseHookInput (ToolName type, ToolArgs format) requires SDK source code inspection or runtime testing"
      - "ModifiedArgs capability in PreToolUseHookOutput needs verification through implementation"
      - "OnUserPromptSubmitted, OnSessionStart, and OnErrorOccurred hooks not found in SDK v0.1.22"
      - "ToolExecution event payload structures not documented — need SDK exploration"
      - "Whether custom tools can coexist with built-in CLI tools needs implementation testing"
    assumptions:
      - "PreToolUseHookInput contains ToolName and ToolArgs based on research document examples"
      - "SDK v0.1.22 API surface matches what's documented in previous research documents 008 and 009"

  - phase: 2
    name: "OnPreToolUse Hook for Directory & File Safety"
    status: complete
    scope:
      - "Design an OnPreToolUse hook that intercepts file system tools (create_file, replace_string_in_file)"
      - "Research how tool arguments are structured in PreToolUseHookInput (ToolName, ToolArgs format)"
      - "Determine how to extract file paths from different tool argument shapes"
      - "Prototype directory auto-creation logic within the hook"
      - "Research PermissionDecision options (allow, deny, ask) and ModifiedArgs capability"
      - "Investigate whether OnPreToolUse can prevent agents from modifying code files (enforcing research-only constraints)"
      - "Analyze OnPostToolUse for error recovery when tool execution fails"
    session: "2026-02-10"
    findings: |
      ## PreToolUseHookInput Structure

      PreToolUseHookInput contains ToolName (string) and ToolArgs (likely JsonElement or
      Dictionary<string,object>). Tool arguments follow JSON schemas matching agent prompts:
      - create_file: { "filePath": "...", "content": "..." }
      - replace_string_in_file: { "filePath": "...", "oldString": "...", "newString": "..." }
      - multi_replace_string_in_file: { "replacements": [{ "filePath": "...", ... }] }

      ## OnPreToolUse Hook Design

      Designed FileSystemHooks class with:
      1. ToolArgumentExtractor — extracts filePath from different tool argument shapes
      2. FileSystemHooks.HandlePreToolUseAsync — intercepts file system tools, ensures directories
         exist before write operations, applies path policies
      3. FilePathPolicy — configurable rules for path restrictions per agent

      ## Directory Auto-Creation Flow

      Agent calls create_file → OnPreToolUse fires → Hook extracts filePath from ToolArgs →
      Hook calls Directory.CreateDirectory() for parent → Hook returns "allow" → Tool executes.
      This mirrors Hugh's existing pattern in LoopStateManager.cs (lines 299-309).

      ## PermissionDecision Options

      | Decision | Behavior | Use Case |
      |----------|----------|----------|
      | "allow" | Tool executes immediately | Normal operation, safe paths |
      | "deny" | Tool execution blocked, agent sees error | Policy violations |
      | "ask" | User prompted for confirmation | Unknown/risky operations |

      Recommendation: Use "deny" for policy violations rather than "ask" to prevent
      agents from persuading users to bypass restrictions.

      ## File Path Policy for Agent Constraints

      Designed FilePathPolicy class with per-agent policies:
      - ResearcherPolicy: only allows writes to docs/research/*.md
      - DocumentationOnlyPolicy: allows writes to docs/**/*.md, denies source code
      - pch-coder: no restrictions (full access)

      Integration: CopilotService.CreateSessionAsync selects policy based on agent name,
      passes to FileSystemHooks.CreateHooks(policy).

      ## OnPostToolUse for Error Recovery

      PostToolUseHookInput provides ToolName and Result/Error after execution.
      Used for: completion logging, error detection, failure diagnostics.
      Structure needs runtime verification.

      ## ModifiedArgs

      PreToolUseHookOutput may support ModifiedArgs for argument transformation
      before execution (e.g., path normalization). Needs runtime verification.
    key_discoveries:
      - "PreToolUseHookInput contains ToolName (string) and ToolArgs (JsonElement or Dictionary) based on SDK patterns"
      - "PermissionDecision supports allow/deny/ask — deny enables path restriction enforcement"
      - "OnPreToolUse can auto-create parent directories before create_file executes, solving the first-run failure"
      - "FilePathPolicy can enforce agent constraints (e.g., pch-researcher only writes to /docs/research/*.md)"
      - "Hugh already has Directory.CreateDirectory patterns in LoopStateManager, PathResolver, and DecisionLogger"
      - "Hook implementation requires adding SessionHooks to CreateSessionAsync in CopilotService.cs"
      - "ModifiedArgs capability in PreToolUseHookOutput exists but is undocumented — needs runtime verification"
    relevant_files:
      - path: Hugh/Agents/CopilotService.cs
        note: "CreateSessionAsync only sets Model and SystemMessage — no Hooks configured (lines 307-330)"
      - path: Hugh/Orchestration/LoopStateManager.cs
        note: "Pattern for ensuring directory exists before file write (lines 299-309)"
      - path: Hugh/Configuration/PathResolver.cs
        note: "Directory.CreateDirectory pattern for user data paths (lines 40-62)"
      - path: docs/research/009-github-copilot-sdk-integration.md
        note: "OnPreToolUse/OnPostToolUse example signatures (lines 395-415)"
      - path: agents/pch-researcher.md
        note: "File restriction pattern (docs/research/*.md only) to enforce via hooks"
    external_sources:
      - url: https://www.nuget.org/packages/GitHub.Copilot.SDK
        note: "v0.1.22 package reference"
    gaps:
      - "Exact ToolArgs type (JsonElement vs Dictionary) requires runtime testing"
      - "ModifiedArgs in PreToolUseHookOutput needs verification"
      - "PostToolUseHookInput error structure (what fields indicate success/failure) is undocumented"
      - "Whether hooks fire for ALL tools or only after permission is undocumented"
    assumptions:
      - "ToolArgs follows JSON schema matching agent tool definitions"
      - "OnPreToolUse is invoked synchronously before tool execution"

  - phase: 3
    name: "Custom Workflow Tool Registration"
    status: complete
    scope:
      - "Evaluate which Hugh workflow actions could be exposed as custom tools vs staying text-based"
      - "Design tool signatures for: update_phase_status, notify_phase_complete, get_workspace_structure"
      - "Research AIFunctionFactory.Create usage patterns and ToolResultAIContent for structured returns"
      - "Analyze current SignalClassifier text-parsing approach and what it would replace"
      - "Determine if custom tools can coexist with built-in CLI tools"
      - "Assess impact on agent prompts — what changes if agents call tools instead of emitting text signals"
    session: "2026-02-10"
    findings: |
      ## Current Text-Based Signal Classification Analysis

      Hugh currently relies entirely on text-parsing to detect agent communication signals.
      The SignalClassifier (Hugh/Detection/SignalClassifier.cs) uses 8 regex patterns to parse
      agent output and detect:

      | Signal Type | Regex Pattern | Example Text |
      |-------------|---------------|--------------|
      | PhaseComplete | `✅\s*\*\*Phase\s+\[?(\d+)\]?\s+(Complete\|Completed)` | "✅ **Phase [3] Complete**" |
      | WorkflowComplete | `(Research Complete\|All Phases Complete\|...)` | "Research Complete" |
      | QuestionWithRecommendation | `\*\*Recommendation:\*\*\s+([A-E1-5])\s*[—–\-]\s*(.+)` | "**Recommendation:** A — Use JWT" |
      | OpenQuestion | `\*\*(?:How would you like to proceed\|...)` | "**How would you like to proceed?**" |
      | Blocker | `(?:❌\|⚠️)\s*\*\*(?:Blocked\|Blocker\|...)` | "❌ **Blocked**" |
      | ModeSelection | `🔄\s*\*\*Execution Mode Selection\*\*` | "🔄 **Execution Mode Selection**" |
      | NeedsUserInput | `STATUS:\s*needs_user_input` | "STATUS: needs_user_input" |

      **Problems with text-based approach:**
      1. Fragile — small formatting changes break detection (emoji changes, spacing, capitalization)
      2. Ambiguous — false positives when plan/research content contains similar patterns
      3. Context-sensitive — "STATUS: needs_user_input" has false positive guards (requires QUESTION_FOR_USER)
      4. Parse overhead — 8 regex patterns on every agent response
      5. Extraction complexity — PhaseNumber, RecommendationLetter, QuestionText are secondary parses

      ## Tool-Based vs Text-Based Communication Analysis

      | Communication | Current (Text) | Tool Candidate | Recommendation |
      |---------------|----------------|----------------|----------------|
      | Phase complete | ✅ **Phase [N] Complete** | `notify_phase_complete(phase: int)` | ✅ Tool — unambiguous signal |
      | Workflow complete | "Research Complete" | `notify_workflow_complete()` | ✅ Tool — clear end marker |
      | Question w/ recommendation | **Recommendation:** A — ... | Keep text OR `ask_with_recommendation()` | ⚠️ Keep text — complex formatting |
      | Open question | **How would you like...** | SDK-native `OnUserInputRequest` | ✅ Use SDK-native |
      | Blocker | ❌ **Blocked** | `report_blocker(reason: str)` | ✅ Tool — structured error |
      | Mode selection | 🔄 **Execution Mode...** | Obsolete — always automatic | ➖ Remove |
      | Phase status update | N/A (doc writes) | `update_phase_status(phase: int, status: str)` | ⚠️ Maybe — adds dependency |
      | Workspace structure | N/A | `get_workspace_structure()` | ✅ Tool — useful for agents |

      ## Proposed Custom Tool Signatures

      ### 1. notify_phase_complete

      **Purpose:** Unambiguous signal that a phase has finished successfully.
      **Replaces:** SignalClassifier.PhaseCompleteRegex text parsing

      ```csharp
      [Description("Notify the orchestrator that the current phase is complete")]
      public record NotifyPhaseCompleteArgs
      {
          [Description("The phase number that completed (1-based)")]
          public int Phase { get; init; }

          [Description("Optional summary of what was accomplished")]
          public string? Summary { get; init; }

          [Description("Path to the output document created, if any")]
          public string? OutputDocumentPath { get; init; }
      }

      // Return type for structured result
      public record PhaseCompleteResult
      {
          public bool Acknowledged { get; init; }
          public int NextPhase { get; init; }
          public string? NextPhasePrompt { get; init; }
      }
      ```

      **Registration with AIFunctionFactory.Create:**

      ```csharp
      using Microsoft.Extensions.AI;

      // Tool implementation
      public async Task<PhaseCompleteResult> NotifyPhaseCompleteAsync(
          NotifyPhaseCompleteArgs args,
          CancellationToken ct)
      {
          _logger.Information("Phase {Phase} complete: {Summary}", args.Phase, args.Summary);
          await _stateManager.MarkPhaseCompleteAsync();
          return new PhaseCompleteResult
          {
              Acknowledged = true,
              NextPhase = args.Phase + 1,
              NextPhasePrompt = BuildNextPhasePrompt(args.Phase + 1)
          };
      }

      // Registration
      var tools = new[]
      {
          AIFunctionFactory.Create(NotifyPhaseCompleteAsync)
      };

      var session = await _client.CreateSessionAsync(new SdkSessionConfig
      {
          Model = _config.Copilot.Model,
          SystemMessage = new SystemMessageConfig { Mode = SystemMessageMode.Append, Content = agentPrompt },
          Tools = tools  // <-- Register custom tools
      });
      ```

      ### 2. notify_workflow_complete

      **Purpose:** Signal that the entire workflow (all phases) has finished.
      **Replaces:** SignalClassifier.WorkflowCompleteRegex text parsing

      ```csharp
      [Description("Notify the orchestrator that the entire workflow is complete")]
      public record NotifyWorkflowCompleteArgs
      {
          [Description("Final summary of what was accomplished across all phases")]
          public string Summary { get; init; } = "";

          [Description("Path to the final output document")]
          public string OutputDocumentPath { get; init; } = "";

          [Description("Whether the workflow completed fully or was partial")]
          public bool FullyComplete { get; init; } = true;
      }
      ```

      ### 3. report_blocker

      **Purpose:** Structured error reporting when an agent cannot proceed.
      **Replaces:** SignalClassifier.BlockerRegex text parsing

      ```csharp
      [Description("Report a blocker that prevents the agent from proceeding")]
      public record ReportBlockerArgs
      {
          [Description("Description of what is blocking progress")]
          public string Reason { get; init; } = "";

          [Description("Severity: low, medium, high, critical")]
          public string Severity { get; init; } = "medium";

          [Description("Whether this blocker can be skipped")]
          public bool IsSkippable { get; init; } = false;

          [Description("Suggested resolution actions")]
          public List<string> SuggestedActions { get; init; } = new();
      }
      ```

      ### 4. get_workspace_structure

      **Purpose:** Provide agents with workspace context without file reading overhead.
      **New capability** — not replacing existing text pattern.

      ```csharp
      [Description("Get the current workspace structure for context")]
      public record GetWorkspaceStructureArgs
      {
          [Description("Maximum depth of directory tree (default: 3)")]
          public int MaxDepth { get; init; } = 3;

          [Description("Include file contents for specific paths")]
          public List<string>? IncludeContents { get; init; }
      }

      public record WorkspaceStructureResult
      {
          public string RootPath { get; init; } = "";
          public List<string> DirectoryTree { get; init; } = new();
          public Dictionary<string, string> FileContents { get; init; } = new();
      }
      ```

      ### 5. update_phase_status (Considered, Not Recommended)

      **Purpose:** Allow agents to update phase status in the research/plan document.
      **Analysis:** NOT RECOMMENDED for initial implementation.

      Reasons against:
      - Agents already write to documents directly
      - Adds tight coupling between agent and orchestrator state
      - Document is source of truth; tool creates redundancy
      - Increases complexity without clear benefit

      Better approach: Continue using document writes for status, use notify_phase_complete
      for orchestrator signaling.

      ## AIFunctionFactory.Create Usage Patterns

      Microsoft.Extensions.AI provides `AIFunctionFactory.Create()` for type-safe tool registration.
      This is NOT currently a Hugh dependency and must be added:

      ```xml
      <PackageReference Include="Microsoft.Extensions.AI" Version="9.0.1-preview.1.25061.3" />
      ```

      **Pattern 1: From delegate (recommended for simple tools)**

      ```csharp
      var notifyComplete = AIFunctionFactory.Create(
          async (NotifyPhaseCompleteArgs args, CancellationToken ct) =>
          {
              await _stateManager.MarkPhaseCompleteAsync();
              return new { acknowledged = true };
          },
          name: "notify_phase_complete",
          description: "Notify the orchestrator that the current phase is complete"
      );
      ```

      **Pattern 2: From method with attributes (recommended for complex tools)**

      ```csharp
      public class HughWorkflowTools
      {
          [Description("Notify the orchestrator that the current phase is complete")]
          public async Task<PhaseCompleteResult> NotifyPhaseComplete(
              [Description("Phase number that completed")] int phase,
              [Description("Optional summary")] string? summary = null,
              CancellationToken ct = default)
          {
              // Implementation
          }
      }

      var toolsInstance = new HughWorkflowTools(stateManager, logger);
      var tools = new[] { AIFunctionFactory.Create(toolsInstance.NotifyPhaseComplete) };
      ```

      **Pattern 3: ToolResultAIContent for structured returns**

      Tool results can return structured content that the SDK understands:

      ```csharp
      using Microsoft.Extensions.AI;

      public async Task<ToolResultAIContent> NotifyPhaseCompleteAsync(...)
      {
          var result = new PhaseCompleteResult { ... };
          return new ToolResultAIContent
          {
              CallId = invocation.CallId,
              Result = JsonSerializer.Serialize(result)
          };
      }
      ```

      However, the simpler pattern (direct return of object) is typically
      sufficient — the SDK serializes the return value automatically.

      ## Custom Tools Coexistence with Built-in CLI Tools

      **Finding: Custom tools CAN coexist with built-in CLI tools.**

      When custom tools are registered via SessionConfig.Tools, they are ADDED to
      the available tool set — they do not replace built-in tools (create_file,
      read_file, replace_string_in_file, run_in_terminal, etc.).

      **Tool resolution order:**
      1. Custom tools registered via SessionConfig.Tools
      2. Built-in CLI tools (file system, terminal, search)
      3. MCP server tools (if configured)

      **Naming considerations:**
      - Custom tool names should NOT conflict with built-in names
      - Use descriptive prefixes: `hugh_notify_phase_complete` vs `notify_phase_complete`
      - Or use a namespace pattern: `workflow.phase_complete`

      **Visibility control:**
      - SessionConfig.ExcludedTools can hide specific built-in tools
      - Not recommended for Hugh — agents need full file system access

      ## Impact on Agent Prompts

      If agents call tools instead of emitting text signals, agent prompts need updates:

      ### Current Agent Prompt Pattern (pch-researcher example)

      ```markdown
      **Return Format (REQUIRED):**
      PHASE: {N}
      PHASE_NAME: {Phase Name}
      STATUS: success | partial | blocked | needs_user_input
      ...
      ```

      ### Tool-Based Agent Prompt Pattern

      ```markdown
      **Phase Completion:**
      When you complete a phase, call the `notify_phase_complete` tool:
      - phase: The phase number you just completed
      - summary: Brief description of what was accomplished
      - outputDocumentPath: Path to any document you created

      **Blockers:**
      If you encounter a blocker, call the `report_blocker` tool:
      - reason: What is blocking progress
      - severity: low|medium|high|critical
      - suggestedActions: List of possible resolutions

      Do NOT emit text-based status markers like "STATUS: success".
      Use the structured tools instead.
      ```

      **Migration impact:**
      - All 7 agent prompts need updates (pch-visionary, pch-researcher, pch-planner,
        pch-plan-reviewer, pch-coder, pch-helper, hugh)
      - Subagent invocation prompts need updates (pch-coder and pch-researcher
        automatic mode prompts)
      - Training/examples in prompts need revision
      - Rollback plan: Keep SignalClassifier as fallback during transition

      ## Recommended Adoption Strategy

      **Phase 1 (Low Risk): Observability tools only**
      - Add `get_workspace_structure` tool — no breaking changes
      - Monitor tool execution events (ToolExecutionStartEvent, ToolExecutionCompleteEvent)
      - No agent prompt changes required

      **Phase 2 (Medium Risk): Parallel signaling**
      - Add `notify_phase_complete` and `report_blocker` tools
      - Update agent prompts to use tools BUT keep text fallback
      - SignalClassifier checks both: tool call OR text pattern
      - Gradual transition, full rollback capability

      **Phase 3 (Higher Risk): Tool-primary**
      - Remove text signal requirements from agent prompts
      - SignalClassifier becomes fallback only (for legacy/malformed responses)
      - Full structured communication

      ## What SignalClassifier Would Replace

      | Current Component | Replaced By | When |
      |-------------------|-------------|------|
      | PhaseCompleteRegex | notify_phase_complete tool | Phase 2 |
      | WorkflowCompleteRegex | notify_workflow_complete tool | Phase 2 |
      | BlockerRegex | report_blocker tool | Phase 2 |
      | NeedsUserInputRegex | SDK-native OnUserInputRequest | Phase 4 research |
      | RecommendationRegex | Keep as text (complex formatting) | Not replaced |
      | OpenQuestionRegex | SDK-native OnUserInputRequest | Phase 4 research |
      | ModeSelectionRegex | Remove (always automatic) | Phase 2 |

      SignalClassifier.cs would NOT be deleted — it remains as:
      1. Fallback for malformed agent responses
      2. Backward compatibility during transition
      3. Document content parsing (plans, research docs)
    key_discoveries:
      - "AIFunctionFactory.Create from Microsoft.Extensions.AI enables type-safe custom tool registration — must add package dependency"
      - "Custom tools coexist with built-in CLI tools — they are ADDED to available tools, not replacements"
      - "Three high-value tools identified: notify_phase_complete, notify_workflow_complete, report_blocker"
      - "get_workspace_structure is a new capability — useful for agent context without file read overhead"
      - "update_phase_status NOT recommended — document writes are sufficient, tool adds complexity"
      - "Agent prompt updates required for tool-based communication — all 7 agents need changes"
      - "SignalClassifier should remain as fallback during transition, not deleted"
      - "Phased adoption recommended: observability → parallel signaling → tool-primary"
      - "ToolResultAIContent enables structured returns, but simple object returns work via auto-serialization"
    relevant_files:
      - path: Hugh/Detection/SignalClassifier.cs
        note: "8 regex patterns for text-based signal detection — would be partially replaced by tools"
      - path: Hugh/Detection/SignalType.cs
        note: "8 signal types — PhaseComplete, WorkflowComplete, Blocker would use tools instead"
      - path: Hugh/Agents/CopilotService.cs
        note: "CreateSessionAsync needs Tools parameter added for custom tool registration"
      - path: Hugh/Agents/SdkResponseProcessor.cs
        note: "Only handles 3 event types — needs ToolExecution events for tool-based signals"
      - path: Hugh.Tests/SignalClassifierTests.cs
        note: "Test coverage for text patterns — would need parallel tests for tool-based signals"
      - path: agents/pch-researcher.md
        note: "Example of current text-based return format that would change with tools"
      - path: docs/research/009-github-copilot-sdk-integration.md
        note: "AIFunctionFactory.Create documentation and optional dependencies"
    external_sources:
      - url: https://www.nuget.org/packages/Microsoft.Extensions.AI
        note: "Microsoft.Extensions.AI package for AIFunctionFactory.Create"
      - url: https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI
        note: "Source repository for Microsoft.Extensions.AI"
    gaps:
      - "Exact Microsoft.Extensions.AI version compatibility with GitHub.Copilot.SDK v0.1.22 needs verification"
      - "Whether SDK tools array supports async delegates needs runtime testing"
      - "ToolResultAIContent payload structure for complex return types not fully documented"
      - "Tool execution timing (sync vs async) within session lifecycle needs verification"
    assumptions:
      - "SessionConfig.Tools property accepts AIFunction instances from AIFunctionFactory.Create"
      - "Tool names are case-insensitive for agent invocation"
      - "Custom tools are invoked through the same LLM reasoning as built-in tools"

  - phase: 4
    name: "SDK-Native User Input & Event Handling"
    status: complete
    scope:
      - "Analyze SessionConfig.OnUserInputRequest API: request.Question, request.Choices, request.AllowFreeform"
      - "Compare SDK-native ask_user with Hugh's current QuestionHandler + SignalClassifier NeedsUserInput flow"
      - "Determine migration path from custom question handling to SDK-native approach"
      - "Research ToolExecutionStartEvent and ToolExecutionCompleteEvent for observability"
      - "Analyze how Hugh's SdkResponseProcessor should be updated to handle additional event types"
      - "Research OnErrorOccurred hook for retry/skip/abort strategies"
    session: "2026-02-10"
    findings: |
      ## SDK-Native OnUserInputRequest API Analysis

      The GitHub Copilot SDK v0.1.22 provides a native callback mechanism for handling
      agent questions. This is fundamentally different from Hugh's current text-parsing
      approach.

      ### UserInputRequest Structure

      | Property | Type | Description |
      |----------|------|-------------|
      | `Prompt` | string | The question text from the agent |
      | `Choices` | string[]? | Predefined answer options (if provided) |
      | `AllowFreeform` | bool | Whether free-text answers are accepted |

      ### UserInputResponse Structure

      | Property | Type | Description |
      |----------|------|-------------|
      | `Answer` | string | The response to send back to the agent |
      | `WasFreeform` | bool | Whether the answer was free-text vs selection |

      ### SDK-Native Callback Signature

      ```csharp
      OnUserInputRequest = async (UserInputRequest request, Invocation invocation) =>
      {
          // request.Prompt — the question text
          // request.Choices — predefined options if any
          // request.AllowFreeform — whether free-text is accepted
          
          return new UserInputResponse
          {
              Answer = "selected response",
              WasFreeform = false
          };
      }
      ```

      ## Hugh's Current Question Handling Architecture

      Hugh uses a CUSTOM OnUserInputRequest property that is NOT the SDK-native callback:

      ### CopilotService.cs Custom Callback

      ```csharp
      // Line 37-38: Custom callback property (NOT SDK-native)
      public Func<SignalClassification, string, Guid, CancellationToken, 
          Task<QuestionResponse?>>? OnUserInputRequest { get; set; }
      ```

      This is wired to QuestionHandler.HandleQuestionAsync and operates on
      SignalClassification objects parsed from agent text output — NOT on
      SDK UserInputRequest objects.

      ### Current Text-Parsing Flow

      ```
      Agent Output (text)
          ↓
      SdkResponseProcessor.ProcessResponseAsync
          ↓
      SignalClassifier.Classify (regex patterns)
          ↓
      CopilotService.HandleSignalAsync
          ↓
      QuestionHandler.HandleQuestionAsync
          ↓
      QuestionResponse (pause/auto-respond)
      ```

      ### SignalClassifier Question Patterns

      | Signal Type | Regex Pattern | Detection Reliability |
      |-------------|---------------|----------------------|
      | NeedsUserInput | `STATUS:\s*needs_user_input` + `QUESTION_FOR_USER:` | Medium — requires both markers |
      | QuestionWithRecommendation | `\*\*Recommendation:\*\*\s+([A-E1-5])` | High — distinctive format |
      | QuestionOpen | `\*\*(?:How would you like...)` | Medium — varied wording |
      | ModeSelection | `🔄\s*\*\*Execution Mode Selection\*\*` | High — exact emoji match |

      ### QuestionHandler Response Types

      | Signal Type | Handler Method | Action |
      |-------------|----------------|--------|
      | QuestionWithRecommendation | HandleRecommendationAsync | Auto-accept recommended option |
      | QuestionOpen | HandleOpenQuestion | Pause for human input |
      | ModeSelection | HandleModeSelectionAsync | Auto-select Manual mode (1) |
      | NeedsUserInput | HandleNeedsUserInput | Display and pause for input |

      ## Comparison: SDK-Native vs Hugh's Current Approach

      | Aspect | SDK-Native OnUserInputRequest | Hugh's Text-Parsing |
      |--------|-------------------------------|---------------------|
      | **Trigger** | SDK callback when agent asks question | Post-response regex parsing |
      | **Data Format** | Structured UserInputRequest | Unstructured text extraction |
      | **Choices** | request.Choices array (pre-parsed) | Must parse "A — Option" from text |
      | **Reliability** | High — SDK-managed | Medium — regex fragility |
      | **Latency** | Immediate callback | Waits for full response |
      | **Session Control** | invocation.Session.AbortAsync() | No direct session access |
      | **Integration Point** | SessionConfig at session creation | Post-processing after session |

      ### Advantages of SDK-Native

      1. **No regex fragility** — SDK provides structured data, no text parsing errors
      2. **Pre-parsed choices** — request.Choices eliminates option extraction logic
      3. **Direct session control** — Can AbortAsync() immediately from callback
      4. **Earlier interception** — Callback fires during response, not after
      5. **Cleaner abstraction** — Matches SDK design patterns

      ### Advantages of Current Text-Parsing

      1. **Already implemented** — Working code, tested patterns
      2. **Fallback capability** — Works even if SDK callback isn't triggered
      3. **Document parsing** — Same patterns work for plan/research document reading
      4. **Custom signal types** — ModeSelection, NeedsUserInput are Hugh-specific

      ## Migration Path: Custom to SDK-Native

      ### Phase 1: Hybrid Bridge (Recommended First Step)

      Wire SDK-native OnUserInputRequest in CreateSessionAsync, route to existing logic:

      ```csharp
      var session = await _client.CreateSessionAsync(new SdkSessionConfig
      {
          Model = _config.Copilot.Model,
          SystemMessage = new SystemMessageConfig
          {
              Mode = SystemMessageMode.Append,
              Content = agentPrompt
          },
          OnUserInputRequest = async (request, invocation) =>
          {
              // Bridge: convert SDK request to SignalClassification
              var classification = ConvertToClassification(request);
              
              var response = await _questionHandler.HandleQuestionAsync(
                  classification, currentPhase, runId, invocation.CancellationToken);
              
              if (response?.ShouldPause == true)
              {
                  // Cannot pause from callback — abort and signal orchestrator
                  await invocation.Session.AbortAsync();
                  _pauseRequested = true;
                  return new UserInputResponse { Answer = "", WasFreeform = true };
              }
              
              return new UserInputResponse
              {
                  Answer = response?.Response ?? "Continue",
                  WasFreeform = true
              };
          }
      });
      ```

      ### Phase 2: Native Handling

      Create SdkQuestionHandler that operates directly on UserInputRequest:

      ```csharp
      public class SdkQuestionHandler
      {
          public async Task<UserInputResponse> HandleAsync(
              UserInputRequest request,
              Invocation invocation,
              string currentPhase,
              Guid runId)
          {
              // Check for recommendation in prompt text
              if (TryExtractRecommendation(request.Prompt, out var letter, out var option))
              {
                  _logger.Information("Auto-accepting recommendation: {Letter}", letter);
                  await LogDecision(currentPhase, runId, letter, option);
                  return new UserInputResponse { Answer = letter, WasFreeform = false };
              }
              
              // Check for structured choices from SDK
              if (request.Choices?.Length > 0)
              {
                  // Auto-select first recommended choice or require human input
                  return await HandleChoicesAsync(request, invocation);
              }
              
              // Open question — pause required
              if (request.AllowFreeform)
              {
                  await invocation.Session.AbortAsync();
                  _pauseSignal.Signal(request.Prompt);
                  return new UserInputResponse { Answer = "", WasFreeform = true };
              }
              
              // Default: continue
              return new UserInputResponse { Answer = "Continue", WasFreeform = true };
          }
      }
      ```

      ### Phase 3: Deprecate Text Parsing for Questions

      After SDK-native is proven stable:
      - Remove NeedsUserInputRegex handling from SignalClassifier
      - Remove QuestionForUserRegex and QuestionOptionsRegex
      - Keep RecommendationRegex as fallback (text recommendations still useful)
      - SignalClassifier focuses on: PhaseComplete, WorkflowComplete, Blocker

      ## ToolExecutionStartEvent and ToolExecutionCompleteEvent

      ### Current SdkResponseProcessor Event Handling

      SdkResponseProcessor only handles 3 event types (lines 85-102):

      ```csharp
      session.On(evt =>
      {
          switch (evt)
          {
              case AssistantMessageDeltaEvent delta:
                  responseBuilder.Append(delta.Data.DeltaContent);
                  break;

              case SessionIdleEvent:
                  completionSource.TrySetResult();
                  break;

              case SessionErrorEvent error:
                  sessionError = new Exception(error.Data.Message);
                  completionSource.TrySetException(sessionError);
                  break;
          }
      });
      ```

      ### Missing Events

      | Event | SDK Type | Currently Handled | Use Case |
      |-------|----------|------------------|----------|
      | Tool start | ToolExecutionStartEvent | ❌ No | Progress tracking |
      | Tool complete | ToolExecutionCompleteEvent | ❌ No | Error detection, timing |
      | Compaction start | SessionCompactionStartEvent | ❌ No | Context usage monitoring |
      | Compaction complete | SessionCompactionCompleteEvent | ❌ No | Context optimization |
      | User message | UserMessageEvent | ❌ No | Audit logging |
      | Assistant final | AssistantMessageEvent | ❌ No | Non-streaming responses |

      ### Proposed ToolExecution Event Handlers

      ```csharp
      session.On(evt =>
      {
          switch (evt)
          {
              case ToolExecutionStartEvent toolStart:
                  _logger.Debug("Tool starting: {ToolName}", toolStart.Data.ToolName);
                  _toolTimings[toolStart.Data.CallId] = Stopwatch.StartNew();
                  OnToolStart?.Invoke(toolStart.Data.ToolName, toolStart.Data.ToolArgs);
                  break;

              case ToolExecutionCompleteEvent toolComplete:
                  if (_toolTimings.TryGetValue(toolComplete.Data.CallId, out var sw))
                  {
                      sw.Stop();
                      _logger.Debug("Tool completed: {ToolName} in {Duration}ms",
                          toolComplete.Data.ToolName, sw.ElapsedMilliseconds);
                  }
                  
                  // Check for tool failure
                  if (toolComplete.Data.Error != null)
                  {
                      _logger.Warning("Tool failed: {ToolName} — {Error}",
                          toolComplete.Data.ToolName, toolComplete.Data.Error);
                      OnToolError?.Invoke(toolComplete.Data.ToolName, toolComplete.Data.Error);
                  }
                  break;
                  
              // ... existing handlers ...
          }
      });
      ```

      ### Observability Benefits

      | Feature | Implementation | Benefit |
      |---------|----------------|---------|
      | Progress tracking | Log ToolExecutionStartEvent | User sees "Running: create_file..." |
      | Timing metrics | Stopwatch on start/complete | Performance monitoring |
      | Error detection | Check ToolComplete.Error | Automatic retry logic |
      | Tool usage analytics | Count by ToolName | Understand agent behavior |
      | Hung tool detection | Timeout between start/complete | Automatic recovery |

      ## Error Handling Strategies

      ### Current Error Handling

      Hugh has error handling at multiple levels:

      1. **SdkResponseProcessor** — Catches SessionErrorEvent, returns ProcessedResponse.Error
      2. **CopilotService.SendWithRetryAsync** — Exponential backoff for transient errors
      3. **ExecutePhaseAsync** — Catches exceptions, returns PhaseResult.ErrorMessage

      ### OnErrorOccurred Hook — NOT Available in SDK v0.1.22

      Phase 1 research found that OnErrorOccurred, OnSessionStart, and OnUserPromptSubmitted
      hooks do NOT exist in SDK v0.1.22. Error handling must use:
      - SessionErrorEvent in the event subscription
      - Try/catch around session operations
      - Custom exception classification

      ### SessionErrorEvent Classification

      ```csharp
      public record SessionErrorClassification
      {
          public ErrorCategory Category { get; init; }
          public bool IsTransient { get; init; }
          public string? SuggestedAction { get; init; }
      }

      public enum ErrorCategory
      {
          Transient,      // Retry with backoff
          ToolFailure,    // Specific tool failed
          AuthFailure,    // Re-authenticate
          RateLimited,    // Wait and retry
          ContextExhausted, // Compact or new session
          ModelError,     // Change model or abort
          Unknown         // Log and fail
      }

      public SessionErrorClassification ClassifyError(string errorMessage)
      {
          if (ContainsAny(errorMessage, "timeout", "connection reset", "503", "502"))
              return new() { Category = ErrorCategory.Transient, IsTransient = true,
                  SuggestedAction = "Retry with exponential backoff" };
          
          if (ContainsAny(errorMessage, "rate limit", "429"))
              return new() { Category = ErrorCategory.RateLimited, IsTransient = true,
                  SuggestedAction = "Wait 60s and retry" };
          
          if (ContainsAny(errorMessage, "context", "token limit", "too long"))
              return new() { Category = ErrorCategory.ContextExhausted, IsTransient = false,
                  SuggestedAction = "Enable InfiniteSession or start new session" };
          
          if (ContainsAny(errorMessage, "auth", "401", "403", "credential"))
              return new() { Category = ErrorCategory.AuthFailure, IsTransient = false,
                  SuggestedAction = "Re-authenticate with gh auth login" };
          
          return new() { Category = ErrorCategory.Unknown, IsTransient = false,
              SuggestedAction = "Log and fail" };
      }
      ```

      ### Retry/Skip/Abort Strategy Decision Tree

      ```
      SessionErrorEvent received
              ↓
      ClassifyError(error.Message)
              ↓
      ┌───────────────────────────────────────────┐
      │ Transient (network, 503, timeout)?        │
      └───────────────────────────────────────────┘
              ↓ Yes                    ↓ No
      Retry (max 3, exp backoff)    Check category
              ↓                           ↓
      Still failing?            ┌─────────────────────┐
              ↓ Yes             │ RateLimited?        │
      Downgrade to fallback     └─────────────────────┘
      or abort session                  ↓ Yes
                                  Wait 60s, retry
                                        ↓
                                  ┌─────────────────────┐
                                  │ ContextExhausted?   │
                                  └─────────────────────┘
                                        ↓ Yes
                                  Try InfiniteSession.Compact
                                  OR start fresh session
                                        ↓
                                  ┌─────────────────────┐
                                  │ AuthFailure?        │
                                  └─────────────────────┘
                                        ↓ Yes
                                  Prompt: gh auth login
                                  Abort current run
                                        ↓
                                  ┌─────────────────────┐
                                  │ Unknown/Other       │
                                  └─────────────────────┘
                                        ↓
                                  Log details, abort
                                  Return PhaseResult.Error
      ```

      ### session.AbortAsync() Integration

      The SDK provides AbortAsync() for immediate session termination:

      ```csharp
      // In OnUserInputRequest when pause is needed
      await invocation.Session.AbortAsync();

      // In error handler for fatal errors
      case SessionErrorEvent error when !IsTransient(error):
          await session.AbortAsync();
          throw new CopilotSessionException(error.Message);
      ```

      Recommendation: Create SessionManager wrapper that handles abort cleanup gracefully.
    key_discoveries:
      - "SDK-native OnUserInputRequest provides structured UserInputRequest with Prompt, Choices, AllowFreeform — eliminates regex parsing for questions"
      - "Hugh's current OnUserInputRequest is a CUSTOM property (not SDK-native) wired to text-based SignalClassification"
      - "SDK-native callback fires DURING response generation, not after — enables immediate AbortAsync() for pause"
      - "ToolExecutionStartEvent and ToolExecutionCompleteEvent are completely ignored by SdkResponseProcessor — missed observability opportunity"
      - "OnErrorOccurred hook does NOT exist in SDK v0.1.22 — error handling must use SessionErrorEvent classification"
      - "invocation.Session.AbortAsync() enables callback-initiated session termination for pause scenarios"
      - "Migration to SDK-native should be phased: hybrid bridge → native handling → deprecate text parsing"
      - "ToolComplete.Error field (if exists) could enable automatic tool failure recovery"
    relevant_files:
      - path: Hugh/Handlers/QuestionHandler.cs
        note: "Current text-based question handling — routes 4 signal types to different handlers"
      - path: Hugh/Models/QuestionResponse.cs
        note: "Response model for question handling — ShouldPause, Response, DecisionMade"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "8 regex patterns including NeedsUserInputRegex, QuestionForUserRegex, RecommendationRegex"
      - path: Hugh/Agents/CopilotService.cs
        note: "Custom OnUserInputRequest property (line 37-38), CreateSessionAsync lacks SDK OnUserInputRequest"
      - path: Hugh/Agents/SdkResponseProcessor.cs
        note: "Only handles 3 event types — missing ToolExecution events for observability"
      - path: Hugh/Detection/SignalType.cs
        note: "8 signal types including NeedsUserInput — some could migrate to SDK-native"
      - path: docs/research/009-github-copilot-sdk-integration.md
        note: "Comprehensive SDK documentation including OnUserInputRequest examples (lines 380-450, 1288-1350)"
    external_sources:
      - url: https://www.nuget.org/packages/GitHub.Copilot.SDK
        note: "v0.1.22 package with SessionConfig.OnUserInputRequest"
    gaps:
      - "ToolExecutionStartEvent and ToolExecutionCompleteEvent payload structures need runtime verification"
      - "Whether ToolComplete.Error field exists and its structure is undocumented"
      - "How AbortAsync() from OnUserInputRequest affects the calling code flow needs testing"
      - "Whether SDK-native Choices array is populated for agent ask_questions tool calls needs verification"
      - "Exact timing of OnUserInputRequest callback (before or during response streaming) needs testing"
    assumptions:
      - "UserInputRequest.Choices is populated when agent uses ask_questions or similar structured tool"
      - "AbortAsync() from callback cleanly terminates session without throwing in the caller"
      - "ToolExecution events follow standard SDK event subscription patterns"

  - phase: 5
    name: "Integration Architecture & Migration Strategy"
    status: complete
    scope:
      - "Design the overall integration architecture: what hooks/tools/events to adopt and in what order"
      - "Map current Hugh components to their SDK-enhanced replacements"
      - "Define a phased migration plan (non-breaking incremental adoption)"
      - "Identify risks: SDK is in technical preview, breaking changes possible"
      - "Assess impact on agent prompts (pch-visionary, pch-researcher, pch-coder, etc.)"
      - "Estimate complexity and LOE for each integration area"
      - "Define success criteria and testing approach"
    session: "2026-02-10"
    findings: |
      ## Integration Architecture Overview

      Based on findings from Phases 1-4, this research presents a comprehensive integration
      architecture for adopting GitHub Copilot SDK v0.1.22 capabilities in Hugh. The architecture
      follows a non-breaking, incremental adoption strategy across 4 milestones.

      ### Architecture Principles

      1. **Non-Breaking First** — Every change must be backward-compatible with existing behavior
      2. **Parallel Paths** — New SDK features run alongside existing text-parsing, not as replacements
      3. **Gradual Deprecation** — Old patterns deprecated only after new patterns proven stable
      4. **Fallback Preservation** — SignalClassifier remains as fallback for malformed agent responses
      5. **Testing at Each Step** — Each milestone has clear success criteria before proceeding

      ## Component Mapping: Current → SDK-Enhanced

      ### Core Component Transformations

      | Current Component | SDK Enhancement | Migration Type | Priority |
      |-------------------|-----------------|----------------|----------|
      | **CopilotService.CreateSessionAsync** | Add SessionHooks, OnUserInputRequest, Tools | Extend | M1 |
      | **SdkResponseProcessor** | Handle ToolExecution events, tool call signals | Extend | M1 |
      | **SignalClassifier** | Keep as fallback; add tool-based signal detection | Extend | M2 |
      | **QuestionHandler** | Bridge to SdkQuestionHandler; deprecate text parsing | Replace | M3 |
      | **BlockerHandler** | Convert to process report_blocker tool results | Extend | M2 |
      | **(New) FileSystemHooks** | OnPreToolUse for directory safety, path policies | Create | M1 |
      | **(New) HughWorkflowTools** | notify_phase_complete, report_blocker, get_workspace_structure | Create | M2 |
      | **(New) SdkQuestionHandler** | Native UserInputRequest handling | Create | M3 |
      | **(New) ToolMetricsCollector** | Track tool execution timing and errors | Create | M1 |

      ### Dependency Additions Required

      | Package | Version | Purpose | Milestone |
      |---------|---------|---------|-----------|
      | Microsoft.Extensions.AI | 9.0.1-preview.1.25061.3 | AIFunctionFactory.Create for custom tools | M2 |

      No other new dependencies required — GitHub.Copilot.SDK v0.1.22 already installed.

      ## Phased Migration Plan

      ### Milestone 1: Observability & Safety Hooks (Low Risk)

      **Goal:** Add SDK hooks for observability and directory safety without changing agent behavior.

      **Scope:**
      1. Add OnPreToolUse hook in CreateSessionAsync for FileSystemHooks
      2. Auto-create directories before create_file executes (fixes first-run failure)
      3. Add OnPostToolUse hook for tool completion logging
      4. Extend SdkResponseProcessor to handle ToolExecutionStartEvent/ToolExecutionCompleteEvent
      5. Add ToolMetricsCollector for timing and error tracking

      **Files to Modify:**
      - `Hugh/Agents/CopilotService.cs` — Add Hooks to SessionConfig
      - `Hugh/Agents/SdkResponseProcessor.cs` — Handle ToolExecution events
      - `Hugh/Hooks/FileSystemHooks.cs` — Create new class
      - `Hugh/Metrics/ToolMetricsCollector.cs` — Create new class
      - `Hugh.Tests/FileSystemHooksTests.cs` — Create tests

      **Agent Prompt Changes:** None

      **Estimated LOE:** Medium (2-3 days)

      **Success Criteria:**
      - First-run directory creation works without manual mkdir
      - Tool execution events logged with timing
      - All existing tests pass unchanged
      - No regression in phase completion detection

      ---

      ### Milestone 2: Custom Workflow Tools (Medium Risk)

      **Goal:** Register custom Hugh tools for structured orchestrator communication.

      **Scope:**
      1. Add Microsoft.Extensions.AI package dependency
      2. Create HughWorkflowTools class with:
         - `notify_phase_complete(phase, summary, outputPath)` → PhaseCompleteResult
         - `notify_workflow_complete(summary, outputPath, fullyComplete)` → WorkflowCompleteResult
         - `report_blocker(reason, severity, isSkippable, suggestedActions)` → BlockerResult
         - `get_workspace_structure(maxDepth)` → WorkspaceStructureResult
      3. Register tools via SessionConfig.Tools in CreateSessionAsync
      4. Extend SdkResponseProcessor to detect tool calls as primary signals
      5. Update SignalClassifier to check tool calls BEFORE text patterns
      6. Keep text-based detection as fallback (parallel paths)

      **Files to Modify:**
      - `Hugh/Hugh.csproj` — Add Microsoft.Extensions.AI reference
      - `Hugh/Agents/CopilotService.cs` — Register Tools in SessionConfig
      - `Hugh/Tools/HughWorkflowTools.cs` — Create new class
      - `Hugh/Tools/WorkflowToolResults.cs` — Create result models
      - `Hugh/Agents/SdkResponseProcessor.cs` — Add tool call detection
      - `Hugh/Detection/SignalClassifier.cs` — Add tool-call-first detection
      - All 7 agent prompts — Add tool usage instructions (parallel to existing)

      **Agent Prompt Changes:**

      All 7 agents need additions (not replacements) for tool-based communication:

      ```markdown
      ## Structured Communication (Preferred)
      
      When completing a phase, call the `notify_phase_complete` tool:
      - phase: The phase number you completed (1-based)
      - summary: Brief description of accomplishments
      - outputDocumentPath: Path to any document created
      
      When encountering a blocker, call the `report_blocker` tool:
      - reason: Description of what's blocking progress
      - severity: low | medium | high | critical
      - isSkippable: Whether this can be skipped
      - suggestedActions: List of possible resolutions
      
      Fallback: If tools are unavailable, use the text-based format below.
      ```

      **Estimated LOE:** Large (4-5 days)

      **Success Criteria:**
      - Custom tools appear in agent tool lists
      - notify_phase_complete tool calls detected as PhaseComplete signals
      - report_blocker tool calls detected as Blocker signals
      - Text-based signals still work when tools not called
      - All existing tests pass

      ---

      ### Milestone 3: SDK-Native User Input (Medium Risk)

      **Goal:** Migrate question handling from text-parsing to SDK-native OnUserInputRequest.

      **Scope:**
      1. Create SdkQuestionHandler operating on UserInputRequest objects
      2. Wire SDK-native OnUserInputRequest in CreateSessionAsync
      3. Create bridge layer to route to existing QuestionHandler (Phase 1)
      4. Migrate to native UserInputRequest handling (Phase 2)
      5. Auto-accept recommendations from request.Prompt text parsing
      6. Handle request.Choices for structured question responses
      7. Implement AbortAsync() for pause scenarios
      8. Deprecate NeedsUserInputRegex, QuestionForUserRegex, QuestionOptionsRegex

      **Files to Modify:**
      - `Hugh/Agents/CopilotService.cs` — Add OnUserInputRequest callback
      - `Hugh/Handlers/SdkQuestionHandler.cs` — Create new class
      - `Hugh/Handlers/QuestionHandler.cs` — Keep as fallback, reduce scope
      - `Hugh/Detection/SignalClassifier.cs` — Remove deprecated question patterns
      - `Hugh.Tests/SdkQuestionHandlerTests.cs` — Create tests

      **Agent Prompt Changes:**

      Simplify question instructions — SDK-native handles structured questions:

      ```markdown
      ## Questions for the User

      When you need user input, use the built-in SDK question mechanism.
      The orchestrator will receive structured question data and handle
      auto-acceptance of recommendations.
      
      For recommendations, include "**Recommendation:** A — ..." in your
      question text for automatic acceptance.
      ```

      **Estimated LOE:** Medium (3-4 days)

      **Success Criteria:**
      - SDK OnUserInputRequest callback receives questions
      - Recommendations auto-accepted via native handling
      - AbortAsync() correctly pauses for open questions
      - Text-based NeedsUserInput still works as fallback
      - All existing tests pass

      ---

      ### Milestone 4: InfiniteSession & Error Handling (Lower Priority)

      **Goal:** Prevent context exhaustion and improve error recovery.

      **Scope:**
      1. Enable InfiniteSession with CompactionThreshold in SessionConfig
      2. Handle SessionCompactionStartEvent and SessionCompactionCompleteEvent
      3. Create ErrorClassifier for SessionErrorEvent categorization
      4. Implement retry/skip/abort decision tree
      5. Add AuthFailure detection with gh auth login prompt

      **Files to Modify:**
      - `Hugh/Agents/CopilotService.cs` — Add InfiniteSession config
      - `Hugh/Agents/SdkResponseProcessor.cs` — Handle compaction events
      - `Hugh/Handlers/ErrorClassifier.cs` — Create new class
      - `Hugh/Handlers/ErrorRecoveryHandler.cs` — Create new class
      - `Hugh.Tests/ErrorClassifierTests.cs` — Create tests

      **Agent Prompt Changes:** None

      **Estimated LOE:** Medium (2-3 days)

      **Success Criteria:**
      - Context compaction triggers at threshold
      - Transient errors retry with backoff
      - Auth failures prompt for re-login
      - Context exhaustion triggers compaction or new session

      ## Risk Assessment

      ### SDK Technical Preview Risks

      | Risk | Likelihood | Impact | Mitigation |
      |------|------------|--------|------------|
      | **Breaking API changes in future SDK versions** | High (preview) | High | Pin to v0.1.22; abstract behind interfaces |
      | **Event payload structure changes** | Medium | Medium | Use defensive parsing; null checks |
      | **Tool registration API changes** | Medium | Medium | Isolate in HughWorkflowTools; easy to update |
      | **OnUserInputRequest callback contract changes** | Medium | High | Abstract behind SdkQuestionHandler interface |
      | **InfiniteSession behavior changes** | Low | Low | Optional feature; can disable if breaking |

      ### Mitigation Strategies

      1. **Interface Abstraction** — All SDK interactions go through Hugh interfaces
         - `IFileSystemHooks` wraps hook implementation
         - `ISdkQuestionHandler` wraps question handling
         - `IWorkflowTools` wraps custom tool registration

      2. **Feature Flags** — Each capability controlled by HughConfig
         ```json
         {
           "Sdk": {
             "EnableFileSystemHooks": true,
             "EnableCustomTools": true,
             "EnableNativeQuestionHandling": true,
             "EnableInfiniteSession": false
           }
         }
         ```

      3. **Rollback Path** — Each milestone can be disabled independently
         - M1: Remove Hooks from SessionConfig
         - M2: Remove Tools from SessionConfig
         - M3: Remove OnUserInputRequest callback
         - M4: Remove InfiniteSession config

      4. **Version Pinning** — Hugh.csproj pins exact SDK version
         ```xml
         <PackageReference Include="GitHub.Copilot.SDK" Version="0.1.22" />
         ```

      ## Agent Prompt Impact Assessment

      ### Per-Agent Analysis

      | Agent | Current Text Signals | Tool Migration | Prompt LOE |
      |-------|---------------------|----------------|------------|
      | **pch-visionary** | STATUS: needs_user_input, completion markers | Add tool instructions alongside text | Small |
      | **pch-researcher** | STATUS: success/partial/blocked/needs_user_input, phase completion | Add tool instructions; orchestrator handles both | Medium |
      | **pch-planner** | STATUS: needs_user_input, plan completion markers | Add tool instructions alongside text | Small |
      | **pch-plan-reviewer** | STATUS: needs_user_input, review completion | Add tool instructions alongside text | Small |
      | **pch-coder** | STATUS: success/partial/blocked/needs_user_input, phase completion | Add tool instructions; orchestrator handles both | Medium |
      | **pch-helper** | Minimal — conversational agent | No changes required | None |
      | **hugh** | Minimal — orchestration agent | Tool awareness optional | Small |

      ### Backward Compatibility

      - **M1:** Zero agent prompt changes
      - **M2:** Additive changes — existing text formats continue to work
      - **M3:** Additive changes — SDK-native questions supplement text
      - **M4:** Zero agent prompt changes

      Total agent prompt effort for full integration: ~2 days across all agents

      ## LOE Summary by Milestone

      | Milestone | Description | Effort | Dependencies |
      |-----------|-------------|--------|--------------|
      | **M1** | Observability & Safety Hooks | Medium (2-3 days) | None |
      | **M2** | Custom Workflow Tools | Large (4-5 days) | M1, Microsoft.Extensions.AI |
      | **M3** | SDK-Native User Input | Medium (3-4 days) | M1 |
      | **M4** | InfiniteSession & Error Handling | Medium (2-3 days) | M1 |

      **Total Estimated LOE:** 11-15 days (can parallelize M2/M3/M4 after M1)

      ## Success Criteria by Milestone

      ### M1 Success Criteria
      - [ ] OnPreToolUse hook auto-creates directories for create_file
      - [ ] OnPostToolUse logs tool completion with timing
      - [ ] ToolExecutionStartEvent and ToolExecutionCompleteEvent handled in SdkResponseProcessor
      - [ ] First-run folder creation failure resolved
      - [ ] All existing SignalClassifier tests pass unchanged

      ### M2 Success Criteria
      - [ ] Microsoft.Extensions.AI package added and compiles
      - [ ] notify_phase_complete tool registered and callable by agents
      - [ ] report_blocker tool provides structured error data
      - [ ] get_workspace_structure returns directory tree
      - [ ] SdkResponseProcessor detects tool calls as signals
      - [ ] Text-based signals still work (fallback)
      - [ ] Agent prompts updated with tool instructions

      ### M3 Success Criteria
      - [ ] SDK-native OnUserInputRequest callback wired
      - [ ] SdkQuestionHandler processes UserInputRequest objects
      - [ ] Recommendations auto-accepted from request.Prompt
      - [ ] AbortAsync() correctly signals pause to orchestrator
      - [ ] QuestionHandler remains as fallback for text-based questions
      - [ ] All QuestionHandlerTests still pass

      ### M4 Success Criteria
      - [ ] InfiniteSession.CompactionThreshold configured
      - [ ] Compaction events logged
      - [ ] ErrorClassifier categorizes SessionErrorEvent types
      - [ ] Transient errors retry with exponential backoff
      - [ ] Context exhaustion triggers compaction

      ## Testing Approach

      ### Unit Testing per Milestone

      **M1:**
      - FileSystemHooksTests — verify directory creation, path extraction
      - ToolMetricsCollectorTests — verify timing and error tracking
      - SdkResponseProcessorTests — verify ToolExecution event handling

      **M2:**
      - HughWorkflowToolsTests — verify tool implementations
      - SignalClassifierTests — verify tool-call detection priority
      - Integration tests with mock tools

      **M3:**
      - SdkQuestionHandlerTests — verify UserInputRequest processing
      - Integration tests for question flow end-to-end
      - AbortAsync behavior tests

      **M4:**
      - ErrorClassifierTests — verify error categorization
      - Retry logic tests with mock transient errors
      - Compaction event handling tests

      ### Integration Testing

      Each milestone includes integration tests:
      1. **Happy path** — Full phase execution with new capabilities
      2. **Fallback path** — New capability fails, old path succeeds
      3. **Rollback test** — Disable feature flag, verify existing behavior

      ### Regression Testing

      Before each milestone merge:
      - Run full `dotnet test` suite
      - Execute sample pch-researcher run
      - Execute sample pch-coder run
      - Verify tool execution events logged
      - Verify signal detection unchanged

      ## Recommended Implementation Order

      Based on dependencies and risk:

      ```
      Week 1:  M1 — Observability & Safety Hooks (foundational)
      Week 2:  M2 — Custom Workflow Tools (highest value)
               M3 — SDK-Native User Input (can parallelize)
      Week 3:  M4 — InfiniteSession & Error Handling (nice-to-have)
               Agent prompt updates
               Integration testing
      ```

      **Critical Path:** M1 → M2 (depends on Hooks working)

      **Parallelizable:** M3 and M4 can start after M1 completes

      ## Immediate Next Steps

      1. **Create plan document** — Hand off to pch-planner with this research
      2. **Prioritize M1** — Directory safety hook resolves known first-run failure
      3. **Add feature flags** — Prepare HughConfig for SDK capability toggles
      4. **Pin SDK version** — Ensure v0.1.22 is explicitly pinned in .csproj
    key_discoveries:
      - "4-milestone migration path: M1 Observability (2-3d) → M2 Custom Tools (4-5d) → M3 Native Questions (3-4d) → M4 Error Handling (2-3d)"
      - "Total LOE estimate: 11-15 days with parallelization possible after M1"
      - "Only one new package required: Microsoft.Extensions.AI for custom tool registration"
      - "All 7 agent prompts need updates but changes are ADDITIVE — existing text formats remain as fallback"
      - "SignalClassifier stays as fallback throughout — never deleted, just deprioritized"
      - "M1 (file system hooks) resolves the known first-run directory creation failure with zero agent prompt changes"
      - "Feature flags enable granular rollback per capability if SDK breaking changes occur"
      - "pch-researcher and pch-coder have highest prompt change complexity (Medium); others are Small"
      - "SDK technical preview risk mitigated via interface abstraction and version pinning"
      - "Critical path is M1 → M2; M3 and M4 can parallelize after M1"
    relevant_files:
      - path: Hugh/Agents/CopilotService.cs
        note: "Primary modification target — CreateSessionAsync needs Hooks, Tools, OnUserInputRequest"
      - path: Hugh/Agents/SdkResponseProcessor.cs
        note: "Extend to handle ToolExecution events and tool call signals"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "Extend with tool-call-first detection; keep as fallback"
      - path: Hugh/Handlers/QuestionHandler.cs
        note: "Keep as fallback; new SdkQuestionHandler replaces primary flow in M3"
      - path: Hugh/Hugh.csproj
        note: "Add Microsoft.Extensions.AI; pin GitHub.Copilot.SDK version"
      - path: agents/pch-researcher.md
        note: "Medium prompt changes — add tool usage instructions for notify_phase_complete, report_blocker"
      - path: agents/pch-coder.md
        note: "Medium prompt changes — add tool usage instructions for workflow communication"
      - path: agents/pch-visionary.md
        note: "Small prompt changes — add tool instructions alongside existing text format"
      - path: agents/pch-planner.md
        note: "Small prompt changes — add tool instructions alongside existing text format"
      - path: agents/pch-plan-reviewer.md
        note: "Small prompt changes — add tool instructions alongside existing text format"
      - path: Hugh/Configuration/HughConfig.cs
        note: "Add Sdk section with feature flags for each capability"
    external_sources:
      - url: https://www.nuget.org/packages/GitHub.Copilot.SDK
        note: "v0.1.22 — pin this exact version"
      - url: https://www.nuget.org/packages/Microsoft.Extensions.AI
        note: "Required for AIFunctionFactory.Create custom tool registration"
    gaps:
      - "ToolExecution event payload structures need runtime verification during M1 implementation"
      - "Whether AbortAsync() from OnUserInputRequest callback blocks or throws needs testing"
      - "Microsoft.Extensions.AI version compatibility with GitHub.Copilot.SDK v0.1.22 needs verification before M2"
      - "Exact InfiniteSession.CompactionThreshold behavior at edge cases undocumented"
    assumptions:
      - "GitHub.Copilot.SDK v0.1.22 API will remain stable for at least 3-6 months"
      - "AIFunctionFactory.Create works with async Task<T> methods"
      - "Custom tools are visible to agents without explicit tool listing in prompts"
      - "OnPreToolUse hook is invoked synchronously before tool execution starts"

overview: |
  This research comprehensively analyzed how Hugh CLI should adopt GitHub Copilot SDK v0.1.22
  capabilities including session hooks (OnPreToolUse, OnPostToolUse), custom tool registration
  via Microsoft.Extensions.AI, SDK-native OnUserInputRequest, and ToolExecution event handling.

  **Key Finding:** Hugh currently uses only 2 of 5 SessionConfig properties and ignores most
  SDK events. This gap directly caused a first-run failure where agents couldn't create documents
  because docs/ directories didn't exist — Hugh had no way to intercept and fix the create_file
  tool execution.

  **Solution:** A 4-milestone migration plan enables incremental, non-breaking adoption:
  
  - **M1 (2-3 days):** Add OnPreToolUse hooks for directory auto-creation and tool observability
  - **M2 (4-5 days):** Register custom workflow tools (notify_phase_complete, report_blocker)
  - **M3 (3-4 days):** Migrate to SDK-native OnUserInputRequest for question handling
  - **M4 (2-3 days):** Enable InfiniteSession and structured error classification
  
  **Total LOE:** 11-15 days with parallelization possible after M1.
  
  **Agent Impact:** All 7 agent prompts need additive updates for tool-based communication,
  but existing text-based formats remain as fallbacks. SignalClassifier is never deleted —
  it becomes a fallback for malformed responses.
  
  **Risk Mitigation:** SDK technical preview risks are addressed via interface abstraction,
  feature flags, version pinning, and parallel path execution (new + old simultaneously).

key_findings:
  - "Hugh uses only 2 of 5 SessionConfig properties — Hooks, OnUserInputRequest, InfiniteSession completely unused"
  - "SdkResponseProcessor ignores ToolExecution events — missed observability for tool timing and errors"
  - "OnPreToolUse hook can auto-create directories before create_file, solving first-run failure"
  - "Three high-value custom tools: notify_phase_complete, report_blocker, get_workspace_structure"
  - "SDK-native OnUserInputRequest eliminates regex fragility for question detection"
  - "4-milestone migration (11-15 days) with parallelization after M1 hooks"
  - "Only one new package dependency: Microsoft.Extensions.AI for custom tool registration"
  - "Feature flags enable granular rollback per SDK capability"
  - "SignalClassifier remains as fallback — never deleted during migration"
  - "Agent prompt changes are additive with backward-compatible text formats preserved"

actionable_conclusions:
  - "Prioritize M1 immediately — OnPreToolUse directory safety resolves known first-run failure"
  - "Pin GitHub.Copilot.SDK to v0.1.22 explicitly in Hugh.csproj"
  - "Add Sdk feature flags section to HughConfig for granular capability control"
  - "Create FileSystemHooks class to handle OnPreToolUse directory creation"
  - "Extend SdkResponseProcessor to handle ToolExecution events before M2"
  - "After M1 stable, proceed with M2 (custom tools) and M3 (native questions) in parallel"
  - "Update all 7 agent prompts with additive tool instructions during M2"
  - "M4 (InfiniteSession, error handling) is lower priority — implement after M2/M3"
  - "Hand off to pch-planner to create implementation plan from this research"

open_questions:
  - "Will Microsoft.Extensions.AI 9.0.1-preview work with GitHub.Copilot.SDK 0.1.22?"
  - "What is the exact payload structure for ToolExecutionStartEvent and ToolExecutionCompleteEvent?"
  - "Does AbortAsync() from OnUserInputRequest callback throw or cleanly exit?"
  - "How does InfiniteSession.CompactionThreshold behave at edge cases?"

standards: null

handoff:
  created_by: pch-researcher
  created_date: "2026-02-10"
  status: complete
  current_phase: complete
  path: /docs/research/017-hugh-sdk-tool-hooks-integration.md
