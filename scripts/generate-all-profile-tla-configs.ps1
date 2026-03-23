[CmdletBinding()]
param(
    [string]$ProfilesDirectory = "",
    [string]$OutputDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedProfilesDirectory =
    if ([string]::IsNullOrWhiteSpace($ProfilesDirectory)) {
        Join-Path $repoRoot "src\ClearanceGate.Profiles"
    }
    else {
        $ProfilesDirectory
    }
$resolvedOutputDirectory =
    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
        Join-Path $repoRoot "generated\tla"
    }
    else {
        $OutputDirectory
    }

[System.IO.Directory]::CreateDirectory($resolvedOutputDirectory) | Out-Null

$profileFiles = Get-ChildItem -Path $resolvedProfilesDirectory -Filter *.json -File | Sort-Object Name
if ($profileFiles.Count -eq 0) {
    throw "No embedded profile json files were found in '$resolvedProfilesDirectory'."
}

foreach ($profileFile in $profileFiles) {
    $profileName = [System.IO.Path]::GetFileNameWithoutExtension($profileFile.Name)
    $configPath = Join-Path $resolvedOutputDirectory ($profileName + "_profile_conformance.cfg")
    $roleConfigPath = Join-Path $resolvedOutputDirectory ($profileName + "_profile_role_conformance.cfg")

    & (Join-Path $PSScriptRoot "generate-profile-tla-config.ps1") `
        -ProfilePath $profileFile.FullName `
        -OutputPath $configPath `
        -RoleOutputPath $roleConfigPath
}

Write-Host ("Generated profile TLC configs under " + $resolvedOutputDirectory)
