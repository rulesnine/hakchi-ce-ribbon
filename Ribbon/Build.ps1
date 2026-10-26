param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4 no está instalado.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
& $compiler /nologo /target:winexe /optimize+ "/out:$OutputDirectory\hakchi-ribbon.exe" "/win32icon:$PSScriptRoot\..\icon_app.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "$PSScriptRoot\HakchiRibbon.cs" "$PSScriptRoot\Launcher.cs" "$PSScriptRoot\CoverArt.cs"
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar Hakchi Ribbon.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\app.config') -Destination (Join-Path $OutputDirectory 'hakchi-ribbon.exe.config')
Write-Output "Actualización creada en $OutputDirectory"

