param([string]$Destination = 'C:\NES hack\hakchi2-ce-3.9.3-portable')
$ErrorActionPreference = 'Stop'
try {
    $target = [IO.Path]::GetFullPath($Destination)
    $original = Join-Path $target 'hakchi.exe'
    if (-not (Test-Path -LiteralPath $original -PathType Leaf)) { throw "No se encontró hakchi.exe en $target. Extrae los dos archivos hakchi-ribbon.exe y .config manualmente junto a tu hakchi.exe." }
    $version = [Reflection.AssemblyName]::GetAssemblyName($original).Version
    if ($version.Major -ne 3 -or $version.Minor -ne 9 -or $version.Build -ne 3) { throw 'Esta actualización requiere Hakchi CE 3.9.3.' }
    $before = (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'hakchi-ribbon.exe') -Destination (Join-Path $target 'hakchi-ribbon.exe') -Force
    $config = Join-Path $target 'hakchi.exe.config'
    if (-not (Test-Path -LiteralPath $config)) { $config = Join-Path $PSScriptRoot 'hakchi-ribbon.exe.config' }
    Copy-Item -LiteralPath $config -Destination (Join-Path $target 'hakchi-ribbon.exe.config') -Force
    if ((Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash -ne $before) { throw 'El ejecutable original cambió durante la copia.' }
    Write-Host ''
    Write-Host 'Hakchi Ribbon instalado correctamente.' -ForegroundColor Green
    Write-Host 'Cierra todas las ventanas actuales de Hakchi, incluidos los diálogos de módulos.'
    Write-Host "Después abre: $target\hakchi-ribbon.exe"
    Write-Host 'El ejecutable original, tus juegos y ajustes se conservan.'
} catch { Write-Host $_.Exception.Message -ForegroundColor Red; exit 1 }
