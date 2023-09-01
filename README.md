# PwshAzContainerApp

PwshAzContainerApp is a PowerShell binary module built using .NET Core and designed to work with PowerShell Core. It provides cmdlets, written in C#, for interacting with [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/overview) and [Azure Container App Jobs](https://learn.microsoft.com/en-us/azure/container-apps/jobs?tabs=azure-cli).

## Dependencies

`PwshAzContainerApp` depends on the following NuGet packages:

- [Azure.Identity](https://www.nuget.org/packages/Azure.Identity/) (Version 1.10.0)
- [Azure.ResourceManager](https://www.nuget.org/packages/Azure.ResourceManager/) (1.7.0)
- [Azure.ResourceManager.AppContainers](https://www.nuget.org/packages/Azure.ResourceManager.AppContainers/) (Version 1.1.0)
- [PowerShellStandard.Library](https://www.nuget.org/packages/PowerShellStandard.Library/) (Version 7.0.0-preview.1)
- [dnMerge](https://www.nuget.org/packages/dnMerge/) (Version 0.5.15)

Note that the `dnMerge` package is used to merge multiple NuGet packages into a single assembly to improve module compatibility.

## Installation

To install the [PwshAzContainerApp module](https://www.powershellgallery.com/packages/PwshAzContainerApp), you can use the PowerShell Gallery:

```powershell
Install-Module -Name PwshAzContainerApp -Scope CurrentUser -verbose
```

## Usage

`PwshAzContainerApp` provides cmdlets for various Azure Container Apps operations, including creating, getting, updating, and deleting.

### Initialize Azure RM Client

first you need to initialize an Azure Resource Manager client.

#### Azure PowerShell Credential

```powershell
Clear-AzContext -Force
Connect-AzAccount -Tenant XXXXX

Connect-AzResourceManager -verbose
```

#### Azure CLI Credential

```powershell
az login

Connect-AzResourceManager -verbose
```

#### User Assigned Managed Identity

```powershell
$env:MANAGED_IDENTITY_CLIENT_ID = '<user-assigned-managed-identity-client-id'

Connect-AzResourceManager -verbose
```

### Get Azure Container Apps

```powershell
Get-AzContainerAppResource -verbose
```

```powershell
Get-AzContainerAppResource -ResourceGroupName MyGroup -verbose
```

```powershell
Get-AzContainerAppResource -Name MyApp -ResourceGroupName MyGroup -verbose
```

### Create Azure Container App

```powershell
$IdentityResourceId = '<User-Assigned Managed Identity Resource Id>'
$ContainerAppName = "ca-$(-join ((65..90) + (97..122) | Get-Random -Count 8 | ForEach-Object {[char]$_}))".ToLower()
$ResourceGroup = '<MyGroup>'
$ContainerRegistryServer = '<Container Registry Server>'
$ContainerImage = "$ContainerRegistryServer/<container-name>:<tag>"
$ContainerImage = '<ContainerImage>'
$ContainerName = "$(-join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_}))".ToLower()
$EnvironmentName = '<Container App Managed Environment Name>'
$Environment = Get-AzContainerAppEnvironment -Name $EnvironmentName -ResourceGroupName $ResourceGroup
$EnvironmentId = $Environment.id

$Ingress = New-AzContainerAppIngress -External $false -TargetPort 80
$Template = New-AzContainerAppTemplate -ContainerImage $ContainerImage -ContainerName $ContainerName
$Registries = New-AzContainerAppRegistryCredentials -Identity $IdentityResourceId -Server $ContainerRegistryServer

$ContainerApp = New-AzContainerAppResource -Name $ContainerAppName -ResourceGroupName $ResourceGroup -EnvironmentId $EnvironmentId -ConfigIngressObject $ingress -ContainerTemplate $Template -ConfigRegistries $Registries -Identity $IdentityResourceId -verbose
$ContainerApp.Data
```

### Delete Azure Container App

```powershell
Remove-AzContainerAppResource -Name MyApp -ResourceGroupName MyGroup -Verbose
```

```powershell
Remove-AzContainerAppResource -ResourceId $ContainerAppResourceId -Verbose
```

### Create Azure Container App Job

```powershell
$IdentityResourceId = '<User-Assigned Managed Identity Resource Id>'
$IdentityClientId = '<User-Assigned Managed Identity Client Id>'
$ContainerAppJobName = "caj-$(-join ((65..90) + (97..122) | Get-Random -Count 8 | ForEach-Object {[char]$_}))".ToLower()
$ResourceGroup = '<MyGroup>'
$ContainerImage = "mcr.microsoft.com/azure-powershell:latest"
$ContainerName = "$(-join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_}))".ToLower()
$EnvironmentName = '<Container App Managed Environment Name>'
$Environment = Get-AzContainerAppEnvironment -Name $EnvironmentName -ResourceGroupName $ResourceGroup
$EnvironmentId = $Environment.id

$customObjects = @([PSCustomObject]@{Name = "MANAGED_IDENTITY_CLIENT_ID"; Value = $IdentityClientId})
$Template = New-AzContainerAppJobTemplate -ContainerImage $ContainerImage -ContainerName $ContainerName -ContainerEnv $customObjects -verbose

$ContainerAppJob = New-AzContainerAppJobResource -Name $ContainerAppJobName -ResourceGroupName $ResourceGroup -EnvironmentId $EnvironmentId -ContainerTemplate $Template -Identity $IdentityResourceId -verbose
$ContainerAppJob.Data
```

## Contributing

Contributions are welcome! If you find any issues or have suggestions for improvements, feel free to open an issue or submit a pull request.

## License

This project is licensed under the MIT License.

## References

* https://github.com/Azure/azure-sdk-for-net/blob/Azure.ResourceManager.ContainerService_1.2.0-beta.2/sdk/containerapps/Azure.ResourceManager.AppContainers/README.md
* https://www.nuget.org/packages/Azure.ResourceManager.AppContainers
* https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.appcontainers?view=azure-dotnet
* https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.appcontainers.containerappresource.start?view=azure-dotnet