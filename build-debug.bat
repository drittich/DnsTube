set PORTABLE=
set EXEPATH=%~p0
cd %EXEPATH%\DnsTube
dotnet publish -r win-x64 -c Debug --self-contained true -o bin\Debug\Standalone
cd %EXEPATH%
pause