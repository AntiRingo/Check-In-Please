param([string]$UnityPath = 'F:\CheckInPleaseTools\Unity\6000.3.28f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity Editor not found: $UnityPath" }
$sourceProject = Join-Path $PSScriptRoot 'UnityDemo'
$stagingRoot = 'F:\CheckInPleaseTools\BuildWorkspace'
$project = Join-Path $stagingRoot 'UnityDemo'
$logDirectory = Join-Path $PSScriptRoot 'Builds'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $project | Out-Null
# UnityLinker splits commas in assembly paths. Use an ASCII staging path.
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
  $from = Join-Path $sourceProject $folder
  $to = [IO.Path]::GetFullPath((Join-Path $project $folder))
  if (-not $to.StartsWith($stagingRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe staging destination' }
  if ((Test-Path -LiteralPath $to) -and ((Get-Item -LiteralPath $to).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Staging destination must not be a link' }
  $syncOutput = & robocopy.exe $from $to /MIR /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
  if ($LASTEXITCODE -ge 8) { throw ("Unable to sync $folder into staging project: " + ($syncOutput -join [Environment]::NewLine)) }
}
$log = Join-Path $logDirectory 'unity-build.log'
$argsList = @('-batchmode','-nographics','-projectPath',('"' + $project + '"'),'-executeMethod','DemoBuilder.Build','-logFile',('"' + $log + '"'))
$p = Start-Process -FilePath $UnityPath -ArgumentList $argsList -WindowStyle Hidden -PassThru -Wait
if ($p.ExitCode -ne 0) { throw "Unity build failed ($($p.ExitCode)). See $log for the specific cause." }
$built = Join-Path $stagingRoot 'Builds\Windows'
if (-not (Test-Path -LiteralPath (Join-Path $built 'build-success.json'))) { throw 'Build did not produce a success marker' }
$published = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Builds\Windows'))
if (-not $published.StartsWith($PSScriptRoot + '\Builds\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe build destination' }
if ((Test-Path -LiteralPath $published) -and ((Get-Item -LiteralPath $published).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Build destination must not be a link' }
$syncOutput = & robocopy.exe $built $published /MIR /XD Validation /XF '*.log' /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -ge 8) { throw ('Unable to publish completed build: ' + ($syncOutput -join [Environment]::NewLine)) }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Docs\QuickStart.zh-CN.txt') -Destination (Join-Path $published 'README.txt') -Force
Write-Output (Join-Path $PSScriptRoot 'Builds\Windows\CheckInPlease.exe')
