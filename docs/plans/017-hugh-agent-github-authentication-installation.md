id: "017"
type: plan
title: "Hugh Agent GitHub Authentication and Installation Improvements"
status: in-progress
created: "2025-02-10"
updated: "2025-02-10"
owner: pch-planner
version: "v2.1"
research_source: "/docs/research/015-hugh-agent-github-authentication-installation.md"

versions:
  - version: "v1.0"
    date: "2025-02-10"
    author: pch-planner
    changes: "Initial plan skeleton created"
  - version: "v1.1"
    date: "2025-02-10"
    author: pch-planner
    changes: "Decisions recorded, technical design, dependencies, and risks completed"
  - version: "v2.0"
    date: "2025-02-10"
    author: pch-planner
    changes: "Execution phases added, holistic review completed"
  - version: "v2.1"
    date: "2025-02-10"
    author: pch-plan-reviewer
    changes: "Review completed: fixed line number references, corrected Tier 2 download to use asset URL with Accept header instead of browser_download_url"

introduction: |
  This plan updates the Hugh agent's Workflow A (installation) to handle private
  repository authentication. Currently, the workflow uses the public GitHub API
  via Invoke-RestMethod, which returns 404 when the pch-development/hugh repository
  is private. The updated workflow adds authentication detection before API calls,
  uses `gh release download` as the preferred method for private repos, implements
  a three-tier download fallback (gh CLI → authenticated API → manual URL), and adds
  explicit user consent markers at key decision points.

decisions:
  - session: 1
    date: "2025-02-10"
    decision_point: "How many fallback tiers for downloading release assets?"
    answer: "C — Three-Tier: gh CLI + Authenticated API + Manual"
    rationale: "Unauthenticated API will 404 on private repo — dead code. Three tiers balance resilience with prompt readability."
  - session: 2
    date: "2025-02-10"
    decision_point: "Should execution confirmation be a separate numbered step with ⏸️ marker?"
    answer: "A — Yes, Separate Consent Step"
    rationale: "Defense-in-depth with explicit ⏸️ marker makes consent visible and auditable. Low cost, high clarity."

holistic_review:
  decision_interactions:
    - "Auth detection (Decision 1) and consent markers (Decision 2) complement each other:
       auth detection determines WHICH download tier to use, consent markers ensure
       the user controls WHEN execution happens. No conflicts."
    - "Three-tier fallback means Tier 2 (authenticated API) only activates when gh CLI
       is unavailable BUT a token env var is set. If neither gh nor token exists, the
       workflow skips directly to manual fallback — this is correct behavior."
  architectural_considerations:
    - "Single-file change (agents/hugh.md) with no code dependencies — low risk"
    - "Workflow A is self-contained; changes do not affect Workflows B-G"
    - "PowerShell patterns mirror existing C# patterns in GitHubAuthDetector.cs for consistency"
  trade_offs_accepted:
    - "Dropped unauthenticated API tier — would always 404 on private repo, accepted as dead code elimination"
    - "Added workflow length (~60 more lines) — accepted for improved reliability and user safety"
  risks_acknowledged:
    - "String replacement boundaries in agents/hugh.md require careful identification (risk-1, mitigated by step 1.1)"
    - "Asset naming assumed stable based on release workflow (risk-2, mitigated by wildcard patterns)"

overview:
  feature: "Update Workflow A in agents/hugh.md for authenticated GitHub release downloads"
  objectives:
    - "Add authentication detection (GH_TOKEN → GITHUB_TOKEN → gh auth status) before GitHub API calls"
    - "Use `gh release download` as Tier 1 download method for private repository support"
    - "Implement three-tier download fallback: gh CLI → authenticated API → manual URL"
    - "Add explicit USER CONSENT markers at installation method choice and execution confirmation"
    - "Provide specific, actionable error messages for each failure mode"
  target_file: "agents/hugh.md"
  scope: "Workflow A section only — no changes to other workflows, CLI code, or agent prompts"

requirements:
  functional:
    - "Authentication detection must check GH_TOKEN, GITHUB_TOKEN, then gh auth status in priority order"
    - "Download must use gh release download when gh CLI is available and authenticated"
    - "Fallback must degrade gracefully through three tiers if preferred method fails"
    - "User must explicitly choose between Installer and Portable ZIP before download begins"
    - "User must explicitly confirm before installer execution or ZIP extraction"
    - "Error messages must be specific to each failure mode with actionable recovery steps"
    - "Version information must be retrievable via gh release view or API response parsing"
  non_functional:
    - "Workflow must remain readable and maintainable as a prompt section in agents/hugh.md"
    - "PowerShell patterns must work on PowerShell 5.1+ and PowerShell 7+"
    - "Auth detection should be fast — environment variable checks before subprocess calls"
  out_of_scope:
    - "No changes to Hugh CLI source code (Hugh/ directory)"
    - "No changes to other workflows (B through G)"
    - "No changes to other agent prompts (pch-*.md files)"
    - "No subagent question passthrough changes (research confirmed Hugh is not a subagent)"
    - "No changes to CI/CD release workflow"

technical_design:
  architecture:
    summary: |
      This change modifies ONLY the Workflow A section in agents/hugh.md. No application
      code, CLI source, or other agent prompts are affected. The workflow transforms from
      a 6-step public-API-only flow to an 8-step authenticated flow with three download
      tiers and two explicit user consent checkpoints.
    current_flow: |
      Step 1: Check installation (Get-Command hugh)
      Step 2: Query GitHub public API (Invoke-RestMethod — fails 404 on private repo)
      Step 3: Ask installer vs portable preference
      Step 4a/4b: Download via browser_download_url + Invoke-WebRequest
      Step 5: Advise PATH
      Step 6: Fallback to manual URL
    new_flow: |
      Step 1: Check current installation (Get-Command hugh)
      Step 2: Detect GitHub authentication (NEW — GH_TOKEN → GITHUB_TOKEN → gh auth status)
      Step 3: ⏸️ USER CONSENT — Choose installation method (Installer vs Portable ZIP)
      Step 4: Get latest release version (gh release view or authenticated API)
      Step 5: Download asset (three-tier: gh CLI → authenticated API → manual URL)
      Step 6: ⏸️ USER CONSENT — Confirm before execution
      Step 7: Execute installation, verify, clean up
      Step 8: Post-installation guidance (PATH, new terminal)
  codebase_patterns:
    - pattern: "PowerShell auth detection"
      location: "Hugh/Setup/Detection/GitHubAuthDetector.cs"
      usage: "Mirror the three-tier detection (env vars → gh auth status) in PowerShell"
    - pattern: "Process exit code checking"
      location: "Hugh/Setup/Detection/ProcessRunner.cs"
      usage: "Model for running gh CLI and checking $LASTEXITCODE"
    - pattern: "Asset naming convention"
      location: ".github/workflows/release.yml (lines 72, 84-85)"
      usage: "hugh-setup-{VERSION}.exe and hugh-portable-{VERSION}-win-x64.zip (no 'v' prefix in filename)"
    - pattern: "Existing Workflow A structure"
      location: "agents/hugh.md (Workflow A section, lines 396-449)"
      usage: "Replace this section in-place, preserving surrounding workflow sections"
  data_models: {}
  api_contracts: {}
  component_specs:
    auth_detection_function:
      description: "PowerShell function to determine GitHub authentication method"
      logic: |
        1. Check $env:GH_TOKEN — if non-empty, return 'token' with source 'GH_TOKEN'
        2. Check $env:GITHUB_TOKEN — if non-empty, return 'token' with source 'GITHUB_TOKEN'
        3. Check if gh CLI exists (Get-Command gh)
        4. If gh exists, run `gh auth status 2>&1` and check $LASTEXITCODE
        5. If exit code 0, return 'gh-auth'
        6. Otherwise return 'none'
      validation: "Use [string]::IsNullOrWhiteSpace() for token checks, not simple truthiness"
    version_retrieval:
      description: "Get latest release version, preferring gh CLI"
      tier_1: "gh release view --repo pch-development/hugh --json tagName | ConvertFrom-Json"
      tier_2: "Invoke-RestMethod with Authorization Bearer header from env var token"
      notes: "Version tag may have 'v' prefix (e.g., v1.0.0); asset filenames strip the 'v'"
    asset_download:
      description: "Three-tier download fallback"
      tier_1:
        method: "gh release download"
        command_installer: "gh release download --repo pch-development/hugh --pattern 'hugh-setup-*.exe' --dir $env:TEMP --clobber"
        command_portable: "gh release download --repo pch-development/hugh --pattern 'hugh-portable-*-win-x64.zip' --dir $env:TEMP --clobber"
        requires: "gh CLI installed and authenticated"
        advantages: "Handles private repos automatically, no manual token management"
      tier_2:
        method: "Authenticated Invoke-RestMethod + Invoke-WebRequest"
        requires: "GH_TOKEN or GITHUB_TOKEN environment variable set"
        flow: "API call with Bearer header → parse assets array → download via $asset.url with Accept: application/octet-stream header"
        note: "Must use asset 'url' field (not 'browser_download_url') — the latter redirects to cloud storage that rejects Bearer tokens on private repos"
      tier_3:
        method: "Manual fallback"
        url: "https://github.com/pch-development/hugh/releases/latest"
        trigger: "Both automated tiers failed"
    consent_markers:
      marker_1:
        step: 3
        type: "Installation method choice"
        text: "⏸️ User Choice Required — Installer (recommended) or Portable ZIP?"
        behavior: "WAIT for explicit user response before proceeding"
      marker_2:
        step: 6
        type: "Execution confirmation"
        text: "⏸️ User Confirmation Required — Ready to run installer / extract ZIP. Proceed?"
        behavior: "WAIT for explicit user confirmation before executing"
    error_messages:
      - failure_point: "Public API 404"
        message: "Repository not accessible (404). This may be a private repository. Authenticate with `gh auth login` or set GH_TOKEN, then retry."
      - failure_point: "API 403 rate limit"
        message: "API rate limit exceeded. Wait a few minutes or authenticate with `gh auth login` to increase your limit."
      - failure_point: "gh release download fails"
        message: "gh CLI download failed. Trying authenticated API fallback..."
      - failure_point: "All tiers fail"
        message: "Automated download failed. Download manually from: https://github.com/pch-development/hugh/releases/latest"
      - failure_point: "Installer execution fails"
        message: "Installation failed (exit code {code}). Try downloading and running the installer manually from the releases page."

dependencies:
  - id: dep-1
    description: "Current Workflow A section in agents/hugh.md must be identifiable for replacement"
    type: internal
    status: satisfied
    notes: "Workflow A spans lines 396-449, with ### Workflow B heading at line 450"
  - id: dep-2
    description: "Asset naming convention confirmed in release workflow"
    type: internal
    status: satisfied
    notes: "hugh-setup-{VERSION}.exe and hugh-portable-{VERSION}-win-x64.zip confirmed in .github/workflows/release.yml"
  - id: dep-3
    description: "gh CLI must support 'gh release download' and 'gh release view' commands"
    type: external
    status: satisfied
    notes: "Standard gh CLI features available in all recent versions"

risks:
  - id: risk-1
    description: "Workflow A replacement may not match exact string boundaries in agents/hugh.md"
    likelihood: medium
    impact: low
    mitigation: "Implementer should read the full Workflow A section to identify exact boundaries before replacing"
  - id: risk-2
    description: "gh release download pattern may not match if asset naming changes"
    likelihood: low
    impact: medium
    mitigation: "Patterns use wildcards (hugh-setup-*.exe) which are resilient to version format changes"
  - id: risk-3
    description: "PowerShell 5.1 compatibility — some cmdlets behave differently than PS 7"
    likelihood: low
    impact: low
    mitigation: "Use only PS 5.1-compatible syntax: Invoke-RestMethod, Invoke-WebRequest, Expand-Archive, ConvertFrom-Json"

phases:
  - phase: 1
    name: "Replace Workflow A with Authenticated Installation Flow"
    status: complete
    size: medium
    files_to_modify: 1
    prerequisites:
      - "agents/hugh.md exists with current Workflow A section"
    entry_point: "agents/hugh.md"
    verification: "Read agents/hugh.md and confirm Workflow A section is present with ### heading"
    tasks:
      - step: "1.1"
        task: "Read current Workflow A boundaries"
        description: |
          Read agents/hugh.md and identify the exact start and end of the
          Workflow A section. The section begins with `### Workflow A — Install Hugh`
          and ends just before `### Workflow B — Run a Feature`. Record the exact
          text boundaries for replacement.
        files:
          - "agents/hugh.md"
        acceptance_criteria:
          - "Exact start line identified (### Workflow A heading)"
          - "Exact end line identified (line before ### Workflow B heading)"
          - "Full current content captured for oldString matching"

      - step: "1.2"
        task: "Replace Workflow A with updated 8-step authenticated flow"
        description: |
          Replace the entire Workflow A section (from ### Workflow A heading through the
          last line before ### Workflow B) with the new 8-step workflow. The replacement
          must preserve the ### heading level and overall formatting style consistent
          with other workflows in the file.

          The new Workflow A content must include these 8 steps IN ORDER:

          **Step 1 — Check current installation:**
          ```powershell
          Get-Command hugh -ErrorAction SilentlyContinue
          ```
          If already installed, report version and skip to verification.

          **Step 2 — Detect GitHub authentication (NEW):**
          Three-tier detection before any API calls:
          ```powershell
          # Tier 1: Check GH_TOKEN env var
          if (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
              # Authenticated via GH_TOKEN
          }
          # Tier 2: Check GITHUB_TOKEN env var
          elseif (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
              # Authenticated via GITHUB_TOKEN
          }
          # Tier 3: Check gh auth status
          else {
              $ghCmd = Get-Command gh -ErrorAction SilentlyContinue
              if ($ghCmd) {
                  $null = & gh auth status 2>&1
                  if ($LASTEXITCODE -eq 0) {
                      # Authenticated via gh CLI
                  }
              }
          }
          ```
          Report auth state to user: which method was detected, or "not authenticated"
          with guidance to run `gh auth login` or set `GH_TOKEN`.

          **Step 3 — ⏸️ USER CONSENT — Choose installation method:**
          Present two options:
          - Installer (recommended) — adds to PATH automatically
          - Portable ZIP — manual PATH setup required
          WAIT for explicit user response. Do NOT proceed without a choice.

          **Step 4 — Get latest release version:**
          Try gh CLI first, fall back to authenticated API:
          ```powershell
          # Prefer gh release view
          $releaseInfo = gh release view --repo pch-development/hugh --json tagName 2>&1 | ConvertFrom-Json
          $version = $releaseInfo.tagName

          # Fallback: authenticated API
          $headers = @{}
          if (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
              $headers['Authorization'] = "Bearer $($env:GH_TOKEN)"
          } elseif (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
              $headers['Authorization'] = "Bearer $($env:GITHUB_TOKEN)"
          }
          $release = Invoke-RestMethod -Uri "https://api.github.com/repos/pch-development/hugh/releases/latest" -Headers $headers
          $version = $release.tag_name
          ```

          **Step 5 — Download asset (three-tier fallback):**

          Tier 1 — gh release download (preferred):
          ```powershell
          # Installer
          gh release download --repo pch-development/hugh --pattern "hugh-setup-*.exe" --dir $env:TEMP --clobber
          # Portable
          gh release download --repo pch-development/hugh --pattern "hugh-portable-*-win-x64.zip" --dir $env:TEMP --clobber
          ```

          Tier 2 — Authenticated API (if gh CLI unavailable):
          NOTE: Must use `$asset.url` (not `browser_download_url`) with `Accept: application/octet-stream`.
          The `browser_download_url` redirects to cloud storage that rejects Bearer tokens on private repos.
          ```powershell
          $token = if (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) { $env:GH_TOKEN } else { $env:GITHUB_TOKEN }
          $apiHeaders = @{
              'Authorization' = "Bearer $token"
          }
          $release = Invoke-RestMethod -Uri "https://api.github.com/repos/pch-development/hugh/releases/latest" -Headers $apiHeaders
          $asset = $release.assets | Where-Object { $_.name -like "hugh-setup-*.exe" }
          $downloadHeaders = @{
              'Authorization' = "Bearer $token"
              'Accept' = 'application/octet-stream'
          }
          Invoke-WebRequest -Uri $asset.url -OutFile "$env:TEMP\hugh-setup.exe" -Headers $downloadHeaders
          ```

          Tier 3 — Manual fallback:
          If both automated tiers fail, direct user to:
          `https://github.com/pch-development/hugh/releases/latest`
          with instructions to download the appropriate asset manually.

          Include specific error messages per failure mode:
          - 404: "Repository not accessible. May be private — authenticate with `gh auth login`"
          - 403: "API rate limit exceeded. Wait or authenticate to increase limit."
          - All fail: "Automated download failed. Download manually from releases page."

          **Step 6 — ⏸️ USER CONSENT — Confirm before execution:**
          Display what will be executed (installer path or ZIP extraction target).
          WAIT for explicit user confirmation. Do NOT execute without confirmation.

          **Step 7 — Execute installation, verify, and clean up:**

          7a. Installer path:
          ```powershell
          & "$env:TEMP\hugh-setup.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
          & "$env:LOCALAPPDATA\Programs\Hugh\Hugh.exe" --version
          Remove-Item "$env:TEMP\hugh-setup.exe"
          ```

          7b. Portable ZIP path:
          ```powershell
          Expand-Archive "$env:TEMP\hugh.zip" -DestinationPath "C:\Tools\Hugh" -Force
          $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
          [Environment]::SetEnvironmentVariable("Path", "$currentPath;C:\Tools\Hugh", "User")
          & "C:\Tools\Hugh\Hugh.exe" --version
          Remove-Item "$env:TEMP\hugh.zip"
          ```

          Include error handling: if installer returns non-zero exit code, report
          "Installation failed (exit code {code}). Try running installer manually."

          **Step 8 — Post-installation guidance:**
          - Advise: "Open a new terminal for PATH changes to take effect."
          - Suggest: `hugh doctor` to verify all prerequisites
          - If installed via portable, remind about manual PATH setup
        files:
          - "agents/hugh.md"
        acceptance_criteria:
          - "Workflow A section replaced with 8-step flow"
          - "Step 2 includes three-tier auth detection (GH_TOKEN → GITHUB_TOKEN → gh auth status)"
          - "Step 3 has ⏸️ USER CONSENT marker for installation method choice"
          - "Step 5 implements three-tier download: gh CLI → authenticated API → manual URL"
          - "Step 6 has ⏸️ USER CONSENT marker for execution confirmation"
          - "Error messages are specific and actionable for 404, 403, and total failure"
          - "All PowerShell patterns use [string]::IsNullOrWhiteSpace() for token validation"
          - "Asset patterns use wildcards: hugh-setup-*.exe and hugh-portable-*-win-x64.zip"
          - "Surrounding workflows (B through G) are unchanged"
          - "Formatting style matches other workflow sections in the file"

      - step: "1.3"
        task: "Verify no regressions in surrounding content"
        description: |
          After replacing Workflow A, read the file to verify:
          - Workflow B through G sections are intact and unchanged
          - The Workflow A section has proper ### heading
          - No orphaned or duplicate content at boundaries
          - The file remains valid Markdown
        files:
          - "agents/hugh.md"
        acceptance_criteria:
          - "Workflow B heading (### Workflow B — Run a Feature) immediately follows new Workflow A"
          - "All 7 workflows (A-G) present with correct ### headings"
          - "No duplicate or missing content at replacement boundaries"
          - "File is well-formed Markdown"

standards:
  referenced_standards: "No organizational standards applicable to this plan."

review_session_log:
  questions_pending: 0
  questions_resolved: 1
  last_updated: "2025-02-10"
  entries:
    - number: 1
      issue: "Tier 2 download uses browser_download_url which fails on private repos"
      category: correctness
      decision: "Option A: Use $asset.url with Accept: application/octet-stream header"
      plan_update: "component_specs.asset_download.tier_2 and Phase 1 Step 1.2 Tier 2 code updated"

implementation_complexity:
  files_to_modify: 1
  files_to_modify_score: 1
  new_patterns_introduced: 0
  new_patterns_score: 1
  external_dependencies: 1
  external_dependencies_notes: "gh CLI (optional, graceful fallback)"
  external_dependencies_score: 1
  migration_complexity: 0
  migration_complexity_score: 1
  test_coverage_required: "Manual verification only (prompt file, no unit tests)"
  test_coverage_score: 1
  overall_score: "5/25"
  overall_rating: "Low"

review_summary:
  review_date: "2025-02-10"
  reviewer: pch-plan-reviewer
  original_plan_version: "v2.0"
  reviewed_plan_version: "v2.1"
  review_metrics:
    issues_found: 4
    critical: 1
    major: 1
    minor: 2
    clarifying_questions: 1
    sections_updated:
      - "codebase_patterns (line references)"
      - "dependencies (line references)"
      - "component_specs.asset_download.tier_2 (download method)"
      - "Phase 1 Step 1.2 Tier 2 code (download method)"
  key_improvements:
    - "Corrected Workflow A line references from ~441-502 to verified 396-449"
    - "Fixed Tier 2 API download to use $asset.url with Accept: application/octet-stream instead of browser_download_url, which redirects to cloud storage that rejects Bearer tokens on private repos"
    - "Corrected release workflow line references from 70, 85-86 to 72, 84-85"
  remaining_considerations:
    - "Implementer should still read full Workflow A boundaries to confirm exact replacement text before editing"
  sign_off: "This plan has been reviewed and is Ready for Implementation"

handoff:
  created_by: pch-planner
  created_date: "2025-02-10"
  reviewed_by: pch-plan-reviewer
  review_date: "2025-02-10"
  status: ready-for-implementation
  next_agent: pch-coder
  plan_location: "/docs/plans/017-hugh-agent-github-authentication-installation.md"
