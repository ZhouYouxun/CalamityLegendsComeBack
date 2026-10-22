param([switch]$PrepareOnly)

$ErrorActionPreference = 'Stop'
$workshopId = '3764370770'
$appId = '1281930'
$modName = 'CalamityLegendsComeBack'
$steamRoot = 'F:\SteamLibrary\steamapps'
$liveFolder = Join-Path $steamRoot "workshop\content\$appId\$workshopId"
$mirrorRoot = Join-Path $PSScriptRoot '.codex-tools\publish-workshop'
$mirrorFolder = Join-Path $mirrorRoot "content\$appId\$workshopId"
$launcher = Join-Path $steamRoot 'common\tModLoader\start-tModLoader.bat'

function Test-WorkshopCopy([string]$folder) {
    $manifest = Join-Path $folder 'workshop.json'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { return $false }
    try {
        $data = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
        if ([string]$data.SteamEntryId -ne $workshopId -or $data.ContentType -ne 'Mod') { return $false }
        $packages = @(Get-ChildItem -LiteralPath $folder -Filter "$modName.tmod" -File -Recurse)
        if ($packages.Count -eq 0) { return $false }
        foreach ($package in $packages) {
            $stream = [IO.File]::OpenRead($package.FullName)
            try {
                $header = New-Object byte[] 4
                if ($stream.Read($header, 0, 4) -ne 4 -or [Text.Encoding]::ASCII.GetString($header) -ne 'TMOD') { return $false }
            }
            finally { $stream.Dispose() }
        }
        return $true
    }
    catch { return $false }
}

if (-not (Test-Path -LiteralPath $launcher -PathType Leaf)) {
    throw "找不到 tModLoader 启动文件：$launcher"
}
$gameRunning = (Get-Process -Name 'tModLoader' -ErrorAction SilentlyContinue) -or
    (Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like '*tModLoader.dll*' })
if ($gameRunning) {
    throw '请先退出正在运行的 tModLoader，再从这个发布入口启动。'
}

if (Test-WorkshopCopy $liveFolder) {
    $parent = Split-Path -Parent $mirrorFolder
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    $pending = Join-Path $parent ("$workshopId.pending-" + [guid]::NewGuid().ToString('N'))
    try {
        Copy-Item -LiteralPath $liveFolder -Destination $pending -Recurse -Force
        if (-not (Test-WorkshopCopy $pending)) { throw 'Steam 缓存副本校验失败。' }
        if (Test-Path -LiteralPath $mirrorFolder) {
            $old = Join-Path $parent ("$workshopId.old-" + [guid]::NewGuid().ToString('N'))
            Move-Item -LiteralPath $mirrorFolder -Destination $old
            try { Move-Item -LiteralPath $pending -Destination $mirrorFolder }
            catch {
                Move-Item -LiteralPath $old -Destination $mirrorFolder
                throw
            }
            Remove-Item -LiteralPath $old -Recurse -Force
        }
        else { Move-Item -LiteralPath $pending -Destination $mirrorFolder }
    }
    finally {
        if (Test-Path -LiteralPath $pending) { Remove-Item -LiteralPath $pending -Recurse -Force }
    }
}
elseif (-not (Test-WorkshopCopy $mirrorFolder)) {
    throw 'Steam 工坊文件仍未下载完成，也没有可用的发布副本。请等 Steam 下载完成后重试。'
}

# Keep the rest of the installed Workshop mods visible in this publishing session.
# Only this mod is copied, because Steam may temporarily remove its own folder
# while tModLoader is reading earlier releases during publishing.
$liveMods = Join-Path $steamRoot "workshop\content\$appId"
$mirrorMods = Join-Path $mirrorRoot "content\$appId"
foreach ($item in Get-ChildItem -LiteralPath $liveMods -Directory) {
    if ($item.Name -eq $workshopId) { continue }
    $link = Join-Path $mirrorMods $item.Name
    if (-not (Get-Item -LiteralPath $link -Force -ErrorAction SilentlyContinue)) {
        New-Item -ItemType Junction -Path $link -Target $item.FullName | Out-Null
    }
}
$liveTerraria = Join-Path $steamRoot 'workshop\content\105600'
$mirrorTerraria = Join-Path $mirrorRoot 'content\105600'
if ((Test-Path -LiteralPath $liveTerraria) -and -not (Get-Item -LiteralPath $mirrorTerraria -Force -ErrorAction SilentlyContinue)) {
    New-Item -ItemType Junction -Path $mirrorTerraria -Target $liveTerraria | Out-Null
}

Write-Host "发布副本已就绪：$mirrorFolder"
if ($PrepareOnly) { return }
Write-Host '正在以独立的工坊副本启动 tModLoader。进入游戏后按原来的方式发布模组。'
Start-Process -FilePath $launcher -WorkingDirectory (Split-Path -Parent $launcher) -ArgumentList ('-steamworkshopfolder "' + $mirrorRoot + '"')
