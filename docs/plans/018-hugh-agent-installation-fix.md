```yaml
id: "018"
type: implementation-plan
title: "Hugh Agent Installation Fix — Dynamic Repo Discovery via GitHub CLI"
status: complete
created: "2026-02-10"
updated: "2026-02-10"
owner: pch-planner
version: "v3.1"
versions:
  - version: "v1.0"
    date: "2026-02-10"
    author: pch-planner
    changes: "Initial plan creation with document skeleton"
  - version: "v1.1"
    date: "2026-02-10"
    author: pch-planner
    changes: "Added scope, org detection, agent routing, and installer URL decisions"
  - version: "v1.2"
    date: "2026-02-10"
    author: pch-planner
    changes: "Completed technical design, requirements, dependencies, risks, and execution phases"
  - version: "v2.0"
    date: "2026-02-10"
    author: pch-planner
    changes: "Holistic review completed — no conflicts found, all decisions validated"
  - version: "v3.0"
    date: "2026-02-10"
    author: pch-planner
    changes: "Major revision: switched from git-remote to gh-CLI-based org discovery; removed graceful degradation / Workflow I — installation failure is now a clear stop with no confusing fallbacks"
  - version: "v3.1"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Review completed — corrected reference count (6→7), added implementer note re: Step 0/Step 2 relationship, added review artifacts. Marked ready for implementation."

introduction: |
  The @hugh agent's installation workflow (Workflow A) hardcodes 7 references to
  `pch-development/hugh` — a placeholder repository that was never updated. The
  actual repository is `{org}/.github-private` (verified: PointCHealth/.github-private,
  INTERNAL visibility, releases v1.0.0–v1.0.3). When Hugh is not installed, all
  installation tiers fail because they target a nonexistent repo, leaving users
  stranded with no path forward.
  
  The user may NOT have the repo cloned locally — the @hugh agent can be invoked
  from any workspace. Therefore, repo discovery must use the GitHub CLI (`gh`) to
  query the user's org memberships, find the org whose `.github-private` repo
  contains Hugh release assets, and install from there.
  
  If installation fails (gh not authenticated, no org found, no releases, download
  error), the agent must fail clearly with an actionable error message — no
  confusing fallbacks that make it seem like things are working after a critical
  failure. Hugh is the agent's primary tool; if it can't be installed, the agent
  should say so plainly and stop.

decisions:
  - session: 1
    date: "2026-02-10"
    decision_point: "What scope should this plan cover?"
    answer: "A — Full Research Implementation (REVISED in session 2)"
    rationale: "Originally selected full scope including graceful degradation / Workflow I. Revised in session 2 to remove fallback behaviors — see Decision 5."
  - session: 1
    date: "2026-02-10"
    decision_point: "How should the agent detect the GitHub organization for constructing the repo slug?"
    answer: "C — git remote parsing with user prompt fallback (SUPERSEDED in session 2)"
    rationale: "Originally selected git remote parsing. Superseded in session 2 — see Decision 6."
  - session: 1
    date: "2026-02-10"
    decision_point: "How should Workflow I determine which PCH agent to route to?"
    answer: "B — Direct routing based on request classification (REMOVED in session 2)"
    rationale: "Workflow I removed entirely. Installation failure is now a clear stop — see Decision 5."
  - session: 1
    date: "2026-02-10"
    decision_point: "How should installer/hugh-setup.iss MyAppURL be updated?"
    answer: "A — Use the org's actual URL"
    rationale: "Installer metadata should show the correct repo URL. Org name changes are extremely rare, and parameterizing via /D adds unnecessary build complexity."
  - session: 2
    date: "2026-02-10"
    decision_point: "Should there be a fallback (Workflow I / PCH agent routing) when installation fails?"
    answer: "No — fail clearly with no fallback"
    rationale: "User feedback: offering alternative agents after failing to install Hugh's primary component would be confusing. A clear failure message with actionable diagnostics is better UX than a degraded mode that obscures the real problem."
  - session: 2
    date: "2026-02-10"
    decision_point: "How should org detection work given the user may not have the repo cloned locally?"
    answer: "Use gh CLI to discover orgs and find .github-private with Hugh releases"
    rationale: "The user may invoke @hugh from any workspace — git remote is not reliable. The gh CLI can query user/orgs and then check each org's .github-private for releases with Hugh assets. This works regardless of workspace context. Verified: gh api user/orgs + repos/{org}/.github-private/releases/latest successfully discovers PointCHealth/.github-private with Hugh assets."

holistic_review:
  decision_interactions:
    - interaction: "gh CLI org discovery (Decision 6) + no fallback (Decision 5)"
      assessment: "These reinforce each other well. Since there is no fallback, the gh CLI discovery must be robust and provide clear error messages at each stage (not authenticated, no orgs, no .github-private, no releases, no Hugh assets). Each failure point must have a specific, actionable error message."
    - interaction: "gh CLI org discovery (Decision 6) + installer URL fix (Decision 4)"
      assessment: "Independent. The installer fix is compile-time metadata; org discovery is runtime prompt behavior."
    - interaction: "No fallback (Decision 5) + existing auth detection (Workflow A Step 2)"
      assessment: "Auth detection becomes MORE critical without fallbacks — if gh isn't authenticated, the agent must say so clearly and guide the user to `gh auth login` before retrying. The existing 3-tier auth detection (GH_TOKEN → GITHUB_TOKEN → gh auth) remains, but failure messaging is tightened."
  
  architectural_considerations:
    - concern: "gh CLI must be installed for org discovery"
      assessment: "Workflow A Step 2 already checks for gh auth. If gh itself isn't installed, the existing prerequisite detection (Workflow E / hugh doctor) handles this. The org discovery step should verify gh is available before attempting API calls."
    - concern: "Multiple org memberships — which org has .github-private?"
      assessment: "The discovery iterates all orgs and checks each for .github-private with Hugh release assets. If multiple orgs match (unlikely but possible), use the first match. If none match, provide a clear error listing the orgs that were checked."
    - concern: "API rate limiting on org/repo discovery"
      assessment: "Low risk — discovery only runs once per session when Hugh is not installed. Typical user belongs to 1-3 orgs, so 1-4 API calls total."
  
  trade_offs:
    - trade_off: "Hardcoding PointCHealth in installer/hugh-setup.iss vs dynamic detection"
      accepted_by: "User — Decision 4"
      rationale: "Installer is compiled at release time; runtime detection isn't possible. Org changes are extremely rare."
    - trade_off: "No fallback to PCH agents when installation fails"
      accepted_by: "User — Decision 5"
      rationale: "A clear failure is better UX than a degraded mode that obscures the real problem. Users can always invoke PCH agents manually if they know them."
  
  gaps_identified: "None — scope is focused and well-defined"
  
  risks_acknowledged:
    - "gh CLI not installed — mitigated by existing prerequisite detection"
    - "User belongs to no orgs with .github-private — clear error listing checked orgs"
    - "API calls may be slow for users in many orgs — mitigated by early termination on first match"

overview:
  feature: "Fix Hugh agent installation workflow with gh CLI-based repo discovery"
  objectives:
    - "Replace all hardcoded pch-development/hugh references with dynamically discovered repo slug"
    - "Use gh CLI to discover the user's GitHub orgs and find .github-private with Hugh releases"
    - "Provide clear, actionable error messages at each failure point — no confusing fallbacks"
    - "Fix installer/hugh-setup.iss MyAppURL placeholder"
    - "Ensure installation works regardless of which workspace the user is in"
  user_story: |
    As a developer using the @hugh agent (from any workspace), when Hugh CLI
    is not installed, the agent should use the GitHub CLI to discover my org's
    .github-private repository, find the latest Hugh release, and install it.
    If any step fails (gh not authenticated, no matching org, no releases),
    the agent should tell me exactly what went wrong and how to fix it — not
    offer confusing workarounds.
  research_source: "/docs/research/016-hugh-agent-ux-failure-analysis.md"

requirements:
  functional:
    - id: F1
      title: "Discover repo via gh CLI org enumeration"
      description: |
        Use `gh api user/orgs` to list the user's GitHub org memberships,
        then for each org check `repos/{org}/.github-private/releases/latest`
        for Hugh release assets (hugh-setup-*.exe or hugh-portable-*-win-x64.zip).
        Use the first matching org to construct $repoSlug = "{org}/.github-private".
    - id: F2
      title: "Replace all hardcoded pch-development/hugh references"
      description: "Replace 7 occurrences in agents/hugh.md (6 via $repoSlug substitution in Steps 4-5 per Task 1.2, 1 via block replacement in Step 8 per Task 1.3) and 1 in installer/hugh-setup.iss with the correct repo reference"
    - id: F3
      title: "Repo Discovery step in Workflow A"
      description: |
        Add a new step in Workflow A (before getting the release version) that
        runs the gh CLI org discovery. This only runs when Hugh is not installed.
        Results are cached for the session.
    - id: F4
      title: "Clear failure messages at each discovery/install stage"
      description: |
        Each failure point must have a specific error message and suggested fix:
        - gh not installed → "Install GitHub CLI: winget install GitHub.cli"
        - gh not authenticated → "Run: gh auth login"
        - No orgs found → "Your GitHub account has no org memberships. Hugh is distributed via an org's .github-private repo."
        - No .github-private with Hugh releases → "Checked orgs: [list]. None have Hugh releases in .github-private."
        - Download/install failure → specific error from the download/install step
    - id: F5
      title: "No fallback after installation failure"
      description: |
        When installation fails at any stage, the agent must stop with
        a clear error message. It must NOT offer PCH agent routing, degraded
        modes, or workarounds that obscure the failure. The error should
        explain what failed and what the user can do to fix it.
    - id: F6
      title: "Expanded Session Initialization for detection flow"
      description: |
        The Session Initialization decision tree must route feature requests
        through detection → installation (if needed) → execution. If installation
        fails, stop with error — do not route elsewhere.
  non_functional:
    - id: NF1
      title: "No source code changes"
      description: "All changes are to prompt files (agents/hugh.md) and installer metadata (installer/hugh-setup.iss) only"
    - id: NF2
      title: "Backward compatible"
      description: "When Hugh IS installed, behavior is identical to current — no regressions for the happy path"
    - id: NF3
      title: "CLI operator constraint preserved"
      description: "All changes comply with the HARD CONSTRAINT: CLI Operator Only section — no development work by @hugh"
    - id: NF4
      title: "Works from any workspace"
      description: "Repo discovery does not depend on the user being in a .github-private clone — uses gh CLI API calls, not git remote"
  out_of_scope:
    - "Graceful degradation / fallback to PCH agents when installation fails (explicitly excluded — Decision 5)"
    - "Workflow I (Direct Agent Fallback) — removed"
    - "Installation failure messaging templates that route to other agents — removed"
    - "Local build-from-source as an installation fallback"
    - "Changes to Hugh application source code (Hugh/ directory)"
    - "Changes to other PCH agent prompts (pch-*.md files)"
    - "Installation path changes for non-Windows platforms"

technical_design:
  architecture:
    summary: |
      Fix the @hugh agent's installation workflow so it can discover and install
      Hugh from the correct GitHub repository regardless of which workspace the
      user is in. Uses the GitHub CLI to enumerate the user's org memberships and
      find the org whose .github-private repo contains Hugh release assets.
      
      If any step fails, the agent stops with a clear error message explaining
      what went wrong and how to fix it. No fallback routing to other agents.
    
    repo_discovery_algorithm:
      description: |
        The agent discovers the correct repo slug at runtime using the gh CLI.
        This runs ONCE per session, only when Hugh CLI is not already installed.
      steps:
        - step: 1
          action: "Verify gh CLI is available"
          command: "Get-Command gh -ErrorAction SilentlyContinue"
          on_failure: "STOP — 'GitHub CLI (gh) is not installed. Install it with: winget install GitHub.cli'"
        - step: 2
          action: "Verify gh is authenticated"
          command: "gh auth status"
          on_failure: "STOP — 'GitHub CLI is not authenticated. Run: gh auth login'"
        - step: 3
          action: "List user's org memberships"
          command: "gh api user/orgs --jq '.[].login'"
          on_failure: "STOP — 'Could not retrieve your GitHub organizations. Check your gh authentication.'"
        - step: 4
          action: "For each org, check for .github-private repo with Hugh release assets"
          command: |
            foreach ($org in $orgs) {
                $assets = gh api "repos/$org/.github-private/releases/latest" --jq '.assets[].name' 2>&1
                if ($LASTEXITCODE -eq 0 -and ($assets | Where-Object { $_ -like 'hugh-setup-*' -or $_ -like 'hugh-portable-*' })) {
                    $repoSlug = "$org/.github-private"
                    break
                }
            }
          on_failure: "STOP — 'Checked organizations: [list]. None have Hugh releases in their .github-private repository.'"
        - step: 5
          action: "Cache $repoSlug for the session"
          result: "$repoSlug used by all subsequent Workflow A steps"
      verified: "Tested with gh api user/orgs → PointCHealth → repos/PointCHealth/.github-private/releases/latest → found Hugh assets (v1.0.3)"

  codebase_patterns:
    - pattern: "Tiered auth detection (GH_TOKEN → GITHUB_TOKEN → gh auth)"
      location: "agents/hugh.md, Workflow A Step 2"
      usage: "Repo discovery reuses the same auth — gh CLI must be authenticated"
    - pattern: "Session Initialization decision tree"
      location: "agents/hugh.md, Session Initialization section"
      usage: "Expanded to include detection → installation → fail-stop flow for feature requests"
    - pattern: "Workflow naming convention (A-G)"
      location: "agents/hugh.md, Workflow sections"
      usage: "No new workflows added — changes are within existing Workflow A"
    - pattern: "Error messaging patterns"
      location: "agents/hugh.md, Error Reference & Troubleshooting section"
      usage: "New discovery errors follow same format: cause + specific fix command"

  component_specifications:
    files_modified:
      - file: "agents/hugh.md"
        changes:
          - section: "Workflow A — before Step 1"
            action: "INSERT new preamble: Repo Discovery"
            description: |
              Add a repo discovery step that runs the gh CLI org enumeration algorithm
              described above. This replaces the previous Workspace Environment Detection
              section concept. It executes within Workflow A (not as a separate section)
              because it only matters when installation is needed.
            estimated_lines: "~35 lines"
          - section: "Session Initialization decision tree"
            action: "MODIFY Feature Request branch"
            description: |
              Update Feature Request branch to show:
              Feature Request → Detection Check → Installed → Workflow B;
              Not Installed → Workflow A → success → Workflow B; failure → STOP with error.
              Remove any references to Workflow I or agent routing fallbacks.
            estimated_lines: "~10 lines modified"
          - section: "Workflow A Step 4 (Get Latest Release Version)"
            action: "MODIFY all tiers"
            description: "Replace all pch-development/hugh with $repoSlug (discovered in preamble)"
            estimated_lines: "~6 lines modified"
          - section: "Workflow A Step 5 (Download Asset)"
            action: "MODIFY all tiers"
            description: "Replace all pch-development/hugh with $repoSlug"
            estimated_lines: "~6 lines modified"
          - section: "Workflow A Step 8 (Post-Installation / Total Failure)"
            action: "REPLACE total failure fallback"
            description: |
              Replace the dead-end manual URL fallback with a clear error message:
              "Installation failed. [Specific reason]. To resolve:
              1. Ensure gh CLI is installed and authenticated (gh auth login)
              2. Verify your org's .github-private repo has Hugh releases
              3. Try again after resolving the above"
              NO routing to other agents. NO fallback workflows.
            estimated_lines: "~10 lines modified"
        total_estimated_change: "~70 lines new/modified (significantly smaller than v2.0's ~170 lines)"
      - file: "installer/hugh-setup.iss"
        changes:
          - section: "Application Metadata (line 13)"
            action: "MODIFY MyAppURL value"
            description: "Change from pch-development/hugh to PointCHealth/.github-private"
            estimated_lines: "1 line modified"

dependencies:
  - id: D1
    description: "Research 016 (Hugh Agent UX Failure Analysis) — complete, all 3 phases"
    type: "research"
    status: "complete"
  - id: D2
    description: "agents/hugh.md exists with current Workflow A structure (verified: 914 lines)"
    type: "prerequisite"
    status: "complete"
  - id: D3
    description: "installer/hugh-setup.iss exists with MyAppURL on line 13 (verified: 259 lines)"
    type: "prerequisite"
    status: "complete"
  - id: D4
    description: "GitHub CLI (gh) installed and authenticated — required for org discovery at runtime"
    type: "runtime-prerequisite"
    status: "complete"
    note: "Already a prerequisite for Workflow A Step 2 (auth detection). Existing Workflow E handles gh installation."

risks:
  - id: R1
    risk: "gh CLI not installed on user's machine"
    likelihood: low
    impact: high
    mitigation: "Clear error message with install command: winget install GitHub.cli. Existing Workflow E also handles this."
  - id: R2
    risk: "gh not authenticated — org enumeration fails"
    likelihood: medium
    impact: high
    mitigation: "Clear error message: 'Run gh auth login'. Existing Workflow A Step 2 already checks auth."
  - id: R3
    risk: "User belongs to many orgs — discovery loop slow or hits rate limits"
    likelihood: low
    impact: low
    mitigation: "Early termination on first match. Typical users belong to 1-3 orgs."
  - id: R4
    risk: "No org has .github-private with Hugh releases"
    likelihood: low
    impact: high
    mitigation: "Clear error listing all checked orgs. This is a legitimate configuration issue that should surface clearly."
  - id: R5
    risk: "Hardcoding PointCHealth in installer/hugh-setup.iss — same stale-value problem if org changes"
    likelihood: very-low
    impact: low
    mitigation: "Org changes are extremely rare; installer metadata is cosmetic (Add/Remove Programs display)"

phases:
  - phase: 1
    name: "gh CLI Repo Discovery + Hardcoded Reference Replacement + Clear Failure Messaging"
    status: complete
    size: medium
    files_to_modify: 2
    prerequisites:
      - "agents/hugh.md exists with current Workflow A structure (914 lines)"
      - "installer/hugh-setup.iss exists with MyAppURL on line 13 (259 lines)"
    entry_point: "agents/hugh.md"
    verification: "N/A — single phase"
    tasks:
      - step: "1.1"
        task: |
          Add a Repo Discovery preamble to Workflow A in agents/hugh.md.
          Insert BEFORE "#### Step 1: Check Current Installation" (around line 400),
          but AFTER the "### Workflow A — Install Hugh" heading.
          
          Create a new sub-section titled "#### Step 0: Discover Repository" with:
          
          1. A note: "This step discovers the correct GitHub repository for Hugh releases.
             It runs ONCE per session; the result ($repoSlug) is cached and used by all
             subsequent steps."
          
          2. The discovery algorithm as a PowerShell code block:
             ```powershell
             # Step 0a: Verify gh CLI is available
             $ghCmd = Get-Command gh -ErrorAction SilentlyContinue
             if (-not $ghCmd) {
                 # STOP — "GitHub CLI (gh) is not installed. Install it with: winget install GitHub.cli"
             }
             
             # Step 0b: Verify gh is authenticated
             $ghAuth = gh auth status 2>&1
             if ($LASTEXITCODE -ne 0) {
                 # STOP — "GitHub CLI is not authenticated. Run: gh auth login"
             }
             
             # Step 0c: Enumerate user's orgs and find .github-private with Hugh releases
             $orgs = gh api user/orgs --jq '.[].login' 2>&1
             if ($LASTEXITCODE -ne 0 -or -not $orgs) {
                 # STOP — "Could not retrieve your GitHub organizations. Verify gh authentication with: gh auth status"
             }
             
             $repoSlug = $null
             foreach ($org in $orgs) {
                 $assets = gh api "repos/$org/.github-private/releases/latest" --jq '.assets[].name' 2>&1
                 if ($LASTEXITCODE -eq 0 -and ($assets | Where-Object { $_ -like 'hugh-setup-*' -or $_ -like 'hugh-portable-*' })) {
                     $repoSlug = "$org/.github-private"
                     break
                 }
             }
             
             if (-not $repoSlug) {
                 # STOP — "Could not find Hugh releases. Checked organizations: $($orgs -join ', '). 
                 # None have a .github-private repository with Hugh release assets."
             }
             ```
          
          3. A note: "If Step 0 fails at any point, STOP the entire Workflow A. Display the
             specific error message and do not proceed to Step 1. Do not offer alternative
             agents or workarounds — the user needs to resolve the specific issue before
             Hugh can be installed."
          
          Approximately 35 lines.
          
          **Implementer note:** Existing Step 2 (Detect GitHub Authentication) remains
          unchanged — it determines the download tier strategy (GH_TOKEN vs GITHUB_TOKEN
          vs gh-cli), which is separate from Step 0's repo discovery auth check. Do not
          remove or modify Step 2.
        files:
          - "agents/hugh.md — insert within Workflow A, before Step 1 (~line 400)"
        acceptance_criteria:
          - "New #### Step 0: Discover Repository exists before Step 1 in Workflow A"
          - "Contains complete PowerShell discovery algorithm with 4 stage checks"
          - "Each failure point has a specific, actionable error message"
          - "All failures are STOP conditions — no fallback, no alternative routing"
          - "Documents $repoSlug caching for the session"
          - "Uses gh api user/orgs (not git remote) for org discovery"

      - step: "1.2"
        task: |
          Replace all 6 hardcoded `pch-development/hugh` references in agents/hugh.md
          Workflow A with $repoSlug (the variable set by Step 0).
          
          Each reference must be replaced:
          
          1. Workflow A Step 4, Tier 1 (~line 463):
             `gh release view --repo pch-development/hugh` → `gh release view --repo $repoSlug`
          
          2. Workflow A Step 4, Tier 2 (~line 479):
             `https://api.github.com/repos/pch-development/hugh/releases/latest`
             → `https://api.github.com/repos/$repoSlug/releases/latest`
          
          3. Workflow A Step 4, Tier 3 (~line 494):
             `https://github.com/pch-development/hugh/releases/latest`
             → `https://github.com/$repoSlug/releases/latest`
          
          4. Workflow A Step 5, Tier 1 — first occurrence (~line 508):
             `gh release download $version --repo pch-development/hugh --pattern "hugh-setup-*.exe"`
             → `gh release download $version --repo $repoSlug --pattern "hugh-setup-*.exe"`
          
          5. Workflow A Step 5, Tier 1 — second occurrence (~line 512):
             `gh release download $version --repo pch-development/hugh --pattern "hugh-portable-*-win-x64.zip"`
             → `gh release download $version --repo $repoSlug --pattern "hugh-portable-*-win-x64.zip"`
          
          6. Workflow A Step 5, Tier 3 (~line 554):
             `https://github.com/pch-development/hugh/releases/download/`
             → `https://github.com/$repoSlug/releases/download/`
        files:
          - "agents/hugh.md — Workflow A Steps 4-5 (~lines 460-560)"
        acceptance_criteria:
          - "Zero occurrences of pch-development/hugh remain in agents/hugh.md"
          - "All 6 references now use $repoSlug"
          - "PowerShell code blocks remain syntactically valid"
          - "Variable interpolation is correct in each context (command args vs URL strings)"

      - step: "1.3"
        task: |
          Replace Workflow A Step 8 total failure fallback in agents/hugh.md.
          
          Current fallback (~line 650-665) sends users to a manual download URL.
          Replace the entire "Fallback for total failure" block with a clear error:
          
          ```
          **If all installation attempts fail:**
          > "Hugh installation failed. This could be due to:
          > - GitHub CLI not authenticated → Run `gh auth login`
          > - Network connectivity issues → Check your internet connection
          > - Repository access denied → Verify your org membership includes access to .github-private
          > - No Hugh releases available → Contact your organization admin
          >
          > After resolving the issue, try again by asking me to install Hugh."
          ```
          
          Key requirements:
          - NO URL pointing to pch-development/hugh or any hardcoded repo
          - NO routing to PCH agents or Workflow I
          - Specific, actionable troubleshooting steps
          - Invitation to retry after fixing the issue
        files:
          - "agents/hugh.md — Workflow A Step 8 total failure (~lines 650-665)"
        acceptance_criteria:
          - "Total failure fallback has zero references to pch-development/hugh"
          - "No routing to other agents or workflows"
          - "Contains specific troubleshooting checklist"
          - "Invites user to retry after resolving the issue"

      - step: "1.4"
        task: |
          Modify the Session Initialization decision tree in agents/hugh.md.
          
          Update the "Feature Request?" branch (around line 195) to show the
          detection → installation → fail-stop flow:
          
          ```
          ├─► Feature Request? ("add X", "build Y", "implement Z")
          │       └─► Detection Check
          │               ├─► Installed → Construct hugh "<feature>" → Workflow B
          │               └─► Not Installed → Workflow A (install)
          │                       ├─► Installation succeeds → Workflow B
          │                       └─► Installation fails → STOP with error
          ```
          
          The key difference from the v2.0 plan: "Installation fails" now leads to
          "STOP with error" instead of "Workflow I (agent routing)".
        files:
          - "agents/hugh.md — Session Initialization decision tree (~lines 190-225)"
        acceptance_criteria:
          - "Feature Request branch includes detection → install → fail-stop flow"
          - "Installation failure leads to STOP, not Workflow I or agent routing"
          - "Existing branches (Attached File, CLI Execution, Knowledge, Config, etc.) unchanged"
          - "ASCII tree remains well-formatted"

      - step: "1.5"
        task: |
          Update installer/hugh-setup.iss line 13 to use the correct repo URL.
          
          Change:
          `#define MyAppURL "https://github.com/pch-development/hugh"`
          To:
          `#define MyAppURL "https://github.com/PointCHealth/.github-private"`
        files:
          - "installer/hugh-setup.iss — line 13"
        acceptance_criteria:
          - "MyAppURL is set to https://github.com/PointCHealth/.github-private"
          - "AppPublisherURL, AppSupportURL, AppUpdatesURL (derived from MyAppURL) now resolve correctly"
          - "No other changes to the .iss file"

standards:
  referenced_standards: []
  note: "No organizational standards applicable to this plan."

review_session_log:
  questions_pending: 0
  questions_resolved: 0
  last_updated: "2026-02-10"
  entries: []
  notes: "No clarifying questions required — all issues were Minor severity and resolved directly by the reviewer."

implementation_complexity:
  factors:
    - factor: "Files to modify"
      score: 1
      notes: "2 files (agents/hugh.md, installer/hugh-setup.iss)"
    - factor: "New patterns introduced"
      score: 2
      notes: "gh CLI org discovery algorithm is new but follows existing prompt patterns"
    - factor: "External dependencies"
      score: 1
      notes: "gh CLI (already a prerequisite for Workflow A)"
    - factor: "Migration complexity"
      score: 1
      notes: "No migration — prompt file updates only, immediately effective"
    - factor: "Test coverage required"
      score: 1
      notes: "Manual verification only (prompt file changes, no unit tests)"
  overall_score: "6/25"
  overall_rating: "Low"

review_summary:
  review_date: "2026-02-10"
  reviewer: pch-plan-reviewer
  original_plan_version: "v3.0"
  reviewed_plan_version: "v3.1"
  review_metrics:
    issues_found: 2
    critical: 0
    major: 0
    minor: 2
    clarifying_questions: 0
    sections_updated:
      - "introduction (reference count 6→7)"
      - "requirements.functional.F2 (description corrected)"
      - "phases[0].tasks[0] (implementer note added for Step 0/Step 2 relationship)"
      - "handoff (status updated)"
      - "versions (v3.1 added)"
  key_improvements:
    - "Corrected reference count from 6 to 7 in introduction and F2 to account for the Step 8 fallback occurrence (line 657) handled separately by Task 1.3"
    - "Added implementer note to Task 1.1 clarifying that existing Step 2 (auth tier detection) remains unchanged and is not redundant with Step 0's auth check"
  remaining_considerations:
    - "Research docs (015, 016) and vision doc (004) also contain pch-development/hugh references — these are historical records and correctly excluded from scope"
    - "Step 0 runs before Step 1 in Workflow A; if detection cache is stale and Hugh is already installed, the repo discovery API calls execute unnecessarily — acceptable since it's 1-4 API calls, once per session"
  sign_off: "This plan has been reviewed and is Ready for Implementation"

implementation_notes:
  - phase: 1
    name: "gh CLI Repo Discovery + Hardcoded Reference Replacement + Clear Failure Messaging"
    completed: "2026-02-10"
    execution_mode: "Automatic (Subagent)"
    files_modified:
      - "agents/hugh.md"
      - "installer/hugh-setup.iss"
    files_created: []
    deviations: "None"
    notes: |
      - Task 1.1: Added Step 0: Discover Repository with complete 4-stage PowerShell discovery
        algorithm (gh availability, auth, org enumeration, release asset search). Inserted before
        Step 1 with clear fail-stop instructions.
      - Task 1.2: Replaced all 6 hardcoded pch-development/hugh references with $repoSlug variable
        in Steps 4 and 5 (3 in Step 4, 3 in Step 5).
      - Task 1.3: Replaced total failure fallback with actionable troubleshooting checklist
        (no hardcoded URLs, no agent routing).
      - Task 1.4: Updated Feature Request branch in Session Initialization decision tree to show
        detection → install → fail-stop flow.
      - Task 1.5: Updated MyAppURL in installer from pch-development/hugh to PointCHealth/.github-private.
      - agents/hugh.md grew from 914 to 960 lines due to Step 0 addition.

handoff:
  created_by: pch-planner
  created_date: "2026-02-10"
  reviewed_by: pch-plan-reviewer
  review_date: "2026-02-10"
  implemented_by: pch-coder
  implementation_date: "2026-02-10"
  status: complete
  plan_location: "/docs/plans/018-hugh-agent-installation-fix.md"
```
