# .NET 9.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 9.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 9.0 upgrade.
3. Upgrade PV.Forecasting.App\PV.Forecasting.App.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                   | Current Version | New Version | Description                                        |
|:-----------------------------------------------|:---------------:|:-----------:|:---------------------------------------------------|
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform |   2.0.1         |             | Package functionality included with new framework reference |

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### PV.Forecasting.App\PV.Forecasting.App.csproj modifications

Project properties changes:
  - Target framework should be changed from `.NETFramework,Version=v4.8` to `net9.0`

NuGet packages changes:
  - Microsoft.CodeDom.Providers.DotNetCompilerPlatform should be removed (*Package functionality included with new framework reference*)

Other changes:
  - Project file needs to be converted to SDK-style
