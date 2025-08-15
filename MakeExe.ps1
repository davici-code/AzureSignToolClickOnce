# PowerShell script to merge assemblies using ILMerge
Write-Host "Starting ILMerge process..." -ForegroundColor Green
Write-Host

# Get the directory where the script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

$SourceDir = ".\AzureSignToolClickOnce\bin\Debug"
$OutputDir = ".\"
$MainExe = "AzureSignToolClickOnce.exe"
$OutputExe = "AzureKeyVaultSigner.exe"

Write-Host "Source directory: $SourceDir" -ForegroundColor Cyan
Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan
Write-Host

# Check if source directory exists
if (-not (Test-Path $SourceDir)) {
    Write-Host "ERROR: Source directory not found: $SourceDir" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Check if main executable exists
$MainExePath = Join-Path $SourceDir $MainExe
if (-not (Test-Path $MainExePath)) {
    Write-Host "ERROR: Main executable not found: $MainExePath" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Get all DLL files in the source directory
$DllFiles = Get-ChildItem -Path $SourceDir -Filter "*.dll" | ForEach-Object { $_.FullName }

if ($DllFiles.Count -eq 0) {
    Write-Host "WARNING: No DLL files found in source directory" -ForegroundColor Yellow
}

Write-Host "Merging assemblies..." -ForegroundColor Yellow

# Build the ILMerge command
$OutputPath = Join-Path $OutputDir $OutputExe
$ILMergeArgs = @(
    "/out:$OutputPath"
    $MainExePath
	 "/attr:$MainExePath"
) + $DllFiles

try {
    & ilrepack $ILMergeArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host
        Write-Host "SUCCESS: Merge completed successfully!" -ForegroundColor Green
        Write-Host "Output file: $OutputPath" -ForegroundColor Green
    } else {
        Write-Host
        Write-Host "ERROR: Merge failed with exit code $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host
    Write-Host "ERROR: Failed to execute ILMerge. Make sure it's installed and in your PATH." -ForegroundColor Red
    Write-Host "Exception: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host
Read-Host "Press Enter to exit"