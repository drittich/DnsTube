@echo off
set SERVICE_NAME="DnsTube Service"

echo Stopping service...
sc stop %SERVICE_NAME%
