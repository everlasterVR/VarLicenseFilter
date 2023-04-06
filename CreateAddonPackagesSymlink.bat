@echo off
@cd /d "%~dp0"

if exist "AddonPackages" (
    echo AddonPackages already exists in this folder.
    goto :ExitPrompt
)

@setlocal enableextensions
setlocal EnableExtensions DisableDelayedExpansion
echo This batch file will create a Symbolic Link from this folder to VaM\AddonPackages.
echo/
echo Creating this symbolic link is necessary for VarLicenseFilter to be able to disable
echo and enable packages by adding or removing .disabled files inside the AddonPackage
echo folder.
echo/
echo WARNING: It should be noted that, in theory, creating this symbolic link may also
echo allow a malicious plugin author to delete any or all files in AddonPackages.
echo This risk can be minimized by not downloading plugins from unreliable sources.
echo/

if exist "%SystemRoot%\System32\choice.exe" goto UseChoice

setlocal EnableExtensions EnableDelayedExpansion

:UseChoice
%SystemRoot%\System32\choice.exe /C YN /N /M "Are you sure [Y/N]?"
if not errorlevel 1 goto UseChoice
if errorlevel 2 goto :EOF

:Continue
mklink /D "AddonPackages" "..\..\AddonPackages"
endlocal
goto :ExitPrompt

:ExitPrompt
echo/
echo Press any key to exit . . .
pause>nul
goto :EOF

