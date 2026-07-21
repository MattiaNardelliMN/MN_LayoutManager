@echo off
REM Doppio click su questo file per compilare e installare il plugin in AutoCAD 2024.
REM Ricorda: AutoCAD deve essere CHIUSO.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy.ps1"
