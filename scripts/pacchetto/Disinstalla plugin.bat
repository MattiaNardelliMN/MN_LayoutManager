@echo off
REM Doppio click su questo file per rimuovere il plugin "Gestione Layout".
REM Ricorda: AutoCAD deve essere CHIUSO.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Installa.ps1" -Uninstall
