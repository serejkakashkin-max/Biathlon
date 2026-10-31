@echo off
cd /d "%~dp0"
start "Biathlon M16.2" "%~dp0Biathlon.exe" -force-d3d11 -screen-fullscreen 1 -window-mode borderless
