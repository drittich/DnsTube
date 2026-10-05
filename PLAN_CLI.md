# DnsTube CLI Client - Architectural Plan

## Overview
Create a command-line client for DnsTube that shares the same SQLite database and settings with the Windows Service, while supporting alternate configuration files for testing scenarios.

## Design Decisions

### 1. Project Structure
- **Project Name**: `DnsTube.Cli`
- **Type**: .NET 8.0 Console Application
- **Dependencies**: 
  - Reference to `DnsTube.Core` project
  - Additional NuGet packages:
    - `System.CommandLine` (for command-line parsing)
    - `Spectre.Console` (for colored output and interactive prompts)
    - `Microsoft.Extensions.DependencyInjection`
    - `Microsoft.Extensions.Logging.Console`

### 2. Settings Management Strategy

#### Shared Database Approach
- **Default behavior**: Use the same database location as the service
  - Location: `CommonApplicationData\DnsTube\DnsTube.db`
  - Both CLI and Service read/write to the same SQLite database
  - Settings changes via CLI are immediately available to the service

#### Alternate Config File Support
- **When `--config-file <path>` is specified**:
  - Read initial configuration from the specified JSON file
  - By default, still write updates to the shared database
  - JSON file structure mirrors [`ISettings`](DnsTube.Core/Interfaces/ISettings.cs:6) interface
  - Use case: Testing configurations without affecting service settings

- **When `--config-file-only <path>` is specified**:
  - Read configuration exclusively from the specified JSON file
  - No database is used at all (completely standalone mode)
  - All changes are saved back to the config file (not to database)
  - Use case: Portable configuration, running without service, CI/CD pipelines
  - Note: Some commands that require database history (like logs) will be unavailable

#### Configuration File Format
```json
{
  "EmailAddress": "user@example.com",
  "IsUsingToken": true,
  "ApiKeyOrToken": "your-token-here",
  "UpdateIntervalMinutes": 30,
  "ProtocolSupport": 0,
  "IPv4_API": "https://api.ipify.org/",
  "IPv6_API": "https://api64.ipify.org/",
  "SelectedDomains": [],
  "SkipCheckForNewReleases": false,
  "NetworkAdapter": "_DEFAULT_"
}
```

### 3. Command Structure

#### Hybrid Command/Flag Approach
```bash
# Primary commands
dnstube update [options]
dnstube config [subcommand] [options]
dnstube domains [subcommand] [options]
dnstube status [options]
dnstube list [options]
dnstube test [options]

# Global options (work with any command)
--config-file <path>        # Use alternate config file (still uses database for writes)
--config-file-only <path>   # Use config file exclusively (no database at all)
--json                      # Output in JSON format
--verbose                   # Enable verbose logging
--no-color                  # Disable colored output
```

**Note:** `--config-file` and `--config-file-only` are mutually exclusive. Use `--config-file` when you want to test with alternate settings but keep the shared database. Use `--config-file-only` for completely standalone operation.

#### Command Details

##### `domains` - Manage Selected Domains
```bash
dnstube domains list [options]
dnstube domains add [options]
dnstube domains remove [options]
dnstube domains select [options]
dnstube domains clear [options]

Options for 'list':
  --zone <name>         Filter by zone name
  --type <A|AAAA>       Filter by record type
  --selected-only       Show only currently selected domains

Options for 'add':
  --zone <name>         Zone name (required)
  --name <domain>       DNS record name (required)
  --type <A|AAAA|TXT>   Record type (required)
  --adapter <name>      Network adapter for this domain (optional, defaults to public IP)
  (Interactive mode if no options provided)

**Note on Network Adapters:**
Each domain can optionally specify a network adapter to use for IP address retrieval:
- If not specified or set to `_PUBLIC_`: Uses public IP address
- If set to a specific adapter name: Uses IP from that network adapter
- This is stored in the [`SelectedDomain.NetworkAdapterName`](DnsTube.Core/Models/SelectedDomain.cs:8) property
- The [`IpAddressService.GetIpAddressForRecord()`](DnsTube.Core/Services/IpAddressService.cs:154) method handles per-domain adapter resolution

Options for 'remove':
  --zone <name>         Zone name (required)
  --name <domain>       DNS record name (required)
  --type <A|AAAA|TXT>   Record type (required)
  (Interactive selection if no options provided)

Options for 'select':
  (Interactive multi-select from available domains)

Options for 'clear':
  --confirm             Skip confirmation prompt
```

**Behavior**:
- `list`: Display all available DNS records with checkmarks for selected ones
- `add`: Add domain(s) to selected list (validates against Cloudflare API)
- `remove`: Remove domain(s) from selected list
- `select`: Interactive multi-select interface using Spectre.Console
- `clear`: Remove all selected domains (with confirmation)

**Example Interactive Flow for Domain Selection:**
```
User: dnstube domains select
CLI:  Fetching available domains from Cloudflare...
      
      Select domains to update (use space to toggle, enter to confirm):
      Zone: example.com
      [✓] example.com (A)
      [ ] example.com (AAAA)
      [✓] www.example.com (A)
      [ ] mail.example.com (A)
      
      Zone: example.org
      [ ] example.org (A)
      [✓] api.example.org (A)
      
      3 domains selected. Configure network adapters? (y/n)
User: y
CLI:  Configure network adapter for each domain (leave blank for public IP):
      
      example.com (A)
        Available adapters:
        1. _PUBLIC_ (Use public IP)
        2. Ethernet
        3. Wi-Fi
        Select adapter [1]: 1
      
      www.example.com (A)
        Select adapter [1]: 2
      
      api.example.org (A)
        Select adapter [1]: 1
      
      Configuration saved.
```

**Example Adding Domain with Specific Adapter:**
```bash
# Add domain using public IP (default)
dnstube domains add --zone example.com --name server.example.com --type A

# Add domain using specific network adapter
dnstube domains add --zone example.com --name local.example.com --type A --adapter Ethernet

# List domains showing their adapter configuration
dnstube domains list
```

Output:
```
Zone: example.com
  ✓ server.example.com (A) → Public IP
  ✓ local.example.com (A) → Ethernet adapter
  ✓ www.example.com (A) → Wi-Fi adapter
```

##### `update` - Perform DNS Update
```bash
dnstube update [options]

Options:
  --ipv4-only           Update only IPv4 records
  --ipv6-only           Update only IPv6 records
  --domain <name>       Update only specific domain(s), can be specified multiple times
  --dry-run             Show what would be updated without making changes
```

**Behavior**:
1. Load settings from database (or config file if specified)
2. Validate settings using [`SettingsService.ValidateSettings()`](DnsTube.Core/Services/SettingsService.cs:179)
3. Get current public IP addresses via [`IpAddressService`](DnsTube.Core/Services/IpAddressService.cs:13)
4. Update DNS records via [`CloudflareService.UpdateDnsRecordsAsync()`](DnsTube.Core/Services/CloudflareService.cs:32)
5. Display results with colored output (green for success, red for errors)

##### `config` - Manage Configuration
```bash
dnstube config show [options]
dnstube config set <key> <value> [options]
dnstube config wizard [options]
dnstube config validate [options]
dnstube config export <path> [options]

Options for 'show':
  --reveal-secrets      Show API key/token (default: masked)

Options for 'set':
  --email <address>
  --token <value>
  --api-key <value>
  --interval <minutes>
  --ipv4-api <url>
  --ipv6-api <url>
  --protocol <IPv4|IPv6|Both>
  --adapter <name>

Options for 'wizard':
  (Interactive prompts for all settings)

Options for 'export':
  <path>                Output file path for configuration
```

**Behavior**:
- `show`: Display current settings with masked secrets
- `set`: Update specific setting(s) in database
- `wizard`: Interactive configuration with [`Spectre.Console`](https://spectreconsole.net/) prompts
- `validate`: Check if current settings are valid
- `export`: Save current settings to JSON file

##### `status` - Display Current Status
```bash
dnstube status [options]

Options:
  --check-ip            Fetch and display current public IP
  --check-service       Check if DnsTube service is running
```

**Output includes**:
- Configuration status (valid/invalid)
- Current public IPv4/IPv6 addresses
- Selected domains count
- Last update time (if service is running)
- Service status (running/stopped)

##### `list` - List Zones and DNS Records
```bash
dnstube list zones [options]
dnstube list domains [options]
dnstube list adapters [options]

Options for 'zones':
  --show-ids            Include zone IDs in output

Options for 'domains':
  --selected-only       Show only selected domains
  --zone <name>         Filter by zone name
```

**Behavior**:
- `zones`: Query Cloudflare API for all accessible zones
- `domains`: Show all DNS records (with indicator for selected ones)
- `adapters`: List network adapters via [`IpAddressService.GetNetworkAdapters()`](DnsTube.Core/Services/IpAddressService.cs:106)

##### `test` - Test Configuration
```bash
dnstube test connection [options]
dnstube test api [options]
dnstube test ip [options]

Options for 'connection':
  --ipv4                Test IPv4 connectivity
  --ipv6                Test IPv6 connectivity

Options for 'api':
  (Tests Cloudflare API authentication)

Options for 'ip':
  --adapter <name>      Test IP retrieval from specific adapter
```

**Behavior**:
- Validate credentials and connectivity
- Test API endpoints without making changes
- Verify IP address retrieval

### 4. Output Modes

#### Colored Console Output (Default)
- Use [`Spectre.Console`](https://spectreconsole.net/) for:
  - Colored text (green for success, red for errors, yellow for warnings)
  - Progress bars for operations
  - Tables for listing data
  - Interactive prompts
- Automatically detect TTY and disable colors if not supported
- Respect `--no-color` flag

#### JSON Output Mode
```bash
dnstube status --json
```

Output format:
```json
{
  "success": true,
  "timestamp": "2025-10-14T10:30:00Z",
  "data": {
    "configValid": true,
    "ipv4Address": "203.0.113.1",
    "ipv6Address": "2001:db8::1",
    "selectedDomainsCount": 5,
    "serviceRunning": true
  }
}
```

All commands support JSON output for scripting scenarios.

#### Verbose Logging Mode
```bash
dnstube update --verbose
```

- Enable detailed logging via `ILogger<T>`
- Show HTTP requests/responses
- Display SQL queries
- Show timing information

### 5. Service Architecture

#### Dependency Injection Setup
```csharp
// Program.cs structure
var services = new ServiceCollection();

// CLI-specific services (always needed)
services.AddSingleton<IOutputService, OutputService>();
services.AddSingleton<IConfigFileService, ConfigFileService>();

// Conditional service registration based on mode
if (isStandaloneMode)
{
    // Standalone mode: Use file-based settings, no database
    services.AddSingleton<ISettingsService, FileSettingsService>(); // CLI-specific implementation
    services.AddSingleton<ILogService, ConsoleLogService>();
}
else
{
    // Normal mode: Use database-backed services from DnsTube.Core
    services.AddSingleton<IDbService, DbService>();
    services.AddSingleton<ISettingsService, SettingsService>();
    services.AddSingleton<ILogService, ConsoleLogService>(); // CLI-specific implementation
}

// Common core services (always needed)
services.AddSingleton<ICloudflareService, CloudflareService>();
services.AddSingleton<IIpAddressService, IpAddressService>();

// HTTP clients (similar to service setup)
ConfigureHttpClients(services);

// Logging
services.AddLogging(builder => {
    builder.AddConsole();
    builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
});
```

#### CLI-Specific Services

##### `IOutputService`
Handles output formatting based on mode (colored, JSON, plain text):
```csharp
public interface IOutputService
{
    void WriteSuccess(string message);
    void WriteError(string message);
    void WriteWarning(string message);
    void WriteInfo(string message);
    void WriteJson(object data);
    Task<T> ShowProgressAsync<T>(string description, Func<Task<T>> operation);
    void WriteTable<T>(IEnumerable<T> data, params string[] columns);
}
```

##### `IConfigFileService`
Manages alternate configuration files:
```csharp
public interface IConfigFileService
{
    Task<ISettings> LoadFromFileAsync(string path);
    Task SaveToFileAsync(string path, ISettings settings);
    Task<bool> ValidateFileAsync(string path);
    bool IsStandaloneMode { get; set; }  // True when using --config-file-only
    string? ConfigFilePath { get; set; }  // Path to config file in standalone mode
}
```

##### `ConsoleLogService` (implements `ILogService`)
CLI-specific logging that outputs to console instead of database:
```csharp
public class ConsoleLogService : ILogService
{
    private readonly IOutputService _output;
    
    public async Task WriteAsync(string message, LogLevel level)
    {
        // Write to console instead of database
        // Use appropriate color based on level
    }
    
    // Other ILogService methods adapted for console
}
```

### 6. Configuration Flow

#### First-Time Setup Scenario
```
User: dnstube update
CLI:  No configuration found. Would you like to configure DnsTube now? (y/n)
User: y
CLI:  [Interactive wizard using Spectre.Console]
      - Cloudflare email: user@example.com
      - API Token (recommended) or API Key: [hidden input]
      - Update interval (minutes): 30
      - Protocol support (IPv4/IPv6/Both): Both
      - Would you like to select domains now? (y/n)
      ...
```

#### Using Existing Service Configuration
```
User: dnstube status
CLI:  Configuration loaded from: C:\ProgramData\DnsTube\DnsTube.db
      Status: Valid
      IPv4: 203.0.113.1
      IPv6: 2001:db8::1
      Selected domains: 5
      Service: Running
```

#### Using Alternate Configuration (with database)
```
User: dnstube update --config-file test-config.json
CLI:  Configuration loaded from: test-config.json
      Database: C:\ProgramData\DnsTube\DnsTube.db
      Updating 3 DNS records...
      ✓ Updated A record [example.com] to 203.0.113.1
      ✓ Updated AAAA record [example.com] to 2001:db8::1
      ✓ Updated A record [sub.example.com] to 203.0.113.1
```

#### Using Standalone Configuration (no database)
```
User: dnstube update --config-file-only standalone.json
CLI:  Configuration loaded from: standalone.json
      Mode: Standalone (no database)
      Updating 3 DNS records...
      ✓ Updated A record [example.com] to 203.0.113.1
      ✓ Updated AAAA record [example.com] to 2001:db8::1
      ✓ Updated A record [sub.example.com] to 203.0.113.1
      Configuration saved to: standalone.json
```

### 7. Error Handling

#### User-Friendly Error Messages
```csharp
try
{
    await cloudflareService.UpdateDnsRecordsAsync(protocol, ipAddress);
}
catch (Exception ex)
{
    if (jsonMode)
    {
        output.WriteJson(new { success = false, error = ex.Message });
    }
    else
    {
        output.WriteError($"Failed to update DNS records: {ex.Message}");
        output.WriteInfo("Run with --verbose for more details");
    }
    return 1; // Exit code
}
```

#### Exit Codes
- `0`: Success
- `1`: General error
- `2`: Configuration error
- `3`: Network error
- `4`: API authentication error

### 8. Database Modes

#### Shared Database Mode (Default)
When no config file flags are used, or when `--config-file` is specified:
- SQLite WAL mode is already enabled in [`DbService`](DnsTube.Core/Services/DbService.cs:24)
- Use short-lived database connections
- Implement retry logic for lock timeouts
- Service has priority (CLI should gracefully handle locks)
- Both CLI and Service can access database simultaneously

#### Standalone Mode (`--config-file-only`)
When `--config-file-only` is specified:
- No database connection is created
- All settings are loaded from and saved to the JSON config file
- [`SettingsService`](DnsTube.Core/Services/SettingsService.cs:13) is bypassed with a file-based implementation
- Some features may be unavailable (historical logs, cached data)
- Useful for:
  - CI/CD pipelines
  - Portable configurations
  - Running without service installation
  - Containerized environments

### 9. Project Files

#### DnsTube.Cli.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>dnstube</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\DnsTube.Core\DnsTube.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
    <PackageReference Include="Spectre.Console" Version="0.49.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.1" />
  </ItemGroup>
</Project>
```

### 10. File Structure

```
DnsTube.Cli/
├── Program.cs                    # Entry point, DI setup, command registration
├── DnsTube.Cli.csproj           # Project file
├── Commands/
│   ├── UpdateCommand.cs         # dnstube update
│   ├── ConfigCommand.cs         # dnstube config
│   ├── DomainsCommand.cs        # dnstube domains
│   ├── StatusCommand.cs         # dnstube status
│   ├── ListCommand.cs           # dnstube list
│   └── TestCommand.cs           # dnstube test
├── Services/
│   ├── OutputService.cs         # IOutputService implementation
│   ├── ConfigFileService.cs     # IConfigFileService implementation
│   ├── FileSettingsService.cs   # ISettingsService implementation for standalone mode
│   └── ConsoleLogService.cs     # ILogService implementation for CLI
├── Models/
│   └── CliOptions.cs            # Shared CLI options
└── README.md                    # CLI documentation
```

### 11. Implementation Order

1. **Foundation** (Tasks 1-3)
   - Create project structure and add dependencies
   - Set up DI container and basic command infrastructure
   - Implement settings management with config file support

2. **Core Commands** (Task 4)
   - Implement `status` command (simplest, good starting point)
   - Implement `config` command (essential for setup)
   - Implement `domains` command (domain management with interactive selection)
   - Implement `update` command (core functionality)
   - Implement `list` command (useful for exploration)
   - Implement `test` command (debugging aid)

3. **Output & UX** (Tasks 5-7)
   - Implement colored console output with Spectre.Console
   - Add JSON output mode
   - Add verbose logging mode

4. **Polish** (Tasks 8-12)
   - Enhance error handling and messages
   - Create configuration wizard
   - Write documentation
   - Comprehensive testing

## Key Benefits of This Design

1. **Shared Settings**: CLI and Service use the same database, ensuring consistency
2. **Testing Flexibility**: `--config-file` allows testing without affecting production
3. **User-Friendly**: Colored output, interactive wizard, clear error messages
4. **Scriptable**: JSON mode and exit codes support automation
5. **Debuggable**: Verbose mode provides detailed operation logs
6. **Maintainable**: Reuses existing [`DnsTube.Core`](DnsTube.Core/DnsTube.Core.csproj) services
7. **Flexible**: Hybrid command structure supports both simple and complex usage

## Usage Examples

### Simple one-time update
```bash
dnstube update
```

### Configure and select domains interactively
```bash
dnstube config wizard
dnstube domains select
dnstube update
```

### Add specific domain for updates
```bash
dnstube domains add --zone example.com --name www.example.com --type A
dnstube update --domain www.example.com
```

### Test configuration without making changes
```bash
dnstube update --dry-run --verbose
```

### Use alternate config for testing (with database)
```bash
dnstube update --config-file test.json --dry-run
```

### Use standalone config (no database)
```bash
dnstube update --config-file-only portable.json
```

### Portable configuration for CI/CD
```bash
# Create a portable config
dnstube config export --output ci-config.json

# Use it in CI/CD pipeline without any database
dnstube update --config-file-only ci-config.json --json
```

### Script-friendly status check
```bash
dnstube status --json | jq .data.ipv4Address
```

### Interactive first-time setup
```bash
dnstube config wizard
```

## Next Steps

Once you approve this plan, we can switch to Code mode to implement the solution following this architecture.