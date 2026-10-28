id: "015"
type: research
title: "Hugh Agent GitHub Authentication and Installation Improvements"
status: complete
created: "2025-02-10"
current_phase: "complete"
vision_source: null

introduction: |
  This research investigates how to improve the Hugh agent's installation workflow 
  to handle private repository authentication. Currently, Workflow A uses the public 
  GitHub API to download releases, which fails with 404 when the repository requires 
  authentication. Additionally, there's a concern that subagent orchestration may 
  answer installation permission questions instead of passing them to the end-user.

objectives:
  - "How should the agent detect GitHub authentication status before attempting API calls?"
  - "How can the agent use `gh` CLI to download release assets from private/authenticated repositories?"
  - "What fallback strategies should be implemented when public API access fails?"
  - "How should installation permission questions be handled to ensure end-user confirmation?"

phases:
  - phase: 1
    name: "GitHub Authentication Detection Patterns"
    status: complete
    scope:
      - "Research `gh auth status` output formats and exit codes"
      - "Identify how to detect if user is authenticated to GitHub"
      - "Research token-based authentication via GH_TOKEN environment variable"
      - "Determine best practices for checking auth before API calls"
    session: "2025-02-10"
    findings: |
      ### 1. `gh auth status` Output Formats and Exit Codes

      Exit code 0 confirms authentication; exit code 1 indicates not authenticated or error.

      **Authenticated output format:**
      ```
      github.com
        ✓ Logged in to github.com account USERNAME (keyring)
        - Active account: true
        - Git operations protocol: https
        - Token: gho_************************************
        - Token scopes: 'admin:org', 'gist', 'repo', 'user', 'workflow'
      ```

      **Username parsing patterns** (from GitHubAuthDetector.cs):
      - `"Logged in to github.com as USERNAME"` — extracts text after " as "
      - `"Logged in to github.com account USERNAME"` — extracts text after "account "

      Output may appear on stderr, so both streams should be captured:
      ```csharp
      var username = ParseUsername(result.StandardOutput + result.StandardError);
      ```

      **Not authenticated output:** `You are not logged into any GitHub hosts`

      ### 2. Environment Variable Token Detection

      Detection priority order (already implemented in Hugh):

      | Priority | Variable | Notes |
      |----------|----------|-------|
      | 1 | GH_TOKEN | Primary, preferred for automation |
      | 2 | GITHUB_TOKEN | Fallback, commonly used in CI/CD |

      Implementation pattern:
      ```csharp
      var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
      var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
      if (!string.IsNullOrWhiteSpace(ghToken) || !string.IsNullOrWhiteSpace(githubToken))
      {
          var tokenSource = !string.IsNullOrWhiteSpace(ghToken) ? "GH_TOKEN" : "GITHUB_TOKEN";
      }
      ```

      ### 3. Detection Strategy Order (Best Practice)

      ```
      1. Check GH_TOKEN environment variable (fastest, no subprocess)
            ↓ if not set
      2. Check GITHUB_TOKEN environment variable
            ↓ if not set
      3. Run `gh auth status` command (subprocess, slower but definitive)
            ↓ if gh CLI not installed
      4. Return "cannot determine" — offer to install gh CLI or set token
      ```

      ### 4. Three-State Detection Model

      GitHubAuthDetector returns three states:

      | State | Condition | Message |
      |-------|-----------|---------|
      | Satisfied | Token env var found OR gh auth status exits 0 | "Authenticated to GitHub as {username}" |
      | Misconfigured | gh CLI installed but not authenticated | "GitHub CLI is not authenticated" |
      | Missing | gh CLI not installed | "Cannot check authentication: GitHub CLI is not installed" |

      ### 5. hosts.yml Credential Storage

      gh CLI stores credentials in `%APPDATA%\GitHub CLI\hosts.yml` (Windows).
      File existence does NOT guarantee token validity (could be expired/revoked).

      ### 6. API Authentication Header Pattern

      If using Invoke-RestMethod with authentication:
      ```powershell
      $headers = @{}
      if ($env:GH_TOKEN) {
          $headers["Authorization"] = "Bearer $env:GH_TOKEN"
      } elseif ($env:GITHUB_TOKEN) {
          $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
      }
      $release = Invoke-RestMethod -Uri "https://api.github.com/repos/pch-development/hugh/releases/latest" -Headers $headers
      ```
    key_discoveries:
      - "Exit code 0 from gh auth status confirms authentication; exit code 1 indicates not authenticated"
      - "GH_TOKEN takes precedence over GITHUB_TOKEN in detection order"
      - "Existing GitHubAuthDetector.cs provides a proven three-tier detection: env vars → gh auth status → gh CLI not installed"
      - "gh auth status output may appear on stderr, requiring both streams to be captured"
      - "Empty or whitespace-only token values should be treated as not set"
    relevant_files:
      - path: Hugh/Setup/Detection/GitHubAuthDetector.cs
        note: "Complete implementation of auth detection with env var priority and username parsing"
      - path: Hugh/Setup/Detection/GitHubCliDetector.cs
        note: "CLI version detection pattern"
      - path: Hugh.Tests/Setup/GitHubAuthDetectorTests.cs
        note: "Comprehensive test cases including edge cases"
      - path: Hugh/scripts/test-global-install.ps1
        note: "PowerShell authentication check pattern"
    external_sources: []
    gaps: []
    assumptions:
      - "The pch-development/hugh repository is private, causing the public API to return 404"

  - phase: 2
    name: "Private Repository Asset Download via gh CLI"
    status: complete
    scope:
      - "Research `gh release download` command syntax and options"
      - "Compare authenticated `gh` download vs public API download"
      - "Identify asset pattern matching for installer and portable ZIP"
      - "Research error handling for download failures"
    session: "2025-02-10"
    findings: |
      ### 1. `gh release download` Command Syntax

      ```bash
      gh release download [<tag>] [flags]
      ```

      Key options:
      | Flag | Description |
      |------|-------------|
      | -p, --pattern <glob> | Download only assets matching glob pattern |
      | -D, --dir <directory> | Target directory (default: current directory) |
      | -O, --output <file> | Write single asset to specific file |
      | -R, --repo <OWNER/REPO> | Target repository |
      | --clobber | Overwrite existing files |

      Without an explicit tag, downloads from latest release but --pattern is REQUIRED.

      ### 2. Authenticated gh Download vs Public API

      | Aspect | Public API + Invoke-WebRequest | gh release download |
      |--------|-------------------------------|---------------------|
      | Authentication | Manual (headers required) | Automatic (uses gh auth) |
      | Private repos | 404 error | Works if authenticated |
      | Rate limiting | 60/hr unauthenticated | 5000/hr authenticated |
      | Complexity | 3 steps (API → parse → download) | 1 step |
      | Version info | Returned in API response | Must parse filename or use gh release view |

      ### 3. Asset Pattern Matching

      From release workflow, exact asset naming:
      - Installer: `hugh-setup-{VERSION}.exe` (no 'v' prefix)
      - Portable: `hugh-portable-{VERSION}-win-x64.zip`

      ```powershell
      gh release download --repo pch-development/hugh --pattern "hugh-setup-*.exe" --dir $env:TEMP --clobber
      gh release download --repo pch-development/hugh --pattern "hugh-portable-*-win-x64.zip" --dir $env:TEMP --clobber
      ```

      Version extraction via gh release view:
      ```powershell
      $releaseInfo = gh release view --repo pch-development/hugh --json tagName | ConvertFrom-Json
      $version = $releaseInfo.tagName
      ```

      ### 4. Error Handling

      Exit code 0 = success, 1 = general error.

      Common errors:
      | Scenario | Handling |
      |----------|----------|
      | Not authenticated | Fall back to public API or prompt gh auth login |
      | Repo not found | Check repo name, offer manual URL |
      | No matching assets | Verify pattern, check release page |
      | Network error | Retry with backoff |

      ### 5. Recommended Fallback Order

      ```
      1. Try gh release download (authenticated, handles private repos)
            ↓ if fails or gh not installed
      2. Try public API with auth headers (GH_TOKEN/GITHUB_TOKEN)
            ↓ if 404 or rate-limited
      3. Try public API unauthenticated (works for public repos only)
            ↓ if still fails
      4. Direct user to manual download URL
      ```
    key_discoveries:
      - "gh release download automatically uses authentication from gh auth — no manual token handling needed"
      - "Pattern syntax: --pattern 'hugh-setup-*.exe' for installer, --pattern 'hugh-portable-*-win-x64.zip' for portable"
      - "Without explicit tag, gh release download gets latest release but REQUIRES --pattern flag"
      - "Version info not returned by download — use gh release view --json tagName or parse filename"
      - "--clobber flag recommended to overwrite previous downloads"
      - "Asset names use version WITHOUT 'v' prefix: hugh-setup-1.0.0.exe"
      - "Fallback order: gh CLI → authenticated API → unauthenticated API → manual URL"
    relevant_files:
      - path: agents/hugh.md
        note: "Current Workflow A implementation — target for update"
      - path: .github/workflows/release.yml
        note: "Asset naming patterns confirmation"
      - path: Hugh/Setup/Detection/ProcessRunner.cs
        note: "Model for running gh CLI and capturing exit codes"
    external_sources:
      - url: https://cli.github.com/manual/gh_release_download
        note: "Official gh release download documentation"
    gaps: []
    assumptions:
      - "Repository name pch-development/hugh is correct based on existing code references"

  - phase: 3
    name: "Subagent Question Passthrough for Installation"
    status: complete
    scope:
      - "Review how pch-coder and pch-researcher handle needs_user_input status"
      - "Research if the hugh agent needs similar question passthrough mechanisms"
      - "Identify patterns for ensuring installation permission reaches end-user"
      - "Review existing subagent orchestration documentation"
    session: "2025-02-10"
    findings: |
      ### Key Finding: Hugh Agent Does NOT Run as a Subagent

      The concern about installation permission questions being auto-answered by an
      orchestrator is largely unfounded in the current architecture.

      **Evidence:** Hugh agent front-matter only has outgoing handoffs to pch-helper.
      It is never a handoff target from any orchestrator agent. It is a top-level
      operator that interacts directly with users.

      ### How pch-coder and pch-researcher Handle needs_user_input

      Both use the structured return format:
      ```
      STATUS: needs_user_input
      QUESTION_FOR_USER: [question text]
      QUESTION_TYPE: elicitation | refinement | validation
      QUESTION_OPTIONS: [A/B/C/D or "open-ended"]
      RECOMMENDATION: [option letter or "none"]
      ```

      Subagents MUST return `needs_user_input` immediately when user input is needed.
      Parent orchestrators surface questions via `ask_questions` tool. Plan 013 added
      "When Running as a Subagent" sections to all 6 dev agents but NOT to Hugh.

      ### Two-System Architecture

      | System | Question Handling |
      |--------|------------------|
      | Hugh Agent (agents/hugh.md) | Direct user interaction in chat |
      | Hugh CLI (/Hugh/) | SignalClassifier → QuestionHandler → Console.ReadLine |
      | VS Code Subagents | needs_user_input → ask_questions tool |

      ### Hugh CLI Already Implements Question Passthrough

      - SignalClassifier.cs detects `needs_user_input` via regex
      - QuestionHandler.cs displays questions with ConsoleOutput.WriteSection
      - CopilotService.ExecutePhaseAsync has re-invocation loop (max 3 attempts)

      ### Recommendation

      No additional subagent section needed for Hugh agent because:
      1. It's not designed to be a subagent — it's a top-level CLI operator
      2. Its handoffs are outgoing only — no incoming handoff paths defined
      3. Installation questions are direct — user interaction in chat interface

      ### Alternative: Explicit "User Consent Required" Markers

      For defense-in-depth, Workflow A could add explicit consent markers:
      ```
      ⏸️ User Choice Required — Ask user preference: Installer or Portable ZIP
      ⏸️ User Confirmation Required — Confirm user wants to run installer
      ```
      This makes consent requirements explicit regardless of invocation context.
    key_discoveries:
      - "Hugh agent is NOT a subagent — only has outgoing handoffs to pch-helper"
      - "Installation permission questions go directly to end-user since Hugh is top-level interface"
      - "needs_user_input with QUESTION_FOR_USER is the standard subagent passthrough pattern"
      - "Hugh CLI already implements question passthrough in QuestionHandler and CopilotService"
      - "Plan 013 updated all 6 dev agents but NOT Hugh — correctly, since Hugh is an operator"
      - "The auto-answered installation concern is unfounded in current architecture"
      - "Adding explicit User Consent markers to Workflow A provides defense-in-depth"
    relevant_files:
      - path: agents/hugh.md
        note: "No subagent section; only outgoing handoffs to pch-helper"
      - path: agents/pch-coder.md
        note: "When Running as a Subagent section with needs_user_input pattern"
      - path: docs/plans/013-subagent-question-passthrough.md
        note: "Question passthrough plan — all 6 dev agents updated, not Hugh"
      - path: Hugh/Handlers/QuestionHandler.cs
        note: "HandleNeedsUserInput implementation"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "NeedsUserInput regex detection patterns"
    external_sources: []
    gaps: []
    assumptions:
      - "Concern about auto-answered permissions stems from a theoretical scenario not supported by current architecture"

  - phase: 4
    name: "Workflow A Update Recommendations"
    status: complete
    scope:
      - "Synthesize findings into concrete Workflow A improvements"
      - "Draft updated workflow steps with authentication checking"
      - "Define fallback strategy from public API to gh CLI to manual download"
      - "Document error messages and user guidance for each failure mode"
    session: "2025-02-10"
    findings: |
      ### Complete Updated Workflow A

      **Step 1: Check current installation**
      ```powershell
      Get-Command hugh -ErrorAction SilentlyContinue
      ```

      **Step 2: Detect GitHub authentication** (NEW — before any API calls)
      Three-tier check: GH_TOKEN env var → GITHUB_TOKEN env var → gh auth status
      Report auth state to user before proceeding.

      **Step 3: USER CONSENT — Choose installation method**
      Present Installer (recommended) vs Portable ZIP. WAIT for explicit response.

      **Step 4: Get latest release version**
      Try gh release view --json tagName first, fall back to API with auth headers.

      **Step 5: Download asset (four-tier waterfall)**
      Tier 1: gh release download (handles private repos automatically)
      Tier 2: Authenticated API with Bearer token header
      Tier 3: Unauthenticated API (public repos only)
      Tier 4: Manual fallback with URL and instructions

      **Step 6: USER CONSENT — Confirm before execution**
      Explicit confirmation before running installer or extracting ZIP.

      **Step 7: Execute installation, verify, and clean up**

      **Step 8: Post-installation guidance**

      ### Key PowerShell Patterns

      Auth detection function:
      ```powershell
      function Get-GitHubAuthMethod {
          if (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) { return 'token' }
          if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) { return 'token' }
          $ghCmd = Get-Command gh -ErrorAction SilentlyContinue
          if ($ghCmd) {
              $null = & gh auth status 2>&1
              if ($LASTEXITCODE -eq 0) { return 'gh-auth' }
          }
          return 'none'
      }
      ```

      gh CLI download:
      ```powershell
      gh release download --repo pch-development/hugh --pattern "hugh-setup-*.exe" --dir $env:TEMP --clobber
      ```

      Authenticated API download:
      ```powershell
      $headers = @{}
      if (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
          $headers['Authorization'] = "Bearer $($env:GH_TOKEN)"
      }
      $release = Invoke-RestMethod -Uri "https://api.github.com/repos/pch-development/hugh/releases/latest" -Headers $headers
      ```

      ### Error Message Reference

      | Failure Point | Error Message |
      |--------------|---------------|
      | Public API 404 | "Repository not accessible. May be private — authenticate with gh auth login" |
      | Public API 403 | "API rate limit exceeded. Wait or authenticate with gh auth login" |
      | All tiers fail | "Automated download failed. Download manually from GitHub releases page" |
      | Installer failed | "Installation failed (exit code {code}). Try running installer manually" |

      ### USER CONSENT Markers

      Two explicit consent points:
      1. Installation method choice (Installer vs Portable)
      2. Confirmation before executing installer/extraction
    key_discoveries:
      - "Auth detection MUST happen before any GitHub API calls"
      - "gh release download is preferred — handles auth automatically for private repos"
      - "Four-tier download fallback: gh CLI → authenticated API → unauthenticated API → manual URL"
      - "Two USER CONSENT markers required: method choice and execution confirmation"
      - "Use [string]::IsNullOrWhiteSpace() for token validation"
      - "Asset filenames use version WITHOUT 'v' prefix"
      - "Error messages should be specific and actionable with clear guidance"
    relevant_files:
      - path: agents/hugh.md
        note: "Contains Workflow A that needs to be updated"
    external_sources: []
    gaps: []
    assumptions:
      - "Repository at pch-development/hugh, may be private"
      - "Installer uses Inno Setup conventions per existing workflow"
      - "PowerShell 5.1+ available for Expand-Archive, Invoke-RestMethod"

overview: |
  This research investigated how to improve the Hugh agent's Workflow A (installation)
  to handle private repository authentication and ensure installation permissions
  reach end-users. Four phases examined authentication detection, authenticated
  download methods, subagent question passthrough, and synthesized a complete
  updated workflow.

  The primary issue — public GitHub API calls returning 404 for private repos — is
  resolved by adding authentication detection BEFORE API calls and using `gh release
  download` as the preferred download method. The `gh` CLI automatically handles
  authentication when the user is logged in, eliminating manual token management.

  The secondary concern — subagent orchestration auto-answering installation
  permissions — was found to be unfounded in the current architecture. The Hugh
  agent is a top-level operator that interacts directly with users, not a subagent
  target. However, explicit "User Consent Required" markers are recommended as
  defense-in-depth.

  The updated Workflow A follows an 8-step process with a 4-tier download fallback
  (gh CLI → authenticated API → unauthenticated API → manual URL) and two explicit
  user consent checkpoints.

key_findings:
  - "Authentication detection (GH_TOKEN → GITHUB_TOKEN → gh auth status) must happen before any GitHub API calls"
  - "gh release download is the preferred download method — handles private repo auth automatically"
  - "Four-tier download fallback ensures installation works across all authentication states"
  - "The subagent question passthrough concern is unfounded — Hugh is a top-level operator, not a subagent"
  - "Two explicit USER CONSENT markers needed: installation method choice and execution confirmation"
  - "Existing GitHubAuthDetector.cs provides proven patterns that the agent prompt should mirror"

actionable_conclusions:
  - "Replace Workflow A in agents/hugh.md with the updated 8-step workflow from Phase 4"
  - "Add authentication detection as Step 2, before any GitHub API calls"
  - "Use gh release download as Tier 1 download method with --pattern and --clobber flags"
  - "Add explicit USER CONSENT markers at Steps 3 and 6"
  - "Include comprehensive error messages with actionable user guidance for each failure mode"
  - "No changes needed for subagent question passthrough — current architecture is correct"

open_questions:
  - "Should the fallback manual download URL point to a specific release or the latest releases page?"

standards: []

handoff:
  created_by: pch-researcher
  created_date: "2025-02-10"
  status: complete
  current_phase: complete
  path: /docs/research/015-hugh-agent-github-authentication-installation.md
