param([switch]$PrepareOnly)
$ErrorActionPreference = 'Stop'
$projectFolder = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validationFolder = Join-Path $projectFolder 'Temp/Boss1UnityValidation'
if (Test-Path -LiteralPath (Join-Path $validationFolder 'Temp/UnityLockfile')) {
    throw 'The isolated validation project is already open.'
}
New-Item -ItemType Directory -Path $validationFolder -Force | Out-Null
foreach ($folderName in @('Assets','ProjectSettings')) {
    $destination = Join-Path $validationFolder $folderName
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $projectFolder $folderName) | Copy-Item -Destination $destination -Recurse -Force
}
$editorFolder = Join-Path $validationFolder 'Assets/Editor'
New-Item -ItemType Directory -Path $editorFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Boss1UnityValidation.cs') -Destination $editorFolder -Force
$packageFolder = Join-Path $validationFolder 'Packages'
New-Item -ItemType Directory -Path $packageFolder -Force | Out-Null
$original = Get-Content -LiteralPath (Join-Path $projectFolder 'Packages/manifest.json') -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
foreach ($entry in $original.dependencies.PSObject.Properties) {
    if ($entry.Name.StartsWith('com.unity.modules.') -or $entry.Name -in @('com.unity.inputsystem','com.unity.ugui','com.unity.render-pipelines.universal','com.unity.2d.sprite')) {
        $dependencies[$entry.Name] = $entry.Value
    }
}
$cachePath = Join-Path $projectFolder 'Library/PackageCache'
if (Test-Path -LiteralPath $cachePath) {
    foreach ($cachedPackage in (Get-ChildItem -LiteralPath $cachePath -Directory)) {
        if ($cachedPackage.Name.Contains('@')) {
            $packageName = $cachedPackage.Name.Split('@')[0]
            if ($packageName -notlike 'com.unity.ai.*' -and $packageName -notlike 'com.unity.collab*' -and $packageName -notlike 'com.unity.multiplayer.*') {
                $dependencies[$packageName] = 'file:' + $cachedPackage.FullName.Replace('\','/')
            }
        }
    }
}
[System.IO.File]::WriteAllText((Join-Path $packageFolder 'manifest.json'), (@{dependencies=$dependencies}|ConvertTo-Json -Depth 5))
if ($PrepareOnly) { Write-Output "Prepared: $validationFolder"; return }
$version = (Get-Content -LiteralPath (Join-Path $projectFolder 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1).Split(' ')[1]
$unityExe = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"
$logFile = Join-Path $validationFolder 'validation.log'
$arguments = @('-batchmode','-nographics','-projectPath',('"' + $validationFolder + '"'),'-executeMethod','Boss1UnityValidation.Run','-logFile',('"' + $logFile + '"'))
$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Unity validation PID: $($process.Id)"
Write-Output "Log: $logFile"
Write-Output "Results: $(Join-Path $validationFolder 'boss1-unity-results.txt')"
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity validation failed: $($process.ExitCode). See $logFile." }
if (-not (Test-Path -LiteralPath (Join-Path $validationFolder 'boss1-unity-results.txt'))) { throw 'Unity exited without a validation report.' }
Get-Content -LiteralPath (Join-Path $validationFolder 'boss1-unity-results.txt')

