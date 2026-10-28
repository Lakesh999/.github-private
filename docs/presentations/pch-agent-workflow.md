---
marp: true
theme: default
paginate: true
backgroundColor: #1e1e1e
color: #d4d4d4
style: |
  /* Base styling */
  section {
    background-color: #1e1e1e;
    color: #d4d4d4;
    font-family: 'Segoe UI', sans-serif;
    font-size: 24px;
    padding: 40px 50px;
  }

  /* Headings */
  h1 { color: #007acc; font-size: 1.8em; margin-bottom: 0.3em; }
  h2 { color: #007acc; font-size: 1.4em; margin-bottom: 0.3em; }
  h3 { color: #4ec9b0; font-size: 1.1em; margin-bottom: 0.2em; }
  h4 { color: #d4d4d4; font-size: 1em; }

  /* Links */
  a { color: #3794ff; }

  /* Code blocks */
  code { background-color: #3c3c3c; color: #ce9178; padding: 2px 6px; border-radius: 3px; font-size: 0.85em; }
  pre { background-color: #2d2d30; padding: 0.8em; border-radius: 8px; font-size: 0.8em; border: 1px solid #3c3c3c; }
  pre code { background-color: transparent; color: #d4d4d4; padding: 0; }

  /* Tables */
  table { font-size: 0.8em; width: 100%; border-collapse: collapse; }
  th { background-color: #264f78; color: #ffffff; padding: 8px 12px; text-align: left; }
  td { background-color: #2d2d2d; padding: 6px 12px; border-bottom: 1px solid #454545; }

  /* Lists */
  ul, ol { margin: 0.3em 0; padding-left: 1.2em; }
  li { margin: 0.15em 0; line-height: 1.4; }

  /* Blockquotes */
  blockquote { background: #252526; border-left: 4px solid #007acc; padding: 15px 20px; margin: 0; border-radius: 0 4px 4px 0; }

  /* Two-column layout */
  .two-column { display: flex; gap: 30px; }
  .two-column > div { flex: 1; }

  /* Agent text colors */
  .agent-visionary { color: #4ec9b0; font-weight: 600; }
  .agent-researcher { color: #569cd6; font-weight: 600; }
  .agent-planner { color: #dcdcaa; font-weight: 600; }
  .agent-reviewer { color: #c586c0; font-weight: 600; }
  .agent-coder { color: #6a9955; font-weight: 600; }
  .agent-helper { color: #9cdcfe; font-weight: 600; }

  /* Agent badges */
  .agent-badge { display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 0.85em; font-weight: 600; background-color: #252526; border: 1px solid currentColor; }

  /* Mode comparison boxes */
  .mode-box { flex: 1; padding: 20px; background-color: #252526; border-radius: 8px; border: 2px solid #454545; }
  .mode-box.manual { border-color: #dcdcaa; }
  .mode-box.automatic { border-color: #6a9955; }
  .mode-box.manual h3 { color: #dcdcaa; }
  .mode-box.automatic h3 { color: #6a9955; }

  /* Title slide styling */
  section.lead { text-align: center; display: flex; flex-direction: column; justify-content: center; }
  section.lead h1 { font-size: 2.2em; }
  .subtitle { font-size: 1.1em; color: #007acc; margin-bottom: 1em; }

  /* Highlight and muted text */
  .highlight { color: #007acc; font-weight: 600; }
  .muted { color: #808080; font-size: 0.9em; }

  /* Mermaid diagram container */
  svg[id^="mermaid"] { max-height: 400px; }
  .mermaid { background: transparent !important; }
---

<!-- Mermaid diagram support -->
<script type="module">
import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
mermaid.initialize({
  startOnLoad: true,
  theme: 'dark',
  themeVariables: {
    primaryColor: '#264f78',
    primaryTextColor: '#ffffff',
    primaryBorderColor: '#007acc',
    lineColor: '#4ec9b0',
    secondaryColor: '#2d2d2d',
    tertiaryColor: '#1e1e1e'
  }
});
</script>

<!-- _class: lead -->
<!-- _paginate: false -->

# PCH Agent Workflow

<div class="subtitle">AI-Powered Software Development with GitHub Copilot</div>

**Vincent Miceli**
Point C Automation Team

---

# Agenda

* Copilot Overview
* Meet the Agents
* Workflow
* Examples
* Journey
* Getting Started

---

# About This Presentation

Built with **Marp** and VS Code Dark+ styling

---

<!-- Section 2: Copilot Overview -->

# What is GitHub Copilot?

Your **AI pair programmer** integrated directly into your IDE

* Code completion and suggestions
* Natural language to code translation
* Context-aware assistance
* Chat interface for interactive help

---

# Custom Agents in VS Code

* Extend Copilot with **specialized behaviors**
* Invoke with `@agent-name` syntax:

```markdown
@pch-planner Create a plan for adding user authentication
```

* Agents have specific expertise and workflows
* They maintain context and follow structured processes
* Hand off work to other agents when appropriate

---

<!-- Section 3: PCH Agents -->

# Meet the PCH Agents

A coordinated team of specialized AI assistants

* <span class="agent-visionary">@pch-visionary</span> — Transforms ideas into vision documents
* <span class="agent-researcher">@pch-researcher</span> — Creates research outlines and executes research
* <span class="agent-planner">@pch-planner</span> — Creates detailed implementation plans
* <span class="agent-reviewer">@pch-plan-reviewer</span> — Validates plans before implementation
* <span class="agent-coder">@pch-coder</span> — Implements code from approved plans
* <span class="agent-helper">@pch-helper</span> — Guides users through the workflow

---

# <span class="agent-visionary">pch-visionary</span>

### Role
Transforms raw ideas into structured vision documents

### Process
* 6-stage guided Q&A
* Combines business analyst + solution architect perspectives
* Asks clarifying questions to refine the concept

### Output
Creates structured documents in `/docs/vision/`

---

# <span class="agent-researcher">pch-researcher</span> (1/2)

### Role
Creates phased research outlines, then delegates execution

### Process
* Creates research outline first
* Breaks into manageable phases
* Executes one phase at a time

### Output
Creates research documents in `/docs/research/`

---

# <span class="agent-researcher">pch-researcher</span> (2/2)

## Execution Modes

<div class="two-column">
<div class="mode-box manual">

### Manual Mode
* One phase per session
* User control between phases
* Review points for adjustment

</div>
<div class="mode-box automatic">

### Automatic Mode
* Subagent orchestration
* Hands-free multi-phase execution
* Structured progress reports

</div>
</div>

---

# <span class="agent-planner">pch-planner</span> (1/2)

### Role
Creates highly detailed implementation plans

### Process
* Incremental Q&A approach
* One question at a time with options
* Builds section by section

### Handoff
Automatically hands off to <span class="agent-reviewer">@pch-plan-reviewer</span>

---

# <span class="agent-planner">pch-planner</span> (2/2)

## Plan Structure

* **Phases** — Logical groupings of work
* **Tasks** — Specific implementation steps
* **Acceptance Criteria** — How to verify completion
* **File Paths** — Exact locations for changes
* **Dependencies** — Prerequisites between tasks

### Output
`/docs/plans/`

---

# <span class="agent-reviewer">pch-plan-reviewer</span>

### Role
Validates plans before implementation

### Process
* Risk-based review depth
* Shallow review for simple plans
* Deep review for complex/risky changes

### Output
Marks plans as **"Ready for Implementation"**

---

# <span class="agent-coder">pch-coder</span> (1/2)

### Role
Implements code from approved plans

### Process
* Reads and follows the plan exactly
* Follows existing code patterns
* Production-ready code — no placeholders

### Output
Updates plan progress as tasks complete

---

# <span class="agent-coder">pch-coder</span> (2/2)

## Execution Modes

<div class="two-column">
<div class="mode-box manual">

### Manual Mode
* One phase per session
* User control between phases
* Review code before continuing

</div>
<div class="mode-box automatic">

### Automatic Mode
* Subagent orchestration
* Fresh context per phase
* Blocker handling options

</div>
</div>

---

# <span class="agent-helper">pch-helper</span>

### Role
Guides users through the agent workflow

### Capabilities
* Entry point for new users
* Explains which agent to use
* Can hand off based on your needs

### Best For
**Start here if you're not sure where to begin!**

---

# Agent Summary

| Agent | Purpose | Modes | Output |
|-------|---------|-------|--------|
| <span class="agent-visionary">visionary</span> | Ideas → Vision | — | /docs/vision/ |
| <span class="agent-researcher">researcher</span> | Research outlines | Manual / Auto | /docs/research/ |
| <span class="agent-planner">planner</span> | Implementation plans | — | /docs/plans/ |
| <span class="agent-reviewer">reviewer</span> | Plan validation | — | Approved plans |
| <span class="agent-coder">coder</span> | Code implementation | Manual / Auto | Production code |
| <span class="agent-helper">helper</span> | User guidance | — | Handoffs |

---

<!-- Section 4: Workflow -->

# Workflow Overview

<pre class="mermaid">
flowchart LR
    V[pch-visionary]:::visionary --> R[pch-researcher]:::researcher
    R --> PL[pch-planner]:::planner
    PL --> PR[pch-plan-reviewer]:::reviewer
    PR --> C[pch-coder]:::coder
    H[pch-helper]:::helper -.->|guidance| V
    H -.-> R
    H -.-> PL

    classDef visionary fill:#4ec9b0,stroke:#1e1e1e,color:#1e1e1e
    classDef researcher fill:#569cd6,stroke:#1e1e1e,color:#1e1e1e
    classDef planner fill:#dcdcaa,stroke:#1e1e1e,color:#1e1e1e
    classDef reviewer fill:#c586c0,stroke:#1e1e1e,color:#1e1e1e
    classDef coder fill:#6a9955,stroke:#1e1e1e,color:#1e1e1e
    classDef helper fill:#9cdcfe,stroke:#1e1e1e,color:#1e1e1e
</pre>

---

# Entry Points

| Scenario | Start With |
|----------|------------|
| New idea or feature concept | <span class="agent-visionary">@pch-visionary</span> |
| Need to explore or understand the codebase | <span class="agent-researcher">@pch-researcher</span> |
| Known feature ready for planning | <span class="agent-planner">@pch-planner</span> |
| Not sure where to begin | <span class="agent-helper">@pch-helper</span> |

---

# Multi-Session Design

### Phased Execution
Work is broken into manageable phases

### Context Window Management
Fresh sessions provide full context capacity

### Document-Based Persistence
Progress is saved to files between sessions

---

# Execution Modes

<div class="two-column">
<div class="mode-box manual">

### Manual Mode
* One phase per session
* Full user control
* Review before continuing
* **Best for:** Complex changes

</div>
<div class="mode-box automatic">

### Automatic Mode
* Subagent orchestration
* Hands-free execution
* Fresh context per phase
* **Best for:** Trusted plans

</div>
</div>

---

<!-- Section 5: Example Prompts -->

# See It In Action

<div class="subtitle">Example prompts for each agent</div>

* Each agent is invoked with the `@agent-name` syntax in VS Code chat
* Let's see how you might use each one...

---

# <span class="agent-visionary">@pch-visionary</span>

```markdown
@pch-visionary I have an idea for a notification system that alerts users 
when their subscription is about to expire. Can you help me develop this 
into a proper vision document?
```

* The visionary will ask clarifying questions and build a structured vision document

---

# <span class="agent-researcher">@pch-researcher</span>

```markdown
@pch-researcher Research how authentication is implemented in this 
codebase. I need to understand the patterns before adding OAuth support.
```

* The researcher will create a research outline and explore the codebase systematically

---

# <span class="agent-planner">@pch-planner</span>

```markdown
@pch-planner Create an implementation plan for adding a user dashboard 
feature. The vision document is at docs/vision/002-user-dashboard.md
```

* The planner will ask questions and build a detailed, phased implementation plan

---

# <span class="agent-reviewer">@pch-plan-reviewer</span>

```markdown
@pch-plan-reviewer Review the implementation plan at 
docs/plans/003-user-dashboard.md and check for any issues before 
we start coding.
```

* The reviewer validates the plan and marks it ready for implementation

---

# <span class="agent-coder">@pch-coder</span>

```markdown
@pch-coder Implement Phase 1 of the plan at 
docs/plans/003-user-dashboard.md. Use automatic mode.
```

* The coder implements production-ready code following the approved plan

---

# <span class="agent-helper">@pch-helper</span>

```markdown
@pch-helper I'm new to this workflow. I have a feature idea but 
I'm not sure which agent to use first. Can you guide me?
```

* The helper explains the workflow and recommends the right starting point

---

<!-- Section 6: Journey -->

# Your Development Journey

<div class="subtitle">From idea to production with PCH agents</div>

* Imagine you're building a SaaS application from scratch...
* You have ambitious goals, but how do you stay organized and consistent?
* Let's see how the PCH agents help you through each stage.

---

# MVP Phase

<div class="subtitle">The first week of development</div>

* **Day 1-2: Vision** — Start with <span class="agent-visionary">@pch-visionary</span> to capture your idea
* **Day 3-5: Research** — <span class="agent-researcher">@pch-researcher</span> explores patterns and best practices
* **Day 6-10: First Feature** — <span class="agent-planner">@pch-planner</span> → <span class="agent-reviewer">@pch-plan-reviewer</span> → <span class="agent-coder">@pch-coder</span>

---

# Iteration Phase

<div class="subtitle">Release 1.1, 1.2, 1.3...</div>

* 🔄 New feature requests come in
* Flow: <span class="agent-planner">@pch-planner</span> → <span class="agent-reviewer">@pch-plan-reviewer</span> → <span class="agent-coder">@pch-coder</span>
* Benefits:
  * Each feature follows the same reliable workflow
  * Knowledge accumulates in `/docs/`
  * Consistent quality across the codebase

---

# Production

<div class="subtitle">Six months later...</div>

* 🎉 Full application running in production
* 📚 Complete documentation trail
* 📋 Every decision recorded in plans and research
* 👥 New team members can understand the history
* That's the power of structured, agent-assisted development.

---

<!-- Section 7: Closing -->

# Getting Started

1) **Install the agents** — Add the PCH agent prompts to your VS Code workspace
2) **Start with pch-helper** — Ask `@pch-helper` to guide you through the workflow
3) **Follow the workflow** — Vision → Research → Plan → Review → Code

---

# Questions?

<div class="subtitle">Let's discuss!</div>

---

<!-- _class: lead -->

# Thank You

**Vincent Miceli**
Point C Automation Team
