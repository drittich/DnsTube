# DnsTube CLI

Command-line interface for DnsTube - A dynamic DNS updater for Cloudflare.

## Overview

DnsTube CLI provides a command-line interface to manage and update DNS records on Cloudflare. It shares the same configuration database with the DnsTube Windows Service, while also supporting standalone operation with configuration files.

## Features

- **Shared Configuration**: Uses the same SQLite database as the DnsTube service
- **Standalone Mode**: Can operate completely independently with JSON config files
- **Per-Domain Network Adapters**: Configure specific network adapters for individual DNS entries
- **Interactive Configuration**: Step-by-step wizard for initial setup
- **Interactive Domain Selection**: Multi-select interface for choosing domains to manage
- **Colored Output**: Rich terminal output with automatic TTY detection
- **JSON Mode**: Machine-readable output for scripting and automation
- **Verbose Logging**: Detailed operation logs for debugging

## Installation

### Build from Source

```bash
cd DnsTube.Cli
dotnet build -c Release
```

The compiled executable will be in `bin/Release/net8.0/dnstube.exe`

### Add to PATH (Optional)

For easier access, add the output directory to your system PATH.

## Usage

### Global Options

All commands support these global options:

```bash
--config-file <path>        # Use alternate config file (still uses database for writes)
--config-file-only <path>   # Use config file exclusively (no database)
--json                      # Output in JSON format
--verbose, -v               # Enable verbose logging
--no-color                  # Disable colored output
```

### Commands

#### `status` - Display Current Status

```bash
dnstube status [options]

Options:
  --check-ip              Fetch and display current public IP
  --check-service         Check if DnsTube service is running

Examples:
  dnstube status
  dnstube status --check-ip --check-service
  dnstube status --json | jq .data.ipv4Address
```

#### `update` - Perform DNS Update

```bash
dnstube update [options]

Options:
  --ipv4-only             Update only IPv4 records
  --ipv6-only             Update only IPv6 records
  --domain <name>         Update only specific domain(s)
  --dry-run               Show what would be updated without making changes

Examples:
  dnstube update
  dnstube update --dry-run
  dnstube update --domain example.com --ipv4-only
```

*(Implementation in progress)*

#### `config` - Manage Configuration

```bash
dnstube config <subcommand> [options]

Subcommands:
  show        Display current configuration
  set         Update specific settings
  wizard      Interactive configuration wizard
  validate    Check if configuration is valid
  export      Save configuration to JSON file

Examples:
  dnstube config show
  dnstube config show --reveal-secrets
  dnstube config set --email user@example.com --token abc123
  dnstube config wizard
  dnstube config export my-config.json
```

*(Implementation in progress)*

#### `domains` - Manage Selected Domains

```bash
dnstube domains <subcommand> [options]

Subcommands:
  list        List available DNS records
  add         Add domain to selected list
  remove      Remove domain from selected list
  select      Interactive multi-select interface
  clear       Remove all selected domains

Options for 'add':
  --zone <name>           Zone name (required)
  --name <domain>         DNS record name (required)
  --type <A|AAAA|TXT>     Record type (required)
  --adapter <name>        Network adapter (optional, defaults to public IP)

Examples:
  dnstube domains list
  dnstube domains select
  dnstube domains add --zone example.com --name www.example.com --type A
  dnstube domains add --zone example.com --name local.example.com --type A --adapter Ethernet
```

*(Implementation in progress)*

#### `list` - List Resources

```bash
dnstube list <subcommand> [options]

Subcommands:
  zones       List Cloudflare zones
  domains     List DNS records
  adapters    List network adapters

Options for 'zones':
  --show-ids              Include zone IDs

Options for 'domains':
  --selected-only         Show only selected domains
  --zone <name>           Filter by zone

Examples:
  dnstube list zones
  dnstube list domains --selected-only
  dnstube list adapters
```

*(Implementation in progress)*

#### `test` - Test Configuration and Connectivity

```bash
dnstube test <subcommand> [options]

Subcommands:
  connection  Test network connectivity
  api         Test Cloudflare API authentication
  ip          Test IP address retrieval

Options for 'connection':
  --ipv4                  Test IPv4 connectivity only
  --ipv6                  Test IPv6 connectivity only

Options for 'ip':
  --adapter <name>        Test specific network adapter

Examples:
  # Test API authentication
  dnstube test api
  dnstube test api --verbose
  
  # Test network connectivity
  dnstube test connection
  dnstube test connection --ipv4
  dnstube test connection --ipv6
  
  # Test IP retrieval
  dnstube test ip
  dnstube test ip --adapter Ethernet
  dnstube test ip --verbose
  
  # JSON output for scripting
  dnstube test api --json | jq .results.accessibleZones
```

The test command helps diagnose connectivity and configuration issues:
- **connection**: Tests network connectivity to IP detection APIs, reports latency
- **api**: Validates Cloudflare API credentials and lists accessible zones
- **ip**: Tests IP address retrieval from configured APIs and network adapters

## Configuration Modes

### 1. Shared Database Mode (Default)

Uses the same database as the DnsTube service:

```bash
dnstube status
# Configuration loaded from: C:\ProgramData\DnsTube\DnsTube.db
```

Changes made via CLI are immediately available to the service.

### 2. Alternate Config with Database

Reads initial settings from a file but writes to the shared database:

```bash
dnstube update --config-file test-config.json
# Configuration loaded from: test-config.json
# Database: C:\ProgramData\DnsTube\DnsTube.db
```

Useful for testing configurations without affecting production settings.

### 3. Standalone Mode

Completely independent operation with no database:

```bash
dnstube update --config-file-only portable.json
# Configuration loaded from: portable.json
# Mode: Standalone (no database)
```

Perfect for:
- CI/CD pipelines
- Portable configurations
- Running without service installation
- Containerized environments

## Configuration File Format

```json
{
  "emailAddress": "user@example.com",
  "isUsingToken": true,
  "apiKeyOrToken": "your-token-here",
  "updateIntervalMinutes": 30,
  "protocolSupport": 0,
  "ipv4_API": "https://api.ipify.org/",
  "ipv6_API": "https://api64.ipify.org/",
  "selectedDomains": [
    {
      "zoneName": "example.com",
      "dnsName": "www.example.com",
      "type": "A",
      "networkAdapterName": "_PUBLIC_"
    },
    {
      "zoneName": "example.com",
      "dnsName": "local.example.com",
      "type": "A",
      "networkAdapterName": "Ethernet"
    }
  ],
  "skipCheckForNewReleases": false
  // Note: Legacy 'networkAdapter' field removed - use per-domain 'networkAdapterName' instead
}
```

### Protocol Support Values
- `0` = IPv4 only
- `1` = IPv6 only  
- `2` = Both IPv4 and IPv6

## Network Adapters (Per-Domain Configuration)

DnsTube uses **per-domain network adapter configuration** exclusively. Each DNS entry can specify which network adapter to use for IP address retrieval:

```bash
# Use public IP (default)
dnstube domains add --zone example.com --name server.example.com --type A

# Use specific network adapter for a local domain
dnstube domains add --zone example.com --name local.example.com --type A --adapter Ethernet
```

### Network Adapter Values
- `_PUBLIC_` = Use public IP address (default when not specified)
- `<adapter name>` = Use specific adapter's IP (e.g., "Ethernet", "Wi-Fi")

When updating DNS records, the system will:
1. Use the public IP if `networkAdapterName` is null, empty, or `_PUBLIC_`
2. Use the IP address from the specified adapter otherwise
3. Fall back to public IP if the specified adapter is not found

**Note**: There is no global network adapter setting. Each domain must specify its own adapter configuration.

## Exit Codes

- `0` - Success
- `1` - General error
- `2` - Configuration error
- `3` - Network error
- `4` - API authentication error

## Examples

### First-Time Setup

```bash
# Interactive wizard
dnstube config wizard

# Select domains interactively
dnstube domains select

# Perform first update
dnstube update
```

### Regular Usage

```bash
# Check status
dnstube status --check-ip

# Update DNS
dnstube update

# View in JSON for scripting
dnstube status --json | jq .data
```

### CI/CD Pipeline

```bash
# Export config from service
dnstube config export --output ci-config.json

# Use standalone mode in pipeline
dnstube update --config-file-only ci-config.json --json
```

### Testing Configurations

```bash
# Test without making changes
dnstube update --dry-run --verbose

# Test with alternate config
dnstube update --config-file test.json --dry-run
```

## Architecture

The CLI is built with:

- **.NET 8.0** - Modern cross-platform framework
- **System.CommandLine** - Robust command-line parsing
- **Spectre.Console** - Rich terminal UI
- **Shared Core Services** - Reuses DnsTube.Core for all Cloudflare operations

### Project Structure

```
DnsTube.Cli/
├── Program.cs                  # Entry point, DI setup, command registration
├── Models/
│   └── CliOptions.cs           # Global CLI options
├── Services/
│   ├── OutputService.cs        # Colored/JSON output handling
│   ├── ConfigFileService.cs    # Config file operations
│   ├── ConsoleLogService.cs    # Console-based logging
│   └── FileSettingsService.cs  # Standalone mode settings
└── Commands/
    ├── UpdateCommand.cs        # DNS update operations
    ├── ConfigCommand.cs        # Configuration management
    ├── DomainsCommand.cs       # Domain selection
    ├── StatusCommand.cs        # Status display
    ├── ListCommand.cs          # Resource listing
    └── TestCommand.cs          # Connectivity testing
```

## Troubleshooting

### "No configuration found"

First-time users need to configure DnsTube:

```bash
dnstube config wizard
```

### "Service not running" warning

The CLI can work independently of the service. This is just informational.

### JSON parsing errors with --config-file

Verify your JSON file is valid:

```bash
dnstube config export valid-config.json
# Use this as a template
```

### Network adapter not found

List available adapters:

```bash
dnstube list adapters
```

## Contributing

This CLI is part of the DnsTube project. See the main README for contribution guidelines.

## License

Same license as DnsTube project.