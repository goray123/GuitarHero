# Run in a disposable project so the user's open editor and scenes stay intact.
param([switch]$PrepareOnly)

$ErrorActionPreference = 'Stop'
$projectFolder = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validationFolder = Join-Path $projectFolder 'Temp/CombatFeelValidation'
if (Test-Path -LiteralPath (Join-Path $validationFolder 'Temp/UnityLockfile')) {
    throw 'The isolated validation project is already open. Wait for its Unity process to exit before running again.'
}
Write-Output "Preparing isolated validation project: $validationFolder"
New-Item -ItemType Directory -Path $validationFolder -Force | Out-Null
foreach ($folderName in @('Assets', 'ProjectSettings')) {
    $destination = Join-Path $validationFolder $folderName
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $projectFolder $folderName) | Copy-Item -Destination $destination -Recurse -Force
}
$editorFolder = Join-Path $validationFolder 'Assets/Editor'
New-Item -ItemType Directory -Path $editorFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CombatFeelValidation.cs') -Destination $editorFolder -Force
$packageFolder = Join-Path $validationFolder 'Packages'
New-Item -ItemType Directory -Path $packageFolder -Force | Out-Null
# Pin installed packages to the local cache, avoiding registry downloads.
$manifest = Get-Content -LiteralPath (Join-Path $projectFolder 'Packages/manifest.json') -Raw | ConvertFrom-Json
$cachedDependencies = [ordered]@{}
foreach ($entry in $manifest.dependencies.PSObject.Properties) {
    if ($entry.Name.StartsWith('com.unity.modules.')) { $cachedDependencies[$entry.Name] = $entry.Value }
}
foreach ($cachedPackage in (Get-ChildItem -LiteralPath (Join-Path $projectFolder 'Library/PackageCache') -Directory)) {
    if ($cachedPackage.Name.Contains('@')) {
        $packageName = $cachedPackage.Name.Split('@')[0]
        $cachedDependencies[$packageName] = 'file:' + $cachedPackage.FullName.Replace('\', '/')
    }
}
$manifestText = @{ dependencies = $cachedDependencies } | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $packageFolder 'manifest.json'), $manifestText)
$versionLine = Get-Content -LiteralPath (Join-Path $projectFolder 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
$version = $versionLine.Split(' ')[1]
$unityExe = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"
$logFile = Join-Path $validationFolder 'validation.log'
if ($PrepareOnly) {
    Write-Output "Prepared validation project: $validationFolder"
    return
}
$launchArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $validationFolder + '"'), '-executeMethod', 'CombatFeelValidation.Run', '-logFile', ('"' + $logFile + '"'))
$validationProcess = Start-Process -FilePath $unityExe -ArgumentList $launchArguments -WindowStyle Hidden -PassThru
Write-Output "Unity validation PID: $($validationProcess.Id)"
Write-Output "Log: $logFile"
Write-Output "Results: $(Join-Path $validationFolder 'combat-feel-results.txt')"
