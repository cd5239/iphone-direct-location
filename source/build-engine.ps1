param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$BuildDirectory)
$ErrorActionPreference='Stop'
$env:PYINSTALLER_CONFIG_DIR=Join-Path $BuildDirectory 'cache'
& $Python -m PyInstaller --noconfirm --onedir --console --name LocationEngine --distpath (Join-Path $BuildDirectory 'dist') --workpath (Join-Path $BuildDirectory 'build') --specpath $BuildDirectory --paths $PSScriptRoot --collect-data pymobiledevice3 --collect-all pytun_pmd3 --collect-submodules pytcp --collect-submodules pymobiledevice3.dtx --copy-metadata pymobiledevice3 --copy-metadata pmd-pytcp --exclude-module IPython --exclude-module tkinter --exclude-module av --exclude-module matplotlib (Join-Path $PSScriptRoot 'engine.py')
if($LASTEXITCODE -ne 0){throw 'Engine build failed'}
Write-Output (Join-Path $BuildDirectory 'dist\LocationEngine')
