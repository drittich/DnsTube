# DnsTube

## What is this?

DnsTube is Windows client for dynamically updating Cloudflare DNS entries with your public IP address.

Most of us have home computers with dynamically assigned IP addresses provided by our ISPs. If you want to serve up a web site, be able to access files remotely, or use RDP, etc., from the internet, it becomes challenging to locate your machine when the IP address is constantly changing. This is the problem DNS was designed to solve, so by getting a domain name and updating the DNS entries for it as-needed, you can always access your computer by its domain name, and forget about IP addresses.

This is where DnsTube comes in. Cloudflare provides free DNS hosting for your domain, and also provides an API for updating DNS entries, i.e., the IP address in this case. By running DnsTube on your computer, it will periodically check the public-facing IP address and update the Cloudflare DNS entry as needed.

![image](https://user-images.githubusercontent.com/1222810/113607965-e5474700-9617-11eb-917a-1f80aad9c039.png)

## Features

* Can update A (IPv4), AAAA (IPv6), SPF, and TXT records.
* Support both Cloudflare API keys and tokens
* Supports API tokens scoped to specific zones
* Does updates on an adjustable timer, e.g., every 30 minutes
* Supports minimize on load, check for updates

## Downloading

Head over to the [Releases](https://github.com/drittich/DnsTube/releases/latest) page to download the latest binary.

## Installation

You have four executables to choose from, and you can extract and copy the application files to a folder of your choice. (DnsTube requires .NET 5, so you may be prompted to install it if you choose a non-self-contained version.)

- *DnsTube-vX.X.X.7z*: normal application, requires .NET 5 runtime to be installed
- *DnsTube-SelfContained-vX.X.X.7z*: normal self-contained application, does not require .NET 5 runtime to be installed
- *DnsTube-Portable-vX.X.X.7z*: portable application, requires .NET 5 runtime to be installed
- *DnsTube-Portable-SelfContained-vX.X.X.7z*: portable self-contained application, does not require .NET 5 runtime to be installed

You can choose to manually launch the application, or make it [run automatically at startup in Windows 10](https://support.microsoft.com/en-us/windows/add-an-app-to-run-automatically-at-startup-in-windows-10-150da165-dcd9-7230-517b-cf3c295d89dd).

## Configuration & Usage

You will need to create an account with Cloudflare and make it the DNS authority for your domain. You then need to configure your DNS entries as appropriate. See [Managing DNS records in Cloudflare](https://support.cloudflare.com/hc/en-us/articles/360019093151-Managing-DNS-records-in-Cloudflare) for more info.

You can use [nslookup](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/nslookup) to make sure DNS resolution is working correctly, e.g., 
```
nslookup mydomain.com
```

After that, you'll need to generate an API Token (preferred) or Key in order to access the API via DnsTube. The details for doing that can be found at [Creating API tokens](https://developers.cloudflare.com/api/tokens/create).

Once you have done this, enter your Cloudflare email address, token or key, and zone ID. You'll also went to select whether you are updating IPv4 addresses, ipv6 addresses or both.

After you have the settings configured, from then on you just launch it and leave it running.

## UI

**Top Pane**: The UI shows a list of domains for zone you have provded at the top, and you can check off the ones you want DnsTube to update. 

**Lower Pane**: The lower pane shows a running log of activity. 

**Fetch List** button: If you have modified your DNS entries manually in Cloudflare, you can click **Fetch List** to update the list of domains shown in DnsTube. 

**Manual Update** button: You can click **Manual Update** whenever you want to force an IP address update before the timer interval occurs - I often do this when I have just finished using a VPN.

## Notes

1. DnsTube only updates existing Cloudflare records. It will not create or remove records.
2. DnsTube must currently be run as a logged-in user. A future release will support running it as a service.

## Building

This solution was built using Visual Studio 2019. It's probably best to use 2019 or later. 

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
