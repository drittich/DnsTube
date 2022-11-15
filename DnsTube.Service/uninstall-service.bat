@echo off
set SERVICE_NAME="DnsTube Service"

rem abort if we're not running in an elevated command prompt
net.exe session 1>NUL 2>NUL || goto :not_admin

echo Uninstalling service...
sc stop %SERVICE_NAME%
sc delete %SERVICE_NAME%
goto :eof

:not_admin
echo ERROR: Please run as a local administrator to install %SERVICE_NAME%
exit /b 1

