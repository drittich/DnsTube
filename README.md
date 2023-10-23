

<h2 align="center">DnsTube</h2>

<h4 align="center"><b>Access your computer from anywhere</b></h4>

<p align="center">
	<a href="https://github.com/drittich/DnsTube">
		<img src="https://img.shields.io/github/downloads/drittich/DnsTube/total" height="20"/>
		<img src="https://img.shields.io/github/downloads/drittich/dnstube/v2.3.0/total" height="20"/>
		<img src="https://img.shields.io/github/downloads/drittich/dnstube/v2.4.0-beta/total" height="20"/>
		<img src="https://img.shields.io/github/issues-raw/drittich/dnstube" height="20"/>
		<img src="https://img.shields.io/github/stars/drittich/dnstube?style=social" height="20"/>
	</a>
</p>

A Dynamic DNS (DDNS) Windows service for Cloudflare.

## What is this?

DnsTube is a Windows service that helps you access your computer remotely even if its IP address changes. It does this by using Cloudflare’s free DNS hosting and API to update the DNS entries for your domain name. This way, you can always access your computer using its domain name instead of having to remember its IP address.

## Features

* Runs as a Windows service (no Windows login required)
* Can update A (IPv4), AAAA (IPv6), SPF, and TXT records
* Detects network changes and updates DNS automatically
* [Release DnsTube v2.4.0-beta2](https://github.com/drittich/DnsTube/releases/tag/v2.4.0-beta2) supports split VPN tunnelling and lets you choose which network adapter to use
* Supports both Cloudflare API keys and tokens (can be scoped to specific zones)

## Requirements

* You must set Cloudflare as the DNS authority for your domain
* You must have Administrator permissions for the computer you are installing DnsTube on
* You must have .NET 7 installed

## UI

<img src="https://user-images.githubusercontent.com/1222810/208830234-1f54db2c-9090-44cc-8743-b3f3bbc84e2b.png" width="800">

## Settings

<img src="https://user-images.githubusercontent.com/1222810/208830426-ac008974-1c28-47ab-94b4-acf6bbc433e3.png" width="800">

## Downloading 

Head over to the [Releases](https://github.com/drittich/DnsTube/releases/latest) page to download the latest binary.

## Installing

Note: This application is built using .NET 7. You may need to download and install it from here: https://dotnet.microsoft.com/en-us/download. Once you've installed .NET 7:

- Extract the DnsTube package to a folder of your choice
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

Once the service has started you can view the UI at the URL http://localhost:5666/index.html. See the **Configuration** section for additional config, or if you need to host the UI on a different port.

## Configuration

>If you haven't already done so, you will need to create an account with Cloudflare and make it the DNS authority for your domain. You then need to configure your DNS entries as appropriate. See [Managing DNS records in Cloudflare](https://support.cloudflare.com/hc/en-us/articles/360019093151-Managing-DNS-records-in-Cloudflare) for more info.

By default the service hosts the web application on your local machine at port 5666. If you wish to change this, edit `appsettings.json` accordingly. Once the service is running you can launch the interface at http://localhost:5666/ (or whatever port you have chosen).

After that, you'll need to generate an API Token (preferred) or Key in order to access the API via DnsTube. The details for doing that can be found at [Creating API tokens](https://developers.cloudflare.com/api/tokens/create).

Then, go the DsnTube settings page and enter your email address, API key/token, etc. Go back to the main tab (refresh if necessary) and you should see a table listing your Cloudflare DNS entries. Check off the ones you want to dynamically update and the service should take it from there.

You can use [nslookup](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/nslookup) to make sure DNS resolution is working correctly, e.g., 
```
nslookup mydomain.com
```

## Updating

- Download the latest release from https://github.com/drittich/DnsTube/releases/latest and uncompress
- Open a command prompt as Administrator and stop the existing service by running `stop-service.bat`. Note, the service will stop more quickly if you close the web UI.
- Copy the new uncompressed files over the existing ones. The configuration is stored elsewhere so will be preserved.
- Start the service again by running `start-service.bat` 

## Uninstalling
Open a command prompt as Administrator and uninstall the service using  `uninstall-service.bat`. Note, the service will stop more quickly if you close the web UI. You should see the following output:

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

## Notes

1. DnsTube only updates existing Cloudflare records. It will not create or remove records.
2. Configuration is stored in a separate folder than the application, so when you updated, your configuration is preserved. (An exception to this is if you have chnged the port the application is host with in `appsettings.json`.)
3. The location of your configuration file is shown in UI on the Settings page.

## Development

The front-end application must be built before starting the .NET application. To do this, open a command prompt and go to `[YOUR_INSTALL_FOLDER]\DnsTube.Service\ClientApp`. Then run
```
npm install
npm run build
```

This will build your files and copy them to the `[YOUR_INSTALL_FOLDER]\DnsTube.Service\wwwroot` folder.

At this point you can load the solution in Visual Studio 2022 and run the application with `Ctrl - F5`. You should then be able to load the application UI at the URL http://localhost:5666.


## The Name

We all know the internet is a [series of tubes](https://en.wikipedia.org/wiki/Series_of_tubes). This application uses those very same tubes to update your DNS.

## Contributing

Contributions are welcome!

## Authors

* **D'Arcy Rittich**

## License

This project is licensed under the MIT License - see the [LICENSE](/LICENSE) file for details.
