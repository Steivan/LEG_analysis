# .NET 9.0 Upgrade Report

## Project target framework modifications

| Project name                                   | Old Target Framework        | New Target Framework         | Commits                   |
|:-----------------------------------------------|:---------------------------:|:----------------------------:|---------------------------|
| PV.Forecasting.App\PV.Forecasting.App.csproj   | .NETFramework,Version=v4.8  | net9.0                       | 842451fb                  |

## NuGet Packages

| Package Name                                   | Old Version | New Version | Commit Id                                 |
|:-----------------------------------------------|:-----------:|:-----------:|-------------------------------------------|
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform |   2.0.1     |  (removed)  | 842451fb                                  |

## All commits

| Commit ID              | Description                                                              |
|:-----------------------|:-------------------------------------------------------------------------|
| 8b7fed83               | Commit upgrade plan                                                      |
| 842451fb               | Store final changes for step 'Upgrade PV.Forecasting.App\PV.Forecasting.App.csproj' |
| f790d1bc               | Commit changes before fixing errors.                                     |


## Project feature upgrades

Contains summary of modifications made to the project assets during different upgrade stages.

### PV.Forecasting.App

Here is what changed for the project during upgrade:

- The project was converted to SDK-style.
- The project's output type was changed from `Exe` to `Library` to resolve a build error.

## Next steps

- You can now switch back to your `master` branch to continue your previous work.
- The upgrade changes are saved in the `upgrade-to-NET9` branch. You can merge this branch into `master` when you are ready.
