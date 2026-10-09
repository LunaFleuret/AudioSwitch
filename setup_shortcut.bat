@echo off
setlocal

set "EXE_PATH=publish\AudioSwitch.exe"

if not exist "%EXE_PATH%" (
    set "EXE_PATH=bin\Release\net8.0-windows\publish\AudioSwitch.exe"
)

if not exist "%EXE_PATH%" (
    echo Error: AudioSwitch.exe not found. Please run build.bat first.
    pause
    exit /b 1
)

echo Creating shortcut with AppUserModelID...
"%EXE_PATH%" --shortcut

echo.
echo Shortcut created successfully!
echo You can now right click AudioSwitch.lnk and select "Pin to taskbar".
pause
