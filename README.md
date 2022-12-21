#### *"Access your home computer from anywhere"*

## DnsTube
A Windows client for dynamically updating Cloudflare DNS entries with your public IP address.

## What is this?

Most of us have home computers with dynamically assigned IP addresses provided by our ISPs. If you want to serve up a web site, be able to access files remotely, or use RDP, etc., from the internet, it becomes challenging to locate your machine when the IP address is constantly changing. This is the problem DNS was designed to solve, so by getting a domain name and updating the DNS entries for it as-needed, you can always access your computer by its domain name, and forget about IP addresses.

This is where DnsTube comes in. Cloudflare provides free DNS hosting for your domain, and also provides an API for updating DNS entries, i.e., the IP address in this case. By running DnsTube on your computer, it will periodically check the public-facing IP address and update the Cloudflare DNS entry as needed.

## Features

* Can update A (IPv4), AAAA (IPv6), SPF, and TXT records.
* Support both Cloudflare API keys and tokens
* Supports API tokens scoped to specific zones
* Runs as a Windows service, so login not required

## Main UI

<img src="https://user-images.githubusercontent.com/1222810/208830234-1f54db2c-9090-44cc-8743-b3f3bbc84e2b.png" width="800">

## Settings UI

<img src="https://user-images.githubusercontent.com/1222810/208830426-ac008974-1c28-47ab-94b4-acf6bbc433e3.png" width="800">

## Downloading 

Head over to the [Releases](https://github.com/drittich/DnsTube/releases/latest) page to download the latest binary.

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

## Configuration & Usage

If you haven't already done so, you will need to create an account with Cloudflare and make it the DNS authority for your domain. You then need to configure your DNS entries as appropriate. See [Managing DNS records in Cloudflare](https://support.cloudflare.com/hc/en-us/articles/360019093151-Managing-DNS-records-in-Cloudflare) for more info.

You can use [nslookup](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/nslookup) to make sure DNS resolution is working correctly, e.g., 
```
nslookup mydomain.com
```

After that, you'll need to generate an API Token (preferred) or Key in order to access the API via DnsTube. The details for doing that can be found at [Creating API tokens](https://developers.cloudflare.com/api/tokens/create).




## UI

**Top Pane**: The UI shows a list of domains for the zone(s) you have provided, and you can check off the ones you want DnsTube to update. 

**Lower Pane**: The lower pane shows a running log of activity. 

## Notes

1. DnsTube only updates existing Cloudflare records. It will not create or remove records.

## Building

This solution can be built using Visual Studio 2022.

## The Name

We all know the internet is a [series of tubes](https://en.wikipedia.org/wiki/Series_of_tubes). This application uses those very same tubes to update your DNS.

## Contributing

Contributions are welcome!

## Authors

* **D'Arcy Rittich**

## License

This project is licensed under the MIT License - see the [LICENSE](/LICENSE) file for details.

## Acknowledgments

Some of the UI was inspired by [CloudFlare-DDNS-Updater](https://github.com/birkett/CloudFlare-DDNS-Updater). 
