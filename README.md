# PwshAzContainer Module

PwshAzContainer is a PowerShell binary module built using .NET Core and designed to work with PowerShell Core. It provides cmdlets, written in C#, for interacting with [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/overview), [Azure Container App Jobs](https://learn.microsoft.com/en-us/azure/container-apps/jobs?tabs=azure-cli), and [Azure Container Instances](https://learn.microsoft.com/en-us/azure/container-instances/container-instances-overview).

## Dependencies

`PwshAzContainer` depends on the following NuGet packages:

- [Azure.Identity](https://www.nuget.org/packages/Azure.Identity/) (Version 1.10.0)
- [Azure.ResourceManager.AppContainers](https://www.nuget.org/packages/Azure.ResourceManager.AppContainers/) (Version 1.1.0)
- [Azure.ResourceManager.ContainerInstance](https://www.nuget.org/packages/Azure.ResourceManager.ContainerInstance) (1.1.0)
- [PowerShellStandard.Library](https://www.nuget.org/packages/PowerShellStandard.Library/) (Version 7.0.0-preview.1)

## Installation

To install the [PwshAzContainer module](https://www.powershellgallery.com/packages/PwshAzContainer), you can use the PowerShell Gallery:

```powershell
Install-Module -Name PwshAzContainer -Scope CurrentUser -verbose
```

## Usage

`PwshAzContainer` provides cmdlets for various Azure Container Apps, Jobs and Instances operations, including creating, getting, updating, and deleting.

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

### Start Azure Container App Job

```powershell
Start-AzContainerAppJobResource -Name "MyJob" -ResourceGroupName "MyGroup" -Verbose
```

```powershell
$commands = @("pwsh","-c","whoami")
Start-AzContainerAppJobResource -Name "MyJob" -ResourceGroupName "MyGroup" -ContainerCommand $commands -Verbose
```

```powershell
$envVariables = @([PSCustomObject]@{Name = "EMAIL_SENDER"; Value = "acccount@<domain>.com"}) 
$commands = @('pwsh','-c','write-host $env:EMAIL_SENDER')
Start-AzContainerAppJobResource -Name "MyJob" -ResourceGroupName "MyGroup" -ContainerCommand $commands -ContainerEnv $envVariables -Verbose
```

### Get Job Execution History

```powershell
Get-AzContainerAppJobExecution -JobName "MyJob" -ResourceGroupName "MyGroup" -verbose
```

```powershell
Get-AzContainerAppJobExecution -JobName "MyJob" -ResourceGroupName "MyGroup" -ExecutionName "NewExecution" -verbose
```

### Create Azure Container Instance (Private Access)

```powershell
$IdentityResourceId = '<User-Assigned Managed Identity Resource Id>'
$ContainerGroupName = "ci-$(-join ((65..90) + (97..122) | Get-Random -Count 8 | ForEach-Object {[char]$_}))".ToLower()
$ResourceGroup = '<MyGroup>'
$ContainerRegistryServer = '<Container Registry Server>'
$ContainerImage = "$ContainerRegistryServer/<container-name>:<tag>"
$ContainerName = "$(-join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_}))".ToLower()
$SubnetId = "/subscriptions/<SubscriptionId>/resourceGroups/<MyRG>/providers/Microsoft.Network/virtualNetworks/<vnet-name>/subnets/<subnet-name>"

$ContainerInstancePorts = @(
    $ContainerInstancePort80 = New-AzContainerInstancePort -Port 80 -Protocol 'Tcp'
    $ContainerInstancePort443 = New-AzContainerInstancePort -Port 443 -Protocol 'Tcp'
    $ContainerInstancePort53 = New-AzContainerInstancePort -Port 53 -Protocol 'Udp'
)
$ContainerGroupPorts = @(
    $ContainerGroupPort80 = New-AzContainerGroupPort -Port 80 -Protocol 'Tcp'
    $ContainerGroupPort443 = New-AzContainerGroupPort -Port 443 -Protocol 'Tcp'
    $ContainerGroupPort53 = New-AzContainerGroupPort -Port 53 -Protocol 'Udp'
)
$Command = @("tail","-f","/dev/null")
$ContainerInstance = New-AzContainerInstanceContainer -Name $ContainerName -Image $ContainerImage -Ports $ContainerInstancePorts -Command $Command

$Registries = New-AzContainerGroupRegistryCredentials -Identity $IdentityResourceId -Server $ContainerRegistryServer

$ContainerGroup = New-AzContainerGroupResource -Name $ContainerGroupName -ResourceGroupName $ResourceGroup -ContainerInstance $ContainerInstance -ImageRegistryCredential $Registries -Ports $ContainerGroupPorts -IpAddressType "Private" -SubnetId $SubnetId -Identity $IdentityResourceId -verbose

$ContainerGroup.Data
```

### Create Azure Container Instance (Public Access)

```powershell
$IdentityResourceId = '<User-Assigned Managed Identity Resource Id>'
$ContainerGroupName = "ci-$(-join ((65..90) + (97..122) | Get-Random -Count 8 | ForEach-Object {[char]$_}))".ToLower()
$ResourceGroup = '<MyGroup>'
$ContainerRegistryServer = '<Container Registry Server>'
$ContainerImage = "$ContainerRegistryServer/<container-name>:<tag>"
$ContainerName = "$(-join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_}))".ToLower()
$SubnetId = "/subscriptions/<SubscriptionId>/resourceGroups/<MyRG>/providers/Microsoft.Network/virtualNetworks/<vnet-name>/subnets/<subnet-name>"

$ContainerInstancePorts = @(
    $ContainerInstancePort80 = New-AzContainerInstancePort -Port 80 -Protocol 'Tcp'
    $ContainerInstancePort443 = New-AzContainerInstancePort -Port 443 -Protocol 'Tcp'
    $ContainerInstancePort53 = New-AzContainerInstancePort -Port 53 -Protocol 'Udp'
)
$ContainerGroupPorts = @(
    $ContainerGroupPort80 = New-AzContainerGroupPort -Port 80 -Protocol 'Tcp'
    $ContainerGroupPort443 = New-AzContainerGroupPort -Port 443 -Protocol 'Tcp'
    $ContainerGroupPort53 = New-AzContainerGroupPort -Port 53 -Protocol 'Udp'
)
$Command = @("tail","-f","/dev/null")
$envVariables = @([PSCustomObject]@{Name = "SITE_FQDN"; Value = "<mydomain>[.]com"}) 
$ContainerInstance = New-AzContainerInstanceContainer -Name $ContainerName -Image $ContainerImage -Ports $ContainerInstancePorts -Command $Command -ContainerEnv $envVariables

$Registries = New-AzContainerGroupRegistryCredentials -Identity $IdentityResourceId -Server $ContainerRegistryServer

$ContainerGroup = New-AzContainerGroupResource -Name $ContainerGroupName -ResourceGroupName $ResourceGroup -ContainerInstance $ContainerInstance -ImageRegistryCredential $Registries -Ports $ContainerGroupPorts -IpAddressType "Public" -Identity $IdentityResourceId -verbose

$ContainerGroup.Data
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
* https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.containerinstance?view=azure-dotnet
* https://github.com/Azure/azure-rest-api-specs-examples/tree/main/specification/containerinstance/resource-manager/Microsoft.ContainerInstance/stable/2023-05-01/examples-dotnet