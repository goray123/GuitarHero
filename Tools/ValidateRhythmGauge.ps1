param([switch]$PrepareOnly)

& (Join-Path $PSScriptRoot 'ValidateCombatFeel.ps1') -Suite RhythmGauge -PrepareOnly:$PrepareOnly
