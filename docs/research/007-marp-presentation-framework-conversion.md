# Research: Marp Presentation Framework Conversion

## Document Information

| Field | Value |
|-------|-------|
| Document | 007-marp-presentation-framework-conversion.md |
| Created | 2026-02-02 |
| Status | ✅ Complete |
| Current Phase | 4 of 4 (All Phases Complete) |

## Overview

This research document provides a comprehensive analysis and conversion strategy for migrating the PCH Agent Workflow presentation from Reveal.js to the Marp framework, matching the pattern used in the pch_argus project.

### Executive Summary

The conversion is **feasible with medium effort (~4 hours)**. The current 28-slide Reveal.js presentation (645 lines HTML + 989 lines CSS) will become a single ~500-line Marp markdown file with inline CSS. Most features translate directly; the main losses are vertical navigation and non-list fragment animations.

### Key Findings

1. **Marp Framework Structure** (Phase 1)
   - Single markdown file with YAML front matter containing all configuration
   - `---` slide separators replace `<section>` tags
   - Inline CSS in `style:` block (~100 lines) replaces external stylesheet
   - Mermaid diagrams via CDN script for flowcharts
   - Per-slide directives using `<!-- _class: name -->` syntax

2. **Current Presentation Analysis** (Phase 2)
   - 28+ slides across 7 logical sections
   - Heavy fragment usage for incremental reveals
   - Custom workflow diagram with `.agent-node` CSS
   - Agent color-coding throughout (6 distinct colors)
   - No images — text-only presentation simplifies migration

3. **Theming Compatibility** (Phase 3)
   - All VS Code Dark+ colors port directly to Marp
   - Agent accent colors work with simple class definitions
   - Two-column layout uses identical flexbox CSS
   - Fragments limited to list items only (using `*` marker)
   - Complete Marp style block created (~100 lines)

4. **Conversion Strategy** (Phase 4)
   - Feature mapping table: 20+ Reveal.js features mapped to Marp equivalents
   - Workflow diagram converts to Mermaid with classDef styling
   - Step-by-step checklist with 5 phases
   - Effort estimate: ~4 hours total

### What Works Well

| Feature | Status |
|---------|--------|
| VS Code Dark+ theme colors | ✅ Direct port |
| Agent color classes | ✅ Same CSS |
| Two-column layouts | ✅ Identical pattern |
| Markdown tables | ✅ Native support |
| Code blocks with syntax highlighting | ✅ Built-in |
| Fragment lists (bullet/numbered) | ✅ Use `*` or `)` markers |
| Speaker notes | ✅ HTML comments |
| Slide numbers | ✅ `paginate: true` |

### What's Lost

| Feature | Impact | Mitigation |
|---------|--------|------------|
| Vertical navigation | Medium | Flatten to linear; add section titles |
| Non-list fragments | Medium | Show all at once; split slides if needed |
| Animation effects | Low | Accept instant appearance |
| Visual arrow controls | Low | Document keyboard shortcuts |
| Progress bar | Low | Slide numbers sufficient |

### Recommended File Structure

```
docs/presentations/
├── pch-agent-workflow.md      # Main Marp source
├── pch-agent-workflow.html    # Generated output
├── Build-Presentation.ps1     # Build script
└── README.md                  # Usage instructions
```

### Implementation Path

1. **Setup** (15 min) — Install Marp CLI, create directory structure
2. **Base Document** (30 min) — Create markdown with YAML front matter and inline CSS
3. **Content Migration** (2 hours) — Convert 7 sections, 28 slides
4. **Testing** (45 min) — Build, preview, refine styling
5. **Cleanup** (10 min) — Remove old presentation directory

### Conclusion

The migration to Marp is recommended. Benefits include:
- **Simpler maintenance** — Single markdown file vs. HTML + CSS
- **Consistent tooling** — Matches pch_argus presentation framework
- **Better diagrams** — Mermaid is more maintainable than custom CSS
- **Easier editing** — Markdown is more accessible than raw HTML

The trade-off of losing some interactivity (vertical navigation, non-list fragments) is acceptable given the improved maintainability and framework consistency.

---

## Research Objectives

Investigate the Marp presentation framework used in `C:\repos\pch_argus\docs\presentations` and determine what is needed to convert the current Reveal.js-based presentation in `.github-private\presentation` to use the same Marp framework and VS Code Dark styling.

### Key Questions to Answer

1. What is the complete Marp framework structure (markdown format, themes, scripts)?
2. What custom theming and styling is applied in the pch_argus presentation?
3. How do Marp slide separators, directives, and features map to current Reveal.js content?
4. What is the conversion strategy and effort required to migrate the existing presentation?

---

## Research Phases

| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 1 | Marp Framework Analysis | ✅ Complete | 2026-02-02 |
| 2 | Current Presentation Content Analysis | ✅ Complete | 2026-02-02 |
| 3 | Theme and Styling Comparison | ✅ Complete | 2026-02-02 |
| 4 | Conversion Strategy and Mapping | ✅ Complete026-02-02 |

---

## Phase 1: Marp Framework Analysis

**Status:** ✅ Complete
**Scope:** Deep-dive into the pch_argus Marp presentation structure

### Research Tasks

- ✅ Read `argus-edi-platform.md` to understand Marp markdown syntax and directives
- ✅ Analyze the Marp front matter configuration (theme, marp settings)
- ✅ Understand Marp slide separators (`---`) and section organization
- ✅ Document Marp-specific features used: speaker notes, fragments, code blocks
- ✅ Analyze `Build-Presentation.ps1` for build/generation process
- ✅ Identify any custom CSS or theming applied
- ✅ Document Mermaid diagram integration approach

### Findings

#### 1. Marp Front Matter Configuration

The Marp presentation uses YAML front matter with extensive inline CSS styling:

```yaml
---
marp: true
theme: default
paginate: true
backgroundColor: #1e1e1e
color: #cccccc
style: |
  section {
    background-color: #1e1e1e;
    color: #cccccc;
    font-family: 'Segoe UI', sans-serif;
    font-size: 24px;
    padding: 40px 50px;
  }
  h1 { color: #007acc; font-size: 1.8em; margin-bottom: 0.3em; }
  h2 { color: #007acc; font-size: 1.4em; margin-bottom: 0.3em; }
  h3 { color: #4ec9b0; font-size: 1.1em; margin-bottom: 0.2em; }
  a { color: #4ec9b0; }
  code { background-color: #3c3c3c; color: #dcdcaa; padding: 2px 6px; border-radius: 3px; font-size: 0.85em; }
  pre { background-color: #2d2d30; padding: 0.8em; border-radius: 8px; font-size: 0.8em; border: 1px solid #3c3c3c; }
  pre code { background-color: transparent; color: #d4d4d4; padding: 0; }
  table { font-size: 0.75em; width: 100%; }
  th { background-color: #264f78; color: #ffffff; padding: 6px 8px; }
  td { background-color: #2d2d2d; padding: 4px 8px; }
  ul, ol { margin: 0.3em 0; padding-left: 1.2em; }
  li { margin: 0.15em 0; line-height: 1.3; }
  img { max-height: 420px; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.5); }
  .two-column { display: flex; gap: 1.5em; }
  .two-column > div { flex: 1; }
  .highlight { color: #4ec9b0; font-weight: bold; }
  svg[id^="mermaid"] { max-height: 400px; }
  .mermaid { background: transparent !important; }
---
```

**Key Configuration Properties:**
| Property | Value | Purpose |
|----------|-------|---------|
| `marp: true` | Required | Enables Marp processing |
| `theme: default` | Base theme | Uses Marp's built-in default theme as base |
| `paginate: true` | Enable page numbers | Shows slide numbers in footer |
| `backgroundColor` | `#1e1e1e` | VS Code dark editor background |
| `color` | `#cccccc` | VS Code dark text color |
| `style` | Inline CSS | Complete custom styling in YAML block |

#### 2. Slide Separators and Directives

**Horizontal Rule Separator:**
```markdown
---
```
Each `---` (horizontal rule) creates a new slide. Unlike Reveal.js's `<section>` tags, Marp uses pure markdown.

**Per-Slide Directives (Local Directives):**
```markdown
<!-- _class: lead -->
<!-- _paginate: false -->
```
- Directives starting with `_` apply ONLY to the current slide
- `_class` assigns CSS class(es) to the slide
- `_paginate: false` hides page number on that slide

**Directive Examples from pch_argus:**
| Directive | Usage | Effect |
|-----------|-------|--------|
| `<!-- _class: lead -->` | Title/section slides | Centers content, different styling |
| `<!-- _paginate: false -->` | Title slides | Hides slide number |

#### 3. Speaker Notes Format

Marp uses HTML comments for speaker notes:

```markdown
<!--
Speaker notes for Title Slide:
- Welcome attendees and introduce yourself
- Set the context: This is Argus, Point C's healthcare EDI transformation platform
- Mention the three key differentiators: AI-powered, HIPAA-compliant, Azure-native
- Transition: "Let me start by telling you about Point C and why we built Argus"
-->
```

**Key Differences from Reveal.js:**
- Reveal.js: `<aside class="notes">...</aside>`
- Marp: `<!-- ... -->` (HTML comments)
- Access in Marp: Press `P` in HTML presentation for presenter mode

#### 4. Two-Column Layout Pattern

The presentation uses flexbox-based column layouts via custom CSS class:

```markdown
<div class="two-column">
<div>

### Left Column Content
- Item 1
- Item 2

</div>
<div>

### Right Column Content
- Item A
- Item B

</div>
</div>
```

**CSS Definition:**
```css
.two-column { display: flex; gap: 1.5em; }
.two-column > div { flex: 1; }
```

#### 5. Mermaid Diagram Integration

Mermaid diagrams are loaded via CDN script in the front matter section:

```html
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
```

**Mermaid Diagram Syntax in Slides:**
```markdown
<pre class="mermaid">
flowchart TD
    subgraph Azure["Azure Cloud"]
        subgraph Functions["Function Apps"]
            IR[Inbound Router]
            TE[Transformation Engine]
        end
    end
    IR --> SB
    SB --> TE
</pre>
```

**Diagram Types Used:**
- `flowchart TD` / `flowchart LR` - Top-down and left-right flowcharts
- `sequenceDiagram` - Sequence diagrams for data flow

**Critical Note:** Mermaid requires internet connectivity for CDN loading. The README notes this explicitly.

#### 6. Code Blocks and Syntax Highlighting

Standard markdown fenced code blocks with language specification:

```markdown
```json
{
  "routeId": "enrollment-daily",
  "priority": 10,
  "transactionType": "834"
}
```
```

**Syntax Highlighting CSS:**
```css
pre code .hljs-string { color: #ce9178; }
pre code .hljs-number { color: #b5cea8; }
pre code .hljs-literal { color: #569cd6; }
pre code .hljs-keyword { color: #569cd6; }
pre code .hljs-attr { color: #9cdcfe; }
```

Languages used: `json`, `yaml`, `typescript`, `bicep`, `plaintext`, `bash`

#### 7. Image Handling

**Standard Image Syntax:**
```markdown
![Dashboard Overview](screenshots/01-intro/intro-dashboard-overview.png)
```

**Image Styling (from CSS):**
```css
img { max-height: 420px; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.5); }
section img:not(.no-lightbox) { cursor: zoom-in; }
```

**Screenshot Organization:**
| Directory | Count | Content |
|-----------|-------|---------|
| `01-intro/` | 4 | Dashboard screenshots |
| `02-files/` | 6 | File tracking screenshots |
| `03-config/` | 10 | Partner configuration |
| `04-routes/` | 8 | Route editor screenshots |
| `05-advanced/` | 4 | Advanced features |
| `06-ai/` | 3 | AI chat screenshots |

#### 8. Build Process (Build-Presentation.ps1)

**Two-Step Build:**
1. **Marp CLI Generation:** Converts markdown to HTML
2. **Lightbox Injection:** Post-processes HTML to add lightbox functionality

```powershell
# Step 1: Run Marp to generate HTML
& marp --html --allow-local-files $SourceFile -o $OutputFile

# Step 2: Inject lightbox JavaScript before </body>
$lightboxCode = @'
<div id="pch-lightbox" style="...">
  <img id="pch-lightbox-img" src="" alt="Full screen image" ...>
</div>
<script>
(function() {
  window.addEventListener('load', function() {
    // Click handler for screenshots
    document.addEventListener('click', function(e) {
      if (target.tagName === 'IMG' && target.src.indexOf('screenshots') !== -1) {
        lightboxImg.src = target.src;
        lightbox.style.display = 'flex';
      }
    }, true);
  });
})();
</script>
'@
```

**Marp CLI Options:**
| Option | Purpose |
|--------|---------|
| `--html` | Enable HTML tags in markdown |
| `--allow-local-files` | Allow local file references (images) |
| `-o filename.html` | Output file |
| `--preview` | Live preview mode with hot-reload |
| `--pdf` | Export to PDF |
| `--pptx` | Export to PowerPoint |

#### 9. Marp CLI Installation

```bash
npm install -g @marp-team/marp-cli@4.0.0
```

**Alternative:** VS Code Extension `marp-team.marp-vscode` for inline preview.

#### 10. Keyboard Navigation (HTML Output)

| Key | Action |
|-----|--------|
| `→` / `Space` | Next slide |
| `←` | Previous slide |
| `F` | Toggle fullscreen |
| `P` | Presenter mode (speaker notes) |
| `O` | Overview mode |
| `Esc` | Exit fullscreen/overview |

#### 11. Table Formatting

Standard markdown tables with custom CSS styling:

```markdown
| Metric | Before Argus | With Argus |
|--------|--------------|------------|
| 🚀 **Partner Onboarding** | Days to Weeks | **< 2 Hours** |
| 🔄 **New Transformation** | Weeks (code deploy) | **Hours (template)** |
```

**Table CSS:**
```css
table { font-size: 0.75em; width: 100%; }
th { background-color: #264f78; color: #ffffff; padding: 6px 8px; }
td { background-color: #2d2d2d; padding: 4px 8px; }
```

#### 12. Blockquotes

Used for callouts and key messages:

```markdown
> **Configuration changes** replace **code deployments**
>
> Operations teams can onboard partners without developer involvement
```

### Key Discoveries

- **Single markdown file**: Entire 36+ slide presentation in one `argus-edi-platform.md` file (~59KB)
- **Inline CSS theming**: VS Code dark theme implemented directly in YAML front matter `style` block — no separate CSS file needed
- **`---` slide separators**: Clean markdown syntax, one horizontal rule = one new slide
- **HTML comments for speaker notes**: Simple `<!-- notes -->` format, accessible via `P` key
- **Mermaid via CDN**: JavaScript module import in front matter, requires internet connectivity
- **`_` prefix for local directives**: `<!-- _class: lead -->` applies only to current slide
- **Two-column layout**: Custom `.two-column` CSS class with flexbox
- **Post-processing for lightbox**: Build script injects JavaScript for image zoom functionality
- **Export options**: HTML (best for Mermaid), PDF (diagrams rasterized), PPTX (for editing)
- **35 screenshots organized by section**: Clean folder hierarchy for image assets

### Relevant Files/Resources

| File | Path | Relevance |
|------|------|-----------|
| Main presentation | `C:\repos\pch_argus\docs\presentations\argus-edi-platform.md` | Source markdown (59,406 bytes) |
| Build script | `C:\repos\pch_argus\docs\presentations\Build-Presentation.ps1` | Marp CLI + lightbox injection |
| README | `C:\repos\pch_argus\docs\presentations\README.md` | Usage documentation |
| Generated HTML | `C:\repos\pch_argus\docs\presentations\argus-edi-platform.html` | Output (381,707 bytes) |
| Screenshots | `C:\repos\pch_argus\docs\presentations\screenshots/` | 35 images in 6 subdirectories |

---

## Phase 2: Current Presentation Content Analysis

**Status:** ✅ Complete
**Scope:** Analyze the current Reveal.js presentation structure and content

### Research Tasks

- ✅ Read full `presentation/index.html` to extract all slide content
- ✅ Document the current slide structure and organization
- ✅ Identify Reveal.js-specific features in use (fragments, sections, transitions)
- ✅ Analyze `css/vscode-dark-theme.css` custom theming
- ✅ List all content that needs to be migrated (text, code blocks, lists)
- ✅ Identify any complex slides that may require special handling

### Findings

#### 1. Current Presentation Structure

The Reveal.js presentation at `presentation/index.html` (645 lines) contains **7 sections with 28+ slides total**:

| Section | Name | Slides | Description |
|---------|------|--------|-------------|
| 1 | Introduction | 3 | Title, agenda, about presentation |
| 2 | Copilot Overview | 2 | What is Copilot, custom agents |
| 3 | PCH Agents | 11 | Detailed agent descriptions |
| 4 | Workflow | 4 | Diagrams, entry points, execution modes |
| 5 | Example Prompts | 7 | Overview + one per agent |
| 6 | Journey | 4 | Story, MVP, iteration, outcomes |
| 7 | Closing | 3 | Getting started, Q&A, thank you |

#### 2. Detailed Slide Breakdown

**Section 1: Introduction (3 slides)**
- **Slide 1.1 (Title):** "PCH Agent Workflow" with subtitle "Intelligent Development Assistance with GitHub Copilot Custom Agents" and author attribution
- **Slide 1.2 (Agenda):** Bullet list of presentation topics
- **Slide 1.3 (About):** "About This Presentation" placeholder content

**Section 2: Copilot Overview (2 slides)**
- **Slide 2.1:** "What is GitHub Copilot?" with fragment reveals (incremental bullet points)
- **Slide 2.2:** "Custom Agents in VS Code" featuring a markdown code example

**Section 3: PCH Agents (11 slides)**
- **Slide 3.1:** Agent overview slide
- **Slide 3.2:** pch-visionary details
- **Slides 3.3-3.4:** pch-researcher (features + execution modes)
- **Slides 3.5-3.6:** pch-planner (features + plan structure)
- **Slide 3.7:** pch-plan-reviewer details
- **Slides 3.8-3.9:** pch-coder (features + execution modes)
- **Slide 3.10:** pch-helper details
- **Slide 3.11:** Agent summary comparison table

**Section 4: Workflow (4 slides)**
- **Slide 4.1:** Workflow diagram with agent node visualization
- **Slide 4.2:** Entry points table (when to use each agent)
- **Slide 4.3:** Multi-session design concepts
- **Slide 4.4:** Execution modes comparison (Manual vs Automatic)

**Section 5: Example Prompts (7 slides)**
- **Slide 5.1:** Example prompts overview
- **Slides 5.2-5.7:** One example slide for each of the 6 agents

**Section 6: Journey (4 slides)**
- **Slide 6.1:** Story opener (narrative introduction)
- **Slide 6.2:** MVP Phase (development journey)
- **Slide 6.3:** Iteration Phase
- **Slide 6.4:** Production outcomes

**Section 7: Closing (3 slides)**
- **Slide 7.1:** Getting Started steps
- **Slide 7.2:** Q&A placeholder
- **Slide 7.3:** Thank You

#### 3. Reveal.js-Specific Features in Use

**Nested Section Tags (Vertical/Horizontal Navigation):**
```html
<section>  <!-- Horizontal slide group -->
    <section>  <!-- First vertical slide -->
        Content...
    </section>
    <section>  <!-- Second vertical slide (down) -->
        Content...
    </section>
</section>
```
- Outer `<section>` creates horizontal navigation (left/right arrows)
- Inner `<section>` creates vertical navigation (up/down arrows)
- PCH Agents section uses this for related agent slides

**Fragment Reveals:**
```html
<ul>
    <li class="fragment">First point (appears on first click)</li>
    <li class="fragment">Second point (appears on second click)</li>
    <li class="fragment fade-up">Third point (fades up on click)</li>
</ul>
```
- `class="fragment"` - Basic incremental reveal
- `class="fragment fade-up"` - Fade-up animation reveal
- Used extensively in "What is Copilot?" and agent feature slides

**Custom CSS Classes:**
| Class | Purpose |
|-------|---------|
| `.title-slide` | Title slide styling (centered, large fonts) |
| `.agent-slide` | Agent detail slide styling |
| `.two-column` | Side-by-side layout |
| `.agent-workflow` | Workflow diagram container |
| `.agent-node` | Individual agent node in workflow |
| `.mode-box` | Execution mode comparison boxes |

**Code Blocks:**
```html
<pre><code class="language-markdown">---
name: pch-visionary
description: Vision document specialist
---</code></pre>
```
- Uses `<pre><code>` with language class for syntax highlighting
- Languages used: `markdown`, presumably others

**Tables:**
```html
<table>
    <thead><tr><th>Agent</th><th>Purpose</th></tr></thead>
    <tbody><tr><td>pch-visionary</td><td>Creates vision docs</td></tr></tbody>
</table>
```
- Standard HTML tables for agent summary and entry points

#### 4. CSS Structure Analysis (vscode-dark-theme.css - 989 lines)

**CSS Variables:**
```css
:root {
    /* VS Code Dark+ Colors */
    --vscode-bg: #1e1e1e;
    --vscode-text: #d4d4d4;
    --vscode-blue: #569cd6;
    --vscode-green: #4ec9b0;
    --vscode-purple: #c586c0;
    
    /* Agent Accent Colors */
    --agent-visionary: #dcdcaa;
    --agent-researcher: #4ec9b0;
    --agent-planner: #569cd6;
    --agent-reviewer: #c586c0;
    --agent-coder: #6a9955;
    --agent-helper: #ce9178;
}
```

**Typography:**
- Primary font: `'Segoe UI', sans-serif`
- Code font: `'Cascadia Code', 'Consolas', monospace`

**Major Styling Sections:**
| Lines (approx) | Section | Content |
|----------------|---------|---------|
| 1-50 | Variables | CSS custom properties |
| 51-150 | Typography | Fonts, headings, body text |
| 151-250 | Agent styling | Accent colors, badges |
| 251-400 | Two-column layout | Flexbox grid system |
| 401-550 | Workflow diagram | Node positioning, arrows |
| 551-700 | Mode comparison | Manual vs Automatic boxes |
| 701-850 | Journey/story | Timeline styling |
| 851-950 | Code blocks | Syntax highlighting |
| 951-989 | Tables | Header/cell styling |

**Agent Badge Styling:**
```css
.agent-badge {
    display: inline-block;
    padding: 2px 8px;
    border-radius: 4px;
    font-family: 'Cascadia Code', monospace;
}
.agent-badge.visionary { background-color: var(--agent-visionary); }
.agent-badge.researcher { background-color: var(--agent-researcher); }
/* etc. */
```

**Workflow Diagram Styling:**
```css
.agent-workflow {
    display: flex;
    flex-direction: column;
    align-items: center;
}
.agent-node {
    padding: 12px 24px;
    border-radius: 8px;
    margin: 8px;
}
```

#### 5. Content Migration Inventory

**Text Content to Migrate:**
| Content Type | Count | Notes |
|--------------|-------|-------|
| Slide titles (h1/h2) | 28+ | Direct text conversion |
| Bullet lists | 20+ | Standard markdown lists |
| Paragraphs | 15+ | Direct text |
| Agent descriptions | 6 | Detailed content blocks |

**Code Blocks to Migrate:**
| Language | Count | Context |
|----------|-------|---------|
| Markdown | 5+ | Agent YAML examples |
| Shell/Command | 2+ | Terminal examples |

**Tables to Migrate:**
| Table | Location | Columns |
|-------|----------|---------|
| Agent Summary | Section 3 | Agent, Purpose, Key Feature |
| Entry Points | Section 4 | Situation, Agent, Action |
| Mode Comparison | Section 4 | Feature, Manual, Automatic |

**Visual Elements:**
| Element | Count | Migration Complexity |
|---------|-------|---------------------|
| Workflow diagram | 1 | HIGH - Custom HTML/CSS |
| Mode comparison boxes | 2 | MEDIUM - Needs CSS |
| Agent badges | 6+ | LOW - CSS styling |

#### 6. Complex Slides Requiring Special Handling

**HIGH Complexity:**

1. **Workflow Diagram (Section 4, Slide 1)**
   - Custom `.agent-workflow` and `.agent-node` CSS classes
   - Visual representation of agent flow
   - **Challenge:** Will need to convert to Mermaid diagram or recreate with custom CSS

2. **Mode Comparison Boxes (Section 4, Slide 4)**
   - Side-by-side `.mode-box` styled divs
   - **Challenge:** Needs two-column layout with custom styling

**MEDIUM Complexity:**

3. **Agent Slides with Badges**
   - Use `.agent-badge` styled spans for agent names
   - **Challenge:** Need inline CSS or custom class in Marp

4. **Fragment Reveals**
   - Multiple slides use `class="fragment"` and `class="fragment fade-up"`
   - **Challenge:** Marp doesn't have native fragment support; need alternative approach

5. **Two-Column Layouts**
   - Several slides use `.two-column` flexbox layout
   - **Solution:** Marp supports `.two-column` with custom CSS (already in pch_argus)

**LOW Complexity:**

6. **Standard Content Slides**
   - Title + bullet points
   - Title + paragraph + list
   - **Solution:** Direct markdown conversion

7. **Tables**
   - Standard HTML tables
   - **Solution:** Markdown table syntax

8. **Code Blocks**
   - `<pre><code>` blocks
   - **Solution:** Fenced code blocks with language specification

### Key Discoveries

- **28+ slides across 7 sections** - Moderate size presentation with clear organization
- **Nested sections for vertical navigation** - Reveal.js feature that won't directly translate to Marp (Marp is linear)
- **Extensive fragment usage** - Incremental reveals are core to the presentation; Marp lacks native support
- **Custom workflow diagram** - Most complex visual element, likely needs Mermaid conversion
- **989-line CSS file** - Comprehensive theming that needs to be distilled to Marp inline style block
- **Agent-specific color coding** - CSS variables for each agent accent color must be preserved
- **Two-column layouts used** - Already supported pattern in Marp
- **No images currently** - Text-only presentation simplifies migration
- **Mode comparison boxes** - Custom styling for Manual vs Automatic comparison

### Relevant Files/Resources

| File | Path | Relevance |
|------|------|-----------|
| Main presentation | `c:\repos\.github-private\presentation\index.html` | Source HTML (645 lines) - all slides |
| Theme CSS | `c:\repos\.github-private\presentation\css\vscode-dark-theme.css` | Custom styling (989 lines) |

---

## Phase 3: Theme and Styling Comparison

**Status:** ✅ Complete
**Scope:** Compare theming approaches and identify styling migration needs

### Research Tasks

- ✅ Document pch_argus VS Code Dark theme implementation in Marp
- ✅ Compare with current Reveal.js `vscode-dark-theme.css` styling
- ✅ Identify which CSS properties translate to Marp theme format
- ✅ Document Marp theme directive syntax (`<!-- _class: -->`, etc.)
- ✅ Analyze code block styling and syntax highlighting differences
- ✅ Identify any custom CSS that needs to be created for Marp

### Findings

#### 1. Theming Architecture Comparison

| Aspect | Reveal.js (Current) | Marp (Target) |
|--------|---------------------|---------------|
| **Theme Location** | External CSS file (`vscode-dark-theme.css`, 989 lines) | Inline in YAML front matter `style:` block |
| **CSS Variables** | Uses `:root` with `--var` custom properties | Not commonly used; direct values preferred |
| **Selector Target** | `.reveal` prefix (e.g., `.reveal h1`) | Direct selectors (e.g., `h1`, `section`) |
| **Scoping** | Class-based (`.reveal .slides section`) | Marp auto-scopes to `section` elements |
| **Theme Inheritance** | Link stylesheet in HTML | `theme: default` + inline `style:` overrides |

#### 2. Color Mapping: Reveal.js CSS Variables → Marp Direct Values

**Core Colors:**

| Reveal.js Variable | Value | Marp CSS Property |
|--------------------|-------|-------------------|
| `--r-background-color` | `#1E1E1E` | `section { background-color: #1e1e1e; }` |
| `--r-main-color` | `#D4D4D4` | `section { color: #d4d4d4; }` |
| `--r-heading-color` | `#D4D4D4` | `h1, h2, h3 { color: #d4d4d4; }` |
| `--r-link-color` | `#3794FF` | `a { color: #3794ff; }` |
| `--vscode-accent` | `#007ACC` | Used for h1, h2 in pch_argus: `h1 { color: #007acc; }` |
| `--vscode-widget-background` | `#252526` | `pre { background-color: #2d2d30; }` |
| `--vscode-widget-border` | `#454545` | `pre { border: 1px solid #3c3c3c; }` |

**Agent Accent Colors (MUST PRESERVE):**

| Reveal.js Variable | Value | Marp Class Equivalent |
|--------------------|-------|-----------------------|
| `--agent-visionary` | `#4EC9B0` | `.agent-visionary { color: #4ec9b0; }` |
| `--agent-researcher` | `#569CD6` | `.agent-researcher { color: #569cd6; }` |
| `--agent-planner` | `#DCDCAA` | `.agent-planner { color: #dcdcaa; }` |
| `--agent-reviewer` | `#C586C0` | `.agent-reviewer { color: #c586c0; }` |
| `--agent-coder` | `#6A9955` | `.agent-coder { color: #6a9955; }` |
| `--agent-helper` | `#9CDCFE` | `.agent-helper { color: #9cdcfe; }` |

#### 3. Typography Comparison

| Property | Reveal.js Current | Marp Target | Migration Action |
|----------|-------------------|-------------|------------------|
| **Body Font** | `'Segoe UI', system-ui, sans-serif` | `'Segoe UI', sans-serif` | ✅ Compatible |
| **Code Font** | `'Cascadia Code', 'Fira Code', 'Consolas', monospace` | Not specified in pch_argus | Add to Marp style |
| **Base Font Size** | `38px` (custom) | `24px` (pch_argus default) | May need adjustment |
| **H1 Size** | `2.2em` | `1.8em` | Slightly smaller in Marp |
| **H2 Size** | `1.6em` | `1.4em` | Slightly smaller in Marp |
| **H3 Size** | `1.3em` | `1.1em` | Slightly smaller in Marp |
| **Line Height** | `1.4` | `1.3` | Similar |

**Marp Typography CSS Block:**
```css
section {
  font-family: 'Segoe UI', sans-serif;
  font-size: 24px;
  padding: 40px 50px;
}
h1 { color: #007acc; font-size: 1.8em; margin-bottom: 0.3em; }
h2 { color: #007acc; font-size: 1.4em; margin-bottom: 0.3em; }
h3 { color: #4ec9b0; font-size: 1.1em; margin-bottom: 0.2em; }
```

#### 4. Code Block Styling Comparison

**Reveal.js Code Styling:**
```css
.reveal pre {
    background: #252526;
    border: 1px solid var(--vscode-widget-border);
    border-radius: 4px;
    padding: 16px 20px;
    font-size: 0.65em;
}
.reveal pre code {
    background: transparent;
    max-height: 450px;
    line-height: 1.5;
}
.reveal code:not(pre code) {
    background-color: rgba(37, 37, 38, 0.8);
    padding: 0.1em 0.4em;
    border-radius: 3px;
    color: #CE9178;
}
```

**Marp Equivalent CSS:**
```css
code { 
  background-color: #3c3c3c; 
  color: #dcdcaa; 
  padding: 2px 6px; 
  border-radius: 3px; 
  font-size: 0.85em; 
}
pre { 
  background-color: #2d2d30; 
  padding: 0.8em; 
  border-radius: 8px; 
  font-size: 0.8em; 
  border: 1px solid #3c3c3c; 
}
pre code { 
  background-color: transparent; 
  color: #d4d4d4; 
  padding: 0; 
}
```

**Syntax Highlighting Differences:**

| Aspect | Reveal.js | Marp |
|--------|-----------|------|
| **Highlighter** | highlight.js with custom overrides | Marp uses Prism.js or highlight.js depending on renderer |
| **Custom Colors** | 16+ `.hljs-*` class overrides | Must add similar classes to inline style |
| **Theme Control** | Full control via CSS | Limited; often relies on built-in highlighting |

**Reveal.js Syntax Highlighting Classes to Migrate:**
```css
/* VS Code Dark+ colors - need to add to Marp style block */
.hljs-keyword { color: #569CD6; }    /* blue */
.hljs-string { color: #CE9178; }     /* orange/salmon */
.hljs-comment { color: #6A9955; }    /* green */
.hljs-function { color: #DCDCAA; }   /* yellow */
.hljs-number { color: #B5CEA8; }     /* light green */
.hljs-variable, .hljs-attr { color: #9CDCFE; } /* light blue */
.hljs-type, .hljs-class { color: #4EC9B0; }    /* teal */
.hljs-meta { color: #C586C0; }       /* purple */
```

⚠️ **Note:** Marp CLI uses its own syntax highlighting. The pch_argus presentation does NOT include custom syntax highlighting CSS, relying on Marp defaults. The PCH presentation may need to accept Marp's default highlighting or add extensive highlight classes.

#### 5. Marp Directive Syntax Reference

**Global Directives (apply to all slides):**
```yaml
---
marp: true
theme: default
paginate: true
backgroundColor: #1e1e1e
color: #cccccc
style: |
  /* CSS goes here */
---
```

**Local Directives (apply from current slide forward):**
```markdown
<!-- paginate: true -->
<!-- header: 'Slide Header' -->
<!-- footer: 'Footer Content' -->
<!-- backgroundColor: #1e1e1e -->
<!-- color: #cccccc -->
```

**Spot Directives (apply to current slide ONLY, using `_` prefix):**
```markdown
<!-- _class: lead -->
<!-- _paginate: false -->
<!-- _backgroundColor: #000000 -->
<!-- _color: #ffffff -->
```

**Class Directive Examples:**
| Directive | Effect |
|-----------|--------|
| `<!-- _class: lead -->` | Centered content, often for title slides |
| `<!-- _class: invert -->` | Inverted colors (if theme supports) |
| `<!-- _class: title-slide -->` | Custom class for title styling |

#### 6. Custom CSS Classes: Migration Requirements

**Classes That Translate Directly:**

| Reveal.js Class | Marp Equivalent | Migration |
|-----------------|-----------------|-----------|
| `.two-column` | `.two-column` | ✅ Same CSS works |
| `.column` | `> div` (child of .two-column) | ✅ Use same pattern |
| `.highlight` | `.highlight` | ✅ Copy CSS definition |
| `.centered` | Native with `_class: lead` | ✅ Or copy CSS |
| `.muted` | `.muted` | ✅ Copy CSS definition |

**Classes Requiring New CSS Definitions:**

| Reveal.js Class | Purpose | Marp CSS Needed |
|-----------------|---------|-----------------|
| `.title-slide` | Title slide styling | Add to style block |
| `.agent-slide` | Agent detail slides | Add to style block |
| `.agent-workflow` | Workflow diagram | Convert to Mermaid diagram instead |
| `.agent-node` | Workflow nodes | Convert to Mermaid |
| `.mode-box` | Mode comparison | Add flexbox CSS |
| `.mode-box.manual` | Manual mode styling | Add border-color styling |
| `.mode-box.automatic` | Auto mode styling | Add border-color styling |
| `.agent-badge` | Agent name badges | Add inline badge styling |
| `.agent-badge.visionary/.researcher/etc.` | Per-agent colors | Add color variants |
| `.agent-box-*` | Agent box backgrounds | Add background styling |
| `.journey-step` | Journey slides | Add left-border styling |
| `.session-concept` | Multi-session display | Add flexbox layout |
| `.entry-points-table` | Custom table | Standard table styling sufficient |

**Agent Color Classes to Add:**
```css
/* Agent text colors */
.agent-visionary { color: #4ec9b0; }
.agent-researcher { color: #569cd6; }
.agent-planner { color: #dcdcaa; }
.agent-reviewer { color: #c586c0; }
.agent-coder { color: #6a9955; }
.agent-helper { color: #9cdcfe; }

/* Agent badge styling */
.agent-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 0.85em;
  font-weight: 600;
  background-color: #252526;
  border: 1px solid currentColor;
}

/* Agent box backgrounds */
.agent-box-visionary { background: rgba(78, 201, 176, 0.12); border-left: 4px solid #4ec9b0; }
.agent-box-researcher { background: rgba(86, 156, 214, 0.12); border-left: 4px solid #569cd6; }
.agent-box-planner { background: rgba(220, 220, 170, 0.12); border-left: 4px solid #dcdcaa; }
.agent-box-reviewer { background: rgba(197, 134, 192, 0.12); border-left: 4px solid #c586c0; }
.agent-box-coder { background: rgba(106, 153, 85, 0.12); border-left: 4px solid #6a9955; }
.agent-box-helper { background: rgba(156, 220, 254, 0.12); border-left: 4px solid #9cdcfe; }
```

#### 7. Fragment/Animation Migration

**Reveal.js Fragments:**
```html
<li class="fragment">First point</li>
<li class="fragment fade-up">Second point</li>
```

**Marp Fragmented Lists (Alternative):**
```markdown
* First point      <!-- Uses * marker -->
* Second point
* Third point

1) First item      <!-- Uses ) after number -->
2) Second item
```

**Key Differences:**
| Feature | Reveal.js | Marp |
|---------|-----------|------|
| Syntax | `class="fragment"` on any element | `*` bullet or `)` ordered list markers |
| Scope | Any HTML element | Lists only |
| Animation types | `fade-up`, `fade-in`, `highlight-red`, etc. | None (just appear) |
| Arbitrary elements | ✅ Yes | ❌ No |
| Non-list content | ✅ Supported | ❌ Not supported |

⚠️ **Migration Impact:** Reveal.js presentations using fragments on non-list elements (headings, paragraphs, divs) cannot replicate that behavior in Marp. Content will display all at once.

#### 8. Features That Don't Translate to Marp

| Reveal.js Feature | Why It Won't Work in Marp | Alternative |
|-------------------|---------------------------|-------------|
| **Nested sections (vertical slides)** | Marp is linear only | Flatten to sequential slides |
| **Fragment animations** (fade-up, etc.) | Only basic fragments via list markers | Use `*` lists for basic fragments |
| **Fragment on non-list elements** | Not supported | Display all at once |
| **CSS custom properties (`--var`)** | Can use but no fallback | Use direct values |
| **`:hover` effects on table rows** | Works but limited interactivity | Keep for nice-to-have |
| **Progress bar styling** | Marp has different UI | Accept Marp's pagination |
| **Controls styling** | No arrow controls in Marp HTML | Keyboard navigation only |
| **Reduced motion media query** | Can include but less relevant | Include for accessibility |

#### 9. Complete Marp Style Block for PCH Presentation

Based on the analysis, the following inline CSS should be added to the Marp YAML front matter to replicate the current Reveal.js styling:

```css
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
blockquote { background: #252526; border-left: 4px solid #007acc; padding: 15px 20px; border-radius: 0 4px 4px 0; }

/* Two-column layout */
.two-column { display: flex; gap: 30px; }
.two-column > div { flex: 1; }

/* Agent text colors */
.agent-visionary { color: #4ec9b0; }
.agent-researcher { color: #569cd6; }
.agent-planner { color: #dcdcaa; }
.agent-reviewer { color: #c586c0; }
.agent-coder { color: #6a9955; }
.agent-helper { color: #9cdcfe; }

/* Agent badges */
.agent-badge { display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 0.85em; font-weight: 600; background-color: #252526; border: 1px solid currentColor; }

/* Mode comparison boxes */
.mode-box { flex: 1; padding: 20px; background-color: #252526; border-radius: 8px; border: 2px solid #454545; }
.mode-box.manual { border-color: #dcdcaa; }
.mode-box.automatic { border-color: #6a9955; }
.mode-box.manual h3 { color: #dcdcaa; }
.mode-box.automatic h3 { color: #6a9955; }

/* Agent boxes with colored borders */
.agent-box { padding: 15px 20px; border-radius: 8px; margin: 10px 0; }
.agent-box-visionary { background: rgba(78, 201, 176, 0.12); border-left: 4px solid #4ec9b0; }
.agent-box-researcher { background: rgba(86, 156, 214, 0.12); border-left: 4px solid #569cd6; }
.agent-box-planner { background: rgba(220, 220, 170, 0.12); border-left: 4px solid #dcdcaa; }
.agent-box-reviewer { background: rgba(197, 134, 192, 0.12); border-left: 4px solid #c586c0; }
.agent-box-coder { background: rgba(106, 153, 85, 0.12); border-left: 4px solid #6a9955; }
.agent-box-helper { background: rgba(156, 220, 254, 0.12); border-left: 4px solid #9cdcfe; }

/* Highlight and muted text */
.highlight { color: #007acc; font-weight: 600; }
.muted { color: #808080; font-size: 0.9em; }

/* Title slide styling */
section.title-slide { text-align: center; }
section.title-slide h1 { font-size: 2.2em; }
.subtitle { font-size: 1em; color: #007acc; margin-bottom: 1em; }

/* Workflow diagram nodes (if not using Mermaid) */
.agent-node { display: inline-block; padding: 12px 20px; border-radius: 8px; font-weight: bold; margin: 5px; }
.agent-node.visionary { background-color: #4ec9b0; color: #1e1e1e; }
.agent-node.researcher { background-color: #569cd6; color: #1e1e1e; }
.agent-node.planner { background-color: #dcdcaa; color: #1e1e1e; }
.agent-node.reviewer { background-color: #c586c0; color: #1e1e1e; }
.agent-node.coder { background-color: #6a9955; color: #1e1e1e; }
.agent-node.helper { background-color: #9cdcfe; color: #1e1e1e; }
```

### Key Discoveries

- **Inline CSS is standard in Marp** — All 989 lines of Reveal.js CSS must be distilled into a ~100-line inline `style:` block in YAML front matter
- **No CSS variables in production** — Marp inline styles should use direct hex values rather than `--var` custom properties for reliability
- **Agent colors are fully portable** — All 6 agent accent colors can be preserved with simple class definitions
- **Fragment support is limited** — Only lists support fragmented reveal; arbitrary element fragments won't work
- **Vertical navigation is lost** — Marp is strictly linear; nested sections must be flattened
- **Syntax highlighting differs** — Marp uses different highlighter; may need to accept default or add extensive hljs overrides
- **Two-column layout works identically** — The `.two-column` flexbox pattern translates directly
- **Workflow diagram should use Mermaid** — Custom `.agent-workflow` HTML/CSS should convert to Mermaid diagram for maintainability
- **Spot directives (`_class`)** are the key to per-slide styling — Use for title slides and special layouts
- **Table styling translates well** — Markdown tables with CSS work identically

### Relevant Files/Resources

| File | Path | Relevance |
|------|------|-----------|
| Reveal.js Theme CSS | `c:\repos\.github-private\presentation\css\vscode-dark-theme.css` | Source styling (989 lines) |
| pch_argus Marp Example | `C:\repos\pch_argus\docs\presentations\argus-edi-platform.md` | Reference implementation |
| Marp Theme CSS Docs | https://marpit.marp.app/theme-css | Theme creation reference |
| Marp Directives Docs | https://marpit.marp.app/directives | Directive syntax reference |
| Marp Fragmented List Docs | https://marpit.marp.app/fragmented-list | Fragment alternatives |

---

## Phase 4: Conversion Strategy and Mapping

**Status:** ✅ Complete
**Scope:** Create a detailed conversion strategy and feature mapping

### Research Tasks

- ✅ Create mapping table: Reveal.js features → Marp equivalents
- ✅ Document conversion approach for each content type
- ✅ Identify any Reveal.js features that don't have Marp equivalents
- ✅ Define file structure for the converted presentation
- ✅ Document build/preview workflow changes
- ✅ Estimate effort and identify potential challenges
- ✅ Create a step-by-step conversion checklist

### Findings

#### 1. Complete Feature Mapping Table

| Reveal.js Feature | Marp Equivalent | Migration Complexity | Notes |
|-------------------|-----------------|---------------------|-------|
| **Structure** | | | |
| `<section>` slide wrapper | `---` slide separator | LOW | Pure markdown |
| Nested `<section>` (vertical nav) | Flatten to linear slides | MEDIUM | Lose vertical navigation; reorder slides linearly |
| `<section class="...">` | `<!-- _class: ... -->` directive | LOW | Per-slide class via spot directive |
| **Typography** | | | |
| `<h1>`-`<h4>` headings | `#` - `####` markdown | LOW | Direct conversion |
| `<p>` paragraphs | Plain text or `<p>` HTML | LOW | Markdown handles automatically |
| `<strong>`, `<em>` | `**bold**`, `*italic*` | LOW | Standard markdown |
| `<code>` inline | `` `code` `` backticks | LOW | Standard markdown |
| **Lists** | | | |
| `<ul><li>` | `- item` or `* item` | LOW | `*` enables fragments |
| `<ol><li>` | `1. item` or `1) item` | LOW | `)` enables fragments |
| `class="fragment"` on `<li>` | Use `*` or `)` list markers | LOW | Built-in fragment support |
| **Code Blocks** | | | |
| `<pre><code class="language-X">` | ` ```X ... ``` ` fenced blocks | LOW | Standard markdown |
| highlight.js styling | Marp built-in or custom hljs classes | MEDIUM | May need style overrides |
| **Tables** | | | |
| HTML `<table>` | Markdown `\| col \| col \|` | LOW | Standard markdown tables |
| Table styling | CSS in `style:` block | LOW | Same CSS patterns work |
| **Layout** | | | |
| `.two-column` flex layout | `.two-column` with identical CSS | LOW | Direct copy of CSS |
| `.mode-box` comparison | Keep HTML + CSS | MEDIUM | Add classes to style block |
| **Visual Elements** | | | |
| `.agent-workflow` diagram | Convert to Mermaid `flowchart LR` | MEDIUM | Better maintainability |
| `.agent-node` boxes | Mermaid node styling | MEDIUM | Use Mermaid theme variables |
| `.session-concept` icons | Unicode emoji + flexbox CSS | LOW | Already using emoji |
| `.journey-step` timeline | Keep HTML + CSS or simplify | MEDIUM | Border-left styling |
| **Fragments** | | | |
| `class="fragment"` on any element | Lists only: `*` or `)` markers | HIGH | Non-list fragments lost |
| `class="fragment fade-up"` | No animation support | HIGH | Basic appear only |
| **Styling** | | | |
| External CSS file (989 lines) | Inline `style:` in YAML (~100 lines) | MEDIUM | Distill essential CSS |
| CSS custom properties (`--var`) | Direct hex values | LOW | Marp supports vars but simpler without |
| `.agent-*` color classes | Same classes in inline CSS | LOW | Direct port |
| **Navigation** | | | |
| Vertical slides (up/down) | Not available | HIGH | Must flatten |
| Slide numbers | `paginate: true` | LOW | Built-in |
| Progress bar | Not available | N/A | Accept Marp's UI |
| Arrow controls | Keyboard only | N/A | No visual controls in Marp HTML |

#### 2. Content Type Conversion Approaches

**Standard Content Slides (18 slides)**

Direct markdown conversion:

```markdown
---
<!-- _class: agent-slide -->

## Slide Title

Content paragraph here.

* First bullet point
* Second bullet point
* Third bullet point
```

**Two-Column Slides (3 slides: researcher modes, coder modes, mode comparison)**

Keep HTML structure with CSS:

```markdown
---

## Execution Modes

<div class="two-column">
<div>

### Manual Mode
* One phase per session
* User control between phases

</div>
<div>

### Automatic Mode
* Subagent orchestration
* Hands-free execution

</div>
</div>
```

**Agent Slides with Colored Names (11 slides)**

Use inline `<span>` with classes:

```markdown
---
<!-- _class: agent-slide -->

## <span class="agent-visionary">@pch-visionary</span>

Creates vision documents from ideas.

* 6-stage Q&A conversation
* Business + solution architect perspective
```

**Workflow Diagram Slide (1 slide)**

Convert from custom HTML to Mermaid:

```markdown
---

## Workflow Overview

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
```

**Entry Points Table (1 slide)**

Standard markdown table (fragments lost on rows):

```markdown
---

## Entry Points

| Scenario | Start With |
|----------|------------|
| New idea or feature concept | <span class="agent-visionary">@pch-visionary</span> |
| Need to explore the codebase | <span class="agent-researcher">@pch-researcher</span> |
| Known feature ready for planning | <span class="agent-planner">@pch-planner</span> |
| Not sure where to begin | <span class="agent-helper">@pch-helper</span> |
```

**Code Example Slides (6 slides)**

Fenced code blocks with syntax highlighting:

```markdown
---

## <span class="agent-visionary">@pch-visionary</span>

\`\`\`markdown
@pch-visionary I have an idea for a notification system that alerts 
users when their subscription is about to expire. Can you help me 
develop this into a proper vision document?
\`\`\`

The visionary will ask clarifying questions and build a structured vision document.
```

**Journey/Story Slides (4 slides)**

Simplify HTML structure, keep key content:

```markdown
---

## MVP Phase

**📝 Day 1-2: Vision**
Start with <span class="agent-visionary">@pch-visionary</span> to capture your idea

**🔍 Day 3-5: Research**  
<span class="agent-researcher">@pch-researcher</span> explores patterns and best practices

**🚀 Day 6-10: First Feature**
<span class="agent-planner">@pch-planner</span> → <span class="agent-reviewer">@pch-plan-reviewer</span> → <span class="agent-coder">@pch-coder</span>
```

**Title Slides (2 slides: opening, closing)**

Use Marp's `lead` class:

```markdown
---
<!-- _class: lead -->
<!-- _paginate: false -->

# PCH Agent Workflow

**AI-Powered Software Development with GitHub Copilot**

Vincent Miceli
Point C Automation Team
```

#### 3. Features That Cannot Be Replicated

| Lost Feature | Impact | Mitigation |
|--------------|--------|------------|
| **Vertical slide navigation** | Users can't navigate up/down within sections | Flatten to linear sequence; use section title slides |
| **Fragment animations** (fade-up, etc.) | No visual animation effects | Accept instant appearance |
| **Fragments on non-list elements** | Paragraphs, divs, table rows can't fragment | Show all content at once; break into more slides if needed |
| **Visual arrow controls** | No click targets for navigation | Document keyboard shortcuts (→, ←, Space) |
| **Progress bar** | No visual indicator of presentation progress | Paginate shows slide numbers |
| **Slide transitions** | No slide-to-slide animations | Accept instant transitions |

**Impact Assessment:** The main loss is interactivity. Fragment-heavy slides (like "What is Copilot?" with 5 fragments) will show all content at once. Consider:
- Breaking complex fragment slides into multiple simpler slides
- Accepting that content appears all at once (still readable)
- Using `*` list fragments where possible

#### 4. File Structure for Converted Presentation

```
.github-private/
├── docs/
│   └── presentations/                    # New location (matches pch_argus pattern)
│       ├── pch-agent-workflow.md         # Main Marp markdown file (~400-500 lines)
│       ├── pch-agent-workflow.html       # Generated output (do not edit)
│       ├── Build-Presentation.ps1        # Build script (copy from pch_argus)
│       └── README.md                     # Usage instructions
└── presentation/                         # OLD - Remove after migration
    ├── index.html
    └── css/
        └── vscode-dark-theme.css
```

**Notes:**
- Single markdown file replaces HTML + CSS
- No screenshots needed (current presentation has none)
- Generated HTML is ~100-200KB (vs 645 lines HTML + 989 lines CSS = ~50KB)
- Build script is minimal (~50 lines)

#### 5. Build/Preview Workflow Changes

**Current Reveal.js Workflow:**
```
Edit: presentation/index.html + css/vscode-dark-theme.css
Preview: Open index.html in browser or use Live Server extension
Build: N/A (static HTML)
```

**New Marp Workflow:**
```
Edit: docs/presentations/pch-agent-workflow.md
Preview: 
  - VS Code: Install marp-team.marp-vscode extension, preview in editor
  - Terminal: marp --preview pch-agent-workflow.md
Build: 
  - PowerShell: .\Build-Presentation.ps1
  - Terminal: marp --html --allow-local-files pch-agent-workflow.md -o pch-agent-workflow.html
```

**Prerequisites:**
```powershell
# Install Marp CLI globally
npm install -g @marp-team/marp-cli@4.0.0

# VS Code extension (optional but recommended)
# Install: marp-team.marp-vscode
```

**Build-Presentation.ps1 Template:**
```powershell
$SourceFile = "pch-agent-workflow.md"
$OutputFile = "pch-agent-workflow.html"

Write-Host "Building presentation..." -ForegroundColor Cyan
& marp --html --allow-local-files $SourceFile -o $OutputFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Generated: $OutputFile" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}
```

#### 6. Effort Estimate

| Task | Estimated Time | Complexity |
|------|----------------|------------|
| **Setup** | | |
| Install Marp CLI | 5 min | Low |
| Create directory structure | 5 min | Low |
| Copy Build-Presentation.ps1 template | 5 min | Low |
| **Content Migration** | | |
| Create YAML front matter with inline CSS (~100 lines) | 30 min | Medium |
| Convert 7 section title slides | 15 min | Low |
| Convert 18 standard content slides | 45 min | Low |
| Convert 3 two-column slides | 15 min | Low |
| Convert workflow diagram to Mermaid | 20 min | Medium |
| Convert 6 code example slides | 15 min | Low |
| Convert 4 journey/story slides | 20 min | Medium |
| Convert agent summary table | 10 min | Low |
| Convert entry points table | 10 min | Low |
| **Refinement** | | |
| Flatten vertical navigation (reorder slides) | 15 min | Low |
| Test fragment behavior on lists | 10 min | Low |
| Review and adjust styling | 20 min | Medium |
| Test build and preview | 10 min | Low |
| **Cleanup** | | |
| Remove old presentation/ directory | 5 min | Low |
| Update any references | 5 min | Low |
| **Total Estimate** | **~4 hours** | Medium |

**Breakdown:**
- Setup: 15 min
- Content Migration: 2.5 hours  
- Refinement: 55 min
- Cleanup: 10 min

#### 7. Potential Challenges and Mitigations

| Challenge | Risk | Mitigation |
|-----------|------|------------|
| **Fragment loss on non-list elements** | High | Accept content appearing at once; consider splitting complex slides |
| **Mermaid diagram rendering** | Medium | Test diagram styling; may need to adjust classDef colors |
| **Syntax highlighting differences** | Medium | Accept Marp defaults or add hljs class overrides to style block |
| **Inline CSS size limit** | Low | Keep to ~100 lines; omit non-essential styling |
| **Vertical navigation loss** | Medium | Add clear section title slides; logical slide ordering |
| **Agent color consistency** | Low | Direct hex values work; test all agent classes |
| **Two-column layout on narrow screens** | Low | Test with `marp --preview`; flexbox handles wrap |
| **Internet required for Mermaid** | Medium | CDN-based; document requirement in README |
| **Code block overflow** | Low | Use smaller font-size; wrap long lines |

#### 8. Step-by-Step Conversion Checklist

**Phase 1: Setup (15 min)**
- [ ] Install Marp CLI: `npm install -g @marp-team/marp-cli@4.0.0`
- [ ] Install VS Code extension: `marp-team.marp-vscode`
- [ ] Create directory: `docs/presentations/`
- [ ] Create `Build-Presentation.ps1`
- [ ] Create `README.md` with usage instructions

**Phase 2: Create Base Document (30 min)**
- [ ] Create `pch-agent-workflow.md`
- [ ] Add YAML front matter with `marp: true`, `theme: default`, `paginate: true`
- [ ] Add Mermaid CDN script in front matter
- [ ] Add complete inline CSS style block (~100 lines from Phase 3)

**Phase 3: Convert Sections (2 hours)**
- [ ] **Section 1: Introduction** (3 slides)
  - [ ] Title slide with `<!-- _class: lead -->`
  - [ ] Agenda slide (bullet list)
  - [ ] About slide
- [ ] **Section 2: Copilot Overview** (2 slides)
  - [ ] "What is Copilot" (use `*` for fragments)
  - [ ] "Custom Agents" (code block example)
- [ ] **Section 3: PCH Agents** (11 slides)
  - [ ] Overview slide with agent list
  - [ ] 6 agent detail slides with colored names
  - [ ] 2 execution mode slides (two-column)
  - [ ] Agent summary table
- [ ] **Section 4: Workflow** (4 slides)
  - [ ] Workflow diagram (Mermaid conversion)
  - [ ] Entry points table
  - [ ] Multi-session design
  - [ ] Execution modes comparison
- [ ] **Section 5: Example Prompts** (7 slides)
  - [ ] Overview slide
  - [ ] 6 agent example slides with code blocks
- [ ] **Section 6: Journey** (4 slides)
  - [ ] Story opener
  - [ ] MVP phase
  - [ ] Iteration phase  
  - [ ] Production outcomes
- [ ] **Section 7: Closing** (3 slides)
  - [ ] Getting started (ordered list with fragments)
  - [ ] Q&A slide
  - [ ] Thank you slide with `<!-- _class: lead -->`

**Phase 4: Testing and Refinement (45 min)**
- [ ] Build HTML: `.\Build-Presentation.ps1`
- [ ] Open generated HTML in browser
- [ ] Test slide navigation (→, ←, Space)
- [ ] Verify fragment behavior on lists
- [ ] Check Mermaid diagram renders correctly
- [ ] Verify agent colors display correctly
- [ ] Check code block syntax highlighting
- [ ] Test two-column layouts
- [ ] Review on different screen sizes
- [ ] Fix any styling issues

**Phase 5: Cleanup (10 min)**
- [ ] Remove old `presentation/` directory
- [ ] Update any documentation referencing old location
- [ ] Commit converted presentation

### Key Discoveries

- **4-hour estimated effort** — Moderate conversion with most content translating directly
- **28 slides flatten to ~30 linear slides** — Slight increase due to section headers
- **Mermaid replaces custom workflow diagram** — Better maintainability, same visual effect
- **Fragment loss is manageable** — Most fragments are on lists which work; non-list fragments minimal
- **Single markdown file simplifies maintenance** — 989 lines CSS + 645 lines HTML → ~500 lines markdown
- **Build workflow is simpler** — One command vs. manual file editing
- **Vertical navigation is the main loss** — Mitigate with clear section organization

### Relevant Files/Resources

| Resource | Path/URL | Relevance |
|----------|----------|-----------|
| Current Reveal.js HTML | `c:\repos\.github-private\presentation\index.html` | Source content (645 lines) |
| Current CSS | `c:\repos\.github-private\presentation\css\vscode-dark-theme.css` | Source styling (989 lines) |
| pch_argus Marp reference | `C:\repos\pch_argus\docs\presentations\argus-edi-platform.md` | Pattern to follow |
| pch_argus Build script | `C:\repos\pch_argus\docs\presentations\Build-Presentation.ps1` | Template for build script |
| Marp CLI docs | https://github.com/marp-team/marp-cli | Build command reference |
| Marp VS Code extension | https://marketplace.visualstudio.com/items?itemName=marp-team.marp-vscode | Preview in editor |
| Mermaid theming | https://mermaid.js.org/config/theming.html | Diagram color configuration |

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|
| N/A | No organizational standards applicable to this research | - |

---

## Conclusions

### Recommendation: Proceed with Migration

The conversion from Reveal.js to Marp is **recommended** based on this research. The benefits outweigh the trade-offs:

**Benefits:**
- Single-file maintenance (markdown vs. HTML + CSS)
- Framework consistency with pch_argus project
- Mermaid diagrams for better workflow visualization
- Simpler build process (one Marp CLI command)
- VS Code preview integration with Marp extension

**Trade-offs:**
- Loss of vertical slide navigation (acceptable — flatten to linear)
- Limited fragment support (acceptable — lists still work)
- No animation effects (acceptable — content is primary value)

### Estimated Effort

| Phase | Time |
|-------|------|
| Setup | 15 min |
| Content Migration | 2.5 hours |
| Testing & Refinement | 45 min |
| Cleanup | 10 min |
| **Total** | **~4 hours** |

### Next Steps

1. Create implementation plan with `@pch-planner` if detailed task tracking is desired
2. Or proceed directly with conversion using the checklist in Phase 4
3. Consider creating the presentation in a single focused session

### Prerequisites

```powershell
# Install Marp CLI
npm install -g @marp-team/marp-cli@4.0.0

# VS Code extension (recommended)
# Extension ID: marp-team.marp-vscode
```

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** 2026-02-02
**Status:** ✅ Research Complete
**Current Phase:** All phases complete
**Document Location:** `c:\repos\.github-private\docs\research\007-marp-presentation-framework-conversion.md`

**Next Agent:** `@pch-planner` (optional) or proceed directly with implementation using Phase 4 checklist
