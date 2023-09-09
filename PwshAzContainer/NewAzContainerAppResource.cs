using System.Management.Automation;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerAppResource")]
    [OutputType(typeof(ContainerAppData))]
    public class NewAzContainerAppResource : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Mandatory = true)]
        public string ResourceGroupName { get; set; }

        [Parameter]
        public string SubscriptionId { get; set; }

        [Parameter(Mandatory = true)]
        public string EnvironmentId { get; set; }

        [Parameter]
        [ValidateSet("Multiple","Single")]
        public string ConfigActiveRevisionsMode { get; set; } = "Multiple";

        [Parameter]
        public ContainerAppIngressConfiguration? ConfigIngressObject { get; set; }

        [Parameter()]
        public List<ContainerAppRegistryCredentials>? ConfigRegistries { get; set; }

        [Parameter(Mandatory = true)]
        public ContainerAppTemplate ContainerTemplate { get; set;}

        [Parameter()]
        public string Location { get; set; } = "East US";

        [Parameter()]
        public string? Identity { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            // Retrieve the ARM client from session state
            if (SessionState.PSVariable.Get("AzRMClient").Value is ArmClient client)
            {
                WriteVerbose("[+] Successfully retrieved the ARM client from session state.");
                try
                {
                    // Get the subscription
                    if (string.IsNullOrEmpty(SubscriptionId))
                    {
                        WriteVerbose("[+] Getting default subscription...");
                        SubscriptionResource subscription = client.GetDefaultSubscriptionAsync().GetAwaiter().GetResult();
                        SubscriptionId = subscription.Data.SubscriptionId;
                    }
                    WriteVerbose($"[+] Using subscription: {SubscriptionId}...");

                    WriteVerbose($"[+] Getting Resource Group: {ResourceGroupName}");
                    ResourceIdentifier resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName);
                    ResourceGroupResource resourceGroupResource = client.GetResourceGroupResource(resourceGroupResourceId);

                    // get the collection of this ContainerAppResource
                    WriteVerbose("[+] Getting Azure Container Apps...");
                    ContainerAppCollection collection = resourceGroupResource.GetContainerApps();

                    WriteVerbose("[+] Defining Container App Data...");
                    string containerAppName = Name;
                    var appData = new ContainerAppData(new AzureLocation(Location))
                    {
                        EnvironmentId = new ResourceIdentifier(EnvironmentId),
                        Configuration = new ContainerAppConfiguration()
                        {
                            ActiveRevisionsMode = ConfigActiveRevisionsMode,
                            Ingress = ConfigIngressObject ?? null,
                        },
                        Template = ContainerTemplate,
                    };

                    if (ConfigRegistries != null && ConfigRegistries.Count > 0)
                    {
                        foreach (var credential in ConfigRegistries)
                        {
                            appData.Configuration.Registries.Add(credential);
                        }
                    }

                    if(!string.IsNullOrEmpty(Identity))
                    {
                        ManagedServiceIdentity managedIdentity;

                        if(Identity == "system")
                        {
                            managedIdentity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned);
                        }
                        else
                        {
                            managedIdentity = new ManagedServiceIdentity(ManagedServiceIdentityType.UserAssigned);
                            managedIdentity.UserAssignedIdentities.Add(new ResourceIdentifier(Identity), new UserAssignedIdentity());
                        }
                        appData.Identity = managedIdentity;
                    }

                    WriteVerbose("[+] Creating Azure Container App...");
                    ArmOperation<ContainerAppResource> lro = collection.CreateOrUpdateAsync(WaitUntil.Completed, containerAppName, appData).GetAwaiter().GetResult();
                    ContainerAppResource result = lro.Value;
                    WriteVerbose("[+] Azure Container App creation Results:");
                    WriteObject(result);
                }
                catch (Azure.RequestFailedException ex)
                {
                    // Catch ARM-related exceptions
                    WriteError(new ErrorRecord(ex, "AzureRequestFailedError", ErrorCategory.ConnectionError, this));
                }
                catch (Exception ex)
                {
                    // Catch other unexpected exceptions
                    WriteError(new ErrorRecord(ex, "UnexpectedError", ErrorCategory.NotSpecified, this));
                }
            }
            else
            {
                WriteVerbose("[+] ARM client not found in session state.");
                ThrowTerminatingError(new ErrorRecord(
                    new PSInvalidOperationException("ARM client not found in session state."),
                    "ARMClientNotFound", ErrorCategory.ResourceUnavailable, null));
            }
        }
    }
}