# Vision: Self-Operating Development Loop

## Document Information

| Field | Value |
|-------|-------|
| Document | 002-self-operating-development-loop.md |
| Created | 2026-02-05 |
| Status | ✅ Complete |
| Owner | pch-visionary |
| Version | v1.0 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-05 | pch-visionary | Initial vision capture |

---

## Vision Session Log

| # | Date | Stage | Topic | Question Summary | Answer | Impact |
|---|------|-------|-------|------------------|--------|--------|
| 1 | 2026-02-05 | Stage 1 | Problem | What pain point are you solving? | Automate agent phase transitions (vision→research→planning→code) with automatic document passing | Core problem defined |
| 2 | 2026-02-05 | Stage 1 | Current State | Describe current workflow | Handful of handoffs including plan-reviewer; many end-user questions during planning/review; wants external comms (Teams/email/SMS); wants autonomous mode where model answers questions | Key features identified |
| 3 | 2026-02-05 | Stage 1 | Success Metrics | How will you measure success? | Autonomy-based: measure how long system runs unattended | Primary success = sustained autonomous operation |
| 4 | 2026-02-05 | Stage 2 | Users | Who will use this? | Solo user (developer initiating features, reviewing outputs) | Single-user system design |
| 5 | 2026-02-05 | Stage 3 | Primary Goal | Confirm goal statement | Confirmed: Enable fire-and-forget feature initiation with autonomous agent progression | Core goal locked |
| 6 | 2026-02-05 | Stage 3 | Non-Goals | What's out of scope? | All suggested: multi-user, GUI, deployment, custom agents, cross-repo, production SLA | Scope boundaries defined |
| 7 | 2026-02-05 | Stage 4 | Functional Requirements | Confirm capabilities | Approved 5 core FRs; deferred external notifications (Teams/email/SMS) to Phase 2 | MVP scope locked |
| 8 | 2026-02-05 | Stage 4 | Non-Functional Requirements | Top quality attributes? | Reliability, Observability, Resilience/Fault tolerance | NFRs defined |
| 9 | 2026-02-05 | Stage 4 | Constraints | Technical constraints? | .NET Core on Windows is the only hard requirement; Copilot SDK is preferred but not mandated | Constraints clarified |
| 10 | 2026-02-05 | Stage 5 | Solution Style | What type of solution? | Console Application (CLI) — simple invocation, runs in terminal | Architecture style selected |
| 11 | 2026-02-05 | Stage 5 | Agent Invocation | How to invoke PCH agents? | Via Copilot SDK with custom agent definitions — load prompts into SDK | Integration approach selected |
| 12 | 2026-02-05 | Stage 5 | Security | Security considerations? | Minimal — personal tool, trusted environment; basic logging sufficient | Security approach defined |
| 13 | 2026-02-05 | Stage 6 | Phasing | Confirm MVP scope and phases | Confirmed: Phase 1 = FR-1 to FR-5; Phase 2 = External notifications | Phasing locked |

---

## Section Completion Status

| Section | Status | Notes |
|---------|--------|-------|
| Executive Summary | ✅ Complete | Vision synthesized |
| Problem Statement | ✅ Complete | Stage 1 |
| Stakeholders & Users | ✅ Complete | Stage 2 — Single user system |
| Goals & Objectives | ✅ Complete | Stage 3 — Goals + non-goals defined |
| Requirements Overview | ✅ Complete | Stage 4 — FRs, NFRs, constraints |
| Architecture Vision | ✅ Complete | Stage 5 — CLI, Copilot SDK, minimal security |
| Product Phasing Strategy | ✅ Complete | Stage 6 — MVP + Phase 2 defined |
| Research Topics | ✅ Complete | 5 topics, 4 open questions |
| Risk Assessment | ✅ Complete | 5 risks identified |

---

## Executive Summary

**One-Sentence Vision:** A .NET Core CLI that orchestrates PCH agents in an autonomous loop, enabling fire-and-forget feature development from idea to implementation with minimal human intervention.

**Target Outcome:** System runs 4+ hours unattended, automatically progressing through vision → research → planning → review → code phases, answering its own clarifying questions, and only surfacing critical decisions to the user.

**Key Constraints:**
1. .NET Core on Windows
2. Uses GitHub Copilot SDK (Technical Preview) for agent orchestration
3. Single-user personal productivity tool (not enterprise-grade)

---

## Problem Statement

### Business Context

Current PCH agent workflow requires manual intervention at each phase boundary:
- User must manually invoke each agent (pch-visionary → pch-researcher → pch-planner → pch-coder)
- User must manually attach/pass documents between sessions
- Context is lost between sessions requiring re-reading of documents
- No automated validation gates between phases

### Current State

- **Workflow**: vision → research → planning → plan-review → coding (5+ handoffs)
- **Manual burden**: User must invoke each agent, attach documents, answer numerous clarifying questions
- **Questions block progress**: Planning and review phases ask many questions requiring human input
- **No external notification**: Questions only appear in VS Code chat interface
- **No autonomous mode**: Every question requires human decision

### Desired Future State

Fully automated orchestration loop that:
- Moves between agent phases automatically
- Passes required documents between sessions
- Uses fresh context per phase (Ralph Wiggum pattern)
- Validates completion before progressing
- **Autonomous mode**: Model can answer its own questions when enabled
- **External notifications**: Route questions to Teams/email/SMS when human input needed
- **Async operation**: Continue working while waiting for human responses

### Success Metrics

| Metric | Current Value | Target Value | Measurement Method |
|--------|---------------|--------------|-------------------|
| Autonomous runtime | Minutes (manual handoffs) | Hours (4+ hours unattended) | Time between human interventions |
| Phase transitions without intervention | 0% | 90%+ | Count automated vs manual handoffs |
| Questions answered autonomously | 0% | 80%+ | Autonomous vs human-answered |

---

## Stakeholders & Users

### Primary Stakeholders

| Stakeholder | Role | Interest | Influence |
|-------------|------|----------|-----------|
| Developer (you) | Primary user | Autonomous development, reduced manual work | High — sole user and decision-maker |

### User Personas

#### Solo Developer

- **Role:** Feature initiator, output reviewer, system operator
- **Goals:** Minimize manual intervention; let system run autonomously for hours
- **Pain Points:** Too many handoffs; questions interrupt workflow; context switching
- **Technical Proficiency:** High (.NET Core, AI/ML, agent systems)

---

## Goals & Objectives

### Primary Goals

| ID | Goal | Priority | Success Indicator |
|----|------|----------|-------------------|
| G-1 | Enable fire-and-forget feature initiation with autonomous agent progression through vision → research → planning → review → code | Must Have | System runs 4+ hours unattended |
| G-2 | Autonomous question answering — model answers its own clarifying questions when enabled | Must Have | 80%+ questions handled without human |
| G-3 | External notification routing — route critical questions to Teams/email/SMS | Should Have | User receives notifications outside VS Code |

### Non-Goals (Explicitly Out of Scope)

- **Multi-user/team support** — Single user only; no collaboration features
- **GUI/Dashboard** — No web interface; CLI and logs only
- **Deployment automation** — Builds code but does not deploy
- **Custom agent development** — Uses existing PCH agents; not a framework for new agents
- **Cross-repository operation** — Single repo at a time
- **Production-grade reliability** — Personal productivity tool, not enterprise SLA

---

## Requirements Overview

### Functional Requirements (High-Level)

| ID | Requirement | Priority | Product Phase |
|----|-------------|----------|---------------|
| FR-1 | Orchestrate agent phases — Automatically invoke pch-visionary → pch-researcher → pch-planner → pch-plan-reviewer → pch-coder in sequence | Must Have | Phase 1 (MVP) |
| FR-2 | Pass documents between phases — Automatically attach vision/research/plan docs to next agent session | Must Have | Phase 1 (MVP) |
| FR-3 | Fresh context per phase — Spawn new agent session for each phase (Ralph Wiggum pattern) | Must Have | Phase 1 (MVP) |
| FR-4 | Autonomous question answering — Model answers its own clarifying questions when autonomous mode enabled | Must Have | Phase 1 (MVP) |
| FR-5 | Completion detection — Detect when a phase is complete and ready for handoff | Must Have | Phase 1 (MVP) |
| FR-6 | External notifications — Route questions to Teams/email/SMS when human input needed | Should Have | Phase 2 |

### Non-Functional Requirements

| ID | Requirement | Priority | Constraint Type |
|----|-------------|----------|-----------------|
| NFR-1 | Reliability — Recover gracefully from failures; retry failed phases automatically | Must Have | Reliability |
| NFR-2 | Observability — Comprehensive logging to see loop activity and debug issues | Must Have | Maintainability |
| NFR-3 | Resilience — Handle agent crashes, API timeouts, malformed outputs gracefully | Must Have | Fault Tolerance |

### Constraints

| Constraint Type | Description | Impact |
|-----------------|-------------|--------|
| Technical | .NET Core on Windows | Runtime platform requirement |
| Technical | GitHub Copilot SDK (preferred, not mandated) | Exploratory — may use alternative if SDK doesn't fit |
| Technical | Copilot SDK is in Technical Preview | API may change; design for flexibility |
| Technical | Uses existing PCH agents | Agent orchestration, not agent development |

---

## Architecture Vision

### Solution Overview

A .NET Core console application that orchestrates PCH agents in an autonomous loop. The CLI is invoked with a feature request, then progresses through agent phases automatically, using the Ralph Wiggum pattern of fresh context per phase.

### Architecture Style

| Aspect | Choice | Rationale |
|--------|--------|-----------|
| Overall Pattern | Console Application (CLI) | Simple invocation, fire-and-forget, logs to console/file |
| Agent Invocation | Copilot SDK (or alternative) | Programmatic agent orchestration |
| State Management | File-based (documents) | Vision/research/plan docs persist state between phases |
| Logging | Console + File | Observability requirement |

### Key Technology Decisions

| Decision Area | Choice | Alternatives Considered | Rationale |
|---------------|--------|------------------------|-----------|
| Runtime | .NET Core | | User requirement |
| Platform | Windows | | User requirement |
| SDK | GitHub Copilot SDK | Direct API calls, VS Code CLI | Preferred — handles LLM orchestration |
| Agent Invocation | Custom agent definitions via SDK | VS Code extension, direct LLM calls | Load PCH prompts into SDK as custom agents |
| Agent System | PCH Agents | | Existing agent definitions |

### Integration Points

| System | Integration Type | Purpose | Criticality |
|--------|-----------------|---------|-------------|
| Copilot CLI | JSON-RPC (via SDK) | Agent runtime | Required for MVP |
| PCH Agents | Prompt Files (.md) | Agent definitions | Required for MVP |
| File System | Read/Write | Document persistence between phases | Required for MVP |

### Security Considerations

| Concern | Approach | Notes |
|---------|----------|-------|
| File system access | Minimal restrictions | Personal tool, trusted environment |
| Code modifications | Git provides rollback | Standard version control safety net |
| Audit | Basic logging (NFR-2) | Observability covers audit needs |

---

## Product Phasing Strategy

### Phase Overview

| Phase | Name | Scope Summary | Target Date | Status |
|-------|------|---------------|-------------|--------|
| 1 | MVP — Core Loop | Autonomous agent orchestration with fresh context | TBD | ⏳ Not Started |
| 2 | Enhanced Communication | External notifications (Teams/email/SMS) | TBD | ⏳ Not Started |

### Phase 1: MVP

**Goal:** Enable fire-and-forget feature development with autonomous agent progression

**Included Capabilities:**

- FR-1: Orchestrate agent phases (vision → research → planning → review → code)
- FR-2: Pass documents between phases automatically
- FR-3: Fresh context per phase (Ralph Wiggum pattern)
- FR-4: Autonomous question answering
- FR-5: Completion detection
- NFR-1: Reliability (retry failed phases)
- NFR-2: Observability (logging)
- NFR-3: Resilience (handle crashes/timeouts)

**Excluded from MVP (Deferred):**

- FR-6: External notifications (Teams/email/SMS) → Phase 2

**Definition of Done:**

- [ ] CLI can be invoked with a feature request
- [ ] System progresses through all 5 agent phases autonomously
- [ ] Documents are passed between phases automatically
- [ ] Autonomous mode answers questions without human input
- [ ] System runs 4+ hours unattended on a typical feature

### Future Phases (Roadmap)

| Phase | Tentative Scope | Dependencies |
|-------|-----------------|--------------|
| Phase 2 | External notifications (Teams/email/SMS) | Phase 1 complete |
| Phase 3 | Parallel agent execution (future consideration) | Phase 2 complete |

---

## Research Topics

| Topic | Priority | Key Questions | Target Phase |
|-------|----------|---------------|--------------|
| Copilot SDK .NET Custom Agents | High | How to load PCH agent prompts as custom agents in the SDK? | Phase 1 |
| Fresh Context Implementation | High | How to spawn completely new agent sessions programmatically? | Phase 1 |
| Completion Detection | High | How to detect when an agent has completed its phase? What signals completion? | Phase 1 |
| Autonomous Question Answering | High | How to intercept questions and route them to the model for self-answering? | Phase 1 |
| Document Attachment API | Medium | How to programmatically attach documents to agent sessions? | Phase 1 |

### Open Questions

| Question | Impact if Unresolved | Research Needed? |
|----------|---------------------|------------------|
| How does Copilot SDK manage tool permissions? | Could limit automation capabilities | Yes |
| Can PCH agents be invoked headlessly outside VS Code? | Core to self-operation | Yes |
| What signals does an agent emit when asking a question vs completing? | Needed for autonomous mode | Yes |
| How to handle multi-phase plans in pch-coder (phases within phases)? | Orchestration complexity | Yes |

---

## Risk Assessment (Vision-Level)

| Risk | Likelihood | Impact | Mitigation Strategy |
|------|------------|--------|---------------------|
| Copilot SDK in Technical Preview — API may change | Medium | High | Design abstraction layer; isolate SDK calls |
| Custom agent loading not supported as envisioned | Medium | High | Research early; have fallback (direct LLM API) |
| Headless VS Code agent invocation not feasible | Medium | Critical | Research in Phase 1; may require architecture pivot |
| Context window limits despite fresh context | Low | Medium | Implement Ralph-style document summarization |
| Completion detection unreliable | Medium | Medium | Define explicit completion markers in agent prompts |

---

## Referenced Standards

| Standard | Source | Relevance | Key Guidance Applied |
|----------|--------|-----------|---------------------|

> Note: Standards queried from pch-standards-space during vision capture.

---

## Handoff Information

**Created By:** pch-visionary
**Created Date:** 2026-02-05
**Status:** ✅ Complete — Ready for Research
**Next Agent:** pch-researcher
**Vision Document Location:** `/docs/vision/002-self-operating-development-loop.md`

**Research Priority Areas:**
1. Copilot SDK .NET custom agent loading
2. Headless agent invocation feasibility
3. Completion detection mechanisms
4. Autonomous question interception
