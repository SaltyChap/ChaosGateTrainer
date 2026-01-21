@echo off
echo Building Chaos Gate Trainer...
echo.

dotnet publish -c Release -r win-x64 --self-contained true -o publish

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo.
    echo Executable located at: publish\ChaosGateTrainer.exe
    echo.
    pause
) else (
    echo.
    echo Build failed! Make sure you have .NET 8.0 SDK installed.
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
)
