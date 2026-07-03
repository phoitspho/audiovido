@echo off
REM ─── AUDIOVIDO: commit + push current progress ──────────────────
REM Double-click after a work session to back up + share with Pedram.
cd /d "%~dp0"
git add -A
for /f "tokens=1-3 delims=/ " %%a in ("%date%") do set TODAY=%%a-%%b-%%c
git commit -m "Progress update %TODAY% %time%"
git push
echo.
echo ============ Pushed to GitHub ============
pause
