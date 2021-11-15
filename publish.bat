@echo off
IF "%~1" NEQ "" set EXE_VERSION=-%1
set SOLUTION_PATH=%~p0
set PROJECT_PATH=%SOLUTION_PATH%\DnsTube
set PUBLISH_PATH=%PROJECT_PATH%\bin\Publish
set ZIP_EXE_PATH="C:\Program Files\7-Zip\7z.exe"

if exist %PUBLISH_PATH%\NUL del /f/s/q %PUBLISH_PATH% > nul & rmdir /s/q %PUBLISH_PATH%

cd %PROJECT_PATH%

:::: portable versions
::
set PORTABLE=PORTABLE

:: portable 
echo publishing portable version...
dotnet publish --nologo -r win-x64 -c Release --self-contained false -o %PUBLISH_PATH%\TEMP
ren %PUBLISH_PATH%\TEMP\DnsTube.exe DnsTube-Portable%EXE_VERSION%.exe
ren %PUBLISH_PATH%\TEMP\DnsTube.pdb DnsTube-Portable%EXE_VERSION%.pdb

cd %PUBLISH_PATH%\TEMP
%ZIP_EXE_PATH% a -t7z -r -mx9 ..\DnsTube-Portable%EXE_VERSION%.7z *.*
cd %PROJECT_PATH%
del /f/s/q %PUBLISH_PATH%\TEMP > nul & rmdir /s/q %PUBLISH_PATH%\TEMP

:: portable self-contained
echo publishing portable self-contained version...
dotnet publish --nologo -r win-x64 -c Release --self-contained true -p:PublishSingleFile=false -o %PUBLISH_PATH%\TEMP
ren %PUBLISH_PATH%\TEMP\DnsTube.exe DnsTube-Portable-SelfContained%EXE_VERSION%.exe
ren %PUBLISH_PATH%\TEMP\DnsTube.pdb DnsTube-Portable-SelfContained%EXE_VERSION%.pdb

cd %PUBLISH_PATH%\TEMP
%ZIP_EXE_PATH% a -t7z -r -mx9 ..\DnsTube-Portable-SelfContained%EXE_VERSION%.7z *.*
cd %PROJECT_PATH%
del /f/s/q %PUBLISH_PATH%\TEMP > nul & rmdir /s/q %PUBLISH_PATH%\TEMP

:::: non-portable versions
::
set PORTABLE=

::regular
echo publishing regular version...
dotnet publish --nologo -r win-x64 -c Release --self-contained false -o %PUBLISH_PATH%\TEMP
ren %PUBLISH_PATH%\TEMP\DnsTube.exe DnsTube%EXE_VERSION%.exe
ren %PUBLISH_PATH%\TEMP\DnsTube.pdb DnsTube%EXE_VERSION%.pdb

cd %PUBLISH_PATH%\TEMP
%ZIP_EXE_PATH% a -t7z -r -mx9 ..\DnsTube%EXE_VERSION%.7z *.*
cd %PROJECT_PATH%
del /f/s/q %PUBLISH_PATH%\TEMP > nul & rmdir /s/q %PUBLISH_PATH%\TEMP

:: regular self-contained
echo publishing regular self-contained version...
dotnet publish --nologo -r win-x64 -c Release --self-contained true -p:PublishSingleFile=false -o %PUBLISH_PATH%\TEMP
ren %PUBLISH_PATH%\TEMP\DnsTube.exe DnsTube-SelfContained%EXE_VERSION%.exe
ren %PUBLISH_PATH%\TEMP\DnsTube.pdb DnsTube-SelfContained%EXE_VERSION%.pdb

cd %PUBLISH_PATH%\TEMP
%ZIP_EXE_PATH% a -t7z -r -mx9 ..\DnsTube-SelfContained%EXE_VERSION%.7z *.*
cd %PROJECT_PATH%
del /f/s/q %PUBLISH_PATH%\TEMP > nul & rmdir /s/q %PUBLISH_PATH%\TEMP

cd %SOLUTION_PATH%
