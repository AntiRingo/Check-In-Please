$editor = 'F:\CheckInPleaseTools\Unity\6000.3.28f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $editor)) { throw 'Unity Editor is not installed at the expected F: path.' }
$project = Join-Path $PSScriptRoot 'UnityDemo'
Start-Process -FilePath $editor -ArgumentList '-projectPath',('"' + $project + '"')
