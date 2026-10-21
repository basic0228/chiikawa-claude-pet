$ErrorActionPreference = 'Stop'
$cscPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $cscPath)) { $cscPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
& $cscPath /nologo /target:winexe /optimize+ ("/out:" + (Join-Path $PSScriptRoot 'ChiikawaDesktopPet.exe')) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll ("/resource:" + (Join-Path $PSScriptRoot 'characters.png') + ",characters.png") (Join-Path $PSScriptRoot 'ChiikawaDesktopPet.cs') (Join-Path $PSScriptRoot 'Questions.cs') (Join-Path $PSScriptRoot 'QuestionTheme.cs') (Join-Path $PSScriptRoot 'NavigationAndCharacterTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
