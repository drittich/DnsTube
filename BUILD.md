# Build Instructions

The .csproj file has been configured to build a self-contained, single-file win-x64 binary. To build, go to a command prompt in the project folder, and run

```dotnet publish -r win-x64 -c Release --self-contained true`

This will create a binary within `bin\Release\netcoreapp3.1\win-x64\publish`.