param()
$ErrorActionPreference = 'Stop'
$projectFolder = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = (Get-Content -LiteralPath (Join-Path $projectFolder 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1).Split(' ')[1]
$editorData = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Data"
$templateRoot = Join-Path $editorData 'Resources/PackageManager/ProjectTemplates/libcache'
$template = Get-ChildItem -LiteralPath $templateRoot -Directory | Where-Object { $_.Name -like 'com.unity.template.2d*' } | Select-Object -First 1
$packageAssemblies = Join-Path $projectFolder 'Library/ScriptAssemblies'
if (-not (Test-Path -LiteralPath (Join-Path $packageAssemblies 'Unity.InputSystem.dll'))) {
    $packageAssemblies = Join-Path $template.FullName 'ScriptAssemblies'
}
$validationFolder = Join-Path $projectFolder 'Temp/Boss1Validation'
New-Item -ItemType Directory -Path $validationFolder -Force | Out-Null
$references = @(Get-ChildItem -LiteralPath (Join-Path $editorData 'NetStandard/ref/2.1.0') -Filter '*.dll') +
    @(Get-ChildItem -LiteralPath (Join-Path $editorData 'Managed/UnityEngine') -Filter 'UnityEngine*.dll') +
    @(Get-Item -LiteralPath (Join-Path $packageAssemblies 'Unity.InputSystem.dll')) +
    @(Get-Item -LiteralPath (Join-Path $packageAssemblies 'UnityEngine.UI.dll'))
$options = @('/nologo','/target:library','/langversion:latest','/nostdlib+','/nowarn:0649',
    ('/out:"' + (Join-Path $validationFolder 'Assembly-CSharp.dll') + '"'))
$options += $references | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$options += Get-ChildItem -LiteralPath (Join-Path $projectFolder 'Assets/Scripts') -Filter '*.cs' -Recurse |
    ForEach-Object { '"' + $_.FullName + '"' }
$responseFile = Join-Path $validationFolder 'runtime-compile.rsp'
Set-Content -LiteralPath $responseFile -Value $options
& (Join-Path $editorData 'MonoBleedingEdge/bin/mono.exe') (Join-Path $editorData 'MonoBleedingEdge/lib/mono/msbuild/Current/bin/Roslyn/csc.exe') /noconfig ("@" + $responseFile)
if ($LASTEXITCODE -ne 0) { throw 'Unity runtime script compilation failed.' }
Write-Output "PASS: All Assets/Scripts compiled against Unity $version runtime assemblies."

