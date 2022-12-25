@echo off
set SERVICE_NAME="DnsTube Service"

rem abort if we're not running in an elevated command prompt
net.exe session 1>NUL 2>NUL || goto :not_admin

rem check that .NET 7 SDK is installed
dotnet --list-sdks | findstr /C:8. /B
if %ErrorLevel% equ 0 (
    echo Found .NET 7 SDK
) else (
    echo .NET 7 SDK not installed, please download and install from https://dotnet.microsoft.com/en-us/download/dotnet/7.0
	echo Exiting
	goto :eof
)

echo Creating service...
sc create %SERVICE_NAME% binPath="%~dp0DnsTube.Service.exe" start=auto 
sc description %SERVICE_NAME% "Updates Cloudflare DNS entries with the public IP address of this computer"
echo Starting service...
sc start %SERVICE_NAME%
goto :eof

:not_admin
echo ERROR: Please run as a local administrator to install %SERVICE_NAME%
exit /b 1

