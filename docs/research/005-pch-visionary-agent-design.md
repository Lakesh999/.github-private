# Research: pch-visionary Agent Design

## Document Information

| Field | Value |
|-------|-------|
| Document | 005-pch-visionary-agent-design.md |
| Created | 2026-02-01 |
| Status | ✅ Complete |
| Current Phase | 6 of 6 (All Phases Complete) |

## Overview

This research document provides a comprehensive design for `pch-visionary`, a new agent that serves as the **first agent** in the PCH workflow. The agent combines business analyst and solution architect capabilities to transform raw ideas into structured vision documents through guided Q&A.

### Key Design Decisions

1. **Follows pch-planner's incremental pattern** — NOT subagent orchestration like pch-researcher/pch-coder. Vision capture requires continuous conversational context that subagents would break.

2. **Six-stage question flow** — Problem Space → Stakeholders → Goals → Requirements → Architecture → Phasing. Each stage has completion criteria and templates for different question types (Elicitation, Refinement, Validation).

3. **Vision documents in `/docs/vision/`** — 13-section template blending problem space (BA perspective) and solution space (architect perspective) with explicit Research Topics section for pch-researcher handoff.

4. **Three handoff paths** — Self-continuation (session recovery), pch-researcher (standard flow), pch-planner (skip research for simple visions). All use `send: false` to allow user review.

5. **Standards integration via pch-standards-space** — Organizational alignment during vision capture, especially in Architecture Vision stage.

### Implementation Summary

| Aspect | Recommendation |
|--------|----------------|
| Agent file size | ~1,100 lines |
| Execution mode | Single-session Q&A (no Manual/Automatic selection) |
| Subagent usage | None (Phase 4 finding) |
| Primary pattern | Incremental document building (like pch-planner) |
| pch-helper updates | Critical path (~100 lines) |
| copilot-instructions.md | Minor updates (~20 lines) |
| Rollout approach | 4 phases over 3+ weeks |

### Research Topics Generated

This research identified the following areas for implementation:
- Complete 15-section agent outline with content specifications
- Embedded vision document template (13 sections)
- Four-phase implementation plan with task breakdowns
- Five test scenarios with validation checklists
- Phased rollout plan with success metrics

---

## Research Objectives

Design a new `pch-visionary` agent that serves as the **first agent** in the PCH workflow, helping users transform raw ideas into structured vision documents. The agent should combine business analyst and solution architect capabilities to guide users through idea refinement before research and implementation.

### Key Questions to Answer

1. **Workflow Integration:** How should pch-visionary integrate with the existing agent chain (before pch-researcher)?
2. **Question Strategy:** What questioning framework should the agent use to extract requirements, architecture, and design intent?
3. **Document Structure:** What should the vision document format look like to support downstream research and planning?
4. **Phasing Strategy:** How should the agent help users break solutions into logical product phases?
5. **Subagent Usage:** Can subagents gather phase-specific information while preserving main agent context?
6. **Handoff Design:** What information should be passed to pch-researcher when the vision document is complete?

---

## Research Phases

| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 1 | Existing Agent Patterns Deep Dive | ✅ Complete | 2026-02-01 |
| 2 | Vision Document Structure Design | ✅ Complete | 2026-02-01 |
| 3 | Business Analyst & Solution Architect Question Frameworks | ✅ Complete | 2026-02-01 |
| 4 | Subagent Strategy for Phase Information Gathering | ✅ Complete | 2026-02-01 |
| 5 | Workflow Integration & Handoffs | ✅ Complete | 2026-02-01 |
| 6 | Implementation Template & Recommendations | ✅ Complete | 2026-02-01 |

---

## Phase 1: Existing Agent Patterns Deep Dive

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-01
**Scope:** Analyze pch-researcher and pch-planner to understand agent structure patterns that pch-visionary should follow

### Key Discoveries

- All agents use consistent YAML frontmatter with name, description, model (Claude Opus 4.5), and handoffs array
- Three agents (researcher, planner, reviewer) include mcp-servers config for standards integration via copilot_spaces
- "Documentation only — no code changes" constraint applies to all agents except pch-coder
- One-question-at-a-time with immediate documentation is a universal pattern for interactive agents
- Decision trees with ASCII boxes are used for session initialization logic
- Document numbering uses zero-padded 3-digit format ({NNN}-{description}.md)
- Status tracking uses consistent emoji set: ⏳ Not Started, 🔄 In Progress, ✅ Complete, ❌ Blocked
- Both pch-researcher and pch-coder support dual execution modes (Manual/Automatic)
- Handoff Information section is standard at document end
- Critical rules use visual emphasis: 🚨 emoji headers, ASCII box diagrams, violation tables
- Question format follows Options (A/B/C/D) → Recommendation → Rationale structure

### Detailed Findings

#### 1. YAML Frontmatter Patterns

All agents follow a consistent YAML frontmatter structure:

```yaml
---
name: agent-name
description: Brief description of the agent's purpose and expertise
model: Claude Opus 4.5
handoffs:
  - label: Human-readable label for the handoff action
    agent: target-agent-name
    prompt: The prompt that will be sent to the target agent
    send: true/false  # Whether to auto-send or prepare handoff
mcp-servers:  # Optional - used by researcher, planner, reviewer
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
---
```

**Handoff Pattern Analysis:**

| Agent | Handoff Target | send: value |
|-------|----------------|-------------|
| pch-researcher | pch-researcher (continue) | false |
| pch-planner | pch-plan-reviewer | true (auto) |
| pch-plan-reviewer | pch-coder | true (auto) |
| pch-coder | pch-planner | false |
| pch-helper | All agents | false |

**Pattern for pch-visionary:** Should handoff to `pch-researcher` with `send: true` for automatic continuation.

#### 2. Core Responsibilities Structure

All agents use priority-ordered bulleted lists (5-7 items):

- **pch-researcher:** Create outline → STOP → Mode selection → Execute phases → Synthesize
- **pch-planner:** Create doc first → Ask questions ONE at a time → Update after each → Log decisions → Generate phases
- **pch-coder:** Read plan → Execute ONE phase → Identify next → Execute tasks → Update progress → Handle errors → End session
- **pch-plan-reviewer:** Review quality → Identify gaps → Ask ONE question → Document decision → Update plan → Mark ready

**Pattern for pch-visionary:** 5-7 responsibilities including: create vision document, ask questions one at a time, document answers immediately, guide phasing, synthesize vision, handoff to researcher.

#### 3. Session Initialization & Mode Selection

Decision tree pattern with ASCII boxes:

```
User message received
        │
        ▼
┌───────────────────────────────────┐
│ Does user reference an existing   │
│ document (path or attached)?      │
└───────────────────────────────────┘
        │
   NO   │   YES
   ▼    │    ▼
   NEW  │  CONTINUATION
```

Mode selection applies to pch-researcher and pch-coder for multi-phase execution.

**Pattern for pch-visionary:** May not need mode selection since vision documents are typically single-session Q&A workflows.

#### 4. Critical Rules Patterns

Visual emphasis techniques:
1. **🚨 Emoji Headers** — `## 🚨 THE ONE RULE THAT MATTERS 🚨`
2. **ASCII Box Diagrams** — For workflow visualization
3. **Violation Tables** — "What You Must Never Do" format
4. **⚠️ WARNING blocks** — For inline critical notes

Common constraints:
| Rule | researcher | planner | coder | reviewer |
|------|------------|---------|-------|----------|
| Documentation only | ✅ | ✅ | ❌ | ✅ |
| One question at a time | ✅ | ✅ | N/A | ✅ |
| Update after each answer | ✅ | ✅ | ✅ | ✅ |
| Save before stopping | ✅ | ✅ | ✅ | ✅ |

#### 5. Document Creation Workflows

**Skeleton → Incremental Build Pattern (pch-planner):**
1. Create document skeleton with all headers
2. Ask ONE clarifying question
3. Wait for response → Update document
4. Repeat until complete
5. Holistic review
6. Handoff

**Pattern for pch-visionary:** Should follow skeleton → incremental build pattern. Vision documents created in `/docs/vision/` with `{NNN}-{description}.md` format.

#### 6. Document Output Structure Comparison

**Research Document:**
- Document Information table
- Overview (completed after phases)
- Research Objectives / Key Questions
- Research Phases table
- Phase sections with Findings/Discoveries
- Referenced Standards
- Conclusions
- Handoff Information

**Plan Document:**
- Document Information table
- Version History
- Planning Session Log
- Holistic Review Summary
- Overview and Objectives
- Requirements (Functional/Non-Functional/Out of Scope)
- Technical Design
- Dependencies and Prerequisites
- Risk Assessment
- Execution Plan with Phases
- Referenced Standards
- Handoff Information

**Pattern for pch-visionary:** Vision documents should blend both:
- Document Information table
- Vision Session Log (tracking answers)
- Overview/Summary
- Requirements section
- Architecture Overview
- Product Phases (unique to vision)
- Referenced Standards
- Handoff Information

#### 7. Question Format Pattern

```markdown
### [Question Topic]

**Question:** [Clear, specific question]

**Options:**

A) **[Option Name]**
   [Description, trade-offs]

B) **[Option Name]**
   [Description, trade-offs]

**Recommendation:** [Letter] — [Option Name]

**Rationale:** [Why recommended]
```

### Relevant Files/Resources

- [pch-researcher.md](agents/pch-researcher.md) — Phased research, mode selection, subagent patterns
- [pch-planner.md](agents/pch-planner.md) — Incremental document creation, question format
- [pch-coder.md](agents/pch-coder.md) — Phase execution, mode selection patterns
- [pch-plan-reviewer.md](agents/pch-plan-reviewer.md) — Review process, immediate documentation
- [pch-helper.md](agents/pch-helper.md) — Workflow guidance, agent descriptions

### Gaps & Limitations

None identified.

### Assumptions

- pch-visionary will follow the incremental build pattern (like pch-planner) rather than phased outline pattern (like pch-researcher)
- Vision documents will be stored in `/docs/vision/` following existing conventions

---

## Phase 2: Vision Document Structure Design

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-01
**Scope:** Design the structure of documents produced by pch-visionary under `/docs/vision/`

### Key Discoveries

- Vision documents blend problem space (BA) and solution space (architect) into a single cohesive document
- Product phasing requires explicit "Deferred" items to prevent MVP scope creep
- Research Topics section serves as the explicit handoff interface to pch-researcher
- Vision Reference section should be added to research documents for traceability
- Section Completion checklist enables incremental progress tracking during Q&A
- Architecture Vision captures intent at appropriate abstraction (not detailed design)
- Non-Goals section is critical for scope management
- Success Metrics enable outcome validation after implementation
- Constraints section (technical, business, regulatory, resource) shapes architecture early

### Detailed Findings

#### Complete Vision Document Template

```markdown
# Vision: [Solution/Feature Title]

## Document Information

| Field | Value |
|-------|-------|
| Document | {NNN}-{description}.md |
| Created | [Date] |
| Status | 🔄 In Progress / ✅ Complete |
| Owner | pch-visionary |
| Version | v1.0 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | [Date] | pch-visionary | Initial vision capture |

---

## Vision Session Log

| Session | Date | Topic Area | Question | Answer | Rationale |
|---------|------|------------|----------|--------|-----------|

---

## Executive Summary

> **Note:** Populated after all vision questions are complete.

**One-Sentence Vision:** [What we're building and why]
**Target Outcome:** [What success looks like]
**Key Constraints:** [Top 3 constraints shaping the solution]

---

## Problem Statement

### Business Context
### Current State
### Desired Future State
### Success Metrics

| Metric | Current Value | Target Value | Measurement Method |

---

## Stakeholders & Users

### Primary Stakeholders

| Stakeholder | Role | Interest | Influence |

### User Personas

#### [Persona Name]
- **Role:** / **Goals:** / **Pain Points:** / **Technical Proficiency:**

---

## Goals & Objectives

### Primary Goals

| ID | Goal | Priority | Success Indicator |
|----|------|----------|-------------------|
| G-1 | [Goal] | Must Have | [Indicator] |

### Non-Goals (Explicitly Out of Scope)
- [Excluded items]

---

## Requirements Overview

### Functional Requirements (High-Level)

| ID | Requirement | Priority | Product Phase |
|----|-------------|----------|---------------|
| FR-1 | [Capability] | Must Have | Phase 1 (MVP) |

### Non-Functional Requirements

| ID | Requirement | Priority | Constraint Type |
|----|-------------|----------|-----------------|
| NFR-1 | [Quality] | Must Have | Performance |

### Constraints

| Constraint Type | Description | Impact |
|-----------------|-------------|--------|
| Technical | [Constraint] | [Impact] |
| Business | [Constraint] | [Impact] |

---

## Architecture Vision

### Solution Overview
### Architecture Style

| Aspect | Choice | Rationale |
|--------|--------|-----------|
| Overall Pattern | [e.g., Microservices] | [Why] |
| Frontend | [e.g., SPA] | [Why] |
| Backend | [e.g., REST API] | [Why] |
| Data Storage | [e.g., Relational] | [Why] |

### Key Technology Decisions

| Decision Area | Choice | Alternatives Considered | Rationale |

### Integration Points

| System | Integration Type | Purpose | Criticality |

### Security Considerations

| Concern | Approach | Notes |

---

## Product Phasing Strategy

### Phase Overview

| Phase | Name | Scope Summary | Target Date | Status |
|-------|------|---------------|-------------|--------|
| 1 | MVP | [Core functionality] | [Date] | ⏳ Not Started |

### Phase 1: MVP

**Goal:** [What Phase 1 achieves]
**Included Capabilities:**
**Excluded from MVP (Deferred):**
**Definition of Done:**

### Future Phases (Roadmap)

| Phase | Tentative Scope | Dependencies |

---

## Research Topics

| Topic | Priority | Key Questions | Target Phase |
|-------|----------|---------------|--------------|

### Open Questions

| Question | Impact if Unresolved | Research Needed? |

---

## Risk Assessment (Vision-Level)

| Risk | Likelihood | Impact | Mitigation Strategy |

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |

---

## Handoff Information

**Created By:** pch-visionary
**Created Date:** [Date]
**Status:** ✅ Complete — Ready for Research
**Next Agent:** pch-researcher
**Vision Document Location:** `/docs/vision/{NNN}-{description}.md`
```

#### Linking Vision to Research Documents

Traceability model from vision → research → plan:

```
Vision Document (/docs/vision/)
    │
    ├── Research Topics section → pch-researcher focus
    ├── Requirements → Referenced in Research Findings
    └── Product Phases → Scope boundaries
           │
           ▼
Research Document (/docs/research/)
    │
    ├── Vision Reference section (NEW)
    │   - Vision Document path
    │   - Target Phase
    │   - Key Requirements (FR-1, NFR-2)
    └── Topics from Vision
           │
           ▼
Plan Document (/docs/plans/)
    └── Vision & Research Foundation section
```

#### Vision Reference Section (for Research Docs)

```markdown
## Vision Reference

| Field | Value |
|-------|-------|
| Vision Document | `/docs/vision/001-feature-name.md` |
| Vision Title | [Feature Name] |
| Target Phase | [Which product phase] |
| Key Requirements | FR-1, FR-3, NFR-2 |
```

### Relevant Files/Resources

- [pch-planner.md](agents/pch-planner.md) — Plan document structure patterns
- [pch-researcher.md](agents/pch-researcher.md) — Research document structure patterns
- Existing plan documents — Examples of tables and sections

### Gaps & Limitations

None identified.

### Assumptions

- Vision documents are primarily single-session Q&A workflows
- Research documents should add Vision Reference section for traceability
- MoSCoW prioritization is appropriate for vision-level requirements

---

## Phase 3: Business Analyst & Solution Architect Question Frameworks

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-01
**Scope:** Research questioning strategies for extracting vision details from users

### Research Tasks

- [x] Research business analysis questioning techniques (5 Whys, INVEST, MoSCoW)
- [x] Research solution architecture discovery frameworks (C4 model, 4+1 view)
- [x] Define question categories (problem space, solution space, constraints, stakeholders)
- [x] Design one-question-at-a-time workflow (like pch-planner)
- [x] Create recommendation format (when agent should suggest approaches)
- [x] Define clarification vs. suggestion balance
- [x] Document decision logging format for user answers

### Key Discoveries

- **Question Flow Architecture**: Six-stage progression from problem space → stakeholders → goals → requirements → architecture → phasing, enabling systematic vision refinement
- **Adaptive Question Types**: Three question modes (Elicitation, Refinement, Validation) applied contextually based on user input clarity
- **Multi-Option Format with Recommendations**: Following pch-planner pattern—3-5 options with letter identifiers, recommendation, and context-specific rationale
- **Clarification vs. Suggestion Matrix**: Clear rules for when to ask open questions vs. provide structured options based on topic ambiguity and user expertise
- **BA Technique Integration**: 5 Whys for problem root cause, INVEST for requirement quality, MoSCoW for prioritization—applied at appropriate question stages
- **Architecture Discovery Alignment**: C4 model influences question hierarchy (context → solution → components), 4+1 view shapes non-functional considerations
- **Incremental Documentation Pattern**: Each answer triggers immediate vision document update before proceeding to next question

### Detailed Findings

#### 1. Questioning Framework Architecture

The pch-visionary agent uses a **six-stage question flow** designed to transform vague ideas into structured vision documents:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     VISION QUESTIONING FLOW                              │
│                                                                          │
│  Stage 1: Problem Space ──► Stage 2: Stakeholders ──► Stage 3: Goals    │
│      │                          │                          │             │
│      ▼                          ▼                          ▼             │
│  "What problem?"            "For whom?"              "What success?"     │
│                                                                          │
│  Stage 4: Requirements ──► Stage 5: Architecture ──► Stage 6: Phasing   │
│      │                          │                          │             │
│      ▼                          ▼                          ▼             │
│  "What capabilities?"      "How built?"             "What order?"        │
└─────────────────────────────────────────────────────────────────────────┘
```

**Stage Completion Criteria:**

| Stage | Section Populated | Minimum Questions | Exit Criteria |
|-------|-------------------|-------------------|---------------|
| 1. Problem Space | Problem Statement | 2-4 | Business context + success metrics defined |
| 2. Stakeholders | Stakeholders & Users | 1-3 | Primary users and stakeholders identified |
| 3. Goals | Goals & Objectives | 2-3 | At least 2 prioritized goals + non-goals |
| 4. Requirements | Requirements Overview | 3-6 | Core FRs + NFRs + constraints captured |
| 5. Architecture | Architecture Vision | 2-4 | Solution style + key technology decisions |
| 6. Phasing | Product Phasing Strategy | 2-4 | MVP scope + deferred items explicit |

#### 2. Question Types and When to Use Each

Three primary question types, applied based on context:

##### Type A: Elicitation Questions (Open-Ended)

**Purpose:** Extract information the user hasn't considered or articulated
**Format:** Open question → Listen → Follow-up if needed
**When to Use:**
- Initial exploration of a topic area
- User provides vague or incomplete input
- Need to understand user's mental model

**Template:**
```
### [Topic Area]

**Question:** [Open-ended question to extract information]

*Take your time to describe this fully. I'll help structure your thoughts after.*
```

**Examples:**
- "What problem are you trying to solve?"
- "Who will be using this solution?"
- "What does success look like for this initiative?"

##### Type B: Refinement Questions (Multiple Choice)

**Purpose:** Help user choose between well-defined options
**Format:** Options (A/B/C/D) → Recommendation → Rationale (pch-planner pattern)
**When to Use:**
- Topic has established patterns or approaches
- User needs guidance on trade-offs
- Narrowing down from broad concepts to specific choices

**Template:**
```
### [Topic Area]

**Question:** [Specific question with clear decision point]

**Options:**

A) **[Option Name]**
   [Description, trade-offs, when appropriate]

B) **[Option Name]**
   [Description, trade-offs, when appropriate]

C) **[Option Name]**
   [Description, trade-offs, when appropriate]

---

**Recommendation:** [Letter] — [Option Name]

**Rationale:** [Why this option fits based on what user has shared so far]
```

##### Type C: Validation Questions (Confirm/Refine)

**Purpose:** Verify understanding or get approval before proceeding
**Format:** Summary statement → Confirm or correct
**When to Use:**
- After synthesizing multiple user inputs
- Before moving to next stage
- Capturing implicit requirements explicitly

**Template:**
```
### Confirming [Topic]

Based on what you've shared, here's my understanding:

> [Synthesized summary of user's input]

**Is this accurate?** If not, what should I adjust?

1. ✅ Yes, that's correct — proceed
2. ✏️ Partially correct — [specify what to change]
3. ❌ No, let me clarify — [provide correction]
```

#### 3. Clarification vs. Suggestion Decision Matrix

When should pch-visionary ask open questions vs. provide structured options?

| Scenario | User Input Quality | Topic Type | Approach |
|----------|-------------------|------------|----------|
| **Blank slate** | None/vague | Any | **Elicitation** — Open question to understand intent |
| **Partial info** | Some context given | Technical | **Refinement** — Options with recommendation |
| **Partial info** | Some context given | Business | **Elicitation** — Open follow-up to understand nuance |
| **Clear direction** | Specific but incomplete | Any | **Validation** — Confirm + fill gaps |
| **Contradictory** | Conflicting statements | Any | **Clarification** — Highlight conflict, ask to resolve |
| **Expert user** | Detailed and clear | Any | **Validation** — Confirm and move quickly |

**Decision Logic for Each Question:**

```
┌─────────────────────────────────────────────────────────────┐
│ Does the topic have established patterns/options?          │
└─────────────────────────────────────────────────────────────┘
                    │
         NO        │         YES
         ▼         │          ▼
┌─────────────┐    │   ┌──────────────────────────────────┐
│ Elicitation │    │   │ Has user provided enough context │
│ (Open Q)    │    │   │ to make a recommendation?        │
└─────────────┘    │   └──────────────────────────────────┘
                   │              │
                   │    NO        │        YES
                   │    ▼         │         ▼
                   │ ┌─────────┐  │  ┌─────────────────┐
                   │ │ Elicit  │  │  │ Refinement with │
                   │ │ context │  │  │ recommendation  │
                   │ │ first   │  │  └─────────────────┘
                   │ └─────────┘  │
```

#### 4. Business Analysis Techniques Integration

##### 5 Whys — Root Cause Discovery

**Application Stage:** Problem Space (Stage 1)
**Purpose:** Ensure the vision addresses root causes, not symptoms

**Integrated Question Pattern:**

```
### Understanding the Root Problem

**Question:** You mentioned [surface problem]. Let's dig deeper.
*Why* is this a problem? What happens because of it?

[User responds]

**Follow-up:** And why does [that consequence] matter?

[Continue until root cause is clear, typically 3-5 iterations]

---

**Root Cause Summary:**
Based on our discussion, the underlying problem is:
> [Synthesized root cause]

Does this capture the core issue?
```

##### INVEST Criteria — Requirement Quality

**Application Stage:** Requirements (Stage 4)
**Purpose:** Ensure requirements are well-formed for implementation

| Criterion | Validation Question |
|-----------|---------------------|
| **I**ndependent | "Can this be built and delivered separately from other requirements?" |
| **N**egotiable | "Is the 'how' flexible, or is there a specific implementation required?" |
| **V**aluable | "What user/business value does this provide?" |
| **E**stimable | "Is this scoped clearly enough to estimate effort?" |
| **S**mall | "Can this be completed within a single development phase?" |
| **T**estable | "How will you know when this is working correctly?" |

**Integrated Question Pattern:**

```
### Validating Requirement: [FR-X]

Let me check this requirement is well-formed:

**Current statement:** [Requirement as captured]

**Validation:**

1. **Value:** What problem does this solve for users?
2. **Testability:** How will you verify this works?
3. **Independence:** Does this depend on other requirements being done first?

[Based on answers, refine the requirement statement]
```

##### MoSCoW — Priority Classification

**Application Stage:** Goals (Stage 3) and Requirements (Stage 4)
**Purpose:** Establish clear priorities for phasing decisions

**Integrated Question Pattern:**

```
### Prioritizing [Requirement/Goal]

**Question:** How critical is [item] to the initial release?

**Options:**

A) **Must Have**
   Essential for launch. Without this, the solution doesn't solve the core problem.
   *Cannot be deferred to later phases.*

B) **Should Have**
   Important but not blocking. Significant value, but workarounds exist.
   *Candidate for Phase 1 if time permits, otherwise Phase 2.*

C) **Could Have**
   Desirable enhancement. Nice to have but not critical to core value.
   *Defer to Phase 2 or later.*

D) **Won't Have (this release)**
   Explicitly excluded from current vision scope.
   *Documented as out of scope.*

---

**Recommendation:** [Letter] based on [rationale from user's earlier context]

**Rationale:** [Why this priority level fits]
```

#### 5. Architecture Discovery Integration

##### C4 Model Influence — Question Hierarchy

The C4 model (Context, Containers, Components, Code) informs question abstraction levels:

| C4 Level | Vision Question Focus | Example Questions |
|----------|----------------------|-------------------|
| **Context** | System boundaries, users, external systems | "What systems will this integrate with?" |
| **Containers** | High-level technology choices, deployment | "Web app, mobile, API, or combination?" |
| **Components** | Major functional areas (not detailed design) | "What are the major capability areas?" |
| **Code** | *Not covered* — too detailed for vision | Deferred to pch-planner |

**Architecture Stage Question Flow:**

```
Context Level (Stage 5a)
├── "What systems does this need to integrate with?"
├── "Is this standalone or part of a larger ecosystem?"
└── "What are the system boundaries?"

Container Level (Stage 5b)
├── "What type of solution is this? (Web/Mobile/API/Desktop)"
├── "Where will this be hosted? (Cloud/On-prem/Hybrid)"
└── "What data stores are needed?"

Component Level (Stage 5c)
├── "What are the major functional modules?"
├── "What cross-cutting concerns matter? (Auth, logging, etc.)"
└── "What's the security model?"
```

##### 4+1 View Influence — Non-Functional Considerations

| View | Vision Relevance | Questions Triggered |
|------|------------------|---------------------|
| **Logical** | Domain concepts | "What are the core entities/concepts?" |
| **Process** | Concurrency, workflows | "Are there async processes or workflows?" |
| **Development** | Team structure, modularity | "Is this single team or multi-team?" |
| **Physical** | Deployment, infrastructure | "Any infrastructure constraints?" |
| **Scenarios** | Use cases | "Walk me through a typical user journey" |

#### 6. Stage-by-Stage Question Templates

##### Stage 1: Problem Space

**Entry Point — Initial Elicitation:**

```
### What Problem Are We Solving?

Let's start with the core problem.

**Question:** In a few sentences, describe the problem or opportunity 
you're trying to address. What's not working today, or what new 
capability do you need?

*Don't worry about solutions yet — just focus on the problem.*
```

**Follow-up — Business Context:**

```
### Business Context

**Question:** Help me understand the business context:

1. **Who is impacted** by this problem today?
2. **What's the cost** of not solving it? (time, money, frustration)
3. **Why now?** What's driving the urgency to address this?
```

**Follow-up — Success Metrics:**

```
### Defining Success

**Question:** How will you know if this initiative is successful?

**Options:**

A) **Quantitative metrics**
   Specific numbers: reduce time by X%, increase conversion by Y%, etc.
   *Measurable and objective*

B) **Qualitative outcomes**
   User satisfaction, stakeholder approval, capability unlocked.
   *Important but harder to measure*

C) **Milestone-based**
   Success = shipping specific capabilities by target dates.
   *Delivery-focused*

D) **Combination**
   Mix of quantitative and qualitative measures.
   *Most comprehensive but requires more definition*

---

**Recommendation:** D — Combination

**Rationale:** Effective visions typically need both measurable outcomes 
and qualitative goals to fully capture success.
```

##### Stage 2: Stakeholders & Users

**Primary Users:**

```
### Who Will Use This?

**Question:** Who are the primary users of this solution?

For each user type, I'll want to understand:
- Their role and what they're trying to accomplish
- Their technical proficiency
- Their biggest pain points

Let's start with the **most important user type**. Describe them.
```

**Stakeholder Mapping:**

```
### Key Stakeholders

Beyond end users, who else has a stake in this solution?

**Question:** Who are the key stakeholders?

**Typical stakeholder types to consider:**

| Stakeholder Type | Questions to Answer |
|------------------|---------------------|
| **Sponsor** | Who's funding/approving this? What do they care about? |
| **Subject Matter Experts** | Who knows the domain deeply? |
| **Technical Teams** | Who will build and maintain this? |
| **Downstream Systems** | Who depends on this data/functionality? |

Who are the 2-3 most important stakeholders I should know about?
```

##### Stage 3: Goals & Objectives

**Goal Elicitation:**

```
### Primary Goals

**Question:** What are the top 3 goals for this initiative?

For each goal, try to state it as:
> "Enable [user type] to [accomplish what] so that [benefit]."

Start with Goal #1 — the most important outcome.
```

**Non-Goals:**

```
### Defining Non-Goals

**Question:** What is explicitly **out of scope** for this vision?

Non-goals are important because they:
- Prevent scope creep during implementation
- Set stakeholder expectations
- Focus the solution on what matters most

What should we explicitly exclude?

**Common non-goals to consider:**
- Support for [specific platforms/browsers]
- Integration with [specific system]
- [Specific user type] support
- Advanced features like [examples]
```

##### Stage 4: Requirements Overview

**Functional Requirements:**

```
### Core Capabilities

Based on your goals, let's define the key capabilities.

**Question:** What must the solution be able to **do**?

List the essential functions. For each, I'll ask about priority.

*Example format: "Users can [action] so that [benefit]"*
```

**Non-Functional Requirements:**

```
### Quality Attributes

**Question:** What quality attributes matter for this solution?

**Options (select all that apply):**

A) **Performance**
   Response time requirements, throughput needs

B) **Scalability**
   Expected user growth, data volume growth

C) **Security**
   Compliance requirements, data sensitivity

D) **Reliability**
   Uptime requirements, disaster recovery needs

E) **Usability**
   Accessibility requirements, user experience standards

F) **Maintainability**
   Team capabilities, long-term maintenance considerations

---

Which are **most critical** for this solution? (Pick top 2-3)
```

**Constraints:**

```
### Constraints

**Question:** What constraints will shape this solution?

**Constraint types to consider:**

| Type | Example Questions |
|------|-------------------|
| **Technical** | Must use specific technology? Integrate with legacy system? |
| **Business** | Budget limits? Timeline requirements? |
| **Regulatory** | Compliance requirements (GDPR, HIPAA, SOC2)? |
| **Resource** | Team size? Skill limitations? |

What constraints should I be aware of?
```

##### Stage 5: Architecture Vision

**Solution Style:**

```
### Solution Approach

**Question:** What type of solution are we building?

**Options:**

A) **Web Application**
   Browser-based application accessible via URL.
   *Best for: Cross-platform access, easy updates, no installation*

B) **Mobile Application**
   Native or hybrid mobile app (iOS/Android).
   *Best for: Offline capability, device features, app store presence*

C) **API / Backend Service**
   Backend services consumed by other applications.
   *Best for: Integration, data services, microservices*

D) **Desktop Application**
   Installed application for Windows/Mac/Linux.
   *Best for: Heavy processing, offline-first, specialized hardware*

E) **Combination / Hybrid**
   Multiple client types sharing backend services.
   *Best for: Multi-platform requirements*

---

**Recommendation:** [Based on requirements gathered so far]

**Rationale:** [Why this fits]
```

**Technology Decisions:**

```
### Key Technology Decisions

**Question:** Are there any technology decisions already made or required?

**Areas to consider:**

| Decision Area | Question |
|---------------|----------|
| **Platform** | Cloud provider preference? On-premises requirement? |
| **Languages** | Any language requirements or preferences? |
| **Frameworks** | Existing frameworks to leverage? |
| **Data Storage** | Relational, NoSQL, or specific database required? |
| **Integration** | Specific APIs or systems to integrate with? |

What's already decided vs. open for recommendation?
```

##### Stage 6: Product Phasing

**MVP Definition:**

```
### Defining the MVP

**Question:** What is the **minimum** set of capabilities needed for initial release?

The MVP should:
- Solve the core problem (even if not perfectly)
- Deliver value to primary users
- Be achievable within a reasonable timeline

From the requirements we discussed, which are **essential for Phase 1**?
```

**Deferred Items:**

```
### What Gets Deferred?

**Question:** What capabilities will be explicitly **deferred** to later phases?

These are important features that won't be in the MVP.

**Deferred Item Template:**

| Capability | Why Deferred | Target Phase |
|------------|--------------|--------------|
| [Feature] | [Not critical for initial value] | Phase 2 |

What should be on the deferred list?
```

#### 7. Decision Logging Format

Every user answer triggers an immediate update to the Vision Session Log:

```markdown
## Vision Session Log

| # | Date | Stage | Topic | Question Summary | Answer | Impact |
|---|------|-------|-------|------------------|--------|--------|
| 1 | 2026-02-01 | Problem Space | Core Problem | What problem are we solving? | [User's answer summarized] | → Problem Statement |
| 2 | 2026-02-01 | Problem Space | Success Metrics | How will you measure success? | Chose Option D (Combination) | → Success Metrics table |
| 3 | 2026-02-01 | Stakeholders | Primary Users | Who will use this? | Product managers, developers | → User Personas |
```

**Logging Rules:**

1. **Log BEFORE proceeding** — Add the entry immediately after user responds
2. **Summarize concisely** — Capture essence, not full verbatim answer
3. **Track impact** — Note which document section is updated
4. **Sequential numbering** — Enables progress tracking
5. **Include stage** — Shows progression through question flow

#### 8. Adaptive Question Flow

The agent should adapt based on user input quality:

**Fast Track (Expert User):**
- User provides detailed, structured input
- Skip elicitation, move quickly to validation
- Batch related confirmations when appropriate

**Deep Dive (Exploratory User):**
- User provides vague or uncertain input
- More elicitation questions
- More validation checkpoints
- Offer examples and suggestions

**Redirect (Off-Track User):**
- User goes into implementation details too early
- Gently redirect to appropriate stage
- "Great thoughts on implementation — let's capture that for later. First, let's nail down [current stage topic]."

#### 9. Synthesis and Handoff Preparation

After completing all six stages, the agent performs synthesis:

```
### Vision Summary

Before we finalize, let me summarize what we've captured:

**Problem:** [One-sentence summary]
**Users:** [Primary user types]
**Goals:** [Top 3 goals]
**MVP Scope:** [Core capabilities]
**Architecture:** [Solution style + key decisions]
**First Phase:** [What's in Phase 1]

---

**Validation Questions:**

1. Does this accurately reflect your vision?
2. Is anything missing that's critical?
3. Is anything included that shouldn't be?

Once confirmed, I'll finalize the vision document and hand off to 
`pch-researcher` to investigate the research topics we identified.
```

### Relevant Files/Resources

- [pch-planner.md](agents/pch-planner.md) — Question format pattern (Options → Recommendation → Rationale)
- [pch-plan-reviewer.md](agents/pch-plan-reviewer.md) — One-question-at-a-time with immediate documentation
- [005-pch-visionary-agent-design.md](docs/research/005-pch-visionary-agent-design.md) — Vision document template from Phase 2

### Gaps & Limitations

None identified.

### Assumptions

- Users will engage with the Q&A process (not just demand immediate output)
- Six-stage flow is appropriate for most vision documents (may need condensed version for simple features)
- MoSCoW prioritization is appropriate for vision-level requirement classification
- The question flow can be interrupted and resumed using the Vision Session Log

---

## Phase 4: Subagent Strategy for Phase Information Gathering

**Status:** ✅ Complete
**Execution Mode:** Automatic (Subagent)
**Completed:** 2026-02-01
**Scope:** Design how subagents can gather information about each solution phase to preserve main agent context

### Research Tasks

- [x] Analyze subagent patterns from pch-coder and pch-researcher
- [x] Define subagent invocation criteria for vision phases
- [x] Design structured return format for phase information subagents
- [x] Create subagent prompt template for phase exploration
- [x] Define orchestrator responsibilities for synthesizing subagent findings
- [x] Address context window management (main agent vs. subagent split)
- [x] Design phase-specific question delegation

### Key Discoveries

- **Recommendation: pch-visionary should NOT use subagents** for its primary Q&A workflow — subagents break conversational context essential for vision capture
- **Subagent purpose mismatch**: pch-coder/pch-researcher use subagents for context-heavy *autonomous* work; pch-visionary requires *interactive* dialogue
- **Context preservation is critical**: The main agent must remember all previous Q&A to ask informed follow-up questions and synthesize coherently
- **Alternative strategy identified**: Document-based incremental building (like pch-planner) instead of subagent delegation
- **Optional subagent use case**: If future versions support "research enrichment" for complex visions, a limited subagent pattern could apply
- **Single-session design validation**: Phase 3's six-stage question flow is designed for single-session completion, confirming no need for cross-session subagent orchestration

### Detailed Findings

#### 1. Analysis of Existing Subagent Patterns

**pch-coder Subagent Pattern:**

| Aspect | pch-coder Approach |
|--------|-------------------|
| **Purpose** | Execute implementation phases autonomously |
| **Invocation Trigger** | User selects "Automatic Mode" for multi-phase execution |
| **Context Passed** | Entire plan document + phase-specific instructions |
| **Return Format** | Structured: STATUS, TASKS_COMPLETED, FILES_MODIFIED, DEVIATIONS, NOTES |
| **Orchestrator Role** | Parse results → update plan → display report → continue or handle blockers |
| **Key Characteristic** | **Autonomous execution** — subagent works independently without user interaction |

**pch-researcher Subagent Pattern:**

| Aspect | pch-researcher Approach |
|--------|------------------------|
| **Purpose** | Conduct research phases autonomously |
| **Invocation Trigger** | User selects "Automatic Mode" for multi-phase research |
| **Context Passed** | Entire research document + phase scope |
| **Return Format** | Structured: STATUS, FINDINGS, KEY_DISCOVERIES, RELEVANT_FILES, GAPS_FOUND |
| **Orchestrator Role** | Parse results → update document → synthesize overview → handle partial/blocked |
| **Key Characteristic** | **Information gathering** — subagent reads files, fetches docs, analyzes patterns |

**Common Subagent Characteristics:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    WHEN SUBAGENTS ARE APPROPRIATE                        │
│                                                                          │
│  ✅ Work is context-heavy (reading many files, documents)               │
│  ✅ Work is autonomous (no user interaction during phase)               │
│  ✅ Results can be structured and returned                               │
│  ✅ Each phase is independent enough to execute in isolation            │
│  ✅ Main agent orchestrates but doesn't need live context               │
│                                                                          │
│                    WHEN SUBAGENTS ARE NOT APPROPRIATE                    │
│                                                                          │
│  ❌ Work requires ongoing conversation with user                         │
│  ❌ Each response depends on accumulated Q&A context                     │
│  ❌ The "session" IS the deliverable (not a document updated later)      │
│  ❌ Context must flow continuously between questions                     │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 2. Why pch-visionary Should NOT Use Subagents

**Fundamental Workflow Difference:**

| Agent | Workflow Type | User Interaction | Subagent Fit |
|-------|--------------|------------------|--------------|
| pch-coder | Execute → Report | Minimal (review results) | ✅ Excellent |
| pch-researcher | Gather → Document | Minimal (review findings) | ✅ Excellent |
| **pch-visionary** | **Ask → Listen → Refine → Ask** | **Continuous dialogue** | ❌ Poor fit |

**The Context Preservation Problem:**

pch-visionary's value comes from building understanding through conversation. Consider this example:

```
User: "We need a customer portal."

Agent Q1: "What problem does the portal solve?"
User A1: "Customers can't see their order status without calling support."

Agent Q2: "Who are the primary users?"
User A2: "B2B customers — they manage fleets of orders."

Agent Q3 (informed by A1 + A2): "For B2B fleet management, would you need 
role-based access where account admins see all orders but regular users 
only see their own?"
```

**Q3 requires accumulated context from A1 and A2.** If a subagent asked Q3, it would need the full conversation history passed to it — defeating the purpose of subagent isolation.

**Subagent Context Isolation Breaks Vision Capture:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     WHY SUBAGENTS DON'T WORK HERE                        │
│                                                                          │
│  Main Agent Session:                                                     │
│  ┌─────────────────────────────────────────────────────────────┐        │
│  │ Q1 → A1 → Q2 → A2 → Q3 → A3 → ... → Synthesis               │        │
│  │     ↑                              ↑                         │        │
│  │     └──── context flows ───────────┘                         │        │
│  └─────────────────────────────────────────────────────────────┘        │
│                                                                          │
│  With Subagents (BROKEN):                                                │
│  ┌───────────┐    ┌───────────┐    ┌───────────┐                        │
│  │ Subagent1 │    │ Subagent2 │    │ Subagent3 │                        │
│  │ Q1 → A1   │    │ Q2 → A2   │    │ Q3 → A3   │  ← No shared context!  │
│  └───────────┘    └───────────┘    └───────────┘                        │
│       ↓                ↓                ↓                               │
│  Main agent would need to pass ALL prior Q&A to each subagent           │
│  = No context savings, just added complexity                            │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 3. The Correct Pattern: Document-Based Incremental Building

Instead of subagents, pch-visionary should follow **pch-planner's incremental pattern**:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     INCREMENTAL DOCUMENT BUILDING                        │
│                     (pch-planner pattern)                                │
│                                                                          │
│  1. Create vision document skeleton (all sections, empty)               │
│  2. Ask ONE question                                                     │
│  3. User responds                                                        │
│  4. Update vision document with answer                                   │
│  5. Update Vision Session Log                                            │
│  6. Ask NEXT question (informed by all prior answers in document)       │
│  7. Repeat until all sections complete                                   │
│  8. Perform synthesis and validation                                     │
│  9. Handoff to pch-researcher                                            │
└─────────────────────────────────────────────────────────────────────────┘
```

**Key Difference from Subagent Pattern:**

| Subagent Pattern | Incremental Document Pattern |
|------------------|------------------------------|
| Context in memory → delegate to subagent → get structured return | Context in document → update document → read document |
| Fresh context per phase | Continuous context via document |
| Orchestrator synthesizes results | Agent synthesizes as it goes |
| User reviews after phases complete | User participates throughout |

#### 4. Context Window Management Without Subagents

**Concern:** What if the Q&A session gets long and approaches context limits?

**Mitigation Strategies:**

1. **Document as External Memory:**
   - All Q&A is written to the vision document immediately
   - If context window fills, agent can re-read the document to recover state
   - Document serves as persistent memory across potential session interruptions

2. **Six-Stage Design Limits Scope:**
   - Phase 3 research established a bounded question flow (6 stages, 15-25 questions typical)
   - Most visions complete within a single session (~30-50 exchanges)
   - This is well within context window limits for modern models

3. **Incremental Synthesis:**
   - Agent summarizes as it goes (not just at the end)
   - Executive Summary, section summaries keep running state compact
   - Previous answers are in structured tables (efficient token use)

4. **Session Recovery Protocol:**
   - If user must start a new session mid-vision, agent reads the partially-complete document
   - Vision Session Log shows where to resume
   - No subagent needed — just document-based recovery

```markdown
## Session Recovery (if interrupted)

If the session is interrupted, start a new session and attach the 
partial vision document. The agent will:

1. Read the vision document
2. Check Vision Session Log for last completed question
3. Resume from the next question in sequence
4. No context is lost — everything is in the document
```

#### 5. Optional Future Enhancement: Research Enrichment Subagents

While the core Q&A workflow should NOT use subagents, there's a potential future enhancement where subagents COULD add value:

**Scenario: Research Enrichment for Complex Visions**

Some visions may reference existing systems, codebases, or external technologies. A subagent could gather this context before or during the vision session.

**Potential Use Case:**

```
User: "We want to integrate with our existing authentication system."

Agent: "I can research your current auth implementation to inform my 
architecture questions. Would you like me to do that?"

[If yes, invoke research subagent]

Subagent researches: 
- Current auth patterns in codebase
- Technologies in use (OAuth, JWT, etc.)
- Integration points

Returns structured findings to main agent.

Agent continues Q&A with enriched context:
"I see you're using OAuth 2.0 with Azure AD. For the new portal, 
should we extend this pattern or consider alternatives?"
```

**Research Enrichment Subagent Template (OPTIONAL FUTURE):**

```markdown
You are pch-visionary-researcher enriching vision context.

**Your Assignment:** Research [specific topic] to inform vision questions.
**Vision Document:** {path}
**Research Focus:** {what to investigate}

---

## CURRENT VISION STATE

{relevant sections from vision document}

---

## YOUR INSTRUCTIONS

1. Search the codebase for relevant patterns
2. Identify technologies, conventions, constraints
3. Return findings to inform vision Q&A

**Return Format:**
```
TOPIC: {research topic}
STATUS: success | partial | blocked
FINDINGS: [what was discovered]
RELEVANT_FOR: [which vision section this informs]
SUGGESTED_QUESTIONS: [questions the main agent should ask based on findings]
CONSTRAINTS_FOUND: [constraints that should be captured in vision]
```
```

**When Research Enrichment Would Apply:**

| Scenario | Research Enrichment? | Rationale |
|----------|---------------------|-----------|
| Greenfield project | ❌ No | Nothing to research |
| Simple feature vision | ❌ No | User can describe context |
| Integration with existing codebase | ⚠️ Maybe | Codebase patterns inform architecture questions |
| Compliance-heavy domain | ⚠️ Maybe | Standards research could inform constraints |
| Complex multi-system integration | ✅ Potentially | Understanding current state aids phasing |

**Implementation Recommendation:**

- **Phase 1 (MVP pch-visionary):** No subagents — pure Q&A with document building
- **Phase 2 (Enhancement):** Optional research enrichment trigger when user references existing systems
- **Never:** Subagents for the Q&A flow itself

#### 6. Decision Summary: Subagent Strategy for pch-visionary

| Question | Answer |
|----------|--------|
| Should pch-visionary use subagents for Q&A? | **No** — breaks essential conversational context |
| Should pch-visionary use subagents for phase execution? | **No** — vision capture is a single-session Q&A, not phased execution |
| Should pch-visionary use Manual/Automatic mode selection? | **No** — there's no multi-phase execution to orchestrate |
| Could subagents enhance pch-visionary in the future? | **Yes** — research enrichment for complex integration scenarios |
| What pattern should pch-visionary follow? | **pch-planner's incremental document building** |

#### 7. Comparison with Other Agents

| Capability | pch-coder | pch-researcher | pch-visionary |
|------------|-----------|----------------|---------------|
| **Multi-phase execution** | ✅ Yes (plan phases) | ✅ Yes (research phases) | ❌ No (single session) |
| **Mode selection (Manual/Auto)** | ✅ Yes | ✅ Yes | ❌ No (always interactive) |
| **Subagent invocation** | ✅ Yes (Automatic mode) | ✅ Yes (Automatic mode) | ❌ No |
| **Document as output** | ✅ Updates plan | ✅ Creates research doc | ✅ Creates vision doc |
| **Incremental building** | ⚠️ Partial (phase by phase) | ⚠️ Partial (phase by phase) | ✅ Yes (question by question) |
| **User interaction during work** | ❌ Minimal | ❌ Minimal | ✅ Continuous |
| **Context preservation method** | Document + Subagent returns | Document + Subagent returns | Document + Conversation |

### Relevant Files/Resources

- [pch-coder.md](agents/pch-coder.md#L100-L300) — Subagent invocation, orchestration loop, return format parsing
- [pch-researcher.md](agents/pch-researcher.md#L200-L500) — Subagent patterns, mode selection, orchestrator responsibilities
- [pch-planner.md](agents/pch-planner.md) — Incremental document building pattern (the correct model for pch-visionary)

### Gaps & Limitations

- Research enrichment subagent pattern is theoretical; would need validation if implemented
- No analysis of subagent performance overhead (latency, reliability) — not relevant since recommendation is to avoid subagents

### Assumptions

- pch-visionary sessions will complete within context window limits (~30-50 Q&A exchanges)
- Users prefer interactive dialogue over automated document generation
- The incremental document pattern from pch-planner is proven and reliable

---

## Phase 5: Workflow Integration & Handoffs

**Status:** ✅ Complete
**Execution Mode:** Manual
**Completed:** 2026-02-01
**Scope:** Design how pch-visionary fits into the agent workflow chain

### Research Tasks

- [x] Design YAML frontmatter for pch-visionary
- [x] Define handoff to pch-researcher (what information is passed)
- [x] Update pch-helper to include pch-visionary guidance
- [x] Define when vision document is "complete" and ready for handoff
- [x] Design workflow: visionary → researcher → planner → coder
- [x] Create handoff message templates
- [x] Consider integration with pch-standards-space for organizational alignment

### Key Discoveries

- **YAML frontmatter should include MCP server config** for pch-standards-space integration, enabling organizational alignment during vision capture
- **Handoff to pch-researcher uses `send: false`** (not auto-send) — vision completion should allow user review before proceeding
- **Vision complete criteria requires 5 mandatory sections**: Problem Statement, Goals & Objectives, Requirements Overview, Architecture Vision, and Product Phasing Strategy
- **Research Topics section is the explicit handoff interface** — pch-researcher reads this to generate its phased research outline
- **pch-helper needs significant updates** — new workflow diagram, new "Situation → Starting Agent" entry, and pch-visionary agent description
- **Self-handoff pattern required** for session recovery if vision Q&A is interrupted

### Detailed Findings

#### 1. Complete YAML Frontmatter Design for pch-visionary

Based on patterns established by other agents (Phase 1 findings):

```yaml
---
name: pch-visionary
description: Transforms raw ideas into structured vision documents through guided Q&A, combining business analyst and solution architect perspectives
model: Claude Opus 4.5
mcp-servers:
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
handoffs:
  - label: Continue Vision
    agent: pch-visionary
    prompt: Continue vision capture from the attached document
    send: false
  - label: Start Research
    agent: pch-researcher
    prompt: |
      Begin research based on the completed vision document.
      
      Focus on the Research Topics section which identifies key areas 
      requiring investigation before implementation planning.
      
      Create a phased research outline addressing each research topic 
      with appropriate depth based on priority.
    send: false
  - label: Skip Research - Start Planning
    agent: pch-planner
    prompt: |
      Create an implementation plan based on the completed vision document.
      
      Use the Requirements Overview and Product Phasing Strategy to 
      structure the implementation plan. Focus on Phase 1 (MVP) scope.
    send: false
---
```

**Design Decisions Explained:**

| Field | Value | Rationale |
|-------|-------|-----------|
| `name` | `pch-visionary` | Consistent with pch-* naming convention |
| `description` | Emphasizes "guided Q&A" and dual BA/architect role | Matches scope from research objectives |
| `model` | `Claude Opus 4.5` | Standard across all agents |
| `mcp-servers` | copilot_spaces config | Enables standards integration (like pch-planner, pch-researcher, pch-reviewer) |
| Handoff 1 | Self-handoff for continuation | Session recovery pattern (like pch-researcher) |
| Handoff 2 | To pch-researcher | Primary workflow progression |
| Handoff 3 | To pch-planner (skip research) | For simple visions that don't require deep research |
| `send: false` | All handoffs | User should review vision before proceeding |

**Why `send: false` for all handoffs:**

Unlike pch-planner → pch-plan-reviewer (which auto-sends because the plan is ready for review), pch-visionary handoffs should NOT auto-send because:

1. **Vision completion is a judgment call** — User should verify the vision captures their intent
2. **Research may not be needed** — Some visions (simple features) can skip directly to planning
3. **User may want to iterate** — Vision documents benefit from reflection before research
4. **Multi-path options** — User chooses between research path or direct planning path

#### 2. Handoff to pch-researcher: Information Passing Design

When pch-visionary hands off to pch-researcher, the following context must be available:

**Mandatory Information to Pass:**

| Information | Source Section | Purpose for pch-researcher |
|-------------|----------------|---------------------------|
| Vision document path | Handoff Information | Document reference for research doc's Vision Reference section |
| Research Topics table | Research Topics section | Defines the research phases/scope |
| Open Questions list | Research Topics > Open Questions | Prioritizes what to investigate |
| Constraints | Requirements Overview > Constraints | Shapes research approach |
| Architecture decisions | Architecture Vision | Context for technical research |
| MVP scope | Product Phasing Strategy > Phase 1 | Boundaries for research focus |

**Handoff Message Template (from pch-visionary to user):**

```markdown
✅ **Vision Document Complete**

The vision document has been finalized at: `[document path]`

**Vision Summary:**
- **Problem:** [One-sentence problem summary]
- **Solution:** [One-sentence solution summary]
- **MVP Scope:** [Brief Phase 1 scope description]
- **Research Topics Identified:** [Count]

---

📋 **Research Topics for Investigation:**

| Priority | Topic | Key Questions |
|----------|-------|---------------|
[Populated from Research Topics section]

---

🔄 **Next Steps:**

The vision is ready for the next stage of the workflow.

**Option A: Start Research** (Recommended for complex visions)
Invoke `@pch-researcher` with this vision document attached.
The researcher will create a phased research outline based on the 
Research Topics section above.

**Option B: Skip to Planning** (For simple, well-defined visions)
If no research is needed, invoke `@pch-planner` directly with this 
vision document attached. The planner will create an implementation 
plan based on the Requirements and Phasing sections.

---

**Handoff Information:**
- Vision Document: `[full path]`
- Created: [date]
- Research Topics: [count] identified
- Ready for: pch-researcher or pch-planner
```

**What pch-researcher Does with the Vision Document:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                pch-researcher RECEIVING VISION HANDOFF                   │
│                                                                          │
│  1. Read Vision Document                                                 │
│     ├── Extract Research Topics table                                    │
│     ├── Extract Open Questions                                           │
│     └── Note constraints and architecture decisions                      │
│                                                                          │
│  2. Create Research Document with Vision Reference                       │
│     ┌──────────────────────────────────────────────────────────────┐    │
│     │ ## Vision Reference                                          │    │
│     │                                                               │    │
│     │ | Field | Value |                                             │    │
│     │ |-------|-------|                                             │    │
│     │ | Vision Document | `/docs/vision/001-feature.md` |           │    │
│     │ | Vision Title | [Feature Name] |                             │    │
│     │ | Target Phase | Phase 1 (MVP) |                              │    │
│     │ | Key Requirements | FR-1, FR-3, NFR-2 |                      │    │
│     └──────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  3. Map Research Topics to Research Phases                               │
│     ├── High priority topics → Early phases                             │
│     ├── Related topics → Grouped into same phase                        │
│     └── Architecture topics → May need deep-dive phases                 │
│                                                                          │
│  4. Create phased research outline → STOP                               │
│                                                                          │
│  5. Execute phases (per normal pch-researcher workflow)                 │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 3. Vision Document Completion Criteria

**Definition of "Vision Complete":**

A vision document is ready for handoff when:

| Section | Completion Criteria | Required? |
|---------|---------------------|-----------|
| **Problem Statement** | Business context, current state, desired state, success metrics defined | ✅ REQUIRED |
| **Stakeholders & Users** | At least 1 primary stakeholder and 1 user persona identified | ⚠️ Recommended |
| **Goals & Objectives** | At least 2 goals with priorities + non-goals section populated | ✅ REQUIRED |
| **Requirements Overview** | At least 3 functional requirements + constraints documented | ✅ REQUIRED |
| **Architecture Vision** | Solution style chosen + key technology decisions (or marked as open) | ✅ REQUIRED |
| **Product Phasing Strategy** | MVP scope defined + deferred items listed | ✅ REQUIRED |
| **Research Topics** | Topics extracted from open questions and uncertain areas | ⚠️ Recommended |
| **Executive Summary** | One-sentence vision synthesized | ✅ REQUIRED |

**Completion Checklist (for agent to verify before handoff):**

```markdown
## Vision Completion Checklist

Before handoff, verify all required sections are complete:

### Required Sections (must have content):
- [ ] Executive Summary — One-sentence vision populated
- [ ] Problem Statement — Business context + success metrics
- [ ] Goals & Objectives — 2+ goals + non-goals listed
- [ ] Requirements Overview — 3+ FRs + constraints
- [ ] Architecture Vision — Solution style selected
- [ ] Product Phasing Strategy — MVP scope defined

### Recommended Sections (should have content if applicable):
- [ ] Stakeholders & Users — Primary users identified
- [ ] Research Topics — Open questions documented

### Validation:
- [ ] Executive Summary accurately reflects the captured vision
- [ ] MVP scope is realistic (not everything in Phase 1)
- [ ] Non-goals are explicit (scope boundaries clear)
- [ ] No major contradictions between sections
```

**Agent Behavior When Incomplete:**

If user requests handoff before completion criteria are met:

```markdown
⚠️ **Vision Not Ready for Handoff**

The following required sections are incomplete:

- [ ] **[Section Name]:** [What's missing]
- [ ] **[Section Name]:** [What's missing]

Would you like to:

1. **Complete the missing sections** — I'll guide you through the remaining questions
2. **Mark as draft and handoff anyway** — The vision will be marked incomplete; researcher/planner may need to return questions
3. **Review what we have** — Let me summarize the current state before deciding
```

#### 4. Updated Workflow: visionary → researcher → planner → coder

**Extended Workflow Diagram:**

```
┌──────────────────┐     ┌─────────────────┐     ┌──────────────┐     ┌───────────────────┐     ┌────────────┐
│  pch-visionary   │ ──► │  pch-researcher │ ──► │  pch-planner │ ──► │ pch-plan-reviewer │ ──► │ pch-coder  │
│  (new - first)   │     │   (optional)    │     │              │     │                   │     │            │
└──────────────────┘     └─────────────────┘     └──────────────┘     └───────────────────┘     └────────────┘
       │                        │                       │                      │                      │
       ▼                        ▼                       ▼                      ▼                      ▼
   Vision                  Research              Implementation          Validated              Working
   Document                Document                  Plan                  Plan                   Code
   (/docs/vision/)        (/docs/research/)        (/docs/plans/)        (/docs/plans/)
```

**Skip Paths (Valid Alternatives):**

```
Standard Path (complex feature):
  visionary → researcher → planner → reviewer → coder

Skip Research (simple feature, known domain):
  visionary → planner → reviewer → coder

Skip Vision (enhancement to existing feature):
  researcher → planner → reviewer → coder

Skip Vision and Research (simple task, clear scope):
  planner → reviewer → coder

Direct Implementation (trivial change, trusted developer):
  coder (with task description only)
```

**When to Use Each Starting Point:**

| Situation | Starting Agent | Rationale |
|-----------|----------------|-----------|
| **New product/feature from scratch** | `pch-visionary` | Need to capture problem, stakeholders, requirements |
| **Complex feature with many unknowns** | `pch-visionary` | Vision structures the investigation |
| **Vague idea, needs refinement** | `pch-visionary` | Q&A process crystallizes the vision |
| **Feature area understood, need research** | `pch-researcher` | Skip vision, go straight to technical research |
| **Clear requirements, known codebase** | `pch-planner` | Skip vision and research, straight to planning |
| **Simple, well-defined task** | `pch-planner` | May even skip review for low-risk work |
| **Quick fix/bug fix** | `pch-coder` | Task description sufficient |

#### 5. pch-helper Updates Required

The pch-helper agent needs the following updates to include pch-visionary:

**Update 1: Add pch-visionary Handoff to YAML Frontmatter**

```yaml
handoffs:
  - label: Start Vision Capture
    agent: pch-visionary
    prompt: Begin capturing a vision for the idea the user has described
    send: false
  # ... existing handoffs ...
```

**Update 2: Add pch-visionary to Agent Descriptions**

Insert new section before pch-researcher:

```markdown
#### 0. 💡 pch-visionary (NEW)
**Purpose:** Transforms raw ideas into structured vision documents

**When to use:**
- You have a new product, feature, or initiative idea
- Requirements are unclear or need to be discovered
- You need to align stakeholders on scope and priorities
- The problem space needs structured exploration
- You want to define MVP scope before research/planning

**Key characteristics:**
- Creates vision documents in `/docs/vision/`
- Uses guided Q&A across six stages (problem → stakeholders → goals → requirements → architecture → phasing)
- Asks ONE question at a time with immediate documentation
- Produces actionable Research Topics for pch-researcher
- Integrates with organizational standards via pch-standards-space

**Output:** A comprehensive vision document with problem statement, requirements, architecture vision, and MVP scope

---
```

**Update 3: Update Workflow Diagram**

Replace existing diagram:

```markdown
## Standard Workflow

The typical workflow follows this sequence:

```
┌──────────────────┐     ┌─────────────────┐     ┌──────────────┐     ┌───────────────────┐     ┌────────────┐
│  pch-visionary   │ ──► │  pch-researcher │ ──► │  pch-planner │ ──► │ pch-plan-reviewer │ ──► │  pch-coder │
│   (optional)     │     │   (optional)    │     │              │     │                   │     │            │
└──────────────────┘     └─────────────────┘     └──────────────┘     └───────────────────┘     └────────────┘
       │                        │                       │                      │                      │
       ▼                        ▼                       ▼                      ▼                      ▼
   Vision                  Research              Implementation          Validated              Working
   Document                Document                  Plan                  Plan                   Code
```
```

**Update 4: Update "When to Start Where" Table**

Add new rows:

```markdown
| Situation | Starting Agent | Rationale |
|-----------|----------------|-----------|
| New product or feature idea | `pch-visionary` | Capture requirements before research |
| Unclear or evolving requirements | `pch-visionary` | Q&A crystallizes the vision |
| Stakeholder alignment needed | `pch-visionary` | Vision document is alignment artifact |
| Unfamiliar codebase/technology | `pch-researcher` | Build understanding before planning |
| Complex feature with many unknowns | `pch-researcher` | Reduce risk through investigation |
| Clear requirements, known codebase | `pch-planner` | Skip research, go straight to planning |
| Simple, well-defined task | `pch-planner` | May even skip review for low-risk work |
| Existing plan needs review | `pch-plan-reviewer` | Enter workflow at review stage |
| Reviewed plan ready to implement | `pch-coder` | Enter workflow at implementation |
```

**Update 5: Add Backward Handoff Section**

Add to "Backward Handoffs" section:

```markdown
### Researcher → Visionary
**When:** Research reveals vision gaps
- Requirements were under-specified
- New stakeholder needs discovered
- Scope needs adjustment based on findings

**What happens:** `pch-researcher` documents findings and requests return to `pch-visionary` to update the vision before continuing.

### Planner → Visionary
**When:** Planning reveals vision issues
- Ambiguous requirements discovered
- Architectural decisions need revisiting
- MVP scope is unrealistic

**What happens:** `pch-planner` pauses planning and requests `pch-visionary` to clarify vision gaps.
```

**Update 6: Update Getting Started Section**

```markdown
## Getting Started

Tell me what you're trying to accomplish, and I'll help you determine:

1. **Which agent to start with** based on your situation
   - Starting from a raw idea? → `pch-visionary`
   - Idea is clear, need to research? → `pch-researcher`
   - Requirements clear, ready to plan? → `pch-planner`
2. **What information that agent will need** from you
3. **What to expect** from the workflow
4. **How many sessions** the work might require
```

#### 6. Handoff Message Templates

**Template A: Vision Complete — Handoff to Researcher**

```markdown
✅ **Vision Document Complete**

**Document:** `/docs/vision/[NNN]-[description].md`
**Vision Title:** [Title from document]

---

## Executive Summary

[Copy of Executive Summary from vision document]

---

## Research Topics Identified

| Priority | Topic | Key Questions |
|----------|-------|---------------|
[From Research Topics section]

---

## Next Steps

The vision is complete and ready for research.

🔬 **Recommended:** Invoke `@pch-researcher` with this vision document attached.

The researcher will:
1. Create a phased research outline based on the Research Topics above
2. Investigate each topic across multiple sessions
3. Document findings for use by pch-planner

**Alternative:** If research is not needed (simple vision, well-understood domain), 
invoke `@pch-planner` directly to skip research.

---

**Handoff Information:**
| Field | Value |
|-------|-------|
| Created By | pch-visionary |
| Created Date | [Date] |
| Status | ✅ Complete — Ready for Research |
| Next Agent | pch-researcher (or pch-planner) |
| Document Location | `/docs/vision/[path]` |
```

**Template B: Vision Interrupted — Session Recovery**

```markdown
⏸️ **Vision Session Paused**

**Document:** `/docs/vision/[NNN]-[description].md`
**Current Stage:** [Stage N]: [Stage Name]
**Last Completed Question:** [Question #]

---

## Progress Summary

| Stage | Status |
|-------|--------|
| 1. Problem Space | ✅ Complete |
| 2. Stakeholders | ✅ Complete |
| 3. Goals | 🔄 In Progress |
| 4. Requirements | ⏳ Not Started |
| 5. Architecture | ⏳ Not Started |
| 6. Phasing | ⏳ Not Started |

---

## To Resume

Start a new session and either:

1. **Attach the document:** Add `/docs/vision/[path]` to the chat and say: "Continue vision capture"

2. **Reference the path:** Say: "Continue vision at `/docs/vision/[path]`"

I'll read the Vision Session Log and resume from the next question.
```

**Template C: Skip Research — Handoff to Planner**

```markdown
✅ **Vision Document Complete — Skipping Research**

**Document:** `/docs/vision/[NNN]-[description].md`

---

## Executive Summary

[Copy of Executive Summary]

---

## Why Skip Research?

Based on the vision scope, research may not be necessary because:
- [Reason 1: e.g., "The technology stack is well-understood"]
- [Reason 2: e.g., "Requirements are clear and validated"]
- [Reason 3: e.g., "No open questions requiring investigation"]

---

## Ready for Planning

Invoke `@pch-planner` with this vision document attached.

The planner will use:
- **Requirements Overview** → Functional requirements for the plan
- **Product Phasing Strategy** → Phase structure for execution
- **Architecture Vision** → Technical approach context

---

**Handoff Information:**
| Field | Value |
|-------|-------|
| Created By | pch-visionary |
| Status | ✅ Complete — Skipping Research |
| Next Agent | pch-planner |
| Document Location | `/docs/vision/[path]` |
```

#### 7. Standards Integration via pch-standards-space

**Why Include MCP Server Config:**

pch-visionary should integrate with pch-standards-space because:

1. **Organizational alignment early** — Vision documents benefit from standards awareness at inception, not just planning
2. **Constraint discovery** — Standards often define constraints (technology choices, compliance requirements) relevant to vision
3. **Consistency with other agents** — pch-researcher, pch-planner, and pch-plan-reviewer all use standards integration
4. **Architecture guidance** — Standards may have architecture patterns that inform Architecture Vision section

**How to Use Standards in pch-visionary:**

```markdown
## Standards Integration

At the start of a vision session, query pch-standards-space for relevant standards:

1. **Query with problem domain:** "customer portal requirements" or "authentication patterns"
2. **Extract relevant guidance:** Constraints, architecture patterns, compliance requirements
3. **Incorporate into questions:** Reference standards when asking architecture/constraint questions
4. **Document in vision:** Add to Referenced Standards section

### Example Integration:

**During Architecture Stage:**

Agent queries standards: "web application architecture patterns"

Standards return: "Organization uses Azure-first strategy, React for frontend, .NET for backend"

Agent's architecture question becomes:

```
### Solution Architecture

**Question:** Based on organizational standards, we typically use Azure + React + .NET.
Does this vision align with these standards, or is there a reason to deviate?

**Options:**

A) **Follow Standards**
   Use Azure + React + .NET as per organizational guidelines.
   *Recommended for most projects*

B) **Request Exception**
   Document why a different approach is needed.
   *Requires justification in Architecture Vision section*
```

### Fallback Behavior:

If pch-standards-space is unavailable:

⚠️ **Could not access organizational standards from pch-standards-space. 
Proceeding without standards context.**

Continue with vision capture normally. Note in Referenced Standards section:
"Standards integration unavailable during this session."
```

#### 8. Integration Points Summary

| Integration | From | To | Mechanism | What's Passed |
|-------------|------|-----|-----------|---------------|
| Vision → Research | pch-visionary | pch-researcher | Vision document attachment | Research Topics, constraints, architecture |
| Vision → Planning | pch-visionary | pch-planner | Vision document attachment | Requirements, phasing, architecture |
| Self-recovery | pch-visionary | pch-visionary | Document + Session Log | Resume from last question |
| Standards | pch-standards-space | pch-visionary | MCP server query | Organizational guidelines |
| Research → Vision | pch-researcher | pch-visionary | Backward handoff | Vision gaps found |
| Planning → Vision | pch-planner | pch-visionary | Backward handoff | Ambiguous requirements |

### Relevant Files/Resources

- [pch-helper.md](agents/pch-helper.md) — Workflow documentation to update, handoff patterns, agent descriptions
- [pch-researcher.md](agents/pch-researcher.md#L1-L30) — YAML frontmatter with MCP server config and handoff patterns
- [pch-planner.md](agents/pch-planner.md#L1-L20) — YAML frontmatter patterns, handoff to reviewer
- [pch-plan-reviewer.md](agents/pch-plan-reviewer.md#L1-L20) — Auto-send handoff pattern (send: true)
- [pch-coder.md](agents/pch-coder.md#L1-L15) — Backward handoff pattern (send: false)

### Gaps & Limitations

- **Backward handoff to pch-visionary not yet defined in other agents** — pch-researcher and pch-planner would need updates to include handoff back to visionary
- **Vision document validation is advisory** — Agent cannot enforce completion criteria if user insists on early handoff

### Assumptions

- User will typically complete vision in a single session (Phase 4 finding: ~30-50 Q&A exchanges)
- Vision documents stored in `/docs/vision/` following same numbering convention as other docs
- pch-standards-space is available for standards integration (with graceful fallback)
- Users prefer `send: false` for vision handoffs to allow review before proceeding

---

## Phase 6: Implementation Template & Recommendations

**Status:** ✅ Complete
**Execution Mode:** Manual
**Completed:** 2026-02-01
**Scope:** Synthesize all findings into an agent implementation template

### Research Tasks

- [x] Create complete pch-visionary.md template structure
- [x] Define all major sections and their content
- [x] Provide sample vision document template
- [x] Create implementation plan outline for pch-visionary
- [x] Document testing/validation recommendations
- [x] List required updates to other agents (pch-helper, copilot-instructions.md)
- [x] Provide phased rollout recommendations

### Key Discoveries

- **Agent implementation requires ~1,800-2,200 lines** — Following existing agent patterns with the six-stage question framework and full template library
- **pch-helper updates are critical path** — Workflow diagram, agent descriptions, and situation-to-agent mapping must be updated first for discoverability
- **Vision document template should be embedded in agent** — Complete template included in agent file (not referenced externally) for self-contained operation
- **Phased rollout recommended** — Four-phase approach: Core agent → pch-helper integration → Standards integration → Backward handoffs
- **Testing requires scenario-based validation** — Five test scenarios covering different user personas and vision complexity levels
- **copilot-instructions.md update is minimal** — Only needs workflow diagram update and vision folder reference in "Document Locations"

### Detailed Findings

---

## 1. pch-visionary.md Agent Outline

The following is a complete structural outline showing all sections and their key content. This serves as the blueprint for creating the full agent file.

### 1.1 YAML Frontmatter (Lines ~1-30)

```yaml
---
name: pch-visionary
description: Transforms raw ideas into structured vision documents through guided Q&A, combining business analyst and solution architect perspectives
model: Claude Opus 4.5
mcp-servers:
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
handoffs:
  - label: Continue Vision
    agent: pch-visionary
    prompt: Continue vision capture from the attached document
    send: false
  - label: Start Research
    agent: pch-researcher
    prompt: |
      Begin research based on the completed vision document.
      Focus on the Research Topics section which identifies key areas 
      requiring investigation before implementation planning.
    send: false
  - label: Skip Research - Start Planning
    agent: pch-planner
    prompt: |
      Create an implementation plan based on the completed vision document.
      Use the Requirements Overview and Product Phasing Strategy to 
      structure the implementation plan.
    send: false
---
```

### 1.2 Agent Introduction (Lines ~32-60)

**Content:**
- Role definition as BA/Solution Architect hybrid
- Primary purpose: Transform raw ideas into structured vision documents
- Output location: `/docs/vision/`
- Key differentiator from other agents: Interactive Q&A, not document analysis

### 1.3 Documentation Only Constraint (Lines ~62-75)

**Content:**
- Standard "DO NOT modify source code" constraint
- Match format from pch-planner, pch-researcher, pch-reviewer
- Emphasize vision documents only

### 1.4 Core Responsibilities (Lines ~77-95)

**Content (5-7 bullet points):**
1. Create vision document skeleton first (before Q&A)
2. Ask ONE question at a time using structured format
3. Update vision document after each answer
4. Log all decisions in Vision Session Log
5. Guide user through six-stage question flow
6. Synthesize Executive Summary after all stages
7. Handoff to pch-researcher or pch-planner when complete

### 1.5 Session Initialization Decision Tree (Lines ~97-140)

**Content:**
- ASCII decision tree for: New vision vs. Continuation
- Detection patterns for attached documents vs. path references
- Flow: Check for existing doc → If new: create skeleton → If continuation: load and resume

```
User message received
        │
        ▼
┌───────────────────────────────────┐
│ Does user reference an existing   │
│ vision document (path or          │
│ attached)?                        │
└───────────────────────────────────┘
        │
   NO   │   YES
   ▼    │    ▼
   NEW  │  CONTINUATION
```

### 1.6 Critical Rules Section (Lines ~142-200)

**Content:**
- 🚨 Header formatting for emphasis
- Violation table (what never to do)
- Key rules:
  - ONE question at a time (no batching)
  - Update document BEFORE asking next question
  - Never proceed to next stage until current stage complete
  - Always synthesize before handoff

### 1.7 Document Numbering (Lines ~202-230)

**Content:**
- Zero-padded 3-digit format: `{NNN}-{description}.md`
- Instructions to list `/docs/vision/` directory first
- Find max number → add 1
- Same pattern as pch-planner, pch-researcher

### 1.8 Question Framework (Lines ~232-550)

**Content (from Phase 3):**

#### 1.8.1 Six-Stage Question Flow Overview (~30 lines)
- ASCII diagram of stages
- Stage completion criteria table
- Entry/exit conditions for each stage

#### 1.8.2 Question Types (~60 lines)
- Type A: Elicitation (open-ended)
- Type B: Refinement (multiple choice with recommendation)
- Type C: Validation (confirm/refine)
- When to use each type

#### 1.8.3 Clarification vs. Suggestion Matrix (~40 lines)
- Decision tree for choosing question type
- Scenario-based guidance table

#### 1.8.4 Stage-by-Stage Question Templates (~200 lines)
- **Stage 1: Problem Space**
  - Initial elicitation question
  - Business context follow-up
  - Success metrics question (with options)
  - 5 Whys integration for root cause

- **Stage 2: Stakeholders & Users**
  - Primary users elicitation
  - Stakeholder mapping question

- **Stage 3: Goals & Objectives**
  - Goal elicitation
  - Priority classification (MoSCoW format)
  - Non-goals question

- **Stage 4: Requirements Overview**
  - Functional requirements capture
  - NFR selection (checkbox-style options)
  - Constraints elicitation
  - INVEST validation integration

- **Stage 5: Architecture Vision**
  - Solution style options (Web/Mobile/API/Desktop/Hybrid)
  - Technology decisions question
  - Integration points
  - Security considerations

- **Stage 6: Product Phasing**
  - MVP definition question
  - Deferred items capture
  - Phase boundary validation

### 1.9 Vision Document Template (Lines ~552-750)

**Content (from Phase 2):**
- Complete embedded template (all 13 sections)
- Section markers with completion status
- Tables with proper markdown formatting
- Ready to copy into new vision documents

### 1.10 Decision Logging (Lines ~752-790)

**Content:**
- Vision Session Log format
- Logging rules (log BEFORE proceeding, summarize concisely)
- Example log entries

### 1.11 Adaptive Question Flow (Lines ~792-830)

**Content:**
- Fast Track (expert user) behavior
- Deep Dive (exploratory user) behavior
- Redirect (off-track user) behavior
- Escalation patterns

### 1.12 Vision Completion Criteria (Lines ~832-900)

**Content (from Phase 5):**
- Required sections checklist
- Recommended sections checklist
- Validation rules
- Agent behavior when incomplete

### 1.13 Session Handoff Messages (Lines ~902-1000)

**Content:**
- Template A: Vision Complete → Researcher handoff
- Template B: Vision Interrupted → Session recovery
- Template C: Skip Research → Planner handoff
- Formatting consistent with pch-researcher templates

### 1.14 Standards Integration (Lines ~1002-1050)

**Content:**
- How to query pch-standards-space
- What to do with standards results
- Fallback behavior if unavailable
- Referenced Standards section population

### 1.15 Best Practices (Lines ~1052-1100)

**Content:**
- When to ask open vs. structured questions
- Handling contradictory user inputs
- Keeping user engaged through long Q&A
- Knowing when vision is "good enough"

### Estimated Total: ~1,100 lines of agent instructions

---

## 2. Sample Vision Document Template

This is the complete template that pch-visionary will create when starting a new vision:

```markdown
# Vision: [Solution/Feature Title]

## Document Information

| Field | Value |
|-------|-------|
| Document | {NNN}-{description}.md |
| Created | [Date] |
| Status | 🔄 In Progress |
| Owner | pch-visionary |
| Version | v1.0 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | [Date] | pch-visionary | Initial vision capture |

---

## Vision Session Log

| # | Date | Stage | Topic | Question Summary | Answer | Impact |
|---|------|-------|-------|------------------|--------|--------|

---

## Section Completion Status

| Section | Status |
|---------|--------|
| Executive Summary | ⏳ Pending (completed last) |
| Problem Statement | ⏳ Not Started |
| Stakeholders & Users | ⏳ Not Started |
| Goals & Objectives | ⏳ Not Started |
| Requirements Overview | ⏳ Not Started |
| Architecture Vision | ⏳ Not Started |
| Product Phasing Strategy | ⏳ Not Started |
| Research Topics | ⏳ Not Started |
| Risk Assessment | ⏳ Not Started |

---

## Executive Summary

> **Note:** This section is populated after all vision questions are complete.

**One-Sentence Vision:** [To be completed]
**Target Outcome:** [To be completed]
**Key Constraints:** [To be completed]

---

## Problem Statement

### Business Context

[To be completed during Stage 1]

### Current State

[To be completed during Stage 1]

### Desired Future State

[To be completed during Stage 1]

### Success Metrics

| Metric | Current Value | Target Value | Measurement Method |
|--------|---------------|--------------|-------------------|

---

## Stakeholders & Users

### Primary Stakeholders

| Stakeholder | Role | Interest | Influence |
|-------------|------|----------|-----------|

### User Personas

[To be completed during Stage 2]

---

## Goals & Objectives

### Primary Goals

| ID | Goal | Priority | Success Indicator |
|----|------|----------|-------------------|

### Non-Goals (Explicitly Out of Scope)

- [To be completed during Stage 3]

---

## Requirements Overview

### Functional Requirements (High-Level)

| ID | Requirement | Priority | Product Phase |
|----|-------------|----------|---------------|

### Non-Functional Requirements

| ID | Requirement | Priority | Constraint Type |
|----|-------------|----------|-----------------|

### Constraints

| Constraint Type | Description | Impact |
|-----------------|-------------|--------|

---

## Architecture Vision

### Solution Overview

[To be completed during Stage 5]

### Architecture Style

| Aspect | Choice | Rationale |
|--------|--------|-----------|

### Key Technology Decisions

| Decision Area | Choice | Alternatives Considered | Rationale |
|---------------|--------|------------------------|-----------|

### Integration Points

| System | Integration Type | Purpose | Criticality |
|--------|-----------------|---------|-------------|

### Security Considerations

| Concern | Approach | Notes |
|---------|----------|-------|

---

## Product Phasing Strategy

### Phase Overview

| Phase | Name | Scope Summary | Target Date | Status |
|-------|------|---------------|-------------|--------|
| 1 | MVP | [To be defined] | [TBD] | ⏳ Not Started |

### Phase 1: MVP

**Goal:** [To be completed during Stage 6]

**Included Capabilities:**
- [To be completed]

**Excluded from MVP (Deferred):**
- [To be completed]

**Definition of Done:**
- [To be completed]

### Future Phases (Roadmap)

| Phase | Tentative Scope | Dependencies |
|-------|-----------------|--------------|

---

## Research Topics

| Topic | Priority | Key Questions | Target Phase |
|-------|----------|---------------|--------------|

### Open Questions

| Question | Impact if Unresolved | Research Needed? |
|----------|---------------------|------------------|

---

## Risk Assessment (Vision-Level)

| Risk | Likelihood | Impact | Mitigation Strategy |
|------|------------|--------|---------------------|

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|

---

## Handoff Information

**Created By:** pch-visionary
**Created Date:** [Date]
**Status:** 🔄 In Progress
**Next Agent:** [TBD — pch-researcher or pch-planner]
**Vision Document Location:** `/docs/vision/{NNN}-{description}.md`
```

---

## 3. Implementation Plan Outline for pch-visionary

### Phase 1: Core Agent Creation (Estimated: 2-3 hours)

**Objective:** Create functional pch-visionary.md with essential capabilities

**Tasks:**
1. **Create agent file skeleton** (`/agents/pch-visionary.md`)
   - YAML frontmatter with handoffs
   - All major section headers (empty)

2. **Implement YAML frontmatter**
   - name, description, model
   - mcp-servers config for standards
   - Three handoffs (self, researcher, planner)

3. **Write core sections**
   - Agent introduction and role
   - Documentation-only constraint
   - Core responsibilities

4. **Implement session initialization**
   - Decision tree for new vs. continuation
   - Document loading logic

5. **Implement critical rules**
   - Violation table
   - One-question-at-a-time enforcement

6. **Add document numbering**
   - `/docs/vision/` scanning logic
   - Zero-padded naming convention

**Deliverable:** Basic agent that can be invoked but with placeholder question flow

---

### Phase 2: Question Framework Implementation (Estimated: 3-4 hours)

**Objective:** Implement the complete six-stage question flow

**Tasks:**
1. **Implement six-stage overview**
   - Stage diagram
   - Completion criteria table
   - Stage flow logic

2. **Create question type definitions**
   - Elicitation template
   - Refinement template (with Options format)
   - Validation template

3. **Implement Stage 1: Problem Space**
   - Core problem question
   - Business context follow-up
   - Success metrics with options
   - 5 Whys integration

4. **Implement Stage 2: Stakeholders**
   - Primary users question
   - Stakeholder mapping

5. **Implement Stage 3: Goals**
   - Goal elicitation
   - MoSCoW priority question
   - Non-goals capture

6. **Implement Stage 4: Requirements**
   - Functional requirements
   - NFR options list
   - Constraints elicitation

7. **Implement Stage 5: Architecture**
   - Solution style options
   - Technology decisions
   - Integration points

8. **Implement Stage 6: Phasing**
   - MVP definition
   - Deferred items
   - Phase validation

**Deliverable:** Complete question flow with all templates

---

### Phase 3: Vision Document & Handoffs (Estimated: 2 hours)

**Objective:** Implement document creation and handoff logic

**Tasks:**
1. **Embed vision document template**
   - Complete 13-section template
   - Section completion tracking

2. **Implement decision logging**
   - Vision Session Log format
   - Logging rules

3. **Create completion criteria logic**
   - Required sections checklist
   - Validation rules
   - Incomplete handling

4. **Implement handoff messages**
   - Vision Complete → Researcher
   - Vision Interrupted → Self
   - Skip Research → Planner

5. **Add standards integration**
   - Query instructions
   - Fallback behavior

**Deliverable:** Full document creation and handoff capability

---

### Phase 4: Polish & Best Practices (Estimated: 1-2 hours)

**Objective:** Add finishing touches and edge case handling

**Tasks:**
1. **Implement adaptive question flow**
   - Fast track for expert users
   - Deep dive for exploratory users
   - Redirect for off-track users

2. **Add best practices section**
   - When to use each question type
   - Handling contradictions
   - Session management

3. **Final review pass**
   - Consistency with other agents
   - Template formatting
   - Emoji and status consistency

**Deliverable:** Production-ready pch-visionary.md

---

## 4. Required Updates to Other Files

### 4.1 pch-helper.md Updates (CRITICAL)

**Priority:** HIGH — Required for discoverability

| Section | Update Required |
|---------|-----------------|
| **YAML handoffs** | Add `Start Vision Capture` handoff to pch-visionary |
| **Agent descriptions** | Add new "0. 💡 pch-visionary" section before pch-researcher |
| **Workflow diagram** | Update to show 5-agent chain with pch-visionary first |
| **"When to Start Where" table** | Add rows for vision use cases |
| **Backward handoffs** | Add Researcher→Visionary and Planner→Visionary sections |
| **Getting Started** | Update to mention starting from raw idea |

**Estimated Changes:** ~100 lines added/modified

**Specific Content:**

```yaml
# Add to YAML handoffs
handoffs:
  - label: Start Vision Capture
    agent: pch-visionary
    prompt: Begin capturing a vision for the idea the user has described
    send: false
  # ... existing handoffs ...
```

```markdown
# Add new agent section (before pch-researcher)

#### 0. 💡 pch-visionary
**Purpose:** Transforms raw ideas into structured vision documents

**When to use:**
- You have a new product, feature, or initiative idea
- Requirements are unclear or need to be discovered
- You need to align stakeholders on scope and priorities
- The problem space needs structured exploration
- You want to define MVP scope before research/planning

**Key characteristics:**
- Creates vision documents in `/docs/vision/`
- Uses guided Q&A across six stages
- Asks ONE question at a time with immediate documentation
- Produces actionable Research Topics for pch-researcher
- Integrates with organizational standards

**Output:** A comprehensive vision document with problem statement, 
requirements, architecture vision, and MVP scope
```

### 4.2 copilot-instructions.md Updates (MINOR)

**Priority:** MEDIUM — Informational update

| Section | Update Required |
|---------|-----------------|
| **Agent workflow diagram** | Update ASCII diagram to show pch-visionary |
| **Document locations** | Add `/docs/vision/` to list |
| **Agent descriptions** | Add pch-visionary brief description |

**Estimated Changes:** ~20 lines added/modified

**Specific Content:**

```markdown
# Update Agent Workflow
```
Research → Planning → Review → Implementation
                    ↑
               pch-helper (guidance at any stage)
```
becomes:
```
Vision → Research → Planning → Review → Implementation
                              ↑
                         pch-helper (guidance at any stage)
```

# Update Document Locations
- **Vision Documents**: `/docs/vision/` (created by pch-visionary)
- **Agent Prompts**: `/agents/`
- **Implementation Plans**: `/docs/plans/` (created by pch-planner)
- **Research Documents**: `/docs/research/` (created by pch-researcher)
```

### 4.3 pch-researcher.md Updates (OPTIONAL - Phase 2 rollout)

**Priority:** LOW — Enhancement for traceability

| Section | Update Required |
|---------|-----------------|
| **Research document template** | Add "Vision Reference" section |
| **Document creation instructions** | Check for vision doc reference when creating |

**Estimated Changes:** ~30 lines added

### 4.4 pch-planner.md Updates (OPTIONAL - Phase 2 rollout)

**Priority:** LOW — Enhancement for traceability

| Section | Update Required |
|---------|-----------------|
| **Plan document template** | Add "Vision & Research Foundation" section |
| **Handoffs** | Add backward handoff to pch-visionary |

**Estimated Changes:** ~20 lines added

---

## 5. Testing & Validation Recommendations

### 5.1 Test Scenarios

| Scenario | User Persona | Vision Complexity | Expected Behavior |
|----------|--------------|-------------------|-------------------|
| **S1: Greenfield Product** | Product Manager | High | Full 6-stage flow, ~30+ questions, generates research topics |
| **S2: Simple Feature** | Developer | Low | Condensed flow, ~15 questions, may skip to planner |
| **S3: Technical Enhancement** | Architect | Medium | Deep Stage 5, lighter Stage 2, generates specific research |
| **S4: Session Interruption** | Any | Any | Mid-vision pause, resume from Session Log |
| **S5: Expert Fast-Track** | Senior Architect | High | Quick progression, validation-heavy questions |

### 5.2 Validation Checklist

**For each test scenario, verify:**

| Checkpoint | Validation |
|------------|------------|
| Document created | `/docs/vision/{NNN}-{description}.md` exists with correct numbering |
| Skeleton complete | All 13 sections present in initial document |
| One question at a time | Agent waits for response before next question |
| Immediate documentation | Document updated after each answer |
| Session Log populated | Every Q&A exchange logged with stage and impact |
| Stage progression | All 6 stages visited (or explicitly skipped) |
| Completion validation | Agent checks required sections before handoff |
| Executive Summary | Synthesized accurately from all stages |
| Handoff message | Correct template used, document path accurate |

### 5.3 Edge Case Testing

| Edge Case | Test Approach |
|-----------|---------------|
| User gives one-word answers | Agent should ask clarifying follow-ups |
| User goes into implementation details early | Agent should redirect to current stage |
| User wants to skip stages | Agent should confirm and document skip reason |
| User provides contradictory requirements | Agent should highlight and resolve conflict |
| Standards space unavailable | Agent should continue with warning message |
| Document path doesn't exist | Agent should create `/docs/vision/` directory |

### 5.4 Integration Testing

| Integration | Test |
|-------------|------|
| pch-visionary → pch-researcher | Vision doc handoff populates researcher's Vision Reference |
| pch-visionary → pch-planner | Vision doc provides requirements and phasing context |
| pch-helper → pch-visionary | Helper correctly recommends visionary for raw ideas |
| Standards integration | pch-standards-space query returns organizational guidance |

---

## 6. Phased Rollout Recommendations

### Rollout Phase 1: Core Agent (Week 1)

**Objective:** Deploy functional pch-visionary with basic capabilities

**Scope:**
- [ ] Create pch-visionary.md with full question framework
- [ ] Embed vision document template
- [ ] Implement completion criteria and handoffs
- [ ] Create `/docs/vision/` directory

**Validation:**
- Run Scenario S1 (Greenfield Product) end-to-end
- Verify document creation and handoff to researcher

**Rollback:** Delete agent file if critical issues found

---

### Rollout Phase 2: pch-helper Integration (Week 1-2)

**Objective:** Make pch-visionary discoverable in workflow

**Scope:**
- [ ] Update pch-helper.md with pch-visionary handoff
- [ ] Add agent description to pch-helper
- [ ] Update workflow diagram in pch-helper
- [ ] Update copilot-instructions.md workflow diagram

**Validation:**
- Invoke pch-helper and ask "I have a new feature idea" — should recommend pch-visionary
- Verify handoff button appears and works

**Rollback:** Revert pch-helper changes if issues

---

### Rollout Phase 3: Standards Integration (Week 2)

**Objective:** Enable organizational alignment during vision capture

**Scope:**
- [ ] Verify pch-standards-space MCP configuration works
- [ ] Test standards query during architecture stage
- [ ] Validate fallback behavior when unavailable

**Validation:**
- Run vision capture with standards space enabled
- Verify standards appear in Referenced Standards section

**Dependencies:** pch-standards-space must be available

---

### Rollout Phase 4: Traceability & Backward Handoffs (Week 3+)

**Objective:** Complete workflow integration

**Scope:**
- [ ] Add Vision Reference section to pch-researcher template
- [ ] Add Vision & Research Foundation to pch-planner template
- [ ] Implement backward handoffs (researcher→visionary, planner→visionary)
- [ ] Update pch-helper with backward handoff documentation

**Validation:**
- Full workflow test: visionary → researcher → planner → reviewer → coder
- Verify traceability from code back to vision

**Note:** This phase is enhancement, not required for MVP functionality

---

## 7. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Vision document completion rate | >80% | % of started visions that reach handoff |
| Average questions per vision | 15-35 | Count from Session Log |
| User satisfaction | Positive feedback | Qualitative assessment |
| Downstream usage | >60% proceed to research or planning | Track handoff invocations |
| Time to vision complete | <60 minutes | Session timestamps |
| Research topic generation | >3 topics per vision | Count from Research Topics section |

---

## 8. Open Items for Implementation

| Item | Owner | Notes |
|------|-------|-------|
| Create `/docs/vision/` directory | Implementer | First vision creation will need this |
| Determine if mode selection needed | Design decision | Current recommendation: No (single-session flow) |
| Backward handoff design | Future enhancement | How researcher/planner return to visionary |
| Vision document versioning | Future enhancement | How to handle vision updates after research |

### Relevant Files/Resources

- [pch-planner.md](agents/pch-planner.md) — Reference for incremental document pattern, question format
- [pch-researcher.md](agents/pch-researcher.md) — Reference for session handoff messages, decision trees
- [pch-helper.md](agents/pch-helper.md) — Will require updates for workflow integration
- [copilot-instructions.md](.github/copilot-instructions.md) — Will require minor updates

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|
| PCH Agent Patterns | Agent structure conventions | YAML frontmatter, core responsibilities, session initialization, critical rules formatting |
| Incremental Document Building | pch-planner workflow | One-question-at-a-time, update after each answer, skeleton-first approach |
| Status Emoji Convention | Progress tracking | ⏳ Not Started, 🔄 In Progress, ✅ Complete, ❌ Blocked |
| Document Numbering | File naming | Zero-padded 3-digit format: `{NNN}-{description}.md` |
| Question Format | User interaction | Options (A/B/C/D) → Recommendation → Rationale pattern |

---

## Conclusions

This research provides a complete blueprint for implementing `pch-visionary`, answering all six key questions from the research objectives:

### 1. Workflow Integration
pch-visionary integrates as the **first agent** in the workflow, positioned before pch-researcher. The extended chain becomes: `visionary → researcher → planner → reviewer → coder`. Skip paths are supported for simpler scenarios (direct to planner, or skipping vision entirely for well-defined tasks).

### 2. Question Strategy
A **six-stage question flow** guides users from problem space to phasing decisions. Three question types (Elicitation, Refinement, Validation) are applied based on context. The format follows pch-planner's Options → Recommendation → Rationale pattern for structured choices.

### 3. Document Structure
Vision documents use a **13-section template** in `/docs/vision/` that captures problem statement, stakeholders, goals, requirements, architecture vision, and product phasing. The Research Topics section serves as the explicit handoff interface to pch-researcher.

### 4. Phasing Strategy
Product phasing is captured in Stage 6 with explicit MVP scope and deferred items. The "Excluded from MVP (Deferred)" pattern prevents scope creep and sets clear boundaries for initial implementation.

### 5. Subagent Usage
pch-visionary should **NOT use subagents** for its core workflow. Unlike pch-researcher and pch-coder (which do autonomous work), vision capture requires continuous conversational context that would be broken by subagent isolation. The pch-planner incremental document pattern is the correct model.

### 6. Handoff Design
Three handoff paths are defined: (1) Self-continuation for session recovery, (2) pch-researcher for the standard research-first workflow, (3) pch-planner for simple visions that can skip research. All handoffs use `send: false` to allow user review before proceeding.

### Implementation Readiness

This research provides everything needed to implement pch-visionary:
- ✅ Complete agent outline with 15 sections and content specifications
- ✅ Embedded vision document template (ready to copy)
- ✅ Four-phase implementation plan with task breakdowns
- ✅ Required updates to pch-helper and copilot-instructions.md
- ✅ Testing scenarios and validation checklists
- ✅ Phased rollout plan with success metrics

**Recommended next step:** Hand off to `pch-planner` to create a formal implementation plan for building pch-visionary.md.

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** 2026-02-01
**Status:** ✅ Complete — All Phases Done
**Current Phase:** 6 of 6 (Complete)
**Document Location:** `/docs/research/005-pch-visionary-agent-design.md`

### Next Steps

1. **Create implementation plan** — Invoke `@pch-planner` with this research document to generate a formal implementation plan
2. **Implement pch-visionary.md** — Follow Phase 6 implementation outline (~8-11 hours estimated)
3. **Update pch-helper.md** — Add workflow integration (~1 hour)
4. **Test with scenarios** — Validate using the five test scenarios defined in Phase 6
5. **Phased rollout** — Follow the 4-phase rollout plan over 3+ weeks
