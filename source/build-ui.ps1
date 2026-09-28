$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$outDir=Split-Path $PSScriptRoot
$refs=@('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Xml.dll','System.Xml.Linq.dll','System.Xaml.dll',"$framework\WPF\WindowsBase.dll","$framework\WPF\PresentationCore.dll","$framework\WPF\PresentationFramework.dll")
$argsList=@('/nologo','/target:winexe','/platform:x64','/optimize+',"/win32icon:$PSScriptRoot\app.ico","/win32manifest:$PSScriptRoot\app.manifest","/out:$outDir\iPhone直连定位.exe")
$argsList+=$refs|ForEach-Object{"/r:$_"}
$argsList+="$PSScriptRoot\App.cs"
$argsList+="$PSScriptRoot\Controller.cs"
$argsList+="$PSScriptRoot\Locations.cs"
$argsList+="$PSScriptRoot\LocationControls.cs"
$argsList+="$PSScriptRoot\LocationTests.cs"
$argsList+="$PSScriptRoot\UiState.cs"
$argsList+="$PSScriptRoot\StatusToast.cs"
$argsList+="$PSScriptRoot\UiStateTests.cs"
& "$framework\csc.exe" @argsList
if($LASTEXITCODE -ne 0){throw 'UI build failed'}
& "$framework\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$PSScriptRoot\app.ico" "/win32manifest:$PSScriptRoot\app.manifest" "/out:$outDir\一键修改.exe" /r:System.dll /r:System.Windows.Forms.dll "$PSScriptRoot\QuickApply.cs"
if($LASTEXITCODE -ne 0){throw 'Quick launch build failed'}
