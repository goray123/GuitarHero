# Deterministic, original synthesized support layers. No source recordings.
$ErrorActionPreference = 'Stop'
$audioFolder = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Assets/Audio/Combat'))
$sampleRate = 44100
foreach ($clipName in @('AttackWhoosh', 'GuitarResonance', 'LandingThump')) {
    $duration = switch ($clipName) {
        'AttackWhoosh' { 0.14 }
        'GuitarResonance' { 0.36 }
        'LandingThump' { 0.16 }
    }
    $count = [int]($duration * $sampleRate)
    $samples = [double[]]::new($count)
    $random = [Random]::new(403)
    $filteredNoise = 0.0
    $peak = 0.001
    for ($i = 0; $i -lt $count; $i++) {
        $t = $i / [double]$sampleRate
        $noise = $random.NextDouble() * 2.0 - 1.0
        $filteredNoise += 0.13 * ($noise - $filteredNoise)
        $sample = switch ($clipName) {
            'AttackWhoosh' {
                $envelope = [Math]::Pow([Math]::Sin([Math]::PI * $t / $duration), 2)
                ($noise - $filteredNoise) * $envelope * 0.6
            }
            'GuitarResonance' {
                $chord = 0.0
                foreach ($frequency in @(82.4069, 123.4708, 164.8138)) {
                    for ($harmonic = 1; $harmonic -le 5; $harmonic++) {
                        $chord += [Math]::Sin(2.0 * [Math]::PI * $frequency * $harmonic * $t) / ($harmonic * 3.0)
                    }
                }
                [Math]::Tanh($chord * 1.6) * [Math]::Exp(-11.0 * $t)
            }
            'LandingThump' {
                $phase = 2.0 * [Math]::PI * (45.0 * $t + 1.5 * (1.0 - [Math]::Exp(-30.0 * $t)))
                [Math]::Sin($phase) * [Math]::Exp(-32.0 * $t) + $filteredNoise * [Math]::Exp(-50.0 * $t) * 0.3
            }
        }
        # Short fades prevent clicks, including a held resonance cut at file end.
        $sample *= [Math]::Min(1.0, $t / 0.003) * [Math]::Min(1.0, ($duration - $t) / 0.02)
        $samples[$i] = $sample
        $peak = [Math]::Max($peak, [Math]::Abs($sample))
    }
    $filePath = Join-Path $audioFolder ($clipName + '.wav')
    $writer = [System.IO.BinaryWriter]::new([System.IO.File]::Create($filePath))
    try {
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write([int](36 + $count * 2))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
        $writer.Write([int]16)
        $writer.Write([int16]1)
        $writer.Write([int16]1)
        $writer.Write([int]$sampleRate)
        $writer.Write([int]($sampleRate * 2))
        $writer.Write([int16]2)
        $writer.Write([int16]16)
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write([int]($count * 2))
        foreach ($sample in $samples) { $writer.Write([int16]($sample / $peak * 0.8 * 32767)) }
    }
    finally { $writer.Dispose() }
    Write-Output "Generated $clipName ($count samples)"
}
