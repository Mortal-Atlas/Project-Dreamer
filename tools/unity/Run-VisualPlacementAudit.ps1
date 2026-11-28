[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][ValidateSet('Development', 'HeldOut', 'Supplement')][string]$Phase,
    [Parameter(Mandatory)][string]$LogFile,
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$scratch = (Resolve-Path -LiteralPath $ProjectPath).Path
$product = 'ProjectSettings/ProjectSettings.asset'
if ($source -eq $scratch -or !(Select-String -LiteralPath (Join-Path $scratch $product) -Quiet -Pattern '^  productName: Dreamer Audit Isolated Smoke$')) {
    throw 'Use an isolated Unity project named Dreamer Audit Isolated Smoke.'
}

$scope = @('Assets/DreamMachine', 'Assets/Settings', 'Assets/Scenes/DreamMachine.unity',
    'ProjectSettings/DynamicsManager.asset', 'ProjectSettings/TimeManager.asset', 'ProjectSettings/TagManager.asset',
    'ProjectSettings/QualitySettings.asset', 'ProjectSettings/GraphicsSettings.asset', 'ProjectSettings/ProjectVersion.txt',
    'Packages/manifest.json', 'Packages/packages-lock.json', 'tools/unity/VisualPlacementStudy.cs')
if ($Phase -ne 'Development' -and (git -C $source status --porcelain -- $scope)) {
    throw 'Commit the controller, configuration and study before running a scored cohort.'
}
$files = @(git -C $source ls-files -c -o --exclude-standard -- $scope | Sort-Object -Unique)
if ($LASTEXITCODE -or !$files) { throw 'Cannot enumerate the audit source files.' }

$manifest = Join-Path ([System.IO.Path]::GetTempPath()) ('dreamer-h8-' + [Guid]::NewGuid().ToString('N') + '.txt')
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Git ' + (git -C $source rev-parse HEAD))
$lines.Add('# Phase ' + $Phase)
foreach ($relative in $files) {
    $destination = if ($relative -eq 'tools/unity/VisualPlacementStudy.cs') { 'Assets/Editor/VisualPlacementStudy.cs' } else { $relative }
    $from = Join-Path $source $relative
    $to = Join-Path $scratch $destination
    $expected = (Get-FileHash -LiteralPath $from -Algorithm SHA256).Hash
    if (!(Test-Path -LiteralPath $to) -or (Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash -ne $expected) {
        New-Item -Path (Split-Path -Parent $to) -ItemType Directory -Force | Out-Null
        Copy-Item -LiteralPath $from -Destination $to
    }
    if ((Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash -ne $expected) { throw "Source mismatch $destination" }
    $lines.Add("$expected $destination")
}
$lines.Add("$((Get-FileHash -LiteralPath (Join-Path $scratch $product) -Algorithm SHA256).Hash) $product")
[System.IO.File]::WriteAllLines($manifest, $lines)
$env:DREAMER_H8_SOURCE_MANIFEST = $manifest
$entry = if ($Phase -eq 'Development') { 'Develop' } else { $Phase }
try {
    $process = Start-Process -FilePath $Unity -ArgumentList "-batchmode -force-d3d12 -projectPath `"$scratch`" -executeMethod VisualPlacementStudy.$entry -logFile `"$LogFile`"" -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See $LogFile" }
    Write-Output "Verified audit $Phase finished. Manifest $manifest"
}
finally {
    Remove-Item Env:\DREAMER_H8_SOURCE_MANIFEST
}
