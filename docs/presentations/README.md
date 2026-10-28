# PCH Agent Workflow Presentation

This directory contains the Marp-based presentation for the PCH Agent Workflow system.

## Prerequisites

1. **Node.js** (v18 or higher)
   - Download from: https://nodejs.org/

2. **Marp CLI** (v4.0.0 or higher)
   ```powershell
   npm install -g @marp-team/marp-cli
   ```

## Building the Presentation

Navigate to this directory and run the build script:

```powershell
cd docs/presentations
```

### Basic Build

Generates the HTML presentation file:

```powershell
.\Build-Presentation.ps1
```

### Watch Mode

Automatically rebuilds when the source file changes:

```powershell
.\Build-Presentation.ps1 -Watch
```

Press `Ctrl+C` to stop watch mode.

### Preview Mode

Opens the presentation in a browser with live reload:

```powershell
.\Build-Presentation.ps1 -Preview
```

## Viewing the Presentation

After building, open `pch-agent-workflow.html` in any modern web browser.

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `→` or `Space` | Next slide |
| `←` | Previous slide |
| `Home` | First slide |
| `End` | Last slide |
| `F` | Toggle fullscreen |
| `P` | Toggle presenter mode |
| `O` | Toggle slide overview |
| `Esc` | Exit fullscreen/overview |

## Notes

- **Mermaid Diagrams**: The presentation uses Mermaid for diagrams, which requires CDN access for rendering. Ensure you have internet connectivity when viewing the presentation.
- **Local Files**: The `--allow-local-files` flag enables loading of local images and assets referenced in the presentation.

## File Structure

```
docs/presentations/
├── pch-agent-workflow.md       # Source Marp markdown file
├── pch-agent-workflow.html     # Generated HTML presentation
├── Build-Presentation.ps1      # PowerShell build script
└── README.md                   # This documentation file
```
