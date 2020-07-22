set PORTABLE=PORTABLE
set EXEPATH=%~p0
cd %EXEPATH%\DnsTube
dotnet publish -r win-x64 -c Debug --self-contained true -o bin\Debug\Portable
cd %EXEPATH%
pause