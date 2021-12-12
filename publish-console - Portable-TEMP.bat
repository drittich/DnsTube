@echo off
IF "%~1" NEQ "" set EXE_VERSION=-%1
set SOLUTION_PATH=%~p0
set PROJECT_NAME=DnsTube.Console
set PROJECT_PATH=%SOLUTION_PATH%\%PROJECT_NAME%
set PUBLISH_PATH=c:\temp\console
set ZIP_EXE_PATH="C:\Program Files\7-Zip\7z.exe"

if exist %PUBLISH_PATH%\NUL del /f/s/q %PUBLISH_PATH% > nul & rmdir /s/q %PUBLISH_PATH%

cd %PROJECT_PATH%

:::: portable versions
::
set PORTABLE=PORTABLE

:: portable 
echo publishing portable version...
dotnet publish --nologo -maxCpuCount -property:PORTABLE=PORTABLE -r win-x64 -c Release --self-contained false -p:PublishSingleFile=true -o %PUBLISH_PATH%

pause