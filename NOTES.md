# Developer Notes

## Create .NET Project

```
$module = 'PwshAzContainerApp'

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

```
dotnet new sln

dotnet sln add "$($module).csproj"
```

## Add dnMerge Reference

```xml
<Project Sdk="Microsoft.NET.Sdk">

....

  <ItemGroup>
    <PackageReference Include="dnMerge" Version="0.5.15">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>    
  </ItemGroup>

.....

</Project>
```

## Make Sure Dependencies / References are also Built

Add `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` to your property group section.

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
    <PackageReference Include="PowerShellStandard.Library" Version="7.0.0-preview.1" />
  </ItemGroup>

</Project>

```

## Build Project

Make sure you build `Release` so that `dnMerge` can pack all references to one DLL.

```
dotnet build --configuration Release
```

## Import Module Locally

You can test your PowerShell binary module with all the references because of this property `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in your `.csproj` file. Run the following command:

```powershell
Import-Module PwshAzContainerApp\bin\Release\net7.0\PwshAzContainerApp.dll
```

## Publish to PS Gallery

* Create `output\PwshAzContainerApp` directory
* Copy `PwshAzContainerApp.dll` and `PwshAzContainerApp.psd1` files to `output\PwshAzContainerApp`
* Publish module to PSGallery with the following commands:

```powershell
Publish-Module -Name .\output\PwshAzContainerApp\ -NuGetApiKey XXXXXX -verbose -Debug
```