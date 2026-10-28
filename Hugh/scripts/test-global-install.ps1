<#
.SYNOPSIS
    Tests the global installation of Hugh CLI.

.DESCRIPTION
    This script verifies that Hugh is correctly installed and configured for global use.
    It checks PATH configuration, version output, prerequisite detection, and log creation.

.EXAMPLE
    .\test-global-install.ps1
    Runs all installation verification tests.

.NOTES
    Exit codes:
    0 - All tests passed
    1 - One or more tests failed
#>

[CmdletBinding()]
param()

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:ErrorDetails = @()

function Write-TestHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Details = ""
    )
    
    if ($Passed) {
        Write-Host "[PASS] " -ForegroundColor Green -NoNewline
        Write-Host $TestName
        $script:TestsPassed++
    } else {
        Write-Host "[FAIL] " -ForegroundColor Red -NoNewline
        Write-Host $TestName
        if ($Details) {
            Write-Host "       $Details" -ForegroundColor Yellow
        }
        $script:TestsFailed++
        $script:ErrorDetails += "$TestName : $Details"
    }
}

function Test-HughOnPath {
    <#
    .SYNOPSIS
        Verifies Hugh executable is available on PATH.
    #>
    Write-TestHeader "Test 1: Hugh on PATH"
    
    $hughCommand = Get-Command "hugh" -ErrorAction SilentlyContinue
    
    if ($hughCommand) {
        Write-TestResult "Hugh executable found on PATH" $true
        Write-Host "       Location: $($hughCommand.Source)" -ForegroundColor Gray
        return $true
    } else {
        Write-TestResult "Hugh executable found on PATH" $false "Command 'hugh' not found in PATH"
        
        # Check if hugh exists in common locations
        $commonPaths = @(
            "$env:LOCALAPPDATA\Programs\Hugh\hugh.exe",
            "$env:ProgramFiles\Hugh\hugh.exe",
            "C:\Tools\Hugh\hugh.exe"
        )
        
        foreach ($path in $commonPaths) {
            if (Test-Path $path) {
                Write-Host "       Found at: $path (but not in PATH)" -ForegroundColor Yellow
            }
        }
        
        return $false
    }
}

function Test-HughVersion {
    <#
    .SYNOPSIS
        Runs hugh --version and validates output.
    #>
    Write-TestHeader "Test 2: Version Command"
    
    try {
        $versionOutput = & hugh --version 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-TestResult "hugh --version executed successfully" $true
            Write-Host "       Output: $versionOutput" -ForegroundColor Gray
            return $true
        } else {
            Write-TestResult "hugh --version executed successfully" $false "Exit code: $exitCode"
            return $false
        }
    } catch {
        Write-TestResult "hugh --version executed successfully" $false $_.Exception.Message
        return $false
    }
}

function Test-HughPrerequisites {
    <#
    .SYNOPSIS
        Runs hugh --check to verify prerequisites.
    #>
    Write-TestHeader "Test 3: Prerequisites Check"
    
    try {
        $checkOutput = & hugh --check 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-TestResult "hugh --check passed all prerequisites" $true
            Write-Host "       Output:" -ForegroundColor Gray
            $checkOutput | ForEach-Object { Write-Host "         $_" -ForegroundColor Gray }
            return $true
        } else {
            Write-TestResult "hugh --check passed all prerequisites" $false "Exit code: $exitCode"
            Write-Host "       Output:" -ForegroundColor Yellow
            $checkOutput | ForEach-Object { Write-Host "         $_" -ForegroundColor Yellow }
            return $false
        }
    } catch {
        Write-TestResult "hugh --check passed all prerequisites" $false $_.Exception.Message
        return $false
    }
}

function Test-LogFileCreation {
    <#
    .SYNOPSIS
        Verifies that log files are created in the expected location.
    #>
    Write-TestHeader "Test 4: Log File Creation"
    
    $logDir = Join-Path $env:USERPROFILE ".hugh\logs"
    
    # Check if log directory exists
    if (Test-Path $logDir) {
        Write-TestResult "Log directory exists" $true
        Write-Host "       Location: $logDir" -ForegroundColor Gray
        
        # Check for recent log files
        $recentLogs = Get-ChildItem -Path $logDir -Filter "*.log" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 3
        
        if ($recentLogs) {
            Write-TestResult "Log files present" $true
            $recentLogs | ForEach-Object {
                Write-Host "       - $($_.Name) ($(Get-Date $_.LastWriteTime -Format 'yyyy-MM-dd HH:mm'))" -ForegroundColor Gray
            }
            return $true
        } else {
            Write-TestResult "Log files present" $false "No .log files found in $logDir"
            return $false
        }
    } else {
        Write-TestResult "Log directory exists" $false "Directory not found: $logDir"
        Write-Host "       The directory will be created on first Hugh execution." -ForegroundColor Yellow
        return $false
    }
}

function Test-GitHubCLI {
    <#
    .SYNOPSIS
        Verifies GitHub CLI is installed and authenticated.
    #>
    Write-TestHeader "Test 5: GitHub CLI (Prerequisite)"
    
    # Check if gh is available
    $ghCommand = Get-Command "gh" -ErrorAction SilentlyContinue
    
    if (-not $ghCommand) {
        Write-TestResult "GitHub CLI installed" $false "Command 'gh' not found"
        return $false
    }
    
    Write-TestResult "GitHub CLI installed" $true
    Write-Host "       Location: $($ghCommand.Source)" -ForegroundColor Gray
    
    # Check authentication status
    try {
        $authStatus = & gh auth status 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-TestResult "GitHub CLI authenticated" $true
            return $true
        } else {
            Write-TestResult "GitHub CLI authenticated" $false "Not authenticated. Run 'gh auth login'"
            return $false
        }
    } catch {
        Write-TestResult "GitHub CLI authenticated" $false $_.Exception.Message
        return $false
    }
}

function Test-CopilotExtension {
    <#
    .SYNOPSIS
        Verifies GitHub Copilot CLI extension is installed.
    #>
    Write-TestHeader "Test 6: Copilot CLI Extension (Prerequisite)"
    
    try {
        $extensions = & gh extension list 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -ne 0) {
            Write-TestResult "Copilot CLI extension installed" $false "Could not list extensions"
            return $false
        }
        
        $copilotExt = $extensions | Select-String -Pattern "copilot" -SimpleMatch
        
        if ($copilotExt) {
            Write-TestResult "Copilot CLI extension installed" $true
            Write-Host "       $copilotExt" -ForegroundColor Gray
            return $true
        } else {
            Write-TestResult "Copilot CLI extension installed" $false "Extension not found. Run 'gh extension install github/gh-copilot'"
            return $false
        }
    } catch {
        Write-TestResult "Copilot CLI extension installed" $false $_.Exception.Message
        return $false
    }
}

# =============================================================================
# Main Test Execution
# =============================================================================

Write-Host ""
Write-Host "Hugh Global Installation Test Suite" -ForegroundColor Magenta
Write-Host "====================================" -ForegroundColor Magenta
Write-Host "Running installation verification tests..."
Write-Host ""

# Run tests in order (some depend on previous results)
$hughOnPath = Test-HughOnPath

if ($hughOnPath) {
    Test-HughVersion | Out-Null
    Test-HughPrerequisites | Out-Null
}

Test-LogFileCreation | Out-Null
Test-GitHubCLI | Out-Null
Test-CopilotExtension | Out-Null

# =============================================================================
# Summary
# =============================================================================

Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host " TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""
Write-Host "  Tests Passed: " -NoNewline
Write-Host $script:TestsPassed -ForegroundColor Green
Write-Host "  Tests Failed: " -NoNewline
Write-Host $script:TestsFailed -ForegroundColor $(if ($script:TestsFailed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($script:TestsFailed -gt 0) {
    Write-Host "Failed Tests:" -ForegroundColor Red
    $script:ErrorDetails | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "See troubleshooting section in Hugh/README.md for solutions." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "All tests passed! Hugh is correctly installed." -ForegroundColor Green
    exit 0
}
