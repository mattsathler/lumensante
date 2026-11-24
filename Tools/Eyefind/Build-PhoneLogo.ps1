#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OriginalDirectory,
    [Parameter(Mandatory)][string]$FfdecJar
)
$ErrorActionPreference = 'Stop'
$originals = (Resolve-Path -LiteralPath $OriginalDirectory).Path
$ffdec = (Resolve-Path -LiteralPath $FfdecJar).Path
$work = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../lumen-sante/obj/Eyefind/native/phone'))
[void][IO.Directory]::CreateDirectory($work)
Add-Type -AssemblyName System.Drawing
$image = [Drawing.Image]::FromFile((Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../lumen-sante/Assets/logo.png')).Path)
$bitmap = New-Object Drawing.Bitmap 32, 32, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($image, 0, 0, 32, 32)
    } finally { $graphics.Dispose() }
    $pixels = [Text.StringBuilder]::new(4096)
    for ($y = 0; $y -lt 32; $y++) {
        for ($x = 0; $x -lt 32; $x++) {
            $pixel = $bitmap.GetPixel($x, $y)
            [void]$pixels.AppendFormat('{0:x1}{1:x1}{2:x1}{3:x1}',
                [int][Math]::Round($pixel.A / 17.0), [int][Math]::Round($pixel.R / 17.0),
                [int][Math]::Round($pixel.G / 17.0), [int][Math]::Round($pixel.B / 17.0))
        }
    }
} finally { $bitmap.Dispose(); $image.Dispose() }

function Invoke-Ffdec([string[]]$Arguments) {
    $result = & java -jar $ffdec -onerror abort @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($result -join [Environment]::NewLine) }
}
$patch = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'PhoneLogo.as')
foreach ($model in @('ifruit', 'badger', 'facade')) {
    $file = 'cellphone_' + $model + '.gfx'
    $source = (Resolve-Path -LiteralPath (Join-Path $originals $file)).Path
    $export = Join-Path $work ('original-' + $model)
    Invoke-Ffdec @('-export', 'script', $export, $source)
    $original = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $export 'scripts/frame_1/DoAction.as')
    if ($original.Trim() -notmatch ('^var TIMELINE = new com\.rockstargames\.gtav\.cellphone\.' + $model + '\.CELLPHONE_' + $model.ToUpperInvariant() + '\(this\);\s+TIMELINE\.INITIALISE\(this\);$')) {
        throw "Frame inicial inesperado no celular $model; use o filme original da instalação em uso."
    }
    $frame = Join-Path $work ('patch-' + $model + '/frame_1')
    [void][IO.Directory]::CreateDirectory($frame)
    $code = $patch.Replace('__PHONE_HOME_CLASS__', 'HOMEMENU_' + $model.ToUpperInvariant()).Replace('__LUMEN_PHONE_PIXELS__', $pixels.ToString())
    [IO.File]::WriteAllText((Join-Path $frame 'DoAction.as'), $original + [Environment]::NewLine + $code, [Text.UTF8Encoding]::new($false))
    $output = Join-Path $work $file
    Invoke-Ffdec @('-importScript', $source, $output, (Split-Path -Parent $frame))
    $verify = Join-Path $work ('verify-' + $model)
    Invoke-Ffdec @('-export', 'script', $verify, $output)
    $compiled = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $verify 'scripts/frame_1/DoAction.as')
    if (!$compiled.Contains('lumenHospitalLogo')) { throw "Logo não incorporada ao filme $model." }
    Write-Host "Gerado e verificado: $output"
}
