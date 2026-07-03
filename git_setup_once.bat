@echo off
REM ─── AUDIOVIDO: one-time repo setup ─────────────────────────────
REM Double-click this file ONCE. If a browser window opens asking you
REM to sign in to GitHub, approve it — that's git asking for permission
REM to push with YOUR account.
cd /d "%~dp0"

REM Remove any broken half-initialized repo
if exist ".git" rmdir /s /q ".git"

git init -b main
git config user.name "PHO"
git config user.email "foad.es91@gmail.com"
git add -A
git commit -m "AUDIOVIDO city prototype: 5 playable districts, NXT economy, scene builders"
git remote add origin https://github.com/phoitspho/audiovido.git
git push -u origin main

echo.
echo ================================================
echo   Done! Check https://github.com/phoitspho/audiovido
echo ================================================
pause
