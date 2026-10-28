id: "018"
type: research
title: "AI-Efficient Markdown Document Format for PCH Agents"
status: complete
created: "2025-02-11"
current_phase: "5 of 5"

introduction: |
  The PCH agent prompts (pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer)
  were previously converted from Markdown to YAML document format (plan 012) to reduce
  token usage. This produced inconsistent results from the AI — YAML's strict formatting
  rules and indentation sensitivity made generation unreliable. This research investigates
  each agent's current document generation instructions and designs a consistent, compact
  Markdown format that is optimized for AI consumption while avoiding the consistency
  problems of YAML.

objectives:
  - "What specific YAML formatting issues are causing inconsistency in each agent's output?"
  - "What Markdown conventions can achieve compactness comparable to YAML without the parsing fragility?"
  - "How should a consistent, AI-efficient Markdown document format be defined across all four agents?"
  - "What shared conventions (headers, metadata, status values, tables vs lists) should be standardized?"
  - "How should the consumer agents (pch-plan-reviewer, pch-coder) be updated to read the new format?"

phases:
  - phase: 1
    name: "pch-visionary — Current YAML Template Analysis"
    status: complete
    scope:
      - "Read the full pch-visionary.md agent prompt"
      - "Identify the YAML vision document template and all YAML-referencing instructions"
      - "Catalog all YAML keys, nested structures, and multiline blocks in the template"
      - "Analyze which YAML constructs are most prone to inconsistent generation"
      - "Identify what format the original pre-YAML Markdown template used (from plan 005 / research 005)"
      - "Note which sections are freeform prose vs structured data"
    session: "2025-02-11"
    findings: |
      ## 1. Complete YAML Key Hierarchy

      The pch-visionary YAML template has ~14 top-level keys with significant nesting:

      **Simple scalars (7):** id, type, title, status, created, owner, version
      **Multiline blocks (2):** introduction (literal block scalar), phasing.ordering_rationale
      **Arrays of objects (5):** decisions[], stakeholders[], research_topics[], risks[], standards[]
      **Nested objects (4):** problem{}, goals{}, requirements{}, architecture{}
      **Deeply nested (1):** phasing{} — contains phases[] with 8 keys per object and 2 nested arrays

      ## 2. YAML Constructs Most Prone to Inconsistency

      **HIGH RISK — Deep Nesting (4 levels):**
      phasing.phases[].research_topics[].questions[] requires 8 spaces of indentation.
      AI frequently misaligns indentation at this depth.

      **HIGH RISK — Mixed Array Styles:**
      Template uses BOTH inline arrays (["FR-1", "FR-2"]) AND block arrays (- item)
      within the same level, causing AI to switch styles unpredictably.

      **MEDIUM RISK — Enum-Like Values:**
      Fields like category have 8 possible values shown with pipe separators;
      AI must pick exactly one, but long enum lists increase error chance.

      **MEDIUM RISK — Literal Block Scalars:**
      introduction: | requires precise indentation of continuation lines;
      empty lines within blocks can cause unexpected YAML behavior.

      **LOW RISK:** Simple key-value scalar fields are generally reliable.

      ## 3. Original Pre-YAML Markdown Template (from Research 005)

      The original template used:
      - 14 ## sections with ### subsections (2-3 levels of heading hierarchy)
      - Markdown tables for structured data (| Field | Value |)
      - Bullet points for lists
      - Emoji status values (🔄, ✅, ⏳)
      - Direct prose under headers for freeform text

      Key difference: Original Markdown was much flatter (2-3 nesting levels vs 4+
      in YAML). Tables provided visual structure without indentation sensitivity.

      ## 4. Freeform vs Structured Data

      **Freeform prose (13+ fields):** introduction, problem.context, problem.current_state,
      problem.desired_state, decision answers/impacts, goal descriptions, requirement
      descriptions, constraint descriptions, technology rationales, risk mitigations,
      standards guidance.

      **Structured data — HIGH risk:** phasing.phases[] (8 keys, 2 nested arrays per object)
      **Structured data — MEDIUM risk:** decisions[] (7 keys), requirements.functional[]
      (4 keys), architecture.technology_decisions[] (3 keys), architecture.integrations[] (4 keys)
      **Structured data — LOW risk:** stakeholders[] (4 keys), goals.primary[] (4 keys),
      requirements.constraints[] (3 keys), risks[] (4 keys), handoff{} (5 keys)

      ## 5. YAML-Referencing Instructions in Agent Prompt

      The agent prompt contains explicit YAML generation instructions including:
      - "AI-Optimized Format" section explaining YAML reduces tokens by ~65-75%
      - "Incremental Document Building" requiring building piece by piece
      - "Status Values" section defining plain text values
      - replace_string_in_file with exact YAML string matching guidance
    key_discoveries:
      - "4 levels of nesting in phasing.phases[].research_topics[].questions[] makes indentation errors highly likely"
      - "Template inconsistently uses both inline arrays and block arrays, confusing AI generation"
      - "phasing.phases[] with 8 keys and 2 nested arrays per object is the highest-risk section"
      - "Original Markdown template used tables for structured data — visually clear but token-heavy"
      - "Plan 012 chose 'Structured Top-Level, Freeform Content' but current template has structured content at multiple levels"
      - "Plain text status values replaced emoji and are easier to parse"
    relevant_files:
      - path: agents/pch-visionary.md
        note: "Full agent prompt with YAML template (1856 lines)"
      - path: docs/research/005-pch-visionary-agent-design.md
        note: "Research containing original Markdown template"
      - path: docs/plans/012-ai-optimized-document-format.md
        note: "Plan that converted Markdown to YAML"
    gaps: []
    assumptions:
      - "Inconsistent AI generation manifests primarily as YAML indentation errors and structural malformation"

  - phase: 2
    name: "pch-researcher — Current YAML Template Analysis"
    status: complete
    scope:
      - "Read the full pch-researcher.md agent prompt"
      - "Identify the YAML research document template and all YAML-referencing instructions"
      - "Catalog all YAML keys, nested structures, and the subagent return format"
      - "Analyze how the orchestrator maps subagent results to YAML document keys"
      - "Note which sections are freeform findings vs structured metadata"
    session: "2025-02-11"
    findings: |
      ## YAML Research Document Template

      The YAML template (lines 1438-1488) has 13 top-level keys with max 3-level nesting
      (simpler than pch-visionary's 4 levels).

      **Top-level keys (13):** id, type, title, status, created, current_phase, vision_source,
      introduction, objectives, phases, overview, key_findings, actionable_conclusions,
      open_questions, standards, handoff

      **Phases[] structure:** 10 keys per phase object with 6 nested arrays, all at level 3:
      scope[], key_discoveries[], relevant_files[{path,note}], external_sources[{url,note}],
      gaps[], assumptions[]

      **Freeform fields (3):** introduction (|), findings (|), overview (|)
      The findings field is designed for large prose with embedded code examples.

      ## Subagent Return Format

      The subagent return format is text-based (line-delimited key:value), NOT YAML.
      16 fields including STATUS, FINDINGS, KEY_DISCOVERIES, RELEVANT_FILES, etc.
      The orchestrator must parse this text format and map it to YAML document keys.

      ## Orchestrator Mapping

      | Subagent Field | YAML Location |
      |----------------|---------------|
      | FINDINGS | phases[N].findings |
      | KEY_DISCOVERIES | phases[N].key_discoveries[] |
      | RELEVANT_FILES | phases[N].relevant_files[] |
      | GAPS_FOUND | phases[N].gaps[] |
      | ASSUMPTIONS_MADE | phases[N].assumptions[] |
      | STATUS | phases[N].status |

      ## Comparison to pch-visionary

      | Aspect | pch-visionary | pch-researcher |
      |--------|---------------|----------------|
      | Max nesting depth | 4 levels | 3 levels |
      | Inline/block array mixing | Yes (inconsistent) | No (all block) |
      | Keys per phase object | 8 + 2 nested arrays | 10 + 6 nested arrays |
      | Freeform fields | 3 | 3 |

      ## YAML Generation Risks

      **Lower risk than pch-visionary:** No 4-level nesting, no array style mixing,
      simpler nested objects (max 2-3 keys).

      **Remaining challenges:** Multiline string indentation in findings: |, object
      arrays (relevant_files[], external_sources[]), special character escaping in
      code examples within findings, incremental replace_string_in_file on YAML.
    key_discoveries:
      - "Maximum nesting depth is 3 levels (vs 4 in pch-visionary) — simpler structure"
      - "phases[] has 10 keys per phase with 6 nested arrays, all at level 3"
      - "Subagent return format is text-based (line-delimited key:value), NOT YAML — requires parsing/mapping"
      - "findings field uses YAML multiline string for large freeform prose with embedded code"
      - "Consistent block array style (no inline/block mixing unlike pch-visionary)"
      - "Explicit incremental document building instructions in prompt to mitigate YAML complexity"
    relevant_files:
      - path: agents/pch-researcher.md
        note: "Full agent prompt with YAML template (1810 lines)"
    gaps: []
    assumptions:
      - "The AI-Optimized Format section (lines 1438-1488) is the canonical YAML template currently in use"

  - phase: 3
    name: "pch-planner — Current YAML Template Analysis"
    status: complete
    scope:
      - "Read the full pch-planner.md agent prompt"
      - "Identify the YAML plan document template and all YAML-referencing instructions"
      - "Catalog the plan schema: phases, tasks, decisions, review log, etc."
      - "Analyze which plan constructs are most complex in YAML (nested task lists, conditions)"
      - "Note dependencies between planner output and plan-reviewer/coder consumption"
    session: "2025-02-11"
    findings: |
      ## YAML Plan Document Template

      15+ top-level YAML keys with max 4-level nesting (matches vision complexity).

      **Top-level keys (15+):** id, type, title, status, created, updated, owner,
      version, introduction, versions, decisions, holistic_review, overview,
      requirements, technical_design, dependencies, risks, phases, standards, handoff

      ## Most Complex Constructs (by nesting depth)

      **4 levels — phases[].tasks[]:**
      phases[].tasks[].acceptance_criteria[] and phases[].tasks[].files[]
      Each phase has 9 keys; each task has 5 keys (step, task, files, details,
      acceptance_criteria). The details field uses YAML multiline | for prose
      with embedded code examples.

      **4 levels — technical_design.data_models.new_classes[]:**
      new_classes[].properties[] and new_classes[].methods[]
      With namespace, file, description, plus nested arrays.

      **3 levels — holistic_review:**
      4 nested arrays: decision_interactions[], architectural_considerations[],
      trade_offs_accepted[], risks_acknowledged[]
      Each interaction/concern has multiline analysis/assessment/recommendation.

      **3 levels — requirements:**
      functional[], non_functional[], out_of_scope[] with id/description per item.

      ## YAML Style Patterns

      - Block array style only (no inline [] arrays)
      - Multiline strings via | for prose throughout
      - 2-space indentation
      - Plain text status values
      - Code embedded directly in multiline strings

      ## Downstream Consumption

      **pch-plan-reviewer:** Navigates by YAML keys (phases, tasks, technical_design).
      Checks task granularity (<30 min each). Adds review_session_log entries.

      **pch-coder:** Parses phases[] for not-started phases. Iterates tasks[] within
      each phase. Uses step identifiers (e.g., "3.1", "3.2") in TASKS_COMPLETED
      reporting. Reads acceptance_criteria to verify implementation.

      ## Cross-Agent Comparison

      | Dimension | Visionary | Researcher | Planner |
      |-----------|-----------|------------|--------|
      | Top-level keys | 14 | 13 | 15+ |
      | Max nesting | 4 | 3 | 4 |
      | Phase keys | 8 | 10 | 9 + nested tasks |
      | Unique complexity | research_topics 4-deep | subagent mapping | tasks with step IDs |
      | Array style | Mixed (inline+block) | Block only | Block only |
    key_discoveries:
      - "Plan documents have 15+ top-level YAML keys — most complex of the three agents"
      - "Maximum nesting depth is 4 levels: phases[].tasks[].acceptance_criteria[] and technical_design.data_models.new_classes[].properties[]"
      - "Each phase has 9 keys; each task has 5 keys — significantly more structured than research phases"
      - "holistic_review section has 4 nested array types with multiline prose"
      - "pch-coder parses task step identifiers (e.g., '3.1') for TASKS_COMPLETED reporting — must preserve in format change"
      - "Block array style is mandatory (no inline arrays) — matches research convention"
      - "Plan-reviewer navigates by YAML keys explicitly"
    relevant_files:
      - path: agents/pch-planner.md
        note: "Full agent prompt with YAML template (570 lines)"
      - path: docs/plans/019-hugh-sdk-tool-hooks-integration.md
        note: "Real plan document showing template in use"
    gaps: []
    assumptions: []

  - phase: 4
    name: "pch-plan-reviewer & Consumer Agent Analysis"
    status: complete
    scope:
      - "Read pch-plan-reviewer.md — identify how it reads/parses plan documents"
      - "Read pch-coder.md — identify how it reads plan documents for implementation"
      - "Identify format assumptions these agents make (heading structure, key names, etc.)"
      - "Determine what format changes would be needed for these consumers"
      - "Assess whether the copilot-instructions.md also needs format updates"
    session: "2025-02-11"
    findings: |
      ## pch-plan-reviewer Document Navigation

      Explicitly states (line 23): "Implementation plans now use YAML format with
      structured keys. When reviewing plans, navigate by YAML keys (e.g., phases,
      tasks, technical_design) rather than Markdown headings."

      Uses review_session_log in full YAML structure with entries[] containing
      number, issue, category, decision, plan_update keys.
      Expects handoff section in YAML format.

      ## pch-coder Document Navigation

      Explicitly states (line 13): "Implementation plans now use YAML format with
      structured keys. Navigate plans by YAML keys (e.g., phases, tasks)."

      References task IDs as step numbers ("3.1", "3.2") in TASKS_COMPLETED.
      Updates task statuses in YAML format (step, task, status, notes keys).
      Deployment checkpoint format uses YAML.

      ## CRITICAL: Hugh PlanPhaseParser Format Mismatch

      Hugh's PlanPhaseParser.cs uses MARKDOWN TABLE parsing with EMOJI status,
      NOT YAML parsing:

      Regex: | 1 | Phase Name | [emoji] Status | 0/5 |
      Emoji mapping: ✅=Complete, 🔄=InProgress, ⏳=NotStarted, ❌=Blocked

      This contradicts agent prompts which instruct YAML format with plain text
      status values. The parser expects old Markdown format.

      ## Subagent Communication

      Both pch-coder and pch-researcher use identical TEXT-BASED return formats
      (STATUS:, TASKS_COMPLETED:, etc.) — NOT YAML. Parsed by SignalClassifier
      via regex. A document format change would NOT affect inter-agent communication.

      ## copilot-instructions.md

      References YAML format in session log structure and progress status values.
      Documents Hugh architecture mentioning YAML front matter and YAML document format.

      ## pch-plan-reviewer as Document PRODUCER

      The reviewer also PRODUCES/MODIFIES 6 YAML structures in plan documents:

      **Structures it GENERATES (3):**

      1. review_session_log: 3-level nesting (entries[] with 5 keys each).
         Includes questions_pending, questions_resolved, last_updated, entries[].

      2. implementation_complexity: INCONSISTENT SCHEMA — two incompatible formats
         found in recent plans:
         - Plan 017: Flat format with 12 scalar properties (files_to_modify,
           new_patterns_score, overall_score, etc.)
         - Plan 018: Nested format with factors[] array of objects
         Same reviewer produced both — clear example of YAML format drift.

      3. review_summary: 3-level nesting with review_metrics{} containing 6 keys
         plus 3 nested arrays (sections_updated[], key_improvements[],
         remaining_considerations[]).

      **Structures it MODIFIES (3):**

      1. versions[]: Appends new version entry via replace_string_in_file
      2. handoff{}: Adds reviewed_by, review_date, next_agent; modifies status
      3. Top-level status: Changes from in-review to ready-for-implementation

      **Format Drift Evidence:** The reviewer already handles BOTH Markdown tables
      (older plans like 012) AND YAML (newer plans 015-019) for version history
      and review session logs — proving it can work with either format.

      ## Format Change Impact Analysis

      | Component | Current | Change Needed |
      |-----------|---------|---------------|
      | pch-plan-reviewer | "Navigate by YAML keys" + YAML production | Update navigation AND output templates |
      | pch-coder | "Navigate by YAML keys" | Update to headers |
      | pch-researcher | YAML document template | Template rewrite |
      | copilot-instructions | YAML examples | Update examples |
      | Hugh PlanPhaseParser | ALREADY Markdown tables | No change! |
      | SignalClassifier | Text regex | No change |
      | Subagent return format | Text-based | No change |
    key_discoveries:
      - "pch-plan-reviewer and pch-coder both explicitly instruct navigating by 'YAML keys'"
      - "CRITICAL: Hugh PlanPhaseParser ALREADY parses Markdown tables with emoji status — NOT YAML — format mismatch exists today"
      - "Subagent return format is text-based, not YAML — format change won't affect inter-agent communication"
      - "review_session_log and deployment_checkpoint use full YAML structure in prompts"
      - "copilot-instructions.md documents YAML format but Hugh's parser doesn't parse YAML"
      - "pch-plan-reviewer PRODUCES 3 YAML structures (review_session_log, implementation_complexity, review_summary) and MODIFIES 3 others"
      - "implementation_complexity has TWO incompatible YAML schemas (flat vs nested factors[]) in recent plans — clear schema drift"
      - "Reviewer already handles both Markdown tables (old plans) and YAML (new plans) — proving format flexibility"
    relevant_files:
      - path: agents/pch-plan-reviewer.md
        note: "Lines 23, 379-399, 554-565 contain YAML format references; also produces review_session_log, implementation_complexity, review_summary"
      - path: agents/pch-coder.md
        note: "Lines 13, 246-286, 351-365 contain YAML key navigation and task format"
      - path: agents/pch-researcher.md
        note: "Lines 422-486 (subagent prompt), 1391-1461 (document template)"
      - path: Hugh/Detection/PlanPhaseParser.cs
        note: "Parses Markdown tables with emoji status — NOT YAML"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "Text-based signal detection regex"
      - path: docs/plans/017-hugh-agent-github-authentication-installation.md
        note: "Shows flat implementation_complexity YAML format"
      - path: docs/plans/018-hugh-agent-installation-fix.md
        note: "Shows nested factors[] implementation_complexity YAML format (inconsistent with 017)"
      - path: docs/plans/012-ai-optimized-document-format.md
        note: "Older plan using Markdown tables for version history and review log"
    gaps:
      - "Did not verify actual plan documents to confirm whether YAML or Markdown tables are used in practice"
    assumptions:
      - "Hugh PlanPhaseParser behavior is active runtime behavior, not deprecated code"

  - phase: 5
    name: "Design Consistent AI-Efficient Markdown Format"
    status: complete
    scope:
      - "Define shared Markdown conventions for all document types (metadata block, status, headers)"
      - "Design compact Markdown templates for vision, research, and plan documents"
      - "Specify when to use tables vs bullet lists vs inline metadata"
      - "Define status value conventions (plain text vs emoji)"
      - "Ensure the format is easy for AI to generate consistently and parse reliably"
      - "Compare token usage: original verbose Markdown vs YAML vs proposed compact Markdown"
      - "Draft specific recommendations for each agent prompt's template replacement"
    session: "2025-02-11"
    findings: |
      ## Design Specification: AI-Efficient Markdown Format

      ### Shared Conventions (All Document Types)

      **Metadata Block:** Triple-dash fenced block at document top with simple
      key: value lines (NOT YAML — plain text regex-parseable). 6-10 lines max.
      Required keys: id, type, title, status, created.

      **Status Values (emoji, matching Hugh PlanPhaseParser):**
      ⏳ Not Started | 🔄 In Progress | ✅ Complete | ❌ Blocked | ⚠️ Partial

      **Header Hierarchy:** Max 2 levels (## major sections, ### subsections).
      No #### usage. Bold labels for inline key-value within prose.

      **Structured Data Rules:**
      - Tabular data 3+ columns → Markdown table
      - Status tracking with progress → Markdown table
      - Simple lists → Bullet list
      - Short arrays (2-5 items) → Inline with bold label
      - Freeform prose → Direct text under header
      - Code examples → Fenced code blocks (standard Markdown)

      ### Token Usage Comparison

      | Format | Approx. Tokens | Relative |
      |--------|---------------|----------|
      | Original verbose Markdown | ~4,200 | 100% |
      | Current YAML | ~2,800 | 67% |
      | Proposed compact Markdown | ~3,100 | 74% |

      7% more tokens than YAML is acceptable given: elimination of YAML
      malformation errors, zero Hugh parser changes, familiar AI patterns.

      ### Template Designs

      **Vision:** ---metadata--- then ## sections for Introduction, Problem Space,
      Stakeholders (table), Goals (bullets), Requirements (tables), Architecture
      Vision (tables), Product Phasing (table + ### Phase N subsections with
      scope/research topics/done criteria as bullets), Risks (table), Research
      Topics, Decisions Log (table), Handoff.

      **Research:** ---metadata--- then ## sections for Introduction, Objectives
      (bullets), Research Phases (table), Phase N subsections with Findings
      (prose), Key Discoveries (bullets), Files Analyzed (table), Gaps, Assumptions.
      Overview section at end.

      **Plan:** ---metadata--- then ## sections for Introduction, Version History
      (table), Decisions Log (table), Overview, Requirements (tables), Technical
      Design (tables + code blocks), Dependencies (table), Risks (table), Progress
      Summary (table matching Hugh's parser), Execution Plan with ### Phase N
      subsections containing task tables (Step | Task | Files | Criteria),
      Holistic Review (prose), Review Session Log (table), Handoff.

      ### Agent Prompt Update Recommendations

      | Agent | LOE | Key Change |
      |-------|-----|------------|
      | pch-researcher | 2-3 hrs | YAML template → Markdown template |
      | pch-planner | 3-4 hrs | Most complex; phases[].tasks[] → tables |
      | pch-visionary | 2-3 hrs | YAML template → Markdown with tables |
      | pch-plan-reviewer | 2-3 hrs | Navigation update + review_session_log/implementation_complexity/review_summary output → Markdown tables |
      | pch-coder | 1-2 hrs | "Navigate by YAML keys" → headers |
      | copilot-instructions | 1 hr | Update YAML references |
      | Hugh | 0 hrs | No changes needed! |

      Total estimated LOE: 12-17 hours across all agents.

      ### pch-plan-reviewer Specific Recommendations

      The reviewer's YAML output structures map to Markdown as follows:

      | YAML Structure | Markdown Replacement |
      |----------------|---------------------|
      | review_session_log.entries[] | ## Review Session Log table: # / Issue / Category / Decision / Update |
      | implementation_complexity (either schema) | ## Complexity Assessment table: Factor / Score / Notes |
      | review_summary.review_metrics | ## Review Summary with bold labels + bullets |
      | versions[] append | Append row to Version History table |
      | handoff modification | Update bold label values under ## Handoff |

      This also standardizes the implementation_complexity schema — the table
      format forces a consistent Factor / Score / Notes structure, eliminating
      the flat-vs-nested schema drift discovered between plans 017 and 018.

      ### Migration Order

      1. pch-researcher (simplest, good test case)
      2. pch-planner (most complex, benefits most)
      3. pch-visionary (medium complexity)
      4. pch-plan-reviewer (consumer, depends on 2)
      5. pch-coder (consumer, depends on 2)
      6. copilot-instructions (documentation last)

      Existing documents need NO migration — format applies to new documents only.
    key_discoveries:
      - "Hugh PlanPhaseParser already expects Markdown tables with emoji — proposed format requires ZERO parser changes"
      - "Max 2-level header hierarchy (## major, ### subsection) eliminates deep nesting problems"
      - "Tables replace deeply-nested YAML arrays (phases[].tasks[] becomes table rows under ### Phase N)"
      - "Token usage is ~74% of original verbose Markdown (7% more than YAML) — acceptable trade-off for reliability"
      - "All YAML multiline block scalars become direct prose under section headers"
      - "Subagent return format already text-based — no changes needed for inter-agent communication"
      - "Shared conventions (metadata block, emoji status, table formats) apply uniformly across all three document types"
      - "Migration order: pch-researcher → pch-planner → pch-visionary → pch-plan-reviewer → pch-coder → docs"
      - "Total estimated implementation LOE: 12-17 hours"
      - "pch-plan-reviewer produces 3 YAML structures with schema drift — Markdown tables would standardize them"
    relevant_files:
      - path: Hugh/Detection/PlanPhaseParser.cs
        note: "Already parses Markdown tables with emoji — no changes needed"
      - path: agents/pch-visionary.md
        note: "Lines 1438-1600 need YAML→Markdown template replacement"
      - path: agents/pch-researcher.md
        note: "Lines 1389-1488 need YAML→Markdown template replacement"
      - path: agents/pch-planner.md
        note: "Most complex rewrite due to phases[].tasks[] structure"
      - path: agents/pch-plan-reviewer.md
        note: "Line 23 needs 'navigate by YAML keys' updated"
      - path: agents/pch-coder.md
        note: "Line 13 needs 'navigate by YAML keys' updated"
    gaps:
      - "Did not measure actual token counts from real documents (estimates based on structure analysis)"
      - "Did not test proposed format with AI generation empirically"
    assumptions:
      - "7% token increase vs YAML is acceptable trade-off for generation reliability"
      - "AI models generate Markdown tables more reliably than complex YAML structures"

overview: |
  This research analyzed the YAML document format currently used by four PCH agents
  (pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer/pch-coder) and
  designed a replacement compact Markdown format that eliminates YAML's consistency
  problems while maintaining token efficiency.

  The root cause of YAML generation inconsistency is deep nesting (up to 4 levels),
  mixed inline/block array styles, and indentation-sensitive multiline block scalars.
  The pch-planner and pch-visionary templates are worst affected, with constructs
  like phases[].tasks[].acceptance_criteria[] requiring 8+ spaces of indentation.

  A critical discovery was that Hugh's PlanPhaseParser already parses Markdown tables
  with emoji status indicators — NOT YAML — meaning the codebase is already aligned
  with Markdown format. The proposed compact Markdown format leverages this alignment,
  requiring zero Hugh parser changes.

  The proposed format uses: a simple --- metadata block, max 2-level headers (##/###),
  Markdown tables for structured data, bullet lists for simple arrays, direct prose
  under headers (no YAML block scalars), and emoji status values matching Hugh's parser.
  Token usage is ~74% of original verbose Markdown (7% more than YAML) — an acceptable
  trade-off for reliable AI generation.

  Supplementary analysis of pch-plan-reviewer as a document PRODUCER revealed it generates
  3 YAML structures (review_session_log, implementation_complexity, review_summary) and
  modifies 3 others (versions[], handoff, status). A critical finding was that
  implementation_complexity has TWO incompatible YAML schemas across recent plans (flat
  scalars in plan 017 vs nested factors[] in plan 018) — a clear example of schema drift
  caused by freeform YAML generation. Switching to Markdown tables would standardize this
  output with a consistent Factor / Score / Notes column structure. The reviewer already
  handles both Markdown tables (older plans) and YAML (newer plans), proving format
  migration is feasible.

key_findings:
  - "YAML's 4-level nesting (phasing.phases[].research_topics[].questions[] and phases[].tasks[].acceptance_criteria[]) is the primary cause of inconsistent AI generation"
  - "Hugh's PlanPhaseParser already parses Markdown tables with emoji status — NOT YAML — so switching to Markdown requires zero code changes"
  - "The proposed compact Markdown format uses ~74% tokens of verbose Markdown (7% more than YAML), acceptable for eliminating generation failures"
  - "All three document types (vision, research, plan) can share unified conventions: --- metadata, ## headers, Markdown tables, emoji status"
  - "Subagent return formats are text-based (not YAML), so inter-agent communication needs no changes"
  - "The pch-planner template is the most complex (15+ keys, 4-level nesting) and benefits most from the Markdown simplification"
  - "Migration can be done incrementally: one agent at a time, new documents only, estimated 12-17 hours total"
  - "pch-plan-reviewer produces 3 YAML structures with schema drift (implementation_complexity has 2 incompatible schemas) — Markdown tables would standardize output"

actionable_conclusions:
  - "Replace YAML document templates with compact Markdown templates in all agent prompts"
  - "Use emoji status values (matching Hugh's PlanPhaseParser) instead of plain text status"
  - "Flatten all deeply-nested YAML structures into Markdown tables under ### subsections"
  - "Update pch-plan-reviewer and pch-coder 'navigate by YAML keys' instructions to 'navigate by ## section headers'"
  - "Migrate agents in order: pch-researcher, pch-planner, pch-visionary, pch-plan-reviewer, pch-coder, copilot-instructions"
  - "Standardize pch-plan-reviewer output templates (review_session_log, implementation_complexity, review_summary) as Markdown tables to eliminate schema drift"
  - "No changes needed to Hugh codebase (PlanPhaseParser, SignalClassifier, SdkResponseProcessor)"

open_questions:
  - "Should the --- metadata block use YAML front matter parsing or stay as plain text regex-parseable?"
  - "Should existing documents be migrated to the new format or left as-is (current recommendation: new documents only)?"
  - "What validation should Hugh add to catch malformed Markdown documents early?"

standards: []

handoff:
  created_by: pch-researcher
  created_date: "2025-02-11"
  status: complete
  current_phase: complete
  path: /docs/research/018-markdown-document-format-for-pch-agents.md
