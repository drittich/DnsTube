set PORTABLE=PORTABLE
set EXEPATH=%~p0
cd %EXEPATH%\DnsTube
dotnet publish -r win-x64 -c Release --self-contained true -o bin\Release\Portable
cd %EXEPATH%
pause