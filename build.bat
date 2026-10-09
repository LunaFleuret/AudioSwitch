@echo off
echo ========================================
echo Building AudioSwitch...
echo ========================================

dotnet publish AudioSwitch.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Build succeeded!
    echo Output: publish\AudioSwitch.exe
    echo ========================================
) else (
    echo.
    echo ========================================
    echo Build failed!
    echo ========================================
)
