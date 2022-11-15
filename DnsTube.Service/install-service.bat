@echo off
set SERVICE_NAME="DnsTube Service"

rem abort if we're not running in an elevated command prompt
net.exe session 1>NUL 2>NUL || goto :not_admin

echo Creating service...
sc create %SERVICE_NAME% binPath="%~dp0DnsTube.Service.exe" start=auto 
sc description %SERVICE_NAME% "Updates Cloudflare DNS entries with the public IP address of this computer"
echo Starting service...
sc start %SERVICE_NAME%
goto :eof

:not_admin
echo ERROR: Please run as a local administrator to install %SERVICE_NAME%
exit /b 1

