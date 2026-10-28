```yaml
id: "019"
type: plan
title: "Hugh SDK Tool & Hook Integration"
status: complete
created: "2026-02-10"
updated: "2026-02-10"
owner: pch-planner
version: "v2.5"
research_source: "/docs/research/017-hugh-sdk-tool-hooks-integration.md"

versions:
  - version: "v1.0"
    date: "2026-02-10"
    author: pch-planner
    changes: Initial plan skeleton created
  - version: "v1.1"
    date: "2026-02-10"
    author: pch-planner
    changes: "Decisions recorded: all 4 milestones, directory auto-creation + logging, hugh_ prefix, direct native SdkQuestionHandler, replace-and-remove cleanup, additive agent prompts, existing CompactionThreshold, interface-abstracted tests"
  - version: "v2.0"
    date: "2026-02-10"
    author: pch-planner
    changes: "Full plan complete: requirements, technical design, 8 phases, dependencies, risks, holistic review"
  - version: "v2.1"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Review Decision 9: Never remove text-based signal formats from agent prompts — VS Code dual-environment compatibility. Phases 5.3 and 7.3 modified."
  - version: "v2.2"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Review Decision 10: Keep SignalClassifier regex patterns permanently as fallback — supersedes D5 replace-and-remove. Phases 5 and 7 restructured. F18/F19 updated. R8 resolved."
  - version: "v2.3"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Review Decision 11: QuestionHandler kept as thin delegation wrapper around SdkQuestionHandler for text-detected question signals. Phase 7.2 and 7.3 updated."
  - version: "v2.4"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Added Phase 9: Documentation and copilot instructions updates for new SDK integration components."
  - version: "v2.5"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Re-review fixes: SignalClassifier pattern count corrected 8→9, PathResolver line reference corrected, Phase 7 renamed to reflect D10/D11 content."

introduction: |
  This plan covers the implementation of GitHub Copilot SDK v0.1.22 capabilities
  in Hugh CLI that are currently unused. Hugh only uses 2 of 5 SessionConfig
  properties (Model and SystemMessage), ignores ToolExecution events, and relies
  entirely on fragile regex-based text parsing for agent-to-orchestrator
  communication. This directly caused a first-run failure where agents couldn't
  create documents because docs/ directories didn't exist.

  The plan follows a 4-milestone, non-breaking migration strategy from the
  research document, adopting session hooks, custom workflow tools, SDK-native
  user input handling, and enhanced error management incrementally.

decisions:
  - session: 1
    date: "2026-02-10"
    decision_point: "Should all 4 milestones be included or a subset?"
    options: "A) All 4 Milestones, B) M1+M2 Only, C) M1 Only, D) M1+M2+M3"
    answer: "A — All 4 Milestones (Full Adoption)"
    rationale: "User chose complete SDK integration covering hooks, custom tools, native question handling, error classification, and InfiniteSession. Total LOE ~11-15 days."
  - session: 2
    date: "2026-02-10"
    decision_point: "Should OnPreToolUse include path restriction policies or just directory auto-creation?"
    options: "A) Directory Auto-Creation Only, B) Auto-Creation + Path Restriction, C) Auto-Creation + Logging-Only Monitoring"
    answer: "C — Directory Auto-Creation + Logging-Only Path Monitoring"
    rationale: "Auto-create directories to fix first-run failure. Log warnings when agents write outside expected paths but don't block writes. Provides observability without risk of false-positive denials. Can add enforcement later based on logged data."
  - session: 3
    date: "2026-02-10"
    decision_point: "What naming convention should Hugh use for custom workflow tools?"
    options: "A) hugh_ prefix, B) workflow. dot-namespace, C) No prefix, D) orchestrator_ prefix"
    answer: "A — Prefixed with hugh_"
    rationale: "Concise 5-char prefix, clear ownership, avoids collision with built-in or future SDK tools. Matches project branding. Shorter than orchestrator_ alternative."
  - session: 4
    date: "2026-02-10"
    decision_point: "Migration strategy for SDK-native OnUserInputRequest?"
    options: "A) Hybrid Bridge First, B) Direct Native Implementation, C) Parallel Handlers"
    answer: "B — Direct Native Implementation"
    rationale: "User prefers going directly to a new SdkQuestionHandler that operates natively on SDK UserInputRequest types. Avoids writing throwaway bridge code. Faster to reach final architecture, accepting higher implementation risk in exchange for cleaner design."
  - session: 5
    date: "2026-02-10"
    decision_point: "How should the existing SignalClassifier be handled as tool/callback-based detection replaces it?"
    options: "A) Keep as Permanent Fallback, B) Gradual Deprecation with Feature Flags, C) Replace and Remove After Each Milestone"
    answer: "C — Replace and Remove After Each Milestone"
    rationale: "After M2 is stable, remove PhaseCompleteRegex, WorkflowCompleteRegex, BlockerRegex, ModeSelectionRegex. After M3 is stable, remove NeedsUserInputRegex, QuestionForUserRegex, QuestionOptionsRegex. RecommendationRegex stays permanently. Progressive cleanup keeps codebase lean."
  - session: 6
    date: "2026-02-10"
    decision_point: "How should agent prompts handle the transition between text-based and tool-based signaling?"
    options: "A) Additive Instructions with Text Fallback, B) Replace Text Instructions Immediately, C) Tool-Only Instructions No Mention of Text"
    answer: "A — Additive Instructions with Text Fallback"
    rationale: "Add tool instructions alongside existing text format. 'Prefer tool, fallback to text' ensures agents have a communication path if tool registration fails. Text instructions removed at same time as corresponding SignalClassifier regex patterns — keeps prompts and code in sync."
  - session: 7
    date: "2026-02-10"
    decision_point: "How should InfiniteSession be configured?"
    options: "A) Use Existing CompactionThreshold Setting, B) New Dedicated InfiniteSession Config Section, C) Hardcoded with Kill Switch"
    answer: "A — Use Existing CompactionThreshold Setting"
    rationale: "HughConfig.CopilotConfig.CompactionThreshold already exists at 0.8 and is documented. Wire it into InfiniteSession.CompactionThreshold in CreateSessionAsync. One-line change, no new config surface. Expand later if needed."
  - session: 8
    date: "2026-02-10"
    decision_point: "How should test coverage be structured for SDK hook and tool integration?"
    options: "A) Unit Tests with Interface Abstraction, B) Integration Tests with Live SDK, C) Unit Tests + Manual Verification Checklist"
    answer: "A — Unit Tests with Interface Abstraction"
    rationale: "Extract hook/tool logic into pure functions behind interfaces. Unit test in isolation, matching existing test patterns (SignalClassifierTests, QuestionHandlerTests). Thin SDK wiring layer verified by inspection. Integration tests too fragile for CI."
  - session: 9
    date: "2026-02-10"
    decision_point: "How should the plan handle dual-environment compatibility for agent signaling (VS Code Copilot chat vs Hugh CLI)?"
    options: "A) Never remove text formats from prompts, B) Conditional sections in prompts, C) Separate prompt files per environment, D) Remove text but keep subagent return format"
    answer: "A — Never remove text-based signal formats from agent prompts"
    rationale: "Agent prompts under agents/ are shared between VS Code Copilot chat and Hugh CLI. VS Code agents never have access to hugh_* custom tools (only registered via SDK SessionConfig.Tools in Hugh). Removing text-based formats (Phase 5.3 and 7.3) would break VS Code subagent signaling entirely. SignalClassifier regex patterns in Hugh CLI code can still be removed, but prompt text must stay permanently. This makes D6's 'text fallback' permanent rather than temporary."
  - session: 10
    date: "2026-02-10"
    decision_point: "Should SignalClassifier regex cleanup (Phases 5.1/5.2 and 7.1) still proceed, given that agents will always emit text signals for VS Code compatibility?"
    options: "A) Keep regex cleanup as-is, B) Keep regex as permanent fallback in Hugh CLI too, C) Keep regex with TODO comments for future removal"
    answer: "B — Keep regex as permanent fallback in Hugh CLI too"
    rationale: "Since agents always emit both text signals (for VS Code) and tool signals (for Hugh), the text is always present in the response. Regex patterns cost minimal maintenance and act as a safety net for Hugh CLI if SDK tool events fail silently. Tool events take priority; regex only fires if no tool signal was detected. This supersedes D5's replace-and-remove approach — no SignalClassifier patterns are removed. Phases 5 and 7 restructured accordingly."
  - session: 11
    date: "2026-02-10"
    decision_point: "How should QuestionHandler and HandleSignalAsync question cases be handled, given that SignalClassifier regex patterns are kept permanently (D10)?"
    options: "A) Keep QuestionHandler as permanent text-fallback handler, B) Route text-detected questions to SdkQuestionHandler directly, C) Keep both but make QuestionHandler a thin wrapper around SdkQuestionHandler"
    answer: "C — Keep both but make QuestionHandler a thin delegation wrapper around SdkQuestionHandler"
    rationale: "QuestionHandler stays wired in HandleSignalAsync for text-detected question signals (NeedsUserInput, QuestionOpen, QuestionWithRecommendation). Internally, QuestionHandler constructs a UserInputRequest-compatible object and delegates to SdkQuestionHandler for decision logic (auto-accept, pause, logging). One source of truth for question resolution. HandleSignalAsync routing unchanged. No duplicate decision logic."

review_session_log:
  questions_pending: 0
  questions_resolved: 3
  last_updated: "2026-02-10"
  entries:
    - number: 1
      issue: "Dual-environment compatibility — Phases 5.3 and 7.3 remove text-based signal formats from agent prompts, but VS Code agents don't have access to hugh_* tools"
      category: correctness
      decision: "Option A: Never remove text formats from prompts — keep permanently for VS Code compatibility"
      plan_update: "Decision 9 added. Phases 5.3, 7.3 modified. F17 updated. Holistic review D6+D5 interaction updated."
    - number: 2
      issue: "SignalClassifier regex cleanup (Phases 5.1/5.2 and 7.1) removes Hugh CLI's text fallback, but agents will always emit text signals since prompts are shared"
      category: correctness
      decision: "Option B: Keep regex as permanent fallback in Hugh CLI — tool events primary, text regex secondary"
      plan_update: "Decision 10 added. Phases 5, 7 restructured. F18/F19 replaced. D5 superseded. R8 resolved. Holistic review updated."
    - number: 3
      issue: "Phase 7.2 deprecates QuestionHandler and removes HandleSignalAsync question cases, but D10 keeps regex patterns that still produce question SignalTypes — no handler for them"
      category: correctness
      decision: "Option C: QuestionHandler becomes thin wrapper delegating to SdkQuestionHandler for decision logic"
      plan_update: "Decision 11 added. Phase 7.2 rewritten. Phase 7.3 updated. Holistic review D4+D10+D11 interaction added."

holistic_review:
  decision_interactions:
    - interaction: "Direct native SdkQuestionHandler (D4) + Permanent regex fallback (D10) + Delegation wrapper (D11)"
      analysis: |
        D4 creates SdkQuestionHandler for SDK-native OnUserInputRequest callbacks.
        D10 keeps all SignalClassifier regex patterns permanently, so text-detected
        question signals (NeedsUserInput, QuestionOpen, QuestionWithRecommendation)
        still flow through HandleSignalAsync. D11 makes QuestionHandler a thin wrapper
        that delegates to SdkQuestionHandler for the actual decision logic.
        
        This creates a clean architecture: SdkQuestionHandler is the single source
        of truth for question resolution. It is invoked two ways:
        1. Directly via SDK OnUserInputRequest callback (primary path in Hugh CLI)
        2. Via QuestionHandler delegation when text regex detects a question (fallback)
        
        Both paths converge to the same decision logic (auto-accept, pause, logging).
        No duplicate behavior. HandleSignalAsync routing unchanged.
        
        Mitigation: Phase 6 tests must cover SdkQuestionHandler comprehensively.
        Phase 7 tests must verify QuestionHandler delegation works correctly.

    - interaction: "All 4 milestones (D1) + Permanent regex fallback (D10)"
      analysis: |
        Full adoption means all 4 milestones must land, but with D10 keeping all regex
        patterns, SignalClassifier remains at 9 patterns (unchanged). Tool events are
        the primary signal path; regex is secondary. This is lower risk than the original
        D1+D5 combination since no patterns are removed. The trade-off is slightly more
        code to maintain, but the dual detection provides resilience.

    - interaction: "Additive agent prompts (D6) + Permanent fallback everywhere (D9 + D10)"
      analysis: |
        D6 adds tool instructions alongside existing text formats. D9 keeps text formats
        permanently in agent prompts for VS Code compatibility. D10 keeps regex patterns
        permanently in SignalClassifier for Hugh CLI fallback. Together, these three decisions
        establish a fully additive approach: tool-based signaling is added as the preferred
        path in Hugh CLI, but nothing is ever removed from either agent prompts or Hugh CLI
        code. Both environments always have a working signal path. This is the most
        resilient architecture, trading minimal maintenance cost for maximum reliability.

    - interaction: "hugh_ prefix (D3) + Tool-based signal detection (Phase 4)"
      analysis: |
        The hugh_ prefix enables clean detection in SdkResponseProcessor — any
        ToolExecutionCompleteEvent with a tool name starting with "hugh_" is a
        workflow signal. Non-Hugh tools are ignored. This is a reliable discrimination
        mechanism that doesn't require a registry or configuration.

  architectural_considerations:
    - concern: "CopilotService.CreateSessionAsync is becoming the central configuration hub"
      assessment: |
        After all milestones, CreateSessionAsync will configure 5 SessionConfig properties
        (Model, SystemMessage, Hooks, Tools, OnUserInputRequest, InfiniteSession) and create
        3 dependencies (FileSystemHooks, HughWorkflowTools, SdkQuestionHandler). This is
        approaching the limit of single-method complexity.
      recommendation: |
        Consider extracting a SessionConfigBuilder or factory method in a future refactoring,
        but it's acceptable for now since each dependency is self-contained and the method
        remains linear (no branching logic).

    - concern: "SdkResponseProcessor dual signal detection (tool events + text fallback)"
      assessment: |
        Per D10, SdkResponseProcessor permanently checks both tool call events and text
        regex. The priority rule (tool-first, text-fallback) is the final architecture.
        Both code paths are well-defined and must be documented.
      recommendation: |
        Document the priority rule clearly in SdkResponseProcessor: tool events checked
        first, text regex only fires if no tool signal was detected. This is permanent
        architecture, not a transitional state. Consider adding a log message when the
        text fallback fires, as it indicates tool events may have failed.

    - concern: "runId availability in CreateSessionAsync OnUserInputRequest callback"
      assessment: |
        The SDK-native OnUserInputRequest callback needs runId for decision logging, but
        runId is generated in ExecutePhaseAsync. The callback closure must capture a mutable
        reference or the method signature must change.
      recommendation: |
        Pass runId as a stored field on CopilotService set at the start of ExecutePhaseAsync,
        or pass it to CreateSessionAsync as a parameter. Document this threading concern
        in Phase 6 task 6.2.

  trade_offs_accepted:
    - "Direct native SdkQuestionHandler (no bridge) — faster to final state but higher implementation risk"
    - "Permanent dual signal detection (tool-first + text-fallback) — slightly more code to maintain but maximum resilience (supersedes D5 replace-and-remove)"
    - "Logging-only path monitoring (no enforcement) — provides data but doesn't prevent unauthorized writes"
    - "Full 4-milestone scope — 11-15 day LOE with SDK technical preview risk"

  risks_acknowledged:
    - "SDK technical preview may introduce breaking changes — mitigated by version pinning"
    - "ToolExecution event payloads undocumented — mitigated by defensive coding and runtime logging"
    - "Microsoft.Extensions.AI compatibility unverified — mitigated by early Phase 3.1 verification"
    - "Permanent dual signal detection means slightly more SignalClassifier code to maintain — mitigated by low maintenance cost and high reliability benefit"

overview:
  feature: "Hugh SDK Tool & Hook Integration"
  summary: |
    Adopt GitHub Copilot SDK v0.1.22 capabilities including session hooks
    (OnPreToolUse, OnPostToolUse), custom tool registration via
    Microsoft.Extensions.AI, SDK-native OnUserInputRequest callback, and
    ToolExecution event handling to improve reliability, observability,
    and structured agent-to-orchestrator communication.
  objectives:
    - "Fix first-run failure by auto-creating directories via OnPreToolUse hooks"
    - "Add tool execution observability (timing, errors, progress)"
    - "Register custom workflow tools for structured phase/blocker signaling"
    - "Migrate question handling from regex text parsing to SDK-native callbacks"
    - "Enable InfiniteSession for context window management"
    - "Improve error classification and recovery strategies"
  success_criteria:
    - "First-run directory creation works without manual mkdir — OnPreToolUse auto-creates parent directories"
    - "Tool execution events (start/complete) are logged with timing metrics"
    - "Custom tools (hugh_notify_phase_complete, hugh_report_blocker, hugh_get_workspace_structure) appear in agent tool lists"
    - "Tool-based phase completion signals are detected by SdkResponseProcessor"
    - "SDK-native OnUserInputRequest handles all question types (recommendation, open, mode selection)"
    - "InfiniteSession prevents context exhaustion using existing CompactionThreshold config"
    - "All existing tests pass unchanged after each milestone"
    - "Path monitoring logs warnings when agents write outside expected directories"
    - "SignalClassifier regex patterns kept permanently as fallback in Hugh CLI — tool events are primary, text regex is secondary"
    - "All text-based signal formats preserved in agent prompts for VS Code Copilot chat compatibility"

requirements:
  functional:
    - id: F1
      description: "OnPreToolUse hook intercepts create_file and replace_string_in_file to auto-create parent directories before execution"
    - id: F2
      description: "OnPreToolUse hook logs warnings when agents write to paths outside their expected directories (docs/research/, docs/plans/, etc.)"
    - id: F3
      description: "OnPostToolUse hook logs tool completion with timing and success/failure status"
    - id: F4
      description: "SdkResponseProcessor handles ToolExecutionStartEvent and ToolExecutionCompleteEvent for progress and error tracking"
    - id: F5
      description: "ToolMetricsCollector tracks tool execution counts, timing, and error rates per agent session"
    - id: F6
      description: "Custom tool hugh_notify_phase_complete registered via SessionConfig.Tools — returns PhaseCompleteResult to agent"
    - id: F7
      description: "Custom tool hugh_report_blocker registered via SessionConfig.Tools — returns BlockerResult to agent"
    - id: F8
      description: "Custom tool hugh_notify_workflow_complete registered via SessionConfig.Tools — returns WorkflowCompleteResult"
    - id: F9
      description: "Custom tool hugh_get_workspace_structure registered via SessionConfig.Tools — returns directory tree"
    - id: F10
      description: "SdkResponseProcessor detects tool-based signals (tool call events) as primary signal source, text regex as fallback"
    - id: F11
      description: "SdkQuestionHandler operates natively on SDK UserInputRequest objects with Prompt, Choices, AllowFreeform"
    - id: F12
      description: "SdkQuestionHandler auto-accepts recommendations when Autonomous.AcceptRecommendationsOnly is true"
    - id: F13
      description: "SdkQuestionHandler pauses for open questions (no choices, no recommendation) requiring human input"
    - id: F14
      description: "SDK-native OnUserInputRequest wired in CreateSessionAsync, routing to SdkQuestionHandler"
    - id: F15
      description: "InfiniteSession enabled in CreateSessionAsync using existing CopilotConfig.CompactionThreshold"
    - id: F16
      description: "SessionCompactionStartEvent and SessionCompactionCompleteEvent handled in SdkResponseProcessor for monitoring"
    - id: F17
      description: "All 7 agent prompts updated with additive tool-based signaling instructions (prefer tool, fallback to text). Text-based formats kept permanently — agents are shared between VS Code (no hugh_* tools) and Hugh CLI (has hugh_* tools)"
    - id: F18
      description: "All SignalClassifier regex patterns kept permanently as text-based fallback. Tool events are primary signal source; regex fires only when no tool signal detected. (Supersedes original D5 replace-and-remove approach per review Decision D10)"
    - id: F19
      description: "SdkResponseProcessor implements tool-first priority: check tool call events first, fall back to SignalClassifier.Classify() text regex if no tool signal found. Log when text fallback fires."
  non_functional:
    - id: NF1
      description: "No new external package dependencies except Microsoft.Extensions.AI (required for M2 tool registration)"
    - id: NF2
      description: "All hook and tool logic unit-testable via interface abstraction without live SDK calls"
    - id: NF3
      description: "OnPreToolUse hook adds < 10ms latency to tool execution"
    - id: NF4
      description: "GitHub.Copilot.SDK pinned to v0.1.22 explicitly in Hugh.csproj"
    - id: NF5
      description: "Each milestone deployable independently — no milestone requires a subsequent milestone to function"
  out_of_scope:
    - "Full path restriction enforcement (deny writes) — logging-only per decision"
    - "Custom tools for update_phase_status — agents continue writing to documents directly"
    - "Integration tests with live Copilot SDK — unit tests with interface abstraction only"
    - "Agent prompt changes for MCP tools — only custom SessionConfig.Tools"
    - "Removing any SignalClassifier regex patterns — all kept permanently as fallback per D10"
    - "Removing any text-based signal formats from agent prompts — all kept permanently per D9"

technical_design:
  architecture: |
    The integration adds three new capability layers to Hugh's SDK interaction:

    1. **Hook Layer** (M1): FileSystemHooks class provides OnPreToolUse and OnPostToolUse
       callbacks wired into SessionConfig.Hooks. Handles directory auto-creation,
       path monitoring, and tool completion logging.

    2. **Tool Layer** (M2): HughWorkflowTools class registers custom tools via
       AIFunctionFactory.Create into SessionConfig.Tools. Provides structured
       phase/blocker signaling as an alternative to text-based regex patterns.

    3. **Input Layer** (M3): SdkQuestionHandler provides SDK-native OnUserInputRequest
       callback in SessionConfig.OnUserInputRequest. Replaces text-parsing question
       detection with structured UserInputRequest handling.

    4. **Session Layer** (M4): InfiniteSession configuration and enhanced event handling
       for context compaction and error classification.

    All layers integrate at CopilotService.CreateSessionAsync, which becomes the
    central configuration point for SDK capabilities.

    ```
    CopilotService.CreateSessionAsync
      ├── SessionConfig.Model              (existing)
      ├── SessionConfig.SystemMessage      (existing)
      ├── SessionConfig.Hooks              (M1 - NEW)
      │   ├── OnPreToolUse  → FileSystemHooks
      │   └── OnPostToolUse → FileSystemHooks
      ├── SessionConfig.Tools              (M2 - NEW)
      │   ├── hugh_notify_phase_complete
      │   ├── hugh_notify_workflow_complete
      │   ├── hugh_report_blocker
      │   └── hugh_get_workspace_structure
      ├── SessionConfig.OnUserInputRequest (M3 - NEW)
      │   └── SdkQuestionHandler
      └── SessionConfig.InfiniteSession    (M4 - NEW)
          └── CompactionThreshold = 0.8
    ```

  codebase_patterns:
    - pattern: "Directory.CreateDirectory before file write"
      location: "Hugh/Orchestration/LoopStateManager.cs (lines 299-303), Hugh/Configuration/PathResolver.cs (lines 50-51)"
      usage: "FileSystemHooks.HandlePreToolUseAsync reuses this pattern for auto-creating parent directories"
    - pattern: "Signal classification and routing"
      location: "Hugh/Detection/SignalClassifier.cs, Hugh/Agents/CopilotService.HandleSignalAsync"
      usage: "Tool-based signals extend this pattern; SdkResponseProcessor checks tool events first, then text fallback"
    - pattern: "Event subscription via session.On()"
      location: "Hugh/Agents/SdkResponseProcessor.cs (lines 96-115)"
      usage: "Extended with ToolExecutionStartEvent, ToolExecutionCompleteEvent, SessionCompaction events"
    - pattern: "QuestionResponse factory methods"
      location: "Hugh/Models/QuestionResponse.cs"
      usage: "SdkQuestionHandler returns QuestionResponse.AutoRespond or QuestionResponse.Pause matching existing pattern"
    - pattern: "DecisionLogger audit trail"
      location: "Hugh/Logging/DecisionLogger.cs"
      usage: "SdkQuestionHandler logs decisions via DecisionLogger, same as QuestionHandler"
    - pattern: "Serilog ILogger dependency injection"
      location: "All Hugh classes use constructor-injected Serilog.ILogger"
      usage: "All new classes (FileSystemHooks, HughWorkflowTools, SdkQuestionHandler, ToolMetricsCollector) follow this pattern"

  data_models:
    new_classes:
      - name: FileSystemHooks
        namespace: Hugh.Hooks
        file: Hugh/Hooks/FileSystemHooks.cs
        description: "Handles OnPreToolUse and OnPostToolUse hook callbacks for directory safety and tool monitoring"
        properties:
          - "_logger: ILogger"
          - "_workspacePath: string"
          - "_expectedPaths: Dictionary<string, string[]> — maps agent names to expected write paths"
        methods:
          - "HandlePreToolUseAsync(PreToolUseHookInput input, Invocation invocation) → Task<PreToolUseHookOutput>"
          - "HandlePostToolUseAsync(PostToolUseHookInput input, Invocation invocation) → Task<PostToolUseHookOutput>"
          - "ExtractFilePath(string toolName, object toolArgs) → string? — extracts filePath from tool arguments"
          - "EnsureDirectoryExists(string filePath) → void — creates parent directory if missing"
          - "IsExpectedPath(string agentName, string filePath) → bool — checks against expected path patterns"

      - name: ToolMetricsCollector
        namespace: Hugh.Metrics
        file: Hugh/Metrics/ToolMetricsCollector.cs
        description: "Collects tool execution timing, counts, and error rates per session"
        properties:
          - "_toolExecutions: ConcurrentDictionary<string, List<ToolExecutionRecord>>"
          - "_activeTools: ConcurrentDictionary<string, Stopwatch>"
        methods:
          - "OnToolStart(string toolName) → void"
          - "OnToolComplete(string toolName, bool success, string? error) → void"
          - "GetMetrics() → ToolMetricsSummary"
          - "Reset() → void"

      - name: ToolExecutionRecord
        namespace: Hugh.Metrics
        file: Hugh/Metrics/ToolMetricsCollector.cs
        description: "Record of a single tool execution"
        properties:
          - "ToolName: string"
          - "Duration: TimeSpan"
          - "Success: bool"
          - "Error: string?"
          - "Timestamp: DateTime"

      - name: ToolMetricsSummary
        namespace: Hugh.Metrics
        file: Hugh/Metrics/ToolMetricsCollector.cs
        description: "Aggregated metrics for a session"
        properties:
          - "TotalExecutions: int"
          - "SuccessCount: int"
          - "ErrorCount: int"
          - "ByTool: Dictionary<string, ToolStats>"

      - name: HughWorkflowTools
        namespace: Hugh.Tools
        file: Hugh/Tools/HughWorkflowTools.cs
        description: "Custom workflow tools registered with the Copilot SDK for structured agent-orchestrator communication"
        dependencies:
          - "ILogger"
          - "string workspacePath"
        methods:
          - "NotifyPhaseComplete(int phase, string summary, string? outputDocumentPath) → PhaseCompleteResult"
          - "NotifyWorkflowComplete(string summary, List<string>? documentsCreated) → WorkflowCompleteResult"
          - "ReportBlocker(string reason, string severity, bool isSkippable, List<string>? suggestedActions) → BlockerResult"
          - "GetWorkspaceStructure(int maxDepth, string? filterPath) → WorkspaceStructureResult"

      - name: PhaseCompleteResult
        namespace: Hugh.Tools
        file: Hugh/Tools/WorkflowToolResults.cs
        properties:
          - "Acknowledged: bool"
          - "NextPhaseNumber: int?"
          - "Message: string"

      - name: WorkflowCompleteResult
        namespace: Hugh.Tools
        file: Hugh/Tools/WorkflowToolResults.cs
        properties:
          - "Acknowledged: bool"
          - "Message: string"

      - name: BlockerResult
        namespace: Hugh.Tools
        file: Hugh/Tools/WorkflowToolResults.cs
        properties:
          - "Acknowledged: bool"
          - "Action: string — 'paused' or 'logged'"
          - "Message: string"

      - name: WorkspaceStructureResult
        namespace: Hugh.Tools
        file: Hugh/Tools/WorkflowToolResults.cs
        properties:
          - "RootPath: string"
          - "DirectoryTree: string"
          - "FileCount: int"

      - name: SdkQuestionHandler
        namespace: Hugh.Handlers
        file: Hugh/Handlers/SdkQuestionHandler.cs
        description: "Handles SDK-native UserInputRequest objects. Primary question handler — invoked directly via SDK OnUserInputRequest callback, and indirectly via QuestionHandler delegation for text-detected questions (per D11)."
        dependencies:
          - "DecisionLogger"
          - "AutonomousConfig"
          - "ILogger"
        methods:
          - "HandleAsync(UserInputRequest request, Invocation invocation, string currentPhase, Guid runId) → Task<UserInputResponse>"
          - "DetectRecommendation(string prompt) → (string? letter, string? option)"
          - "ShouldAutoAccept(UserInputRequest request) → bool"

  api_contracts: {}

  component_specifications:
    CreateSessionAsync_changes: |
      CopilotService.CreateSessionAsync (Hugh/Agents/CopilotService.cs, lines 307-330)
      must be extended to include Hooks, Tools, OnUserInputRequest, and InfiniteSession.

      Target signature (after all milestones):
      ```csharp
      var session = await _client.CreateSessionAsync(new SdkSessionConfig
      {
          Model = _config.Copilot.Model,
          SystemMessage = new SystemMessageConfig
          {
              Mode = SystemMessageMode.Append,
              Content = agentPrompt
          },
          Hooks = _fileSystemHooks.CreateHooks(agentDefinition.Name),    // M1
          Tools = _workflowTools.CreateToolSet(),                         // M2
          OnUserInputRequest = _sdkQuestionHandler.HandleRequestAsync,    // M3
          InfiniteSession = new InfiniteSessionConfig                     // M4
          {
              CompactionThreshold = _config.Copilot.CompactionThreshold
          }
      });
      ```

    SdkResponseProcessor_changes: |
      SdkResponseProcessor.ProcessResponseAsync (Hugh/Agents/SdkResponseProcessor.cs)
      event subscription must be extended from 3 events to 7+:
      ```csharp
      session.On(evt =>
      {
          switch (evt)
          {
              case AssistantMessageDeltaEvent delta:      // existing
              case SessionIdleEvent:                       // existing
              case SessionErrorEvent error:                // existing
              case ToolExecutionStartEvent toolStart:      // M1 NEW
              case ToolExecutionCompleteEvent toolComplete: // M1 NEW
              case SessionCompactionStartEvent:            // M4 NEW
              case SessionCompactionCompleteEvent:         // M4 NEW
          }
      });
      ```

dependencies:
  - id: D1
    description: "GitHub.Copilot.SDK v0.1.22 — already installed, provides SessionHooks, OnUserInputRequest, InfiniteSession, ToolExecution events"
    type: existing
    status: available
  - id: D2
    description: "Microsoft.Extensions.AI — required for AIFunctionFactory.Create custom tool registration (M2)"
    type: new-package
    status: not-installed
    notes: "Must verify version compatibility with GitHub.Copilot.SDK v0.1.22. Target v9.0.1-preview.1.25061.3"
  - id: D3
    description: "All existing tests must pass at each milestone boundary"
    type: constraint
    status: active
  - id: D4
    description: "M2 depends on M1 (hooks must be working before adding tools)"
    type: ordering
    status: active
  - id: D5
    description: "M3 can run in parallel with M2 after M1 is complete"
    type: ordering
    status: active
  - id: D6
    description: "M4 can run in parallel with M2/M3 after M1 is complete"
    type: ordering
    status: active

risks:
  - id: R1
    description: "GitHub.Copilot.SDK is in technical preview — API may change or break in future versions"
    severity: medium
    likelihood: medium
    mitigation: "Pin SDK to v0.1.22 in .csproj. Test against specific version. SignalClassifier text fallback kept permanently per D10."
  - id: R2
    description: "PreToolUseHookInput exact structure (ToolArgs type) requires runtime verification"
    severity: low
    likelihood: medium
    mitigation: "Add defensive null checks and try/catch in FileSystemHooks. Log unrecognized argument shapes."
  - id: R3
    description: "Microsoft.Extensions.AI version may not be compatible with GitHub.Copilot.SDK v0.1.22"
    severity: medium
    likelihood: low
    mitigation: "Test package restore and compilation in M2 Phase 1. If incompatible, custom tool registration falls back to SDK-native tool API."
  - id: R4
    description: "ToolExecution event payload structures are undocumented — may not contain expected fields"
    severity: low
    likelihood: medium
    mitigation: "Log raw event objects during M1 development. Adjust ToolMetricsCollector based on actual payloads."
  - id: R5
    description: "Custom tools may not coexist with built-in CLI tools as expected"
    severity: medium
    likelihood: low
    mitigation: "Test tool list visibility in M2 Phase 2. Use hugh_ prefix to avoid name collisions."
  - id: R6
    description: "SDK-native OnUserInputRequest callback timing (before vs during response) is unverified"
    severity: low
    likelihood: medium
    mitigation: "SdkQuestionHandler implements timeout protection. Falls back to text-based handling if callback doesn't fire."
  - id: R7
    description: "InfiniteSession CompactionThreshold edge behavior at 0.8 is undocumented"
    severity: low
    likelihood: low
    mitigation: "Monitor compaction events via SdkResponseProcessor. Adjust threshold in appsettings.json if needed."
  - id: R8
    description: "RESOLVED per D10 — SignalClassifier patterns are kept permanently as fallback. No removal risk."
    severity: none
    likelihood: none
    mitigation: "No mitigation needed — patterns are permanent."

phases:
  - phase: 1
    name: "M1 — FileSystemHooks and Directory Auto-Creation"
    status: complete
    size: medium
    files_to_modify: 5
    prerequisites:
      - "GitHub.Copilot.SDK v0.1.22 installed (already present)"
    entry_point: "Hugh/Hooks/FileSystemHooks.cs (new file)"
    verification: "Project compiles, all existing tests pass"
    tasks:
      - step: "1.1"
        task: "Create Hugh/Hooks/FileSystemHooks.cs with OnPreToolUse handler"
        files:
          - "Hugh/Hooks/FileSystemHooks.cs (new)"
        details: |
          Create the FileSystemHooks class in new Hugh/Hooks/ directory.
          
          Constructor takes: ILogger logger, string workspacePath.
          
          HandlePreToolUseAsync(PreToolUseHookInput input, Invocation invocation):
          - Extract toolName from input
          - For tools "create_file", "replace_string_in_file", "multi_replace_string_in_file":
            - Call ExtractFilePath() to get the file path from tool arguments
            - Call EnsureDirectoryExists() to create parent directory if missing
            - Log Information: "Auto-created directory for {ToolName}: {DirPath}"
          - Return PreToolUseHookOutput with PermissionDecision = "allow"
          
          ExtractFilePath(string toolName, object toolArgs):
          - Parse toolArgs (likely JsonElement) to find "filePath" property
          - For multi_replace_string_in_file, extract from "replacements" array
          - Return first filePath found, or null if not extractable
          - Wrap in try/catch — log Warning on parse failure, return null
          
          EnsureDirectoryExists(string filePath):
          - Get parent directory from Path.GetDirectoryName(filePath)
          - If parent doesn't exist, call Directory.CreateDirectory()
          - Follow pattern from LoopStateManager.cs lines 299-303
        acceptance_criteria:
          - "FileSystemHooks class compiles and handles PreToolUseHookInput"
          - "ExtractFilePath handles create_file, replace_string_in_file, multi_replace_string_in_file arg shapes"
          - "EnsureDirectoryExists creates parent directories without error"
          - "All file system operations wrapped in try/catch with logging"

      - step: "1.2"
        task: "Add path monitoring (logging-only) to FileSystemHooks"
        files:
          - "Hugh/Hooks/FileSystemHooks.cs"
        details: |
          Add expected path monitoring to HandlePreToolUseAsync:
          
          Add field: _expectedPaths as Dictionary<string, string[]> mapping agent names to
          expected write path prefixes (e.g., "pch-researcher" → ["docs/research/"]).
          
          Static factory: CreateExpectedPaths() returns default mapping:
          - "pch-researcher" → ["docs/research/"]
          - "pch-planner" → ["docs/plans/"]
          - "pch-visionary" → ["docs/vision/"]
          - "pch-plan-reviewer" → ["docs/plans/"]
          - "pch-coder" → [] (empty = no restrictions, no warnings)
          - "pch-helper" → [] (no expected writes)
          
          In HandlePreToolUseAsync, after directory creation:
          - Get agent name from invocation context or a stored field
          - If _expectedPaths has entries for the agent AND filePath doesn't match any prefix:
            Log Warning: "Agent {AgentName} writing to unexpected path: {FilePath} (expected: {ExpectedPaths})"
          - NEVER deny — always return "allow"
          
          Add constructor overload: FileSystemHooks(ILogger, string workspacePath, string agentName)
          to accept agent name for path monitoring context.
        acceptance_criteria:
          - "Warning logged when pch-researcher writes outside docs/research/"
          - "No warning logged when pch-coder writes to any path"
          - "No writes are ever denied — always returns allow"

      - step: "1.3"
        task: "Add OnPostToolUse handler for completion logging"
        files:
          - "Hugh/Hooks/FileSystemHooks.cs"
        details: |
          Add HandlePostToolUseAsync(PostToolUseHookInput input, Invocation invocation):
          - Log tool completion: "Tool {ToolName} completed"
          - If input has error/failure information, log Error with details
          - Return PostToolUseHookOutput (default/empty)
          
          Add CreateHooks() factory method that returns SessionHooks:
          ```csharp
          public SessionHooks CreateHooks()
          {
              return new SessionHooks
              {
                  OnPreToolUse = HandlePreToolUseAsync,
                  OnPostToolUse = HandlePostToolUseAsync
              };
          }
          ```
        acceptance_criteria:
          - "HandlePostToolUseAsync logs tool name on completion"
          - "Error information logged when tool execution fails"
          - "CreateHooks() returns correctly wired SessionHooks instance"

      - step: "1.4"
        task: "Wire FileSystemHooks into CopilotService.CreateSessionAsync"
        files:
          - "Hugh/Agents/CopilotService.cs"
        details: |
          In CopilotService constructor:
          - Accept optional workspace path parameter (default: Directory.GetCurrentDirectory())
          
          In CreateSessionAsync (lines 307-330):
          - Create FileSystemHooks instance with logger, workspace path, and agent name
          - Add Hooks property to SdkSessionConfig:
          
          ```csharp
          var fileSystemHooks = new FileSystemHooks(_logger, workspacePath, agentDefinition.Name);
          
          var session = await _client.CreateSessionAsync(new SdkSessionConfig
          {
              Model = _config.Copilot.Model,
              SystemMessage = new SystemMessageConfig
              {
                  Mode = SystemMessageMode.Append,
                  Content = agentPrompt
              },
              Hooks = fileSystemHooks.CreateHooks()
          });
          ```
        acceptance_criteria:
          - "SessionConfig.Hooks set in CreateSessionAsync"
          - "FileSystemHooks created with correct workspace path and agent name"
          - "Existing session creation behavior unchanged (Model, SystemMessage preserved)"

      - step: "1.5"
        task: "Create unit tests for FileSystemHooks"
        files:
          - "Hugh.Tests/FileSystemHooksTests.cs (new)"
        details: |
          Create test class FileSystemHooksTests with tests for:
          
          ExtractFilePath tests:
          - ExtractFilePath_CreateFile_ReturnsFilePath
          - ExtractFilePath_ReplaceStringInFile_ReturnsFilePath
          - ExtractFilePath_MultiReplace_ReturnsFirstFilePath
          - ExtractFilePath_UnknownTool_ReturnsNull
          - ExtractFilePath_MalformedArgs_ReturnsNull
          
          EnsureDirectoryExists tests:
          - EnsureDirectoryExists_CreatesParentDirectory (use temp directory)
          - EnsureDirectoryExists_ExistingDirectory_NoOp
          - EnsureDirectoryExists_NullPath_NoError
          
          IsExpectedPath tests:
          - IsExpectedPath_Researcher_DocsResearch_ReturnsTrue
          - IsExpectedPath_Researcher_SrcCode_ReturnsFalse
          - IsExpectedPath_Coder_AnyPath_ReturnsTrue
          - IsExpectedPath_UnknownAgent_ReturnsTrue
          
          Follow existing test patterns: use NullLogger, test pure logic without SDK dependencies.
        acceptance_criteria:
          - "All FileSystemHooks tests pass"
          - "Tests cover all ExtractFilePath tool argument shapes"
          - "Tests cover expected path monitoring logic"
          - "No SDK dependencies in tests — pure logic testing"

  - phase: 2
    name: "M1 — ToolExecution Event Handling and Metrics"
    status: complete
    size: medium
    files_to_modify: 5
    prerequisites:
      - "Phase 1 complete — FileSystemHooks wired into CreateSessionAsync"
    entry_point: "Hugh/Metrics/ToolMetricsCollector.cs (new file)"
    verification: "Phase 1 tests pass, FileSystemHooks compiles"
    tasks:
      - step: "2.1"
        task: "Create Hugh/Metrics/ToolMetricsCollector.cs"
        files:
          - "Hugh/Metrics/ToolMetricsCollector.cs (new)"
        details: |
          Create ToolMetricsCollector class in new Hugh/Metrics/ directory.
          
          Records:
          - ToolExecutionRecord { ToolName, Duration, Success, Error?, Timestamp }
          - ToolMetricsSummary { TotalExecutions, SuccessCount, ErrorCount, ByTool }
          - ToolStats { Count, SuccessCount, ErrorCount, AvgDuration, MaxDuration }
          
          ToolMetricsCollector:
          - _toolExecutions: ConcurrentDictionary<string, List<ToolExecutionRecord>>
          - _activeTools: ConcurrentDictionary<string, Stopwatch>
          
          Methods:
          - OnToolStart(string toolName): Start stopwatch, store in _activeTools
          - OnToolComplete(string toolName, bool success, string? error):
            Stop stopwatch, create ToolExecutionRecord, add to _toolExecutions
          - GetMetrics() → ToolMetricsSummary: Aggregate all records
          - Reset(): Clear all collections
          
          Thread-safe: Use ConcurrentDictionary and lock where needed.
        acceptance_criteria:
          - "ToolMetricsCollector tracks tool start/complete correctly"
          - "GetMetrics returns accurate aggregated statistics"
          - "Thread-safe for concurrent tool executions"

      - step: "2.2"
        task: "Extend SdkResponseProcessor to handle ToolExecution events"
        files:
          - "Hugh/Agents/SdkResponseProcessor.cs"
        details: |
          In SdkResponseProcessor constructor, add ToolMetricsCollector parameter.
          
          In ProcessResponseAsync event subscription (lines 96-115), add cases:
          
          ```csharp
          case ToolExecutionStartEvent toolStart:
              _metricsCollector.OnToolStart(toolStart.ToolName);
              _logger.LogDebug("Tool execution started: {ToolName}", toolStart.ToolName);
              break;
          
          case ToolExecutionCompleteEvent toolComplete:
              _metricsCollector.OnToolComplete(
                  toolComplete.ToolName,
                  toolComplete.Error == null,
                  toolComplete.Error);
              _logger.LogDebug("Tool execution complete: {ToolName}", toolComplete.ToolName);
              break;
          ```
          
          Note: ToolExecution event property names (ToolName, Error) need runtime verification.
          Add defensive null checks. If event types don't exist in SDK v0.1.22, wrap in
          try/catch and log a warning — don't break existing functionality.
          
          Add ToolMetrics property to ProcessedResponse record:
          ```csharp
          public ToolMetricsSummary? ToolMetrics { get; init; }
          ```
          
          Populate from _metricsCollector.GetMetrics() before returning ProcessedResponse.
        acceptance_criteria:
          - "ToolExecutionStartEvent and ToolExecutionCompleteEvent handled in event switch"
          - "ToolMetrics populated in ProcessedResponse"
          - "Defensive handling if event types don't exist in SDK version"
          - "Existing 3 event handlers unchanged"

      - step: "2.3"
        task: "Update CopilotService to create SdkResponseProcessor with metrics"
        files:
          - "Hugh/Agents/CopilotService.cs"
        details: |
          In ExecutePhaseAsync (lines 135-139), update SdkResponseProcessor construction
          to include ToolMetricsCollector:
          
          ```csharp
          var metricsCollector = new ToolMetricsCollector();
          var responseProcessor = new SdkResponseProcessor(
              _signalClassifier,
              _pathExtractor,
              metricsCollector,
              _loggerFactory.CreateLogger<SdkResponseProcessor>());
          ```
          
          After processing response, log metrics summary:
          ```csharp
          var metrics = processedResponse.ToolMetrics;
          if (metrics != null && metrics.TotalExecutions > 0)
          {
              _logger.Information(
                  "Tool metrics: {Total} executions, {Success} success, {Errors} errors",
                  metrics.TotalExecutions, metrics.SuccessCount, metrics.ErrorCount);
          }
          ```
        acceptance_criteria:
          - "ToolMetricsCollector passed to SdkResponseProcessor"
          - "Tool metrics logged after phase execution"
          - "Existing ExecutePhaseAsync flow unchanged"

      - step: "2.4"
        task: "Pin GitHub.Copilot.SDK version and create unit tests for ToolMetricsCollector"
        files:
          - "Hugh/Hugh.csproj"
          - "Hugh.Tests/ToolMetricsCollectorTests.cs (new)"
        details: |
          In Hugh.csproj, ensure SDK version is explicitly pinned (it already is at 0.1.22,
          verify no floating version ranges).
          
          Create ToolMetricsCollectorTests:
          - OnToolStart_StartsTimer
          - OnToolComplete_RecordsExecution
          - GetMetrics_AggregatesCorrectly
          - GetMetrics_EmptyReturnsZeros
          - Reset_ClearsAllData
          - ConcurrentAccess_ThreadSafe (use Parallel.For)
          - OnToolComplete_WithError_RecordsFailure
          
          Also update SdkResponseProcessorTests to verify the new constructor parameter
          doesn't break existing tests (add ToolMetricsCollector to test setup).
        acceptance_criteria:
          - "GitHub.Copilot.SDK pinned at v0.1.22 (no version range)"
          - "All ToolMetricsCollector tests pass"
          - "Existing SdkResponseProcessorTests updated and passing"

  - phase: 3
    name: "M2 — Custom Workflow Tool Registration"
    status: complete
    size: medium
    files_to_modify: 5
    prerequisites:
      - "Phase 2 complete — ToolExecution events handled in SdkResponseProcessor"
      - "Microsoft.Extensions.AI package added (step 3.1)"
    entry_point: "Hugh/Hugh.csproj (add package), then Hugh/Tools/HughWorkflowTools.cs (new)"
    verification: "Tool events logged correctly, all Phase 1-2 tests pass"
    tasks:
      - step: "3.1"
        task: "Add Microsoft.Extensions.AI package dependency"
        files:
          - "Hugh/Hugh.csproj"
        details: |
          Add package reference to Hugh.csproj:
          ```xml
          <PackageReference Include="Microsoft.Extensions.AI" Version="9.0.1-preview.1.25061.3" />
          ```
          
          Run `dotnet restore` to verify compatibility with existing packages,
          especially GitHub.Copilot.SDK v0.1.22.
          
          If version conflict occurs, try latest stable version or check SDK transitive
          dependencies for the compatible version range.
        acceptance_criteria:
          - "dotnet restore succeeds without version conflicts"
          - "dotnet build succeeds with new dependency"

      - step: "3.2"
        task: "Create Hugh/Tools/WorkflowToolResults.cs with result models"
        files:
          - "Hugh/Tools/WorkflowToolResults.cs (new)"
        details: |
          Create result record types in new Hugh/Tools/ directory:
          
          ```csharp
          namespace Hugh.Tools;
          
          public record PhaseCompleteResult
          {
              public bool Acknowledged { get; init; } = true;
              public int? NextPhaseNumber { get; init; }
              public string Message { get; init; } = "Phase completion acknowledged";
          }
          
          public record WorkflowCompleteResult
          {
              public bool Acknowledged { get; init; } = true;
              public string Message { get; init; } = "Workflow completion acknowledged";
          }
          
          public record BlockerResult
          {
              public bool Acknowledged { get; init; } = true;
              public string Action { get; init; } = "paused";
              public string Message { get; init; } = "Blocker acknowledged — execution paused";
          }
          
          public record WorkspaceStructureResult
          {
              public string RootPath { get; init; } = "";
              public string DirectoryTree { get; init; } = "";
              public int FileCount { get; init; }
          }
          ```
        acceptance_criteria:
          - "All result records compile with correct properties"
          - "Default values set for common success case"

      - step: "3.3"
        task: "Create Hugh/Tools/HughWorkflowTools.cs with tool implementations"
        files:
          - "Hugh/Tools/HughWorkflowTools.cs (new)"
        details: |
          Create HughWorkflowTools class with tool methods and AIFunction registration.
          
          Constructor: ILogger logger, string workspacePath
          
          Methods (decorated with [Description] attributes for SDK registration):
          
          1. NotifyPhaseComplete(int phase, string summary, string? outputDocumentPath):
             - Log Information: "Phase {Phase} complete: {Summary}"
             - Return PhaseCompleteResult { NextPhaseNumber = phase + 1 }
          
          2. NotifyWorkflowComplete(string summary, List<string>? documentsCreated):
             - Log Information: "Workflow complete: {Summary}"
             - Return WorkflowCompleteResult
          
          3. ReportBlocker(string reason, string severity, bool isSkippable, List<string>? suggestedActions):
             - Log Error: "Blocker reported: {Reason} (severity: {Severity})"
             - Return BlockerResult { Action = "paused" }
          
          4. GetWorkspaceStructure(int maxDepth = 3, string? filterPath = null):
             - Build directory tree string from workspacePath
             - Respect maxDepth to limit recursion
             - Count files
             - Return WorkspaceStructureResult
          
          CreateToolSet() factory method:
          ```csharp
          public AIFunction[] CreateToolSet()
          {
              return new[]
              {
                  AIFunctionFactory.Create(NotifyPhaseComplete,
                      name: "hugh_notify_phase_complete",
                      description: "Notify the orchestrator that the current phase is complete"),
                  AIFunctionFactory.Create(NotifyWorkflowComplete,
                      name: "hugh_notify_workflow_complete",
                      description: "Notify the orchestrator that the entire workflow is complete"),
                  AIFunctionFactory.Create(ReportBlocker,
                      name: "hugh_report_blocker",
                      description: "Report a blocker that prevents the agent from proceeding"),
                  AIFunctionFactory.Create(GetWorkspaceStructure,
                      name: "hugh_get_workspace_structure",
                      description: "Get the current workspace directory structure")
              };
          }
          ```
        acceptance_criteria:
          - "All 4 tool methods compile with correct signatures"
          - "CreateToolSet() returns AIFunction array"
          - "Tool names use hugh_ prefix"
          - "GetWorkspaceStructure respects maxDepth"

      - step: "3.4"
        task: "Wire HughWorkflowTools into CopilotService.CreateSessionAsync"
        files:
          - "Hugh/Agents/CopilotService.cs"
        details: |
          In CreateSessionAsync, create HughWorkflowTools and register tools:
          
          ```csharp
          var workflowTools = new HughWorkflowTools(_logger, workspacePath);
          var fileSystemHooks = new FileSystemHooks(_logger, workspacePath, agentDefinition.Name);
          
          var session = await _client.CreateSessionAsync(new SdkSessionConfig
          {
              Model = _config.Copilot.Model,
              SystemMessage = new SystemMessageConfig
              {
                  Mode = SystemMessageMode.Append,
                  Content = agentPrompt
              },
              Hooks = fileSystemHooks.CreateHooks(),
              Tools = workflowTools.CreateToolSet()
          });
          ```
          
          Store HughWorkflowTools reference for signal detection in ExecutePhaseAsync.
        acceptance_criteria:
          - "SessionConfig.Tools set with workflow tools"
          - "Tools and Hooks coexist in same SessionConfig"
          - "Session creation succeeds with both Hooks and Tools"

      - step: "3.5"
        task: "Create unit tests for HughWorkflowTools"
        files:
          - "Hugh.Tests/HughWorkflowToolsTests.cs (new)"
        details: |
          Test pure logic without SDK dependency:
          
          - NotifyPhaseComplete_ReturnsAcknowledged
          - NotifyPhaseComplete_SetsNextPhaseNumber
          - NotifyWorkflowComplete_ReturnsAcknowledged
          - ReportBlocker_ReturnsPausedAction
          - ReportBlocker_LogsError
          - GetWorkspaceStructure_ReturnsTree (use temp directory)
          - GetWorkspaceStructure_RespectsMaxDepth
          - GetWorkspaceStructure_WithFilter_LimitsScope
          - CreateToolSet_ReturnsFourTools
          - CreateToolSet_ToolNamesHaveHughPrefix
        acceptance_criteria:
          - "All HughWorkflowTools tests pass"
          - "GetWorkspaceStructure tested with real temp directories"
          - "CreateToolSet verified to return correct count and names"

  - phase: 4
    name: "M2 — Tool-Based Signal Detection and Agent Prompt Updates"
    status: complete
    size: large
    files_to_modify: 10
    prerequisites:
      - "Phase 3 complete — HughWorkflowTools registered in CreateSessionAsync"
    entry_point: "Hugh/Agents/SdkResponseProcessor.cs"
    verification: "Custom tools registered, Phase 1-3 tests pass"
    tasks:
      - step: "4.1"
        task: "Extend SdkResponseProcessor to detect tool call signals"
        files:
          - "Hugh/Agents/SdkResponseProcessor.cs"
        details: |
          Tool-based signals arrive as ToolExecutionCompleteEvent when custom tools
          like hugh_notify_phase_complete are called by agents.
          
          In the ToolExecutionCompleteEvent handler (added in Phase 2):
          - Check if toolComplete.ToolName starts with "hugh_"
          - If "hugh_notify_phase_complete": Create SignalClassification with
            Type = PhaseComplete, extract phase number from tool result
          - If "hugh_notify_workflow_complete": Type = WorkflowComplete
          - If "hugh_report_blocker": Type = Blocker
          
          Add _toolBasedSignal field to track the most recent tool-based signal.
          In the return path, check _toolBasedSignal BEFORE _signalClassifier.Classify():
          
          ```csharp
          // Tool-based signal takes priority over text-based
          var signal = _toolBasedSignal ?? _signalClassifier.Classify(content);
          ```
          
          This ensures tool calls are the primary signal source with text as fallback.
        acceptance_criteria:
          - "hugh_notify_phase_complete tool call detected as PhaseComplete signal"
          - "hugh_report_blocker tool call detected as Blocker signal"
          - "Tool-based signals take priority over text-based regex"
          - "Text-based fallback still works when no tool call detected"

      - step: "4.2"
        task: "Update pch-coder and pch-researcher agent prompts with tool instructions"
        files:
          - "agents/pch-coder.md"
          - "agents/pch-researcher.md"
        details: |
          Add a new section to both agent prompts (near the existing return format instructions):
          
          ```markdown
          ## Structured Communication (Preferred)
          
          When completing a phase, prefer calling the `hugh_notify_phase_complete` tool:
          - phase: The phase number you completed (1-based)
          - summary: Brief description of what was accomplished
          - outputDocumentPath: Path to any document you created or modified
          
          When encountering a blocker, prefer calling the `hugh_report_blocker` tool:
          - reason: Description of what is blocking progress
          - severity: low | medium | high | critical
          - isSkippable: Whether this blocker can be skipped
          - suggestedActions: List of possible resolutions
          
          When all phases are done, call `hugh_notify_workflow_complete`:
          - summary: Final summary of what was accomplished
          - documentsCreated: List of document paths created
          
          **Fallback:** If these tools are unavailable, use the text-based format below.
          ```
          
          Keep ALL existing text-based format instructions intact — they become the fallback.
        acceptance_criteria:
          - "pch-coder.md has Structured Communication section with all 3 tools"
          - "pch-researcher.md has Structured Communication section with all 3 tools"
          - "Existing text-based format instructions preserved as fallback"
          - "Tool instructions appear before text-based instructions"

      - step: "4.3"
        task: "Update remaining 5 agent prompts with tool instructions"
        files:
          - "agents/pch-planner.md"
          - "agents/pch-plan-reviewer.md"
          - "agents/pch-visionary.md"
          - "agents/pch-helper.md"
          - "agents/hugh.md"
        details: |
          Add the same Structured Communication section to the remaining agents.
          
          For pch-planner and pch-plan-reviewer: Include hugh_notify_phase_complete and
          hugh_report_blocker. These agents don't typically complete research/coding phases
          but may signal blockers.
          
          For pch-visionary: Include all 3 tools — visionary completes a workflow.
          
          For pch-helper: Include hugh_report_blocker only — helper doesn't complete phases.
          
          For hugh: Include all 3 tools — the orchestrator agent uses all signals.
          
          Each prompt maintains existing text-based format as fallback.
        acceptance_criteria:
          - "All 5 remaining agent prompts updated with tool instructions"
          - "Tool instructions appropriate for each agent's role"
          - "Existing text-based formats preserved"

      - step: "4.4"
        task: "Update SdkResponseProcessorTests for tool-based signal detection"
        files:
          - "Hugh.Tests/SdkResponseProcessorTests.cs"
        details: |
          Add tests for tool-based signal detection:
          
          - ToolCall_HughNotifyPhaseComplete_DetectedAsPhaseComplete
          - ToolCall_HughReportBlocker_DetectedAsBlocker
          - ToolCall_HughNotifyWorkflowComplete_DetectedAsWorkflowComplete
          - ToolCall_TakesPriorityOverTextSignal
          - NoToolCall_FallsBackToTextClassification
          - NonHughToolCall_Ignored
          
          These tests may require mocking the event stream or testing the signal
          detection logic extracted into a testable helper method.
        acceptance_criteria:
          - "All tool-based signal detection tests pass"
          - "Priority of tool vs text signals verified"
          - "Non-Hugh tool calls don't trigger signal detection"

  - phase: 5
    name: "M2 — Signal Priority Integration and Verification"
    status: complete
    size: small
    files_to_modify: 3
    prerequisites:
      - "Phase 4 complete and stable — tool-based signals verified working"
      - "At least 1 full end-to-end run with tool-based signaling succeeds"
    entry_point: "Hugh/Agents/SdkResponseProcessor.cs"
    verification: "Tool-based signals working in Phase 4, all tests pass"
    tasks:
      - step: "5.1"
        task: "Document and verify tool-first priority in SdkResponseProcessor"
        files:
          - "Hugh/Agents/SdkResponseProcessor.cs"
        details: |
          **Per Review Decision D10 — DO NOT remove any SignalClassifier regex patterns.**
          All regex patterns are kept permanently as fallback.
          
          Instead, ensure SdkResponseProcessor implements tool-first priority:
          1. Check if any hugh_* tool call events were detected during the session
          2. If yes, use the tool-based signal (PhaseComplete, Blocker, WorkflowComplete)
          3. If no tool signal detected, fall back to SignalClassifier.Classify() text regex
          4. Log a warning when text fallback fires: "No tool-based signal detected,
             falling back to text regex classification" — this may indicate tool
             registration or detection issues
          
          Add XML documentation comments explaining the permanent dual-detection
          architecture and why both paths are maintained.
        acceptance_criteria:
          - "SdkResponseProcessor checks tool events before text regex"
          - "Warning logged when text fallback fires"
          - "XML documentation explains dual-detection architecture"
          - "All existing SignalClassifier regex patterns preserved"

      - step: "5.2"
        task: "Add tests for tool-first priority and text fallback"
        files:
          - "Hugh.Tests/SdkResponseProcessorTests.cs"
        details: |
          Add tests verifying the priority behavior:
          - ToolSignal_TakesPriority_WhenBothPresent
          - TextFallback_UsedWhenNoToolSignal
          - TextFallback_LogsWarning
          - AllRegexPatterns_StillFunctional (verify no patterns were removed)
          
          All existing SignalClassifierTests remain unchanged — every regex
          pattern is still active and tested.
        acceptance_criteria:
          - "Priority behavior tested — tool-first, text-fallback"
          - "Warning logging on fallback tested"
          - "All existing SignalClassifierTests pass unchanged"

      - step: "5.3"
        task: "Verify text-based signal formats remain in agent prompts (no removal)"
        files:
          - "agents/pch-coder.md"
          - "agents/pch-researcher.md"
        details: |
          **Per Review Decision D9 — DO NOT remove text-based signal formats.**
          
          Agent prompts are shared between VS Code Copilot chat and Hugh CLI.
          VS Code agents do NOT have access to hugh_* custom tools (those are
          registered via SDK SessionConfig.Tools only in Hugh CLI). Removing
          text-based formats would break VS Code subagent signaling.
          
          Verification only:
          - Confirm pch-coder.md retains "✅ **Phase [N] Complete**" format
          - Confirm pch-coder.md retains "❌ **Blocked**" format
          - Confirm pch-researcher.md retains both formats
          - Confirm the Structured Communication section (added in Phase 4.2)
            appears BEFORE the text-based formats with "Fallback" language
          
          No file modifications in this step.
        acceptance_criteria:
          - "Text-based signal formats confirmed present in pch-coder.md"
          - "Text-based signal formats confirmed present in pch-researcher.md"
          - "Structured Communication section appears before text formats"
          - "No text-based formats removed from any agent prompt"

  - phase: 6
    name: "M3 — SDK-Native SdkQuestionHandler"
    status: complete
    size: medium
    files_to_modify: 5
    prerequisites:
      - "Phase 1-2 complete (M1 hooks and events working)"
      - "Can run in parallel with Phase 3-5 (M2)"
    entry_point: "Hugh/Handlers/SdkQuestionHandler.cs (new file)"
    verification: "M1 hooks working, all existing tests pass"
    tasks:
      - step: "6.1"
        task: "Create Hugh/Handlers/SdkQuestionHandler.cs"
        files:
          - "Hugh/Handlers/SdkQuestionHandler.cs (new)"
        details: |
          Create SdkQuestionHandler that operates directly on SDK UserInputRequest objects.
          
          Constructor: DecisionLogger decisionLogger, AutonomousConfig autonomousConfig, ILogger logger
          
          HandleAsync(UserInputRequest request, Invocation invocation, string currentPhase, Guid runId):
          
          1. Check if request has a recommendation in Prompt text:
             - Call DetectRecommendation(request.Prompt) to parse "**Recommendation:** A — ..."
             - If found AND autonomousConfig.AcceptRecommendationsOnly == true:
               - Log auto-accept decision via DecisionLogger
               - Return UserInputResponse { Answer = letterValue }
          
          2. Check if request.Choices is populated and has a clear recommendation:
             - If choices exist and exactly one is marked/recommended:
               - Auto-accept it
               - Return UserInputResponse { Answer = recommendedChoice }
          
          3. Check for mode selection pattern in request.Prompt:
             - If contains "Execution Mode Selection":
               - Auto-select "1" (Manual mode)
               - Log decision
               - Return UserInputResponse { Answer = "1" }
          
          4. Open question (no recommendation, no choices, or AllowFreeform):
             - Display question to console via ConsoleOutput
             - Display choices if available
             - Read user input from Console.ReadLine()
             - Return UserInputResponse { Answer = userInput }
          
          DetectRecommendation(string prompt) → (string? letter, string? option):
          - Use RecommendationRegex pattern to extract letter and option name
          - Return null if no recommendation found
          
          ShouldAutoAccept(UserInputRequest request) → bool:
          - True if recommendation detected and AcceptRecommendationsOnly enabled
        acceptance_criteria:
          - "SdkQuestionHandler compiles and handles UserInputRequest objects"
          - "Auto-accepts recommendations when AcceptRecommendationsOnly is true"
          - "Pauses for open questions with no recommendation"
          - "Handles mode selection by auto-selecting 1"
          - "Logs decisions via DecisionLogger"

      - step: "6.2"
        task: "Wire SdkQuestionHandler into CopilotService.CreateSessionAsync"
        files:
          - "Hugh/Agents/CopilotService.cs"
        details: |
          In CopilotService constructor:
          - Create SdkQuestionHandler with DecisionLogger, _config.Autonomous, _logger
          - Store as field: _sdkQuestionHandler
          
          In CreateSessionAsync, add OnUserInputRequest to SessionConfig:
          
          ```csharp
          OnUserInputRequest = async (UserInputRequest request, Invocation invocation) =>
          {
              return await _sdkQuestionHandler.HandleAsync(
                  request, invocation, agentDefinition.Name, runId);
          }
          ```
          
          Note: The runId needs to be accessible in CreateSessionAsync. This may require
          passing runId as a parameter or storing it as a field set in ExecutePhaseAsync.
          
          The existing custom OnUserInputRequest property (line 37-38) remains but is now
          used only as a fallback for text-based signal handling in HandleSignalAsync.
        acceptance_criteria:
          - "SDK-native OnUserInputRequest callback set in SessionConfig"
          - "SdkQuestionHandler receives UserInputRequest objects"
          - "Existing custom OnUserInputRequest property preserved for fallback"
          - "runId accessible in the callback closure"

      - step: "6.3"
        task: "Create unit tests for SdkQuestionHandler"
        files:
          - "Hugh.Tests/SdkQuestionHandlerTests.cs (new)"
        details: |
          Test SdkQuestionHandler logic without live SDK:
          
          DetectRecommendation tests:
          - DetectRecommendation_WithRecommendation_ReturnsLetterAndOption
          - DetectRecommendation_NoRecommendation_ReturnsNull
          - DetectRecommendation_MultipleRecommendations_ReturnsFirst
          
          HandleAsync tests (may need to mock UserInputRequest or create test instances):
          - HandleAsync_WithRecommendation_AutoAccepts
          - HandleAsync_WithRecommendation_AcceptDisabled_Pauses
          - HandleAsync_OpenQuestion_Pauses
          - HandleAsync_ModeSelection_AutoSelectsManual
          - HandleAsync_WithChoices_AutoAcceptsRecommended
          - HandleAsync_LogsDecisionViaDecisionLogger
          
          ShouldAutoAccept tests:
          - ShouldAutoAccept_RecommendationPresent_ReturnsTrue
          - ShouldAutoAccept_NoRecommendation_ReturnsFalse
          - ShouldAutoAccept_AcceptDisabled_ReturnsFalse
        acceptance_criteria:
          - "All SdkQuestionHandler tests pass"
          - "DetectRecommendation tested with various prompt formats"
          - "Auto-accept and pause behaviors verified"
          - "Decision logging verified with mock DecisionLogger"

  - phase: 7
    name: "M3 — QuestionHandler Delegation and Integration Verification"
    status: complete
    size: small
    files_to_modify: 4
    prerequisites:
      - "Phase 6 complete and stable — SdkQuestionHandler verified working"
      - "At least 1 full end-to-end run with SDK-native question handling succeeds"
    entry_point: "Hugh/Detection/SignalClassifier.cs"
    verification: "SDK-native question handling working in Phase 6, all tests pass"
    tasks:
      - step: "7.1"
        task: "Verify question regex patterns remain as fallback in SignalClassifier"
        files:
          - "Hugh/Detection/SignalClassifier.cs"
        details: |
          **Per Review Decision D10 — DO NOT remove any question regex patterns.**
          
          All patterns stay permanently:
          - NeedsUserInputRegex — fallback if SDK OnUserInputRequest doesn't fire
          - QuestionForUserRegex — fallback for question text extraction
          - QuestionOptionsRegex — fallback for option extraction
          - OpenQuestionRegex — fallback for open question detection
          - RecommendationRegex — used by SdkQuestionHandler.DetectRecommendation()
          
          Verification only:
          - Confirm all 9 regex patterns still present in SignalClassifier
          - Confirm Classify() still checks all patterns
          - Confirm SignalType enum values unchanged
          - Confirm QuestionText and QuestionOptions properties still populated
          
          No file modifications in this step.
        acceptance_criteria:
          - "All 9 regex patterns confirmed present in SignalClassifier"
          - "Classify() checks all patterns — no removals"
          - "SignalClassification properties unchanged"
          - "All existing SignalClassifierTests pass unchanged"

      - step: "7.2"
        task: "Refactor QuestionHandler as thin delegation wrapper around SdkQuestionHandler"
        files:
          - "Hugh/Handlers/QuestionHandler.cs"
          - "Hugh/Agents/CopilotService.cs"
        details: |
          **Per Review Decision D11 — QuestionHandler stays active as text-fallback handler,
          delegating to SdkQuestionHandler for decision logic.**
          
          Refactor QuestionHandler:
          - Add SdkQuestionHandler as a constructor dependency
          - In HandleAsync (or the existing question handling method):
            1. Convert the text-parsed SignalClassification (QuestionText, QuestionOptions,
               RecommendationLetter) into a UserInputRequest-compatible object
            2. Delegate to SdkQuestionHandler.HandleAsync() for the actual decision
               (auto-accept recommendation, pause for open questions, log decisions)
            3. Return the QuestionResponse from SdkQuestionHandler's result
          - Remove any duplicated decision logic (auto-accept, pause behavior) —
            SdkQuestionHandler is the single source of truth
          - Keep QuestionHandler's public API unchanged so HandleSignalAsync routing
            in CopilotService doesn't need modification
          
          In CopilotService:
          - HandleSignalAsync cases for QuestionWithRecommendation, QuestionOpen,
            ModeSelection, NeedsUserInput remain — they route to QuestionHandler
            which internally delegates to SdkQuestionHandler
          - Inject SdkQuestionHandler into QuestionHandler during construction
          - SDK-native OnUserInputRequest callback (Phase 6.2) is the primary path;
            HandleSignalAsync question cases are the text-fallback path
          
          Do NOT mark QuestionHandler as [Obsolete] — it remains actively used
          as the text-fallback handler in HandleSignalAsync.
        acceptance_criteria:
          - "QuestionHandler delegates to SdkQuestionHandler for decision logic"
          - "No duplicate auto-accept or pause logic in QuestionHandler"
          - "HandleSignalAsync question cases unchanged — still route to QuestionHandler"
          - "SdkQuestionHandler is single source of truth for question resolution"
          - "QuestionHandler public API preserved (HandleSignalAsync routing unchanged)"

      - step: "7.3"
        task: "Update QuestionHandler tests for delegation pattern and verify prompt preservation"
        files:
          - "Hugh.Tests/QuestionHandlerTests.cs"
        details: |
          QuestionHandlerTests:
          - Update tests to verify delegation to SdkQuestionHandler
          - Test that QuestionHandler constructs correct UserInputRequest from
            text-parsed SignalClassification
          - Test that auto-accept decision comes from SdkQuestionHandler (not duplicated)
          - Test that pause decision comes from SdkQuestionHandler (not duplicated)
          - Use mock SdkQuestionHandler to verify delegation calls
          
          SignalClassifierTests:
          - All existing tests remain unchanged (all regex patterns are permanent per D10)
          - No tests removed
          
          **Per Review Decision D9 — DO NOT remove text-based question formats
          from agent prompts.** Verification only:
          - Confirm pch-coder.md retains all question/status text formats
          - Confirm pch-researcher.md retains all question/status text formats
        acceptance_criteria:
          - "QuestionHandler delegation to SdkQuestionHandler tested"
          - "SignalClassification-to-UserInputRequest conversion tested"
          - "All existing SignalClassifierTests pass unchanged (no tests removed)"
          - "Text question formats confirmed present in agent prompts (NOT removed)"
          - "All tests pass"

  - phase: 8
    name: "M4 — InfiniteSession, Error Classification, and Compaction Events"
    status: complete
    size: small
    files_to_modify: 4
    prerequisites:
      - "Phase 1-2 complete (M1 hooks and events working)"
      - "Can run in parallel with Phase 3-7 (M2/M3)"
    entry_point: "Hugh/Agents/CopilotService.cs"
    verification: "M1 hooks working, all existing tests pass"
    tasks:
      - step: "8.1"
        task: "Enable InfiniteSession in CreateSessionAsync"
        files:
          - "Hugh/Agents/CopilotService.cs"
        details: |
          In CreateSessionAsync, add InfiniteSession to SessionConfig:
          
          ```csharp
          InfiniteSession = new InfiniteSessionConfig
          {
              CompactionThreshold = _config.Copilot.CompactionThreshold
          }
          ```
          
          This uses the existing _config.Copilot.CompactionThreshold (default 0.8)
          from appsettings.json. No new config properties needed.
          
          Verify InfiniteSessionConfig type exists in GitHub.Copilot.SDK v0.1.22.
          If it doesn't exist, wrap in try/catch and log a warning.
        acceptance_criteria:
          - "InfiniteSession set in SessionConfig with CompactionThreshold = 0.8"
          - "Uses existing config property — no new appsettings changes"
          - "Graceful fallback if InfiniteSessionConfig not available in SDK"

      - step: "8.2"
        task: "Handle SessionCompaction events in SdkResponseProcessor"
        files:
          - "Hugh/Agents/SdkResponseProcessor.cs"
        details: |
          In ProcessResponseAsync event subscription, add compaction event handling:
          
          ```csharp
          case SessionCompactionStartEvent:
              _logger.LogInformation("Context compaction started — context window nearing capacity");
              break;
          
          case SessionCompactionCompleteEvent:
              _logger.LogInformation("Context compaction complete — context freed");
              break;
          ```
          
          Verify these event types exist in SDK v0.1.22. If not, remove the cases
          (the switch statement won't match unknown types).
          
          Add CompactionOccurred boolean to ProcessedResponse:
          ```csharp
          public bool CompactionOccurred { get; init; }
          ```
          Set to true if either compaction event was received.
        acceptance_criteria:
          - "Compaction events logged when received"
          - "CompactionOccurred flag set in ProcessedResponse"
          - "Graceful handling if event types don't exist in SDK"

      - step: "8.3"
        task: "Add error classification utilities"
        files:
          - "Hugh/Detection/SessionErrorClassifier.cs (new)"
        details: |
          Create SessionErrorClassifier to categorize session errors:
          
          ```csharp
          namespace Hugh.Detection;
          
          public enum ErrorCategory
          {
              Transient,      // network, timeout, 502/503
              RateLimited,    // rate limit responses
              Authentication, // auth failures
              ToolFailure,    // tool execution errors
              ContextExhausted, // context window full
              Unknown         // unclassified
          }
          
          public record ErrorClassification
          {
              public ErrorCategory Category { get; init; }
              public bool IsRetryable { get; init; }
              public TimeSpan? SuggestedDelay { get; init; }
              public string? SuggestedAction { get; init; }
          }
          
          public class SessionErrorClassifier
          {
              public ErrorClassification Classify(string errorMessage)
              {
                  // Transient: timeout, connection reset, 502, 503
                  // RateLimited: rate limit, 429
                  // Authentication: unauthorized, 401, auth
                  // ContextExhausted: context, token limit
                  // ToolFailure: tool, execution failed
                  // Unknown: default
              }
          }
          ```
          
          Integrate with CopilotService.IsTransientError() — replace the simple
          string checks with SessionErrorClassifier.Classify().
        acceptance_criteria:
          - "SessionErrorClassifier classifies errors into 6 categories"
          - "IsRetryable flag correctly set per category"
          - "SuggestedDelay set for RateLimited (60s) and Transient (exponential)"
          - "Integrates with CopilotService error handling"

      - step: "8.4"
        task: "Create tests for SessionErrorClassifier and final integration verification"
        files:
          - "Hugh.Tests/SessionErrorClassifierTests.cs (new)"
        details: |
          Tests for error classification:
          - Classify_Timeout_ReturnsTransient
          - Classify_ConnectionReset_ReturnsTransient
          - Classify_503_ReturnsTransient
          - Classify_RateLimit_ReturnsRateLimited
          - Classify_429_ReturnsRateLimited
          - Classify_Unauthorized_ReturnsAuthentication
          - Classify_ContextWindow_ReturnsContextExhausted
          - Classify_ToolError_ReturnsToolFailure
          - Classify_Unknown_ReturnsUnknown
          - Classify_IsRetryable_CorrectPerCategory
          - Classify_SuggestedDelay_CorrectPerCategory
          
          Run ALL existing tests to verify no regressions across all phases.
        acceptance_criteria:
          - "All error classification tests pass"
          - "All existing tests across all phases pass"
          - "Full test suite green"

  - phase: 9
    name: "Documentation and Copilot Instructions Updates"
    status: complete
    size: medium
    files_to_modify: 3
    prerequisites:
      - "Phases 1-8 complete — all new components exist and are tested"
    entry_point: ".github/copilot-instructions.md"
    verification: "All new components documented, architecture diagrams updated"
    tasks:
      - step: "9.1"
        task: "Update .github/copilot-instructions.md with new Hugh components"
        files:
          - ".github/copilot-instructions.md"
        details: |
          Update the Hugh Architecture section to document all new components:
          
          **Agent Integration (`Hugh/Agents/`) section:**
          - Update SdkResponseProcessor description: mention dual signal detection
            (tool events primary, text regex fallback per D10), ToolExecution event
            handling, SessionCompaction event handling, ToolMetrics population
          - Update CopilotService description: mention SessionConfig now configures
            Hooks, Tools, OnUserInputRequest, InfiniteSession in addition to
            Model and SystemMessage
          
          **Add new section: Hooks (`Hugh/Hooks/`)**
          - FileSystemHooks — OnPreToolUse/OnPostToolUse callbacks for directory
            auto-creation, path monitoring (logging-only), tool completion logging
          
          **Add new section: Custom Workflow Tools (`Hugh/Tools/`)**
          - HughWorkflowTools — hugh_notify_phase_complete, hugh_report_blocker,
            hugh_notify_workflow_complete, hugh_get_workspace_structure
          - WorkflowToolResults — PhaseCompleteResult, BlockerResult,
            WorkflowCompleteResult, WorkspaceStructureResult
          
          **Add new section: Metrics (`Hugh/Metrics/`)**
          - ToolMetricsCollector — tracks tool execution timing, counts, error rates
          
          **Signal Detection (`Hugh/Detection/`) section:**
          - Update SignalClassifier description: note that all regex patterns are
            permanent (text-fallback for dual-environment compatibility per D9/D10)
          - Add SessionErrorClassifier — categorizes session errors into 6 types
          
          **Question Handling (`Hugh/Handlers/`) section:**
          - Add SdkQuestionHandler — handles SDK-native UserInputRequest objects,
            single source of truth for question resolution (auto-accept, pause, logging)
          - Update QuestionHandler description: now a thin delegation wrapper around
            SdkQuestionHandler for text-detected question signals (per D11)
          
          **Testing section:**
          - Add new test files: FileSystemHooksTests, ToolMetricsCollectorTests,
            HughWorkflowToolsTests, SdkQuestionHandlerTests, SessionErrorClassifierTests
          - Note QuestionHandlerTests updated for delegation pattern
          - Note SdkResponseProcessorTests updated for tool-first priority
          
          **Configuration section (appsettings.json table):**
          - CompactionThreshold is already documented — add note that it now also
            wires into InfiniteSession.CompactionThreshold
        acceptance_criteria:
          - "All new directories (Hooks/, Tools/, Metrics/) documented"
          - "All new classes documented with descriptions"
          - "SdkResponseProcessor dual signal detection documented"
          - "QuestionHandler delegation pattern documented"
          - "SignalClassifier permanent fallback role documented"
          - "All new test files listed in Testing section"
          - "CompactionThreshold InfiniteSession usage noted"

      - step: "9.2"
        task: "Update Hugh/README.md with new architecture components"
        files:
          - "Hugh/README.md"
        details: |
          Update the SDK Integration section and architecture diagram:
          
          **SDK Integration section:**
          - Add bullet: **Session Hooks** — OnPreToolUse/OnPostToolUse for directory
            safety, path monitoring, and tool execution observability
          - Add bullet: **Custom Workflow Tools** — Structured agent-to-orchestrator
            signaling via hugh_* tools (phase completion, blockers, workspace info)
          - Add bullet: **SDK-Native Question Handling** — OnUserInputRequest callback
            for structured question resolution
          - Add bullet: **Infinite Session** — Automatic context compaction at
            configurable threshold
          - Update **Response Processing** bullet: mention dual signal detection
            (tool-first, text-fallback)
          
          **Architecture diagram:**
          Update the ASCII architecture diagram to include new layers:
          ```
          ┌──────────────────────────────────────────────────────────────┐
          │                      Hugh Orchestrator                       │
          ├──────────────────────────────────────────────────────────────┤
          │  LoopStateManager  │  SignalClassifier  │  DocumentManager   │
          └─────────┬──────────┴─────────┬──────────┴─────────┬──────────┘
                    │                    │                    │
                    ▼                    ▼                    ▼
          ┌─────────────────────────────────────────────────────────────┐
          │                       CopilotService                        │
          │  ┌─────────────┐  ┌────────────────┐  ┌──────────────────┐  │
          │  │ AgentLoader │  │ SdkResponseProc│  │ FileSystemHooks  │  │
          │  └─────────────┘  └────────────────┘  └──────────────────┘  │
          │  ┌─────────────┐  ┌────────────────┐  ┌──────────────────┐  │
          │  │ WorkflowTool│  │ SdkQuestionHdlr│  │ RetryPolicy      │  │
          │  └─────────────┘  └────────────────┘  └──────────────────┘  │
          └─────────────────────────────────────────────────────────────┘
          ```
          
          **Project Structure section:**
          Add new directories to the tree:
          ```
          Hugh/
          ├── Hooks/            # SDK session hooks (directory safety, tool monitoring)
          ├── Tools/            # Custom workflow tools (phase/blocker signaling)
          ├── Metrics/          # Tool execution metrics and timing
          ```
          
          **Copilot Configuration Options table:**
          - Add note under CompactionThreshold: "Also wires into
            InfiniteSession.CompactionThreshold for automatic context compaction"
        acceptance_criteria:
          - "SDK Integration section lists all new capabilities (hooks, tools, questions, infinite session)"
          - "Architecture diagram includes FileSystemHooks, WorkflowTools, SdkQuestionHandler"
          - "Project Structure includes Hooks/, Tools/, Metrics/ directories"
          - "CompactionThreshold InfiniteSession usage noted in config table"

      - step: "9.3"
        task: "Update .github/copilot-instructions.md with dual-environment signaling documentation"
        files:
          - ".github/copilot-instructions.md"
        details: |
          Add a new section under Hugh Architecture explaining the dual-environment
          signaling architecture. This is critical context for future development:
          
          **New section: Dual-Environment Agent Signaling**
          
          Document that:
          - Agent prompts under agents/ are shared between VS Code Copilot chat
            and Hugh CLI orchestration
          - VS Code agents use text-based signals only (no access to hugh_* tools)
          - Hugh CLI agents use tool-based signals as primary, text regex as fallback
          - Agent prompts contain BOTH tool-based instructions ("Structured Communication")
            AND text-based format instructions — both must be preserved
          - SignalClassifier regex patterns are permanent (not deprecated) per D10
          - QuestionHandler delegates to SdkQuestionHandler for decision logic per D11
          - SdkResponseProcessor implements tool-first priority with text fallback
          
          Include a summary table:
          | Capability | VS Code Copilot | Hugh CLI |
          |---|---|---|
          | Signal detection | Text regex (orchestrator agent) | Tool events (primary) + text regex (fallback) |
          | Question handling | Text return format | SDK OnUserInputRequest (primary) + text fallback via QuestionHandler delegation |
          | Custom tools | Not available | hugh_* tools via SessionConfig.Tools |
          | Session hooks | Not available | FileSystemHooks via SessionConfig.Hooks |
          | InfiniteSession | Not available | CompactionThreshold = 0.8 |
          
          **IMPORTANT:** Add a warning callout:
          > ⚠️ **Never remove text-based signal formats from agent prompts.**
          > They are required for VS Code Copilot chat compatibility (Decisions D9, D10, D11).
        acceptance_criteria:
          - "Dual-environment signaling section added to copilot-instructions.md"
          - "Environment comparison table included"
          - "Warning about never removing text formats from prompts included"
          - "References to D9, D10, D11 decisions included"
          - "QuestionHandler delegation pattern explained"

implementation_complexity:
  files_to_modify:
    score: 4
    notes: "~18 files across Hugh/Hooks/, Hugh/Tools/, Hugh/Metrics/, Hugh/Handlers/, Hugh/Agents/, Hugh/Detection/, Hugh.Tests/, agents/, .github/, Hugh/README.md"
  new_patterns_introduced:
    score: 3
    notes: "SDK hook callbacks, custom tool registration via AIFunctionFactory, SDK-native OnUserInputRequest callback, QuestionHandler delegation pattern"
  external_dependencies:
    score: 2
    notes: "Microsoft.Extensions.AI (new). GitHub.Copilot.SDK v0.1.22 (existing, technical preview)"
  migration_complexity:
    score: 1
    notes: "Fully additive — no removals or breaking changes. All existing patterns preserved permanently."
  test_coverage_required:
    score: 3
    notes: "7 new test files: FileSystemHooksTests, ToolMetricsCollectorTests, HughWorkflowToolsTests, SdkResponseProcessorTests (updated), SdkQuestionHandlerTests, QuestionHandlerTests (updated for delegation), SessionErrorClassifierTests"
  overall:
    score: "13/25"
    level: "Medium"

review_summary:
  review_date: "2026-02-10"
  reviewer: pch-plan-reviewer
  original_plan_version: "v2.0"
  reviewed_plan_version: "v2.5"
  
  review_metrics:
    issues_found: 3
    critical: 3
    major: 0
    minor: 0
    clarifying_questions_asked: 3
    sections_updated:
      - "decisions (added D9, D10, D11)"
      - "review_session_log (added)"
      - "holistic_review.decision_interactions (3 interactions rewritten)"
      - "holistic_review.architectural_considerations (SdkResponseProcessor dual detection updated)"
      - "holistic_review.trade_offs_accepted (D5 trade-off replaced)"
      - "holistic_review.risks_acknowledged (pattern removal risk replaced)"
      - "requirements.functional F17, F18, F19 (rewritten)"
      - "requirements.out_of_scope (expanded)"
      - "overview.success_criteria (updated)"
      - "risks R1 mitigation, R8 (resolved)"
      - "phases 5 (restructured from cleanup to priority integration)"
      - "phases 7.1, 7.2, 7.3 (restructured from removal/deprecation to verification/delegation)"
      - "data_models.SdkQuestionHandler description (updated)"
      - "versions (v2.1, v2.2, v2.3, v2.4 added)"
      - "implementation_complexity (added)"
      - "Phase 9 added (documentation and copilot instructions updates)"

  key_improvements_made:
    - "CRITICAL: Identified that agent prompts are shared between VS Code and Hugh CLI — text-based signal formats must be kept permanently in prompts (D9). Without this fix, VS Code subagent orchestration would break after cleanup phases."
    - "CRITICAL: SignalClassifier regex patterns kept permanently as fallback in Hugh CLI code (D10). Since agents always emit both text and tool signals, dual detection provides resilience at minimal maintenance cost. Supersedes original D5 replace-and-remove strategy."
    - "CRITICAL: QuestionHandler refactored as thin delegation wrapper instead of deprecated (D11). With regex patterns kept (D10), text-detected question signals still flow through HandleSignalAsync. QuestionHandler now delegates to SdkQuestionHandler, ensuring single source of truth for question resolution."

  remaining_considerations:
    - "SDK technical preview risk (R1) — v0.1.22 may change. Version pinning and permanent text fallback provide strong mitigation."
    - "Microsoft.Extensions.AI compatibility (R3) — must verify in Phase 3.1 before proceeding with M2."
    - "ToolExecution event payloads (R4) — undocumented, requires runtime verification during M1."
    - "CopilotService.CreateSessionAsync complexity — consider SessionConfigBuilder extraction in future if method grows further."
    - "QuestionHandler delegation pattern (D11) — needs careful testing to ensure UserInputRequest conversion from text-parsed SignalClassification is accurate."

  signoff: "This plan has been reviewed and is **Ready for Implementation**."

referenced_standards: "No organizational standards applicable to this plan."

handoff:
  created_by: pch-planner
  created_date: "2026-02-10"
  reviewed_by: pch-plan-reviewer
  review_date: "2026-02-10"
  status: ready-for-implementation
  next_agent: pch-coder
  plan_location: "/docs/plans/019-hugh-sdk-tool-hooks-integration.md"
```
