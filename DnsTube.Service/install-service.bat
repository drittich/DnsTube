@echo off
set SERVICE_NAME="DnsTube Service"

rem abort if we're not running in an elevated command prompt
net.exe session 1>NUL 2>NUL || goto :not_admin

rem check that the ASP.NET Core 10 Runtime is installed (it includes the .NET 10 Runtime)
dotnet --list-runtimes | findstr /B /C:"Microsoft.AspNetCore.App 10."
if %ErrorLevel% equ 0 (
	echo Found ASP.NET Core 10 Runtime
	goto :create_service
)

rem check that a .NET 10 SDK is installed (it includes the ASP.NET Core 10 Runtime)
dotnet --list-sdks | findstr /B /C:"10."
if %ErrorLevel% equ 0 (
	echo Found .NET 10 SDK
	goto :create_service
)

echo ASP.NET Core 10 Runtime not installed, please download and install it from https://dotnet.microsoft.com/en-us/download/dotnet/10.0 or using Microsoft's App-Installer (aka. WinGet-CLI, see https://apps.microsoft.com/detail/9nblggh4nns1): "winget install Microsoft.DotNet.AspNetCore.10"
echo Exiting
exit /b 1

:create_service
echo Creating service...
sc create %SERVICE_NAME% binPath= "%~dp0DnsTube.Service.exe" start= auto
sc description %SERVICE_NAME% "Updates Cloudflare DNS entries with the public IP address of this computer"
echo Starting service...
sc start %SERVICE_NAME%
goto :eof

:not_admin
echo ERROR: Please run as a local administrator to install %SERVICE_NAME%
exit /b 1

