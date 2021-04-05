# DnsTube

A Windows client for dynamically updating Cloudflare DNS entries with your public IP address.

![image](https://user-images.githubusercontent.com/1222810/113607965-e5474700-9617-11eb-917a-1f80aad9c039.png)

## Features

* Can update A (IPv4), AAAA (IPv6), SPF, and TXT records.
* Support both Cloudflare API keys and tokens
* Supports API tokens scoped to specific zones
* Does updates on an adjustable timer, e.g., every 30 minutes
* Supports minimize on load, check for updates

### Notes

1. DnsTube only updates existing Cloudflare records. It will not create or remove records.
2. DnsTube must currently be run as a logged-in user. A future release will support running it as a service.

## Installation

You have four executables to choose from, and you can extract and copy the application files to a folder of your choice. (DnsTube requires .NET 5, so you may be prompted to install it if you choose a non-self-contained version.)

- DnsTube-vX.X.X.7z: normal application, requires .NET 5 runtime to be installed
- DnsTube-SelfContained-vX.X.X.7z: normal self-contained application, does not require .NET 5 runtime to be installed
- DnsTube-Portable-vX.X.X.7z: portable application, requires .NET 5 runtime to be installed
- DnsTube-Portable-SelfContained-vX.X.X.7z: portable self-contained application, does not require .NET 5 runtime to be installed

## Building

This solution was built using Visual Studio 2019. It's probably best to use that version. 

## Downloading

Head over to the [Releases](https://github.com/drittich/DnsTube/releases/latest) page to download the latest binary.

## The Name

We all know the internet is a [series of tubes](https://en.wikipedia.org/wiki/Series_of_tubes). This application uses those very same tubes to update your DNS.


## Contributing

Contributions are welcome!

## Authors

* **D'Arcy Rittich**

## Roadmap

### Recently Added
* Support for Cloudflare API tokens
* Support for IPv6
* Check for updates

### Next Up:
* Run as a Windows service
* Optional Startup shortcut creation

## License

This project is licensed under the MIT License - see the [LICENSE](/LICENSE) file for details.

## Acknowledgments

Some of the UI was inspired by [CloudFlare-DDNS-Updater](https://github.com/birkett/CloudFlare-DDNS-Updater). 
