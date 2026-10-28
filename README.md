# AI Agent Workflow System

This repository contains specialized AI agent prompts designed for GitHub Copilot Workspace. These agents implement a phased, incremental approach to software development that respects AI context window limitations while delivering production-ready code.

---

## 📦 Organization Distribution Setup

This repository is designed to be the `.github-private` repository for your GitHub organization, which enables organization-wide distribution of custom agents to all members.

### Prerequisites

| Requirement | Description |
|-------------|-------------|
| **GitHub Copilot Business or Enterprise** | Custom agents require a paid Copilot plan |
| **Organization Owner Access** | Required to create the `.github-private` repository |
| **VS Code 1.106+** | Custom agents feature requires VS Code 1.106 or later |

### Step 1: Create the .github-private Repository

If you're starting from scratch:

1. Navigate to your GitHub organization
2. Create a new repository named exactly **`.github-private`**
3. Set the visibility to:
   - **Internal** — Grants read access to all organization members (recommended)
   - **Private** — Requires manually granting access to members
4. Clone the repository and add the agents from this project

> 💡 **Tip:** GitHub provides a [template repository](https://github.com/docs/custom-agents-template) to speed up setup.

### Step 2: Repository Structure for Organization Agents

For **organization-wide availability**, agent files must be placed in the root `agents/` folder:

```
.github-private/
├── agents/                          # ✅ Organization-wide agents (available to all members)
│   ├── pch-visionary.md
│   ├── pch-researcher.md
│   ├── pch-planner.md
│   ├── pch-plan-reviewer.md
│   ├── pch-coder.md
│   └── pch-helper.md
├── .github/
│   └── agents/                      # 🔒 Testing-only agents (only accessible in this repo)
│       └── experimental-agent.md
└── docs/
    ├── vision/
    ├── research/
    └── plans/
```

| Location | Availability |
|----------|--------------|
| `agents/` (root) | Available to all organization members across all repositories |
| `.github/agents/` | Only available within the `.github-private` repository (for testing) |

### Step 3: Agent File Format

Agent files use Markdown with YAML frontmatter. Files can use either `.md` or `.agent.md` extension:

```yaml
---
name: agent-name
description: Brief description shown in the agent picker
model: Claude Opus 4.8
tools: ['search', 'read', 'edit']  # Optional: restrict available tools
handoffs:
  - label: Button Label
    agent: target-agent-name
    prompt: Context for next agent
    send: false
---

# Agent Instructions

Your agent's system prompt and instructions go here...
```

### Step 4: VS Code Settings for Organization Agents

Users must enable organization agent discovery in VS Code:

1. Open VS Code Settings (`Ctrl+,` / `Cmd+,`)
2. Search for `customAgents`
3. Enable **Show Organization And Enterprise Agents**

Or add to `settings.json`:

```json
{
  "github.copilot.chat.customAgents.showOrganizationAndEnterpriseAgents": true
}
```

### Step 5: Verify Agent Visibility

After setup, organization members can access agents in VS Code:

1. Open GitHub Copilot Chat (`Ctrl+Alt+I` / `Cmd+Option+I`)
2. Click the agent dropdown at the bottom of the chat view
3. Organization agents appear alongside built-in agents (marked with organization icon)

> ⚠️ **Note:** Users must have read access to the `.github-private` repository for agents to appear.

### Testing Before Release

To test agents before organization-wide release:

1. Place agent files in `.github/agents/` (instead of root `agents/`)
2. Open the `.github-private` repository in VS Code
3. Test the agents within that repository context
4. Once satisfied, move files to the root `agents/` folder
5. Commit and push to the default branch

### Additional VS Code Settings

| Setting | Description |
|---------|-------------|
| `github.copilot.chat.customAgents.showOrganizationAndEnterpriseAgents` | Enable discovery of org/enterprise agents (required) |
| `chat.customAgentInSubagent.enabled` | Enable Automatic Mode for subagent orchestration |

Full `settings.json` example for optimal agent experience:

```json
{
  "github.copilot.chat.customAgents.showOrganizationAndEnterpriseAgents": true,
  "chat.customAgentInSubagent.enabled": true
}
```

### Troubleshooting

| Issue | Solution |
|-------|----------|
| Agents not appearing | Verify `showOrganizationAndEnterpriseAgents` is `true` |
| Agents still missing | Check repository access — user needs read permissions |
| Agents in wrong location | Ensure agents are in root `agents/` folder, not `.github/agents/` |
| YAML parsing errors | Validate frontmatter syntax — use `---` delimiters correctly |
| Subagents not working | Enable `chat.customAgentInSubagent.enabled` setting |

---

## Agent Workflow

The system uses six core agents that work together in a sequential workflow:

```
Vision → Research → Planning → Review → Implementation
                             ↑
                        pch-helper (guidance at any stage)
```

### Available Agents

1. **pch-visionary** - Transforms raw ideas into structured vision documents
   - Guides users through Q&A to clarify requirements and scope
   - Combines business analyst and solution architect perspectives
   - Creates vision documents that feed into research and planning
   - Use when you have a new idea but haven't fully defined requirements

2. **pch-researcher** - Conducts comprehensive, phased research on complex topics
   - Creates structured research documents
   - Executes research in manageable phases across multiple sessions
   - Documents findings progressively to avoid context exhaustion
   - **Supports Manual & Automatic execution modes**

3. **pch-planner** - Creates highly detailed implementation plans
   - Asks clarifying questions one at a time with multiple choice options
   - Builds plan documents incrementally to avoid large response errors
   - Documents all decisions in planning session logs

4. **pch-plan-reviewer** - Reviews plans for quality and completeness
   - Verifies codebase references and technical claims
   - Identifies gaps, ambiguities, and potential issues
   - Updates plans with corrections and enhancements

5. **pch-coder** - Executes implementation from approved plans
   - Implements one phase per session for reliable code quality
   - Updates plans with progress tracking
   - Handles errors gracefully and documents blockers
   - **Supports Manual & Automatic execution modes**

6. **pch-helper** - Guides users on how to use the agent workflow
   - Explains the purpose and capabilities of each agent
   - Helps determine which agent to start with based on your situation
   - Provides advice on workflow sequencing and multi-session work
   - Can hand off to any of the other agents when you're ready

## Execution Modes

Both `pch-coder` and `pch-researcher` support two execution modes, selectable at the start of each session:

### Manual Mode (Default)

The traditional one-phase-per-session workflow:

| Aspect | Description |
|--------|-------------|
| **Control** | Full user control between each phase |
| **Session Flow** | Agent completes one phase, then requests a new session |
| **Review Points** | User can review, adjust, or redirect before continuing |
| **Context** | Each new session starts with full context capacity |
| **Best For** | First-time plans, complex changes, learning the codebase |

### Automatic Mode

Subagent orchestration for hands-free multi-phase execution:

| Aspect | Description |
|--------|-------------|
| **Execution** | Each phase runs in an isolated subagent with fresh context |
| **Orchestration** | Main agent coordinates phases and processes results |
| **Reporting** | Structured returns with status, files modified, and blockers |
| **Blocker Handling** | Options to continue, skip, stop, retry, or clarify |
| **Checkpoints** | Pauses for deployment (coder) or ambiguous scope (researcher) |
| **Best For** | Trusted plans, repetitive tasks, well-defined research |

### Mode Selection Guide

| Scenario | Recommended Mode | Reason |
|----------|-----------------|--------|
| First time running a new plan | Manual | Review each phase before proceeding |
| Trusted, well-tested plan | Automatic | Efficiency gains from orchestration |
| Plan with deployment steps | Manual | Deployment needs manual attention |
| Complex or risky changes | Manual | Better control and review points |
| Repetitive/mechanical tasks | Automatic | Less user overhead |
| Exploratory research | Manual | May need to adjust scope mid-research |
| Well-structured research outline | Automatic | Parallel-friendly, defined scope |

> ⚠️ **Note:** Automatic Mode requires VS Code setting `chat.customAgentInSubagent.enabled: true`

## Prerequisites for Standards Integration (Optional)

To enable organizational standards integration for `pch-planner`, `pch-plan-reviewer`, and `pch-researcher`:

1. **Install GitHub MCP Server** — VS Code extension that enables MCP capabilities
2. **Create Copilot Space** — Organization admin must create a Copilot Space named "pch-standards-space" and share it org-wide
3. **VS Code 1.101+** — Required for remote MCP support

If these prerequisites are not met, the agents will continue working normally and display a fallback warning message.

## Key Features

### Context Window Management
- **One phase per session** - Agents complete discrete work units then stop
- **Fresh context** - New sessions provide full context capacity for better code quality
- **Document-based persistence** - All progress saved to files between sessions

### Incremental Approach
- Build documents **one section at a time**
- Never attempt to generate large documents in a single response
- Update documents after each decision point

### Structured Interaction
- **One question at a time** with 3-5 multiple choice options
- Clear recommendations and rationale for each option
- Progress tracking with status values (`not-started`, `in-progress`, `complete`, `blocked`)

### AI-Optimized Document Format
- Documents use compact Markdown format for structured, readable content
- Max 2-level headers (`##` for major sections, `###` for subsections)
- Markdown tables for structured data; prose for descriptive content
- Status values with emoji for visual scanning: ⏳ Not Started, 🔄 In Progress, ✅ Complete, ❌ Blocked, ⚠️ Partial

## File Structure

This repository follows the `.github-private` pattern for organization-wide agent distribution:

```
.github-private/                     # Repository name (required for org distribution)
├── .github/
│   └── copilot-instructions.md      # Detailed documentation for working with agents
├── agents/                          # Organization-wide agents (root level)
│   ├── pch-visionary.md             # Idea-to-vision document agent
│   ├── pch-researcher.md            # Research specialist (Manual & Automatic modes)
│   ├── pch-planner.md               # Planning specialist agent
│   ├── pch-plan-reviewer.md         # Review specialist agent
│   ├── pch-coder.md                 # Implementation specialist (Manual & Automatic modes)
│   └── pch-helper.md                # Workflow guidance agent
└── docs/
    ├── vision/                      # Vision documents (created by pch-visionary)
    ├── research/                    # Research documents (created by pch-researcher)
    └── plans/                       # Implementation plans (created by pch-planner)
```

> 📍 **Important:** The `agents/` folder is at the repository root for organization-wide visibility. See [Organization Distribution Setup](#-organization-distribution-setup) for details.

## Agent File Format

Each agent file uses **YAML front matter** for metadata:

```yaml
---
name: agent-name
description: Agent's purpose and expertise
model: Claude Opus 4.8
tools: ['search', 'read', 'edit']    # Optional: available tools
handoffs:
  - label: Handoff action label
    agent: target-agent-name
    prompt: Context passed to next agent
    send: true/false
---
```

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for comprehensive documentation on agent architecture, design principles, and usage guidelines.

## Quick Start

1. **Clone this repository** as your organization's `.github-private` repo
2. **Enable VS Code setting:** `github.copilot.chat.customAgents.showOrganizationAndEnterpriseAgents: true`
3. **Open Copilot Chat** and select an agent from the dropdown (e.g., `pch-helper`)
4. **Start with pch-helper** if unsure — it will guide you to the right agent

## Further Reading

- [GitHub Docs: Creating custom agents](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/create-custom-agents)
- [GitHub Docs: Preparing custom agents for your organization](https://docs.github.com/en/copilot/how-tos/administer-copilot/manage-for-organization/prepare-for-custom-agents)
- [VS Code Docs: Custom agents in VS Code](https://code.visualstudio.com/docs/copilot/customization/custom-agents)

