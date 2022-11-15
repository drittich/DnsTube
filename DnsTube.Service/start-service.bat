@echo off
set SERVICE_NAME="DnsTube Service"

echo Starting service...
sc start %SERVICE_NAME%
