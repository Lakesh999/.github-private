id: "016"
type: research
title: "Hugh Agent UX Failure Analysis — Installation Dead-Ends and Graceful Degradation"
status: complete
created: "2026-02-10"
current_phase: "3 of 3"
vision_source: null

introduction: |
  A real user interaction with the @hugh agent revealed critical UX failures.
  The user asked to "build a hello world website" — a simple request. The agent
  correctly identified itself as a CLI operator and attempted to run Hugh, but
  Hugh wasn't installed. The installation workflow then failed because all 6
  repository references in the prompt pointed to `pch-development/hugh` — a
  placeholder that was never updated. The actual repo is `{org}/.github-private`
  (verified: `PointCHealth/.github-private`, INTERNAL visibility, with releases
  v1.0.0–v1.0.3 containing both installer and portable assets). The interaction
  ended in a dead-end with no actionable path forward: the user couldn't build
  their website, couldn't install Hugh, and wasn't offered any viable alternatives.
  
  This research investigates the root causes of this failure, analyzes what
  graceful degradation strategies the agent should employ, and identifies
  prompt improvements to prevent dead-end user experiences.

objectives:
  - "Why did the installation workflow fail, and what assumptions does it make about repository availability?"
  - "What is the correct installation path when Hugh is not installed as a CLI tool?"
  - "What graceful degradation strategies should the agent employ when Hugh cannot be installed or run?"
  - "What changes to the prompt would prevent dead-end user experiences?"

phases:
  - phase: 1
    name: "Installation Workflow Failure Analysis"
    status: complete
    execution_mode: automatic
    completed: "2026-02-10"
    scope:
      - "Trace the exact failure path from the example interaction step by step"
      - "Identify all assumptions Workflow A makes about repository accessibility (pch-development/hugh)"
      - "Analyze what happens when GitHub Releases don't exist or are inaccessible"
      - "Review the fallback tiers (gh CLI → API → manual) and why they all failed"
      - "Identify the missing error handling: what should happen when ALL tiers fail?"
      - "Compare against research 015 (GitHub auth/installation improvements) — were those findings incorporated?"
    session: "2026-02-10"
    findings: |
      ### 1. Exact Failure Path Traced Step-by-Step

      When a user asks "build a hello world website":

      1. **Session Initialization** — Hugh agent classifies this as a "Feature Request" → should execute `hugh "build a hello world website"`
      2. **Detection Check** — Agent runs `Get-Command hugh -ErrorAction SilentlyContinue` → Hugh not found
      3. **Triggers Workflow A** — Agent initiates installation workflow
      4. **Step 2: Auth Detection** — Checks GH_TOKEN → GITHUB_TOKEN → gh auth status
      5. **Step 3: User Consent** — Asks installer vs portable (user may or may not reach this)
      6. **Step 4: Get Release Version — ALL TIERS FAIL:**
         - **Tier 1 (gh CLI):** `gh release view --repo pch-development/hugh` → fails (repo doesn't exist or no access)
         - **Tier 2 (API):** `Invoke-RestMethod -Uri "https://api.github.com/repos/pch-development/hugh/releases/latest"` → 404 (repo not found)
         - **Tier 3 (Manual):** Outputs: "Please download manually from: `https://github.com/pch-development/hugh/releases/latest`"
      7. **DEAD-END:** The manual URL also doesn't work. User cannot proceed.

      ### 2. All Repository Accessibility Assumptions in Workflow A

      There are **6 hardcoded references** to `pch-development/hugh` in the agent prompt, all assuming the repository exists. When it doesn't (or the user lacks access), every single pathway fails.

      | Location | Reference | Assumption |
      |----------|-----------|------------|
      | hugh.md ~L463 | `gh release view --repo pch-development/hugh` | Repo exists, gh can query it |
      | hugh.md ~L479 | `Invoke-RestMethod .../pch-development/hugh/releases/latest` | Repo exists with releases |
      | hugh.md ~L494 | Manual fallback URL | Repo is publicly viewable |
      | hugh.md ~L506 | `gh release download ... --repo pch-development/hugh` | Repo exists with downloadable assets |
      | hugh.md ~L554 | Download manual fallback URL | Release assets are downloadable |
      | hugh.md ~L657 | Total failure fallback URL | User can manually access releases |

      ### 3. Why GitHub Releases Don't Exist or Are Inaccessible

      **ROOT CAUSE VERIFIED:** The repo is `PointCHealth/.github-private` (INTERNAL visibility), NOT `pch-development/hugh`. Releases v1.0.0 through v1.0.3 exist with both `hugh-setup-*.exe` and `hugh-portable-*-win-x64.zip` assets. The installation workflow would have succeeded if it pointed to the correct repo.

      The failure was caused by a hardcoded placeholder URL (`pch-development/hugh`) that was never updated to use dynamic org detection. The actual fix is straightforward: detect the org from `git remote get-url origin` and construct `{org}/.github-private`.

      The URL is also wrong in `installer/hugh-setup.iss` line 13 — same placeholder.

      ### 4. Fallback Tier Analysis — Why They All Failed

      | Tier | Method | Failure Mode | Why Not Graceful |
      |------|--------|--------------|------------------|
      | **1** | gh release view/download | Error, non-zero exit | Just proceeds to Tier 2, no user message |
      | **2** | Invoke-RestMethod with token | 404 Not Found | Catch block says "Repository not found" then proceeds to Tier 3 |
      | **3** | "Download manually from URL" | **Dead-end** | Points user to URL that also doesn't work |

      The "Tier 3 manual fallback" is NOT a fallback — it's a dead-end that redirects the user to the same non-existent resource.

      ### 5. Missing Error Handling: What Should Happen When ALL Tiers Fail

      Currently: **Nothing.** User hits dead-end. What's missing:
      1. Dynamic org detection — The repo URL should be `{org}/.github-private`, detected from git remote
      2. Installation failure fallback — Agent should route to PCH agents when installation fails
      3. Redirect to original task — Agent should offer alternatives to accomplish user's goal without Hugh
      4. Clear "installation not available" message — Would set clear expectations
      5. PCH agent routing — Handoff to @pch-planner, @pch-visionary, etc. based on request type

      ### 6. Comparison with Research 015

      | Recommendation | Status in hugh.md |
      |----------------|-------------------|
      | Auth detection before API calls | ✅ Implemented |
      | Three-tier detection: GH_TOKEN → GITHUB_TOKEN → gh auth | ✅ Implemented |
      | Use `gh release download` as preferred method | ✅ Implemented |
      | Four-tier download fallback | ✅ Reduced to three tiers |
      | User consent markers | ✅ Implemented |
      | Error messages for 404/403 | ✅ Implemented |
      | What to do when ALL tiers fail | ❌ NOT ADDRESSED |
      | Alternative pathways when installation impossible | ❌ NOT IN SCOPE |

      Research 015 focused on making the tiered approach work better, but assumed the repository exists. It never addressed the catastrophic failure case where the repository itself is inaccessible.

      ### 7. The Core Architectural Problem

      The @hugh agent has a single dependency with no fallback: GitHub releases from `pch-development/hugh`. When that dependency fails, the entire agent becomes non-functional with no recovery strategy.

      **POST-RESEARCH CLARIFICATION:** The repo is always `.github-private` under the user's organization — `pch-development/hugh` was a placeholder never updated. The correct reference is `{org}/.github-private`. The org must be detected dynamically, e.g.: `$org = (git remote get-url origin) -replace '.*github\.com[:/]([^/]+)/.*', '$1'`.

      ### 8. Workspace Context (REVISED)

      **Original analysis assumed 3 workspace types.** Post-clarification, there is effectively ONE type:
      - **`.github-private` workspace** — Hugh may or may not be installed as a global CLI tool
      - The release download URL should be `{org}/.github-private` (detected dynamically), not hardcoded

      The user asking "build a hello world website" is ALWAYS in a `.github-private` repo. The agent should detect installed CLI first, then attempt installation from releases (with corrected dynamic org URL), then fall back to routing to PCH agents.
    key_discoveries:
      - "All 6 references to pch-development/hugh assume the repository exists — when it's not, ALL pathways fail"
      - "VERIFIED: Actual repo is PointCHealth/.github-private (INTERNAL visibility). Releases v1.0.0-v1.0.3 exist with installer+portable assets. Installation would work if URL were correct."
      - "The 'Tier 3 manual fallback' is not a fallback — it points users to the same non-existent resource"
      - "Research 015's tiered approach was fully implemented, but never addressed the case where the repository doesn't exist"
      - "When installation fails completely, the agent offers no alternative for the user's original task"
      - "Dynamic org detection needed: parse git remote origin URL to construct {org}/.github-private"
    relevant_files:
      - path: agents/hugh.md
        note: "Contains Workflow A with all 6 hardcoded references to pch-development/hugh and the three-tier fallback that ends in dead-end"
      - path: docs/research/015-hugh-agent-github-authentication-installation.md
        note: "Prior research that improved tiered approach but didn't address repository-doesn't-exist scenario"
      - path: docs/plans/017-hugh-agent-github-authentication-installation.md
        note: "Implementation plan based on research 015"
      - path: installer/hugh-setup.iss
        note: "Line 13 defines MyAppURL as pch-development/hugh — verified as wrong placeholder; actual repo is {org}/.github-private"
    external_sources: null
    gaps:
      - "RESOLVED: Repo is always .github-private under the user's org — pch-development/hugh confirmed as placeholder"
      - "Cannot test actual failure behavior without reproducing the exact user scenario"
    assumptions:
      - "CONFIRMED: pch-development/hugh was a placeholder that was never updated"
      - "The user is always in a .github-private workspace"

  - phase: 2
    name: "Graceful Degradation and Alternative Pathways"
    status: complete
    execution_mode: automatic
    completed: "2026-02-10"
    scope:
      - "When Hugh cannot be installed or run, what alternatives should the agent offer?"
      - "Should the agent recommend using PCH agents directly (@pch-visionary → @pch-researcher → etc.)?"
      - "How should the agent explain the alternatives without overwhelming the user?"
      - "Should there be a 'manual workflow' mode where the agent guides the user through the agent chain?"
      - "How do other CLI-wrapper tools handle 'tool not available' gracefully (e.g., Docker Desktop, Terraform)?"
      - "What is the minimum viable user experience when Hugh is completely unavailable?"
      - "Should the agent prioritize getting the user unblocked over getting Hugh installed?"
    session: "2026-02-10"
    findings: |
      ### 1. Current State: No Alternative Pathways Exist

      The agent has zero graceful degradation when installation fails. When ALL installation paths fail, the agent provides no alternative for accomplishing the user's original request.

      ### 2. PCH Agents Can Do What Hugh Does — Manually

      The @hugh agent already has a handoff defined to @pch-helper, and the PCH agent chain can accomplish the same workflow:
      - @pch-visionary → raw ideas
      - @pch-researcher → investigation
      - @pch-planner → implementation planning
      - @pch-plan-reviewer → plan validation
      - @pch-coder → implementation

      The existing "Or ask @pch-helper" mention should be PROMOTED from afterthought to primary fallback.

      ### 3. Direct Agent Routing — Yes, Absolutely

      | User Request Type | Hugh Alternative | Direct Agent Route |
      |-------------------|------------------|-------------------|
      | Feature request ("build X") | `hugh "build X"` | @pch-planner → @pch-plan-reviewer → @pch-coder |
      | Complex/unclear requirements | `hugh "complex feature"` | @pch-visionary → @pch-researcher → @pch-planner |
      | Simple task, clear scope | `hugh "simple task"` | @pch-planner → @pch-coder |
      | Research-first request | `hugh "investigate X"` | @pch-researcher |

      ### 4. How Other CLI Tools Handle "Tool Not Available"

      - **Docker:** Clear error explaining issue + actionable suggestion + never strands user
      - **Terraform:** Explains why failure occurred + suggests next steps + links docs
      - **npm:** Clear error with actual URL + implicit suggestion (try alternative)
      - **VS Code Remote SSH:** Shows error + Retry button + Open Settings + View Logs

      Key patterns: EXPLAIN briefly, OFFER immediate alternative, KEEP user's goal front and center, DON'T make users debug YOUR tool.

      ### 5. Proposed Graceful Degradation Ladder (3 Tiers)

      ```
      Tier 1: Installed CLI (BEST)
          → Hugh in PATH → Run `hugh "feature"`
      Tier 2: Install from Releases
          → Attempt Workflow A with dynamic org URL ({org}/.github-private)
          → If successful, run installed CLI
      Tier 3: Direct Agent Routing (when installation fails)
          → Route to appropriate PCH agent based on request type
          → Feature request → @pch-planner; Vague → @pch-visionary; Research → @pch-researcher
      ```

      Note: The only installation path is from the user's GitHub org's latest release.
      The agent should not attempt local builds — always install from releases or route to PCH agents.

      ### 6. Agent Should Prioritize Unblocking User Over Installing Hugh

      The user asked to "build a hello world website" — that's their GOAL. Hugh is just one PATH to that goal. Forcing users to fix Hugh installation before accomplishing their goal is poor UX.

      **User-Centric approach:** "I can't install Hugh right now, but I can still help you build your website using the PCH agents directly."

      **The Principle:** The user's goal > the tool's functionality.

      ### 7. Minimum Viable UX

      At minimum, when ALL fallbacks fail:
      1. Acknowledge the limitation clearly (not technical jargon)
      2. Explain what they CAN do (not what they can't)
      3. Provide a next action (not a dead-end)
      4. Never leave the user stranded

      ```
      **Hugh Unavailable**
      Hugh can't be installed or run in this environment.
      You can still accomplish your task by invoking the PCH agents directly:
      - @pch-planner → Creates implementation plans
      - @pch-coder → Implements plans
      For your request, try: @pch-planner build a hello world website
      ```

      ### 8. "Manual Workflow Mode" — Not a Mode, a Fallback

      A dedicated mode adds complexity. Instead, route to @pch-helper which already provides complete workflow guidance via the existing handoff mechanism:
      ```yaml
      handoffs:
        - label: Get Workflow Help
          agent: pch-helper
          prompt: Hugh is unavailable. Help user accomplish "{user_request}" using PCH agents directly.
      ```

      ### 9. Architectural Shift: Gatekeeper → Router

      @hugh is currently a "gatekeeper" to Hugh CLI — if the gate can't be opened, the user is stuck. It should be a "router" that prefers Hugh CLI but routes to PCH agents when Hugh is unavailable. The handoff to @pch-helper is the mechanism — it just isn't used for this purpose.
    key_discoveries:
      - "The agent has NO graceful degradation — all failures lead to dead-ends with no alternative"
      - "PCH agents can do exactly what Hugh automates, just with manual orchestration"
      - "The handoff to @pch-helper already exists but is never used as a fallback for installation failure"
      - "Other CLI tools follow: explain briefly → offer alternative → keep user's goal front and center"
      - "Agent should prioritize UNBLOCKING THE USER over installing Hugh"
      - "3-tier graceful degradation: installed CLI → install from releases → direct agent routing"
      - "Core architectural shift needed: @hugh should be a 'router' not a 'gatekeeper'"
    relevant_files:
      - path: agents/hugh.md
        note: "Contains redirect templates and handoff to pch-helper — needs amendment for installation failure"
      - path: agents/pch-helper.md
        note: "Already provides complete workflow guidance — ideal fallback target"
      - path: agents/pch-planner.md
        note: "Entry point for most user requests when requirements are clear"
      - path: agents/pch-visionary.md
        note: "Entry point for raw ideas with unclear requirements"
    external_sources:
      - url: "general knowledge"
        note: "Docker, Terraform, npm, VS Code Remote error handling patterns"
    gaps:
      - "RESOLVED: PCH agents are always available — they are defined in agents/ within .github-private, the same workspace"
      - "No analysis of what happens if BOTH Hugh AND PCH agents are unavailable (unlikely since agents/ is always present)"
    assumptions:
      - "CONFIRMED: PCH agents are always available as Copilot agents in .github-private workspace"
      - "CONFIRMED: Users invoking @hugh are in .github-private and can invoke other @ agents"

  - phase: 3
    name: "Prompt Improvement Recommendations"
    status: complete
    execution_mode: automatic
    completed: "2026-02-10"
    scope:
      - "Synthesize findings from Phases 1-2 into specific prompt changes"
      - "Draft updated Session Initialization decision tree with new branches for failure scenarios"
      - "Design a 'workspace detection' step that checks for installed CLI vs not installed"
      - "Draft fallback workflow templates for when installation fails"
      - "Draft graceful degradation messaging templates"
      - "Identify which existing Workflow sections (A-G) need modification vs new workflows needed"
      - "Ensure recommendations don't violate the 'CLI operator only' hard constraint"
    session: "2026-02-10"
    findings: |
      ## Executive Summary

      Transform @hugh from a "gatekeeper" (fails if CLI unavailable) to a "router" (prefers CLI, falls back to PCH agents). Requires 3 new structural additions and 5 modifications to existing sections.

      ## 1. New Sections to Add

      ### 1.1 Workspace Environment Detection (Insert before Session Initialization)

      Detection priority order runs ONCE per session, cached:
      1. Installed CLI Check: `Get-Command hugh` → ENV_TYPE = "installed"
      2. No CLI → ENV_TYPE = "not-installed"

      For release downloads (Workflow A), detect org dynamically:
      ```powershell
      $org = (git remote get-url origin) -replace '.*github\.com[:/]([^/]+)/.*', '$1'
      $repoSlug = "$org/.github-private"
      ```

      Environment type actions:
      | ENV_TYPE | Primary Path | Fallback |
      |----------|--------------|----------|
      | installed | Run `hugh` directly | — |
      | not-installed | Attempt Workflow A installation (with dynamic org URL) | Route to PCH agents on failure (Workflow I) |

      ### 1.2 Graceful Degradation Framework (Insert after Session Initialization)

      3-tier degradation ladder:
      - Tier 1: Installed CLI (best) — run `hugh` directly
      - Tier 2: Install from releases — Workflow A with dynamic org URL `{org}/.github-private`
      - Tier 3: Direct agent routing — route to appropriate PCH agent (Workflow I)

      Principle: The user's goal is more important than the tool's installation.

      ### 1.3 Workflow I — Direct Agent Fallback

      When Hugh cannot be installed:
      1. Acknowledge limitation briefly (not technical jargon)
      2. Route based on request type:
         - Feature request → @pch-planner
         - Vague/complex → @pch-visionary
         - Investigation → @pch-researcher
         - Needs guidance → @pch-helper
      3. Provide quick reference of all PCH agents

      ## 2. Existing Sections to Modify

      ### 2.1 Session Initialization — Expand Decision Tree
      Add Step 0: Workspace Environment Detection (ALWAYS FIRST)
      Expand routing to include ENV_TYPE checks before workflow selection.
      Feature requests route to: installed → Workflow B, not-installed → Workflow A with dynamic org URL (then Workflow I on failure).

      ### 2.2 Workflow A — Add Dynamic Org Detection + Installation Failure Handling
      Replace ALL 6 hardcoded `pch-development/hugh` references with dynamic `$org/.github-private` via git remote parsing.
      Replace dead-end Tier 3 with enhanced messaging explaining WHY failure occurred.
      Add new Tier 4: Route to PCH agents (→ Workflow I).
      Never output generic 'visit GitHub releases' without also offering PCH agent alternative.

      ### 2.3 Workflow A Step 8 — Replace Total Failure Fallback
      Replace dead-end "visit releases URL" with: route to agents (Workflow I).
      Never leave user stranded on original task.

      ### 2.4 Redirect Templates — Add Installation-Failed Variant
      New template: "Hugh couldn't be installed right now, but I can still help... recommend @pch-planner directly."

      ## 3. Messaging Templates (3 templates designed)

      1. **Installation Failure — Feature Request:** Route to @pch-planner
      2. **Installation Failure — Complex Request:** Route to @pch-visionary
      3. **Installation Failure — Minimum Viable:** List available agents

      ## 4. Implementation Priority

      1. **Critical:** Dynamic org detection in Workflow A + Workflow I (agent fallback)
      2. **High:** Session Initialization tree + Degradation Framework
      3. **Medium:** Messaging templates + redirect template

      ## 5. Constraint Compliance

      All recommendations comply with CLI operator constraint. No changes to hard constraints needed. All changes are dynamic URL fixes and routing/handoff behaviors — no development work.

      ## 6. Section-by-Line Mapping

      12 specific locations in hugh.md require modification, ~150-200 lines of new content total.
    key_discoveries:
      - "Prompt requires 3 new structural additions: Workspace Detection, Graceful Degradation Framework, Workflow I"
      - "No hard constraint changes needed"
      - "VERIFIED: Workflow A must replace all 6 hardcoded pch-development/hugh with dynamic org detection from git remote"
      - "Workspace detection simplifies to binary: CLI installed? → run it; not installed? → Workflow A → Workflow I on failure"
      - "All messaging follows: user's goal > tool's installation"
      - "Existing @pch-helper handoff should be leveraged for agent routing"
      - "~10 specific file locations need modification, ~150-200 lines of new content"
    relevant_files:
      - path: agents/hugh.md
        note: "Primary file requiring all modifications — ~914 lines, 12 sections need updates"
      - path: agents/pch-helper.md
        note: "Target for Tier 4/Workflow I handoffs"
      - path: agents/pch-planner.md
        note: "Primary routing target for feature requests when Hugh unavailable"
      - path: agents/pch-visionary.md
        note: "Routing target for vague/complex requirements"
    external_sources: null
    gaps:
      - "Cannot verify installation success with corrected dynamic org URL until prompt is updated"
    assumptions:
      - "CONFIRMED: PCH agents are always available in .github-private workspace where @hugh is invoked"
      - "GitHub releases from {org}/.github-private are the sole installation path"
      - "CONFIRMED: installer/hugh-setup.iss MyAppURL also needs updating to use correct repo reference"

overview: |
  The @hugh agent suffers from a critical architectural flaw: it operates as a
  **gatekeeper** to the Hugh CLI with zero graceful degradation. When Hugh isn't
  installed and the `pch-development/hugh` GitHub repository is inaccessible,
  all 6 hardcoded references fail, all 3 installation tiers cascade into a
  dead-end, and the user's original task goes unaccomplished with no alternative.
  
  **Post-research verification (confirmed via GitHub API):** The repo is
  `PointCHealth/.github-private` (visibility: INTERNAL). `pch-development/hugh`
  was a placeholder that was never updated. Releases DO exist (v1.0.0 through
  v1.0.3) with both `hugh-setup-*.exe` and `hugh-portable-*-win-x64.zip`
  assets — the installation workflow would have worked if it pointed to the
  right repo. The correct reference is `{org}/.github-private`, where the org
  must be detected dynamically (e.g., `git remote get-url origin`).
  
  The solution requires an architectural shift from "gatekeeper" to "router":
  the agent should prefer the installed CLI, attempt installation from releases
  (with the corrected dynamic org URL) when not installed, and gracefully fall
  back to routing the user to the appropriate PCH agent when installation fails.
  The 3-tier degradation ladder is: installed CLI → install from releases →
  direct PCH agent routing.
  
  Implementation requires 3 new sections (Workspace Environment Detection,
  Graceful Degradation Framework, Workflow I), amendments to 3 existing
  sections, 3 new messaging templates, and replacement of all 6 hardcoded
  `pch-development/hugh` references with dynamic org-based repo detection.
  Total new content: approximately 150-200 lines.
key_findings:
  - "The @hugh agent has a single point of failure with no recovery: 6 hardcoded references to pch-development/hugh — a placeholder never updated to the actual repo ({org}/.github-private)"
  - "VERIFIED: Actual repo is PointCHealth/.github-private (INTERNAL, releases v1.0.0–v1.0.3 with installer+portable assets). Installation would work if URL were correct. All references must use dynamic org detection via git remote."
  - "The 'Tier 3 manual fallback' is a dead-end, not a fallback — it redirects users to the same non-existent resource"
  - "The core architectural problem is gatekeeper vs router: @hugh should route to PCH agents when Hugh CLI cannot be installed"
  - "Other CLI tools universally follow: explain briefly → offer immediate alternative → keep user's goal front and center"
  - "The existing handoff to @pch-helper is never used as a fallback — it's the mechanism for graceful degradation but isn't wired up"
  - "Implementation requires 3 new sections, 3 section modifications, 3 messaging templates, and dynamic org detection (~150-200 lines of new content)"

actionable_conclusions:
  - "Replace all 6 hardcoded pch-development/hugh references with dynamic org detection: parse git remote origin URL to get {org}/.github-private (verified: PointCHealth/.github-private has releases v1.0.0-v1.0.3)"
  - "Add Workspace Environment Detection as Step 0: CLI installed? → use CLI; not installed? → attempt Workflow A installation with dynamic org URL; installation fails? → Workflow I (route to PCH agents)"
  - "Add Workflow I (Direct Agent Fallback) that routes to appropriate PCH agent based on request type"
  - "Replace Workflow A's dead-end Tier 3 with enhanced messaging + PCH agent routing fallback"
  - "Expand Session Initialization decision tree to include ENV_TYPE routing"
  - "Update installer/hugh-setup.iss MyAppURL to use correct repo reference"
  - "Always offer an alternative that addresses the user's original goal — never leave them stranded"

open_questions:
  - "RESOLVED & VERIFIED: Repo is PointCHealth/.github-private (INTERNAL, releases v1.0.0-v1.0.3 with installer+portable assets). pch-development/hugh was a placeholder. The org is detected via git remote get-url origin."
  - "RESOLVED: Yes — PCH agents are defined in agents/ within .github-private, the same workspace where @hugh is invoked. They are always available."
  - "Should the agent cache installation failure state to avoid re-trying every request in a session?"

standards: null

handoff:
  created_by: pch-researcher
  created_date: "2026-02-10"
  status: complete
  current_phase: complete
  path: /docs/research/016-hugh-agent-ux-failure-analysis.md
