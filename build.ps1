# LoL Asistan.exe dosyasini derler. Windows'ta hazir gelen .NET Framework derleyicisini kullanir, ek kurulum gerekmez.
# Calistirmak icin: powershell -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$fw = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
New-Item -ItemType Directory -Force (Join-Path $root 'out') | Out-Null
& "$fw\csc.exe" /nologo /codepage:65001 /target:winexe /optimize+ `
    "/win32icon:$root\src\icon.ico" "/out:$root\out\LoL Asistan.exe" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll "/r:$fw\System.Web.Extensions.dll" `
    "$root\src\LolAsistan.cs"
if ($LASTEXITCODE -ne 0) { throw "Derleme basarisiz" }
Write-Output "Hazir: $root\out\LoL Asistan.exe"
