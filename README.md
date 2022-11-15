# DnsTube v2

A Windows Service for dynamically updating Cloudflare DNS.

## Installing

- Extract the package to a folder of your choice
- Open a command prompt as Administrator and install the service using  `install-service.bat`.
You should see the following output:
```
PS C:\Program Files\DnsTubeService> .\install-service.bat
sc create "DnsTube Service" binPath="C:\Program Files\DnsTubeService\DnsTube.Service.exe" start=auto
[SC] CreateService SUCCESS
[SC] ChangeServiceConfig2 SUCCESS

SERVICE_NAME: DnsTube Service
		TYPE               : 10  WIN32_OWN_PROCESS
		STATE              : 2  START_PENDING
								(NOT_STOPPABLE, NOT_PAUSABLE, IGNORES_SHUTDOWN)
		WIN32_EXIT_CODE    : 0  (0x0)
		SERVICE_EXIT_CODE  : 0  (0x0)
		CHECKPOINT         : 0x0
		WAIT_HINT          : 0x7d0
		PID                : 21408
		FLAGS              :
PS C:\Program Files\DnsTubeService>
```

## Configuration

By default the service hosts the web application on your local machine at port 5666. If you wish to change this, edit `appsettings.json` accordingly. Once the service is running you can launch the interface at http://localhost:5666/ (or whatever port you have chosen).

Go the settings page and enter your email address, API key/token, etc. Go back to the main tab (refresh if necessary) and you should see a table listing your Cloudflare DNS entries. Check off the ones you want to dynamically update and the service should take it from there.

## Uninstalling
Open a command prompt as Administrator and uninstall the service using  `uninstall-service.bat`. You should see the following output:
```
PS C:\Program Files\DnsTubeService> .\uninstall-service.bat

SERVICE_NAME: DnsTube Service
		TYPE               : 10  WIN32_OWN_PROCESS
		STATE              : 3  STOP_PENDING
								(STOPPABLE, NOT_PAUSABLE, ACCEPTS_SHUTDOWN)
		WIN32_EXIT_CODE    : 0  (0x0)
		SERVICE_EXIT_CODE  : 0  (0x0)
		CHECKPOINT         : 0x0
		WAIT_HINT          : 0x0
[SC] DeleteService SUCCESS
PS C:\Program Files\DnsTubeService>
```


## Project Structure

- **DnsTube.Core project**: Holds core application code 
- **DnsTube.Service project**: Contains the Windows service, the REST API for the web application, and the web applicaiton itself.

## Development

The front-end application must be built before starting the .NET application. To do this, open a command prompt and go to `[YOUR_INSTALL_FOLDER]\DnsTube.Service\ClientApp`. Then run
```
npm install
npm run build
```

This will build your files and copy them to the `[YOUR_INSTALL_FOLDER]\DnsTube.Service\wwwroot` folder.

At this point you can run the application with `Ctrl - F5` and you should be able to launch the application UI at the URL http://localhost:5666.
