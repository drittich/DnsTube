@echo off
IF "%~1" NEQ "" set EXE_VERSION=-%1
set SOLUTION_PATH=%~p0
set PROJECT_PATH=%SOLUTION_PATH%DnsTube.Service
set PUBLISH_PATH=%PROJECT_PATH%\bin\Publish
echo PROJECT_PATH: %PROJECT_PATH%
echo PUBLISH_PATH: %PUBLISH_PATH%

set ZIP_EXE_PATH="C:\Program Files\7-Zip\7z.exe"
if exist %PUBLISH_PATH%\NUL del /f/s/q %PUBLISH_PATH% > nul & rmdir /s/q %PUBLISH_PATH%
cd %PROJECT_PATH%

:: framework-dependent x64
echo publishing framework-dependent x64 version...
dotnet publish --nologo -r win-x64 -c Release --self-contained false -p:PublishSingleFile=true /p:PublishProfile=x64 -o %PUBLISH_PATH%\TEMP
copy ..\README.md %PUBLISH_PATH%\TEMP

cd %PUBLISH_PATH%\TEMP
%ZIP_EXE_PATH% a -t7z -r -mx9 ..\DnsTube%EXE_VERSION%-win-x64.7z *.*
cd %PROJECT_PATH%
del /f/s/q %PUBLISH_PATH%\TEMP > nul & rmdir /s/q %PUBLISH_PATH%\TEMP

:: framework-dependent x86
echo publishing framework-dependent x86 version...
dotnet publish --nologo -r win-x86 -c Release --self-contained false -p:PublishSingleFile=true /p:PublishProfile=x86 -o %PUBLISH_PATH%\TEMP
copy ..\README.md %PUBLISH_PATH%\TEMP

cd %PUBLISH_PATH%\TEMP
%ZIP_EXE_PATH% a -t7z -r -mx9 ..\DnsTube%EXE_VERSION%-win-x86.7z *.*
cd %PROJECT_PATH%
del /f/s/q %PUBLISH_PATH%\TEMP > nul & rmdir /s/q %PUBLISH_PATH%\TEMP

cd %SOLUTION_PATH%
