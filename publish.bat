@echo off
IF "%~1" NEQ "" set EXE_VERSION=-%1
set SOLUTION_PATH=%~p0
set PROJECT_PATH=%SOLUTION_PATH%\DnsTube
set PUBLISH_PATH=%PROJECT_PATH%\bin\Publish

cd %PROJECT_PATH%

set PORTABLE=PORTABLE
dotnet publish --nologo -r win-x64 -c Debug --self-contained false -o %PUBLISH_PATH%
ren %PUBLISH_PATH%\DnsTube.exe DnsTube-Portable-Debug%EXE_VERSION%.exe
ren %PUBLISH_PATH%\DnsTube.pdb DnsTube-Portable-Debug%EXE_VERSION%.pdb
dotnet publish --nologo -r win-x64 -c Release --self-contained false -o %PUBLISH_PATH%
ren %PUBLISH_PATH%\DnsTube.exe DnsTube-Portable%EXE_VERSION%.exe
ren %PUBLISH_PATH%\DnsTube.pdb DnsTube-Portable%EXE_VERSION%.pdb

set PORTABLE=
dotnet publish --nologo -r win-x64 -c Debug --self-contained false -o %PUBLISH_PATH%
ren %PUBLISH_PATH%\DnsTube.exe DnsTube-Debug%EXE_VERSION%.exe
ren %PUBLISH_PATH%\DnsTube.pdb DnsTube-Debug%EXE_VERSION%.pdb
dotnet publish --nologo -r win-x64 -c Release --self-contained false -o %PUBLISH_PATH%
ren %PUBLISH_PATH%\DnsTube.exe DnsTube%EXE_VERSION%.exe
ren %PUBLISH_PATH%\DnsTube.pdb DnsTube%EXE_VERSION%.pdb

cd %SOLUTION_PATH%
