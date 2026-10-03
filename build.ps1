param(
    [string]$Dotnet = 'dotnet'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($env:OS -ne 'Windows_NT') {
    throw 'This WPF application must be built and tested on Windows.'
}

$repoRoot = $PSScriptRoot
$version = '1.9.1'
$artifactRoot = Join-Path $repoRoot 'artifacts'
$runDirectory = Join-Path $artifactRoot (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
$publishDirectory = Join-Path $runDirectory ('Timer-' + $version + '-win-x64')
$testDirectory = Join-Path $runDirectory 'TestResults'
$archivePath = Join-Path $runDirectory ('Timer-' + $version + '-win-x64.zip')

Push-Location $repoRoot
try {
    $sdks = @(& $Dotnet --list-sdks)
    if ($LASTEXITCODE -ne 0 -or !($sdks | Where-Object { $_ -match '^8\.' })) {
        throw '.NET 8 SDK is required. Install it or pass -Dotnet with its dotnet.exe path.'
    }
    New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
    & $Dotnet test 'Timer.sln' -c Release --logger 'trx;LogFileName=tests.trx' --results-directory $testDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed; no release package was created.' }

    & $Dotnet publish 'Timer/Timer.csproj' -c Release -r win-x64 --self-contained true -o $publishDirectory '-p:DebugType=None' '-p:DebugSymbols=false'
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed; no release package was created.' }

    Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging/portable.mode') -Destination $publishDirectory
    Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging/settings.json') -Destination $publishDirectory
    Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination (Join-Path $publishDirectory 'README.md')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publishDirectory, $archivePath, [System.IO.Compression.CompressionLevel]::Optimal, $true)
    $hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $checksumPath = Join-Path $runDirectory 'SHA256SUMS.txt'
    ($hash + '  ' + [System.IO.Path]::GetFileName($archivePath)) | Set-Content -LiteralPath $checksumPath -Encoding ASCII
    Write-Host ('Package: ' + $archivePath)
    Write-Host ('SHA256:  ' + $hash)
}
finally {
    Pop-Location
}
