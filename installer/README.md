# Hugh Installer

This directory contains the Inno Setup script for creating the Hugh CLI Windows installer.

## Prerequisites

1. **Inno Setup 6+** - Download from https://jrsoftware.org/isinfo.php
2. **Published Hugh Application** - Self-contained publish output must exist

## Building the Installer

### Step 1: Publish the Application

From the repository root, run:

```powershell
cd Hugh
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

This creates the self-contained application at:
`Hugh\bin\Release\net8.0\win-x64\publish\`

### Step 2: Build the Installer

Using Inno Setup Compiler (command line):

```powershell
# Navigate to installer directory
cd installer

# Build with a specific version
iscc /DMyAppVersion=1.0.0 hugh-setup.iss

# Or use the default version (0.0.0) for local testing
iscc hugh-setup.iss
```

The installer will be created at: `dist\hugh-setup-{version}.exe`

### Step 3: Using Inno Setup GUI

1. Open Inno Setup Compiler (iscc.exe)
2. Open `hugh-setup.iss`
3. Press F9 or click Build → Compile
4. The output will be in the `dist\` directory

## Testing the Installer

### Installation Test

1. **Run the installer:**
   ```powershell
   .\dist\hugh-setup-1.0.0.exe
   ```

2. **Use default settings** and complete the wizard

3. **Verify installation location:**
   ```powershell
   Test-Path "$env:ProgramFiles\Hugh\Hugh.exe"
   # Or for per-user installation:
   Test-Path "$env:LOCALAPPDATA\Programs\Hugh\Hugh.exe"
   ```

4. **Open a NEW terminal** (important - existing terminals won't have updated PATH)

5. **Verify hugh command works:**
   ```powershell
   hugh --version
   ```

6. **Verify PATH was updated:**
   ```powershell
   $env:Path -split ';' | Where-Object { $_ -like '*Hugh*' }
   ```

### Uninstallation Test

1. **Run the uninstaller** via Windows Settings → Apps → Hugh → Uninstall
   
   Or via command line:
   ```powershell
   & "$env:ProgramFiles\Hugh\unins000.exe"
   ```

2. **Verify files removed:**
   ```powershell
   Test-Path "$env:ProgramFiles\Hugh"  # Should be False
   ```

3. **Open a NEW terminal** and verify PATH was cleaned:
   ```powershell
   $env:Path -split ';' | Where-Object { $_ -like '*Hugh*' }
   # Should return nothing
   ```

### Silent Installation Test

For enterprise deployment testing:

```powershell
# Silent install (shows progress but no interaction)
.\dist\hugh-setup-1.0.0.exe /SILENT

# Very silent (no UI at all)
.\dist\hugh-setup-1.0.0.exe /VERYSILENT

# Specify custom install directory
.\dist\hugh-setup-1.0.0.exe /SILENT /DIR="C:\CustomPath\Hugh"
```

### Silent Uninstallation

```powershell
& "$env:ProgramFiles\Hugh\unins000.exe" /SILENT
```

## Version Management

The installer version is set during compilation:

```powershell
# Set version explicitly
iscc /DMyAppVersion=2.1.0 hugh-setup.iss

# In CI/CD, use the git tag
$version = git describe --tags --abbrev=0
iscc /DMyAppVersion=$version hugh-setup.iss
```

If no version is specified, defaults to `0.0.0`.

## Output

| File | Location | Description |
|------|----------|-------------|
| Installer | `dist\hugh-setup-{version}.exe` | Setup executable for distribution |

## Installed Files

The installer bundles all required components:

| Component | Install Location | Source |
|-----------|-----------------|--------|
| Hugh.exe | `{app}\Hugh.exe` | Self-contained .NET publish output |
| appsettings.json | `{app}\appsettings.json` | Configuration file |
| Agent Files | `{app}\agents\*.md` | PCH agent definitions (pch-*.md) |

The agent files are **required** for Hugh to function. They contain the prompts for:
- `pch-visionary.md` - Vision document creation
- `pch-researcher.md` - Research phase execution
- `pch-planner.md` - Implementation planning
- `pch-plan-reviewer.md` - Plan review
- `pch-coder.md` - Code implementation
- `pch-helper.md` - User assistance

## Troubleshooting

### "Path already exists" warning during test

This is expected if you're reinstalling. The installer correctly detects and skips adding duplicate PATH entries.

### "hugh" command not found after install

Open a **new** terminal window. The PATH update takes effect immediately for Windows Explorer, but existing terminal sessions need to be restarted.

### Permission denied during installation

By default, the installer requests user-level permissions only. If installing to `Program Files`, Windows may prompt for admin rights.

## Icon File (Optional)

To add a custom installer icon:

1. Create a `.ico` file for Hugh
2. Place it at `Hugh\assets\hugh-icon.ico`
3. Uncomment the `SetupIconFile` line in `hugh-setup.iss`
