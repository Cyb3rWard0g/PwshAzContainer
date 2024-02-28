using System.Management.Automation;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.Resources;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.Get, "AzContainerAppEnvironment")]
    [OutputType(typeof(ContainerAppData))]
    public class GetAzContainerAppEnvironment : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string? Name { get; set; }

        [Parameter(Mandatory = true)]
        public string? ResourceGroupName { get; set; }

        [Parameter]
        public string? SubscriptionId { get; set; }

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
                    
                    WriteVerbose($"[+] Getting Azure Container App Managed Environment: {Name}...");
                    ResourceIdentifier containerAppJobResourceId = ContainerAppManagedEnvironmentResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, Name);
                    ContainerAppManagedEnvironmentResource containerAppManagedEnvironment = client.GetContainerAppManagedEnvironmentResource(containerAppJobResourceId);
                    WriteObject(containerAppManagedEnvironment);
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

        private void RetrieveContainerAppsFromResourceGroup(ResourceGroupResource resourceGroup, List<ContainerAppData> containerAppsList)
        {
            WriteVerbose($"[+] Getting container app resources from {resourceGroup.Id}...");
            // Get the Container Apps in the resource group
            ContainerAppCollection collection = resourceGroup.GetContainerApps();

            // Loop through each Container App
            foreach (ContainerAppResource containerApp in collection)
            {
                // Add the Container App data to the list
                WriteVerbose($"[+] Adding data from {containerApp.Data.Name} to array ...");
                containerAppsList.Add(containerApp.Data);
            }
        }

        private ContainerAppResource? RetrieveContainerApp(ResourceGroupResource resourceGroup, string Name)
        {
            // Get the Container Apps in the resource group
            ContainerAppCollection collection = resourceGroup.GetContainerApps();

            if (collection.ExistsAsync(Name).GetAwaiter().GetResult())
            {
                WriteVerbose($"[+] Azure Container App {Name} found in {resourceGroup.Id}");
                return collection.GetAsync(Name).GetAwaiter().GetResult();
            }
            else
            {
                 WriteVerbose($"[+] Azure Container App {Name} does not exist in {resourceGroup.Id}!");
                // Return null when the app doesn't exist
                return null;
            }
        }
    }
}