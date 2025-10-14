

<h2 align="center">DnsTube</h2>

<h4 align="center"><b>Access your computer from anywhere</b></h4>

<p align="center">
	<a href="https://github.com/drittich/DnsTube">
		<img src="https://img.shields.io/github/downloads/drittich/DnsTube/total" height="20"/>
		<img src="https://img.shields.io/github/downloads/drittich/dnstube/v3.0.0/total" height="20"/>
		<img src="https://img.shields.io/github/issues-raw/drittich/dnstube" height="20"/>
		<img src="https://img.shields.io/github/stars/drittich/dnstube?style=social" height="20"/>
	</a>
</p>

A Dynamic DNS (DDNS) Windows service for Cloudflare-managed domains.

---

## Table of Contents

- [Table of Contents](#table-of-contents)
- [What is this?](#what-is-this)
- [Quick Start](#quick-start)
- [Features](#features)
- [Requirements](#requirements)
- [UI](#ui)
- [Downloading](#downloading)
- [Installing](#installing)
- [Configuration](#configuration)
	- [Getting a Cloudflare API token](#getting-a-cloudflare-api-token)
- [Updating](#updating)
- [Uninstalling](#uninstalling)
- [Notes](#notes)
- [Development](#development)
- [The Name](#the-name)
- [Contributing](#contributing)
- [Authors](#authors)
- [License](#license)

---

## What is this?

DnsTube is a Windows service that helps you access your computer remotely even if its IP address changes. It does this by using Cloudflare’s free DNS hosting and API to update the DNS entries for your domain name. This way you can always access your computer using its domain name instead of having to remember its IP address.

## Quick Start

1. [Download the latest release](https://github.com/drittich/DnsTube/releases).
2. Install [.NET 8](https://dotnet.microsoft.com/en-us/download) if needed.
3. Extract the package, open a command prompt as Administrator, and run `install-service.bat`.
4. Open [http://localhost:5666](http://localhost:5666) to complete setup.

---

## Features

* Runs as a Windows service (no Windows login required)
* Can update A (IPv4), AAAA (IPv6), SPF, and TXT records
* Detects network changes and updates DNS automatically
* Supports split VPN tunneling and lets you choose which network adapter to use
* **Per-entry adapter selection** - Choose a specific network adapter for each DNS entry individually
* Supports both Cloudflare API keys and tokens (can be scoped to specific zones)

## Requirements

* You must set Cloudflare as the DNS authority for your domain
* You must have Administrator permissions for the computer you are installing DnsTube on
* You must have .NET 8 installed

## UI

<img src="https://github.com/drittich/DnsTube/assets/1222810/751b4b21-333f-4a5a-bdba-b937474fc2ba" width="800">

## Downloading 

Head over to the [Releases](https://github.com/drittich/DnsTube/releases) page to download the latest binary.

## Installing

Note: This application is built using .NET 8. You may need to download and install it from here: https://dotnet.microsoft.com/en-us/download. Once you've installed .NET 8:

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

Once the service has started you can view the UI at the URL http://localhost:5666/. See the **Configuration** section for additional config, or if you need to host the UI on a different port.

## Configuration

>If you haven't already done so, you will need to create an account with Cloudflare and make it the DNS authority for your domain. You then need to configure your DNS entries as appropriate. See [Cloudflare - Manage DNS records](https://developers.cloudflare.com/dns/manage-dns-records/how-to/create-dns-records/) for more info.

By default the service hosts the web application on your local machine at port 5666. If you wish to change this, edit `appsettings.json` accordingly. Once the service is running you can launch the interface at http://localhost:5666/ (or whatever port you have chosen).

After that, you'll need to generate an API Token (preferred) or Key in order to access the API via DnsTube. The details for doing that can be found at [Cloudflare - Create API token](https://developers.cloudflare.com/fundamentals/api/get-started/create-token/). See the **Getting a Cloudflare API token** section below for a quick walk-through.

Then, go to the DnsTube settings page and enter your email address and API key or token. Return to the main tab (refresh if necessary); you should now see a table listing your Cloudflare DNS entries. Check off the ones you want to dynamically update and the service should take it from there.

**Network Adapter Selection**: For each DNS entry you enable, you can choose which network adapter to use for determining the IP address. This is particularly useful if you:
- Have multiple network connections (e.g., Ethernet and Wi-Fi)
- Use a VPN and want certain domains to use your VPN IP while others use your direct connection
- Want different DNS entries to reflect different network interfaces

To select an adapter for a specific DNS entry:
1. On the main page, locate the DNS entry you want to configure
2. Click the adapter dropdown next to the entry
3. Select the desired network adapter from the list
4. The service will use the IP address from that adapter when updating this DNS entry

If no specific adapter is selected, the service will use the default adapter (typically the one with internet connectivity).

You can use [nslookup](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/nslookup) to make sure DNS resolution is working correctly, e.g.,
```
nslookup mydomain.com
```

### Getting a Cloudflare API token

1. Log in to your Cloudflare dashboard and navigate to **My Profile**.
2. Choose **API Tokens** and click **Create Token**.
3. Start with the **Edit Zone DNS** template. By default this grants the `Zone:DNS:Edit` permission. 
4. Change the token name from `Edit zone DNS` to `DnsTube`
5. Click **Add More** under Permissions and add a permission for `Zone:Zone:Read`.
6. Under **Zone Resources** select which zones you want DnsTube to manage.
7. If you want the token to be limited to specific zones, add them under **Zone Resources**.
8. Click **Continue to summary**, then **Create Token**.
9. Copy the API token for entering within DnsTube, as you will not be able to see it again once you navigate away.

When using a zone‑specific token, add the corresponding Zone IDs (comma-separated) on the DnsTube settings page. Tokens with access to all zones do not require this field.

## Updating

- Download the latest release from https://github.com/drittich/DnsTube/releases/latest and decompress
- Open a command prompt as Administrator and stop the existing service by running `stop-service.bat`. Note, the service will stop more quickly if you close the web UI.
- Copy the new decompressed files over the existing ones. The configuration is stored elsewhere so will be preserved.
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
2. Configuration is stored in a separate folder from the application so when you update, your configuration is preserved. (An exception to this is if you have changed the port the application is hosted on in `appsettings.json`.)
3. The location of the configuration file can be found at the bottom ot the [Settings](http://localhost:5666/settings) page.

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

## Contributors

<a href="https://github.com/drittich/DnsTube/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=drittich/DnsTube" alt="DnsTube project contributors" />
</a>

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=drittich/DnsTube&type=Timeline)](https://www.star-history.com/#drittich/DnsTube&Timeline)

## License

This project is licensed under the MIT License - see the [LICENSE](/LICENSE) file for details.
