#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EyefindGfx,
    [Parameter(Mandatory)][string]$FfdecJar
)

$ErrorActionPreference = 'Stop'
$eyefind = (Resolve-Path -LiteralPath $EyefindGfx).Path
$ffdec = (Resolve-Path -LiteralPath $FfdecJar).Path
$work = Join-Path $PSScriptRoot '../../lumen-sante/obj/Eyefind/native'
$work = [IO.Path]::GetFullPath($work)
$export = Join-Path $work 'original'
$package = Join-Path $work 'scaleform_web'
[void][IO.Directory]::CreateDirectory($work)

function Invoke-Ffdec([string[]]$Arguments) {
    Push-Location -LiteralPath $work
    try {
        $output = & java -jar $ffdec -onerror abort @Arguments
        if ($LASTEXITCODE -ne 0) { throw ($output -join [Environment]::NewLine) }
    }
    finally {
        Pop-Location
    }
}

function Write-FrameScript([string]$Folder, [string]$Source) {
    $frame = Join-Path $Folder 'frame_1'
    [void][IO.Directory]::CreateDirectory($frame)
    [IO.File]::WriteAllText((Join-Path $frame 'DoAction.as'), $Source, [Text.UTF8Encoding]::new($false))
}

Invoke-Ffdec @('-export', 'script', $export, $eyefind)
$original = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $export 'scripts/frame_1/DoAction.as')
if ($original.Trim() -notmatch '^var TIMELINE = new com\.rockstargames\.gtav\.web\.WWW_EYEFIND_INFO\(this\);\s+TIMELINE\.INITIALISE\(this\);$') {
    throw 'O frame inicial não corresponde ao formato do Eyefind Legacy analisado.'
}

[void][IO.Directory]::CreateDirectory($package)
$homeScripts = Join-Path $work 'home-scripts'
$siteScripts = Join-Path $work 'site-scripts'
$homePatch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'EyefindHome.as')
$dashboardPatch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'HomeDashboard.as')
$logoPatch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'LogoSite.as')
$logoPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../lumen-sante/Assets/logo.png')).Path
Add-Type -AssemblyName System.Drawing
$sourceLogo = [Drawing.Image]::FromFile($logoPath)
$smallLogo = New-Object Drawing.Bitmap 64, 64, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $graphics = [Drawing.Graphics]::FromImage($smallLogo)
    try {
        $graphics.Clear([Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($sourceLogo, 0, 0, 64, 64)
    } finally { $graphics.Dispose() }
    $pixels = [Text.StringBuilder]::new(16384)
    for ($y = 0; $y -lt 64; $y++) {
        for ($x = 0; $x -lt 64; $x++) {
            $pixel = $smallLogo.GetPixel($x, $y)
            [void]$pixels.AppendFormat('{0:x1}{1:x1}{2:x1}{3:x1}',
                [int][Math]::Round($pixel.A / 17.0), [int][Math]::Round($pixel.R / 17.0),
                [int][Math]::Round($pixel.G / 17.0), [int][Math]::Round($pixel.B / 17.0))
        }
    }
    $logoPatch = $logoPatch.Replace('__LUMEN_LOGO_PIXELS__', $pixels.ToString())
} finally { $smallLogo.Dispose(); $sourceLogo.Dispose() }
$iconMasks = @()
foreach ($iconName in @('left', 'right', 'home', 'person', 'bed', 'history', 'exams', 'notes', 'medicines', 'calendar')) {
    $iconPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "../../lumen-sante/Assets/$iconName.png")).Path
    $iconBitmap = [Drawing.Bitmap]::FromFile($iconPath)
    try {
        if ($iconBitmap.Width -ne 24 -or $iconBitmap.Height -ne 24) { throw "Ícone $iconName deve ter 24 × 24 pixels." }
        $mask = [Text.StringBuilder]::new(576)
        for ($y = 0; $y -lt 24; $y++) {
            for ($x = 0; $x -lt 24; $x++) {
                [void]$mask.AppendFormat('{0:x1}', [int][Math]::Round($iconBitmap.GetPixel($x, $y).A / 17.0))
            }
        }
        $iconMasks += ('{0}:"{1}"' -f $iconName, $mask.ToString())
    } finally { $iconBitmap.Dispose() }
}
$logoPatch = $logoPatch.Replace('__LUMEN_ICON_MASKS__', ($iconMasks -join ','))
$headerPatch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'CommonHeader.as')
$bannerPatch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'BannerSite.as')
$bannerPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../lumen-sante/Assets/banner.png')).Path
$bannerSource = [Drawing.Image]::FromFile($bannerPath)
$bannerBitmap = New-Object Drawing.Bitmap 128, 48, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $graphics = [Drawing.Graphics]::FromImage($bannerBitmap)
    try {
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $cropWidth = [int]($bannerSource.Width * 0.5)
        $cropHeight = [int]($cropWidth * 48 / 128)
        $cropX = $bannerSource.Width - $cropWidth
        $cropY = [Math]::Max(0, [int](($bannerSource.Height - $cropHeight) / 2))
        $graphics.DrawImage($bannerSource, (New-Object Drawing.Rectangle 0, 0, 128, 48),
            $cropX, $cropY, $cropWidth, $cropHeight, [Drawing.GraphicsUnit]::Pixel)
    } finally { $graphics.Dispose() }
    $bannerPixels = [Text.StringBuilder]::new(18432)
    for ($y = 0; $y -lt 48; $y++) {
        for ($x = 0; $x -lt 128; $x++) {
            $pixel = $bannerBitmap.GetPixel($x, $y)
            [void]$bannerPixels.AppendFormat('{0:x1}{1:x1}{2:x1}',
                [int][Math]::Round($pixel.R / 17.0), [int][Math]::Round($pixel.G / 17.0),
                [int][Math]::Round($pixel.B / 17.0))
        }
    }
    $bannerPatch = $bannerPatch.Replace('__LUMEN_BANNER_PIXELS__', $bannerPixels.ToString())
} finally { $bannerBitmap.Dispose(); $bannerSource.Dispose() }
$patientPatch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'PatientsSite.as')
$agendaPatch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'AgendaSite.as')
$siteSource = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'HospitalSite.as')
Write-FrameScript $homeScripts ($original + [Environment]::NewLine + $homePatch + [Environment]::NewLine + $logoPatch + [Environment]::NewLine + $headerPatch + [Environment]::NewLine + $bannerPatch + [Environment]::NewLine + $dashboardPatch + [Environment]::NewLine + $patientPatch + [Environment]::NewLine + $agendaPatch)
Write-FrameScript $siteScripts $siteSource

# Uncompressed GFx v8, 1280 by 720, 30 fps, one frame with an empty DoAction tag.
$emptyMovie = [byte[]](
    0x47, 0x46, 0x58, 0x08, 28, 0, 0, 0,
    0x80, 0x00, 0x03, 0x20, 0x00, 0x00, 0x01, 0xC2, 0x00,
    0x00, 0x1E, 0x01, 0x00,
    0x01, 0x03, 0x00, 0x40, 0x00, 0x00, 0x00
)
$template = Join-Path $work 'blank.gfx'
[IO.File]::WriteAllBytes($template, $emptyMovie)

$patchedHome = Join-Path $package 'www_eyefind_info.gfx'
$site = Join-Path $package 'www_lumensante_com.gfx'
Invoke-Ffdec @('-importScript', $eyefind, $patchedHome, $homeScripts)
Invoke-Ffdec @('-importScript', $template, $site, $siteScripts)

foreach ($output in @(
    @{ Path = $patchedHome; Name = 'home'; Token = 'lumenHospitalShortcut' },
    @{ Path = $site; Name = 'site'; Token = 'hospitalPage' }
)) {
    $verify = Join-Path $work ('verify-' + $output.Name)
    Invoke-Ffdec @('-export', 'script', $verify, $output.Path)
    $generated = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $verify 'scripts/frame_1/DoAction.as')
    if (!$generated.Contains($output.Token)) { throw "O script não foi incorporado em $($output.Path)." }
}

Get-Item -LiteralPath $patchedHome, $site
