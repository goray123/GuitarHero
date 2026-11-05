param()
$ErrorActionPreference = 'Stop'
$projectFolder = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validationFolder = Join-Path $projectFolder 'Temp/Boss1Validation'
New-Item -ItemType Directory -Path $validationFolder -Force | Out-Null
$compiler = Join-Path ([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'A C# compiler is required. Run this validation in a Windows environment with .NET Framework.'
}
$exe = Join-Path $validationFolder 'Boss1Validation.exe'
& $compiler /nologo /target:exe ("/out:" + $exe) (Join-Path $projectFolder 'Assets/Scripts/Characters/Boss1/Boss1CombatModel.cs') (Join-Path $PSScriptRoot 'Boss1Validation.cs')
if ($LASTEXITCODE -ne 0) { throw 'Boss1 model compilation failed.' }
& $exe (Join-Path $validationFolder 'boss1-results.txt')
if ($LASTEXITCODE -ne 0) { throw 'Boss1 combat validation failed.' }

