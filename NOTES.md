# Developer Notes

## Create .NET Project

```
$module = 'PwshAzContainer'

dotnet new classlib -n $module

Set-Location $module
```

## Set SDK Version

```
dotnet new globaljson --sdk-version 7.0.400
```

## Add NuGet packages

```
dotnet add package PowerShellStandard.Library --version 7.0.0-preview.1
dotnet add package Azure.ResourceManager.AppContainers
dotnet add package Azure.Identity
```

## Create Solution File

change directories to the previous location.

```
cd ..

dotnet new sln

dotnet sln add "$($module)/$($module).csproj"
```

## Make Sure Dependencies / References are also Built

Add `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` to your `.csproj` file's property group section.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Azure.Identity" Version="1.10.0" />
    <PackageReference Include="Azure.ResourceManager.AppContainers" Version="1.1.0" />
    <PackageReference Include="Azure.ResourceManager.ContainerInstance" Version="1.1.0" />
    <PackageReference Include="PowerShellStandard.Library" Version="7.0.0-preview.1" />
  </ItemGroup>

</Project>

```

## Build Project

Create an `output` folder at the root of your project (outside of the module folder) and add a sub-folder with the name of the module `PwshAzContainer`.

```powershell
New-Item -Path output\$module -Type Directory
```

```
dotnet build --configuration Release --output .\output\$module\
```

## Import Module Locally

You can test your PowerShell binary module with all the references because of this property `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in your `.csproj` file. Run the following command:

```powershell
Import-Module .\output\$module\PwshAzContainer.dll
```

## Publish to PS Gallery

* Copy `PwshAzContainer.psd1` file to `output\PwshAzContainer`
* Publish module to PSGallery with the following commands:

```powershell
Publish-Module -Name .\output\PwshAzContainer\ -NuGetApiKey XXXXXX -verbose -Debug
```