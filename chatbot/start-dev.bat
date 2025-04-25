@echo off
setlocal

REM ========================================
REM Start Ollama Serve in background
REM ========================================
echo Starting Ollama server...
start "Ollama" cmd /k "ollama serve"

REM Wait a few seconds to make sure Ollama boots
timeout /t 3 >nul

REM ========================================
REM Preload model into memory
REM ========================================
echo Preloading model into memory...
curl -s http://localhost:11434/api/generate -d "{\"model\": \"llama3:8b-q4_K_M\", \"prompt\": \"Hello\", \"stream\": false}" >nul

REM ========================================
REM Start ChatBot.Server
REM ========================================
echo Starting ChatBot.Server...
start "ChatBot.Server" cmd /k "cd ChatBot.Server && dotnet run"

REM ========================================
REM Start ChatBot.Client
REM ========================================
echo Starting ChatBot.Client...
start "ChatBot.Client" cmd /k "cd ChatBot.Client && dotnet run"

REM ========================================
REM Wait for shutdown
REM ========================================
echo.
echo ======================================
echo   All services started!
echo   Press any key to stop everything...
echo ======================================
pause >nul

taskkill /FI "WINDOWTITLE eq Ollama" /T /F
taskkill /FI "WINDOWTITLE eq ChatBot.Server" /T /F
taskkill /FI "WINDOWTITLE eq ChatBot.Client" /T /F

echo All services stopped.
endlocal
