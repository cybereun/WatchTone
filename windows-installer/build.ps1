param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\release'))
$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output=Join-Path $OutputDirectory 'WatchTone-Windows-Installer-1.0.4.exe'
$sources=Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File | Select-Object -ExpandProperty FullName
$references='System.dll','System.Core.dll','System.Windows.Forms.dll','System.Drawing.dll','System.IO.Compression.dll','System.Net.Http.dll' | ForEach-Object {"/reference:$(Join-Path $framework $_)"}
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu /optimize+ "/win32icon:$PSScriptRoot\watchtone.ico" "/out:$output" @references @sources
if($LASTEXITCODE){throw 'Windows installer build failed'}
Get-Item -LiteralPath $output | Select-Object FullName,Length
