param([Parameter(Mandatory=$true)][string]$Command, [string]$Parameters = '{}')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$connection = Get-Content (Join-Path $projectRoot 'Library/Pipeline/.unity-pipeline-port') -Raw | ConvertFrom-Json
if ($connection.projectPath.TrimEnd('\') -ne $projectRoot.TrimEnd('\')) { throw 'Wrong Unity project' }
$body = @{ command = $Command; parameters = ($Parameters | ConvertFrom-Json); caller = 'plugin'; skill = 'unity-feature-implementation' } | ConvertTo-Json -Depth 20
$headers = @{ Authorization = 'Bearer ' + $connection.evalToken }
Invoke-RestMethod "http://127.0.0.1:$($connection.port)/api/exec" -Method Post -ContentType 'application/json' -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -Headers $headers -NoProxy -TimeoutSec 60 | ConvertTo-Json -Depth 30
