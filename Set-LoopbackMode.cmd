@echo off
rem Launcher for Set-LoopbackMode.ps1 - self-elevates (the setting requires admin).
rem Usage:  Set-LoopbackMode.cmd [enable|disable]   (no arg => interactive prompt)

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%*' -Verb RunAs"
    exit /b
)

set "PS=pwsh"
where pwsh >nul 2>&1 || set "PS=powershell"
"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Set-LoopbackMode.ps1" %*

echo.
pause
