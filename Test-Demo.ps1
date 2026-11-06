param(
    [ValidateSet('All', 'Demo', 'Hotel')][string]$Mode = 'All',
    [string]$PlayerPath = (Join-Path $PSScriptRoot 'Builds\Windows\CheckInPlease.exe'),
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
$PlayerPath = (Resolve-Path -LiteralPath $PlayerPath).Path
if (-not ('CheckInPlease.Tools.BackgroundPlayer' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'Tools\BackgroundPlayer.cs')
}
$logDirectory = Join-Path $PSScriptRoot 'Builds'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$suites = if ($Mode -eq 'All') { @('Hotel', 'Demo') } else { @($Mode) }
foreach ($suite in $suites) {
    $flag = if ($suite -eq 'Hotel') { '-hotelSmokeTest' } else { '-demoSmokeTest' }
    $resultName = if ($suite -eq 'Hotel') { 'hotel-result.json' } else { 'smoke-result.json' }
    $resultPath = Join-Path (Split-Path -Parent $PlayerPath) ('Validation\' + $resultName)
    $log = Join-Path $logDirectory ($suite.ToLowerInvariant() + '-background-validation.log')
    # Keep graphics enabled for screenshots. Never switch to the validation desktop.
    $playerArguments = "$flag -force-d3d11 -force-d3d11-bitblt-model -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile `"$log`""
    $started = [DateTime]::UtcNow
    $player = [CheckInPlease.Tools.BackgroundPlayer]::new($PlayerPath, $playerArguments)
    try {
        Write-Output "$suite validation started on an inactive desktop (PID $($player.Id))."
        while (-not $player.WaitForExit(1000)) {
            if (([DateTime]::UtcNow - $started).TotalSeconds -ge $TimeoutSeconds) {
                throw "$suite validation timed out after $TimeoutSeconds seconds. See $log"
            }
        }
        if ($player.ExitCode -ne 0) { throw "$suite validation failed (exit $($player.ExitCode)). See $log" }
        if (-not (Test-Path -LiteralPath $resultPath) -or (Get-Item -LiteralPath $resultPath).LastWriteTimeUtc -lt $started) {
            throw "$suite validation did not produce a fresh result. See $log"
        }
        $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
        if ($result.passed -ne $true -or $result.errorCount -ne 0) { throw "$suite validation failed. See $resultPath" }
        Write-Output "$suite validation PASS: $resultPath"
    }
    finally { $player.Dispose() }
}
