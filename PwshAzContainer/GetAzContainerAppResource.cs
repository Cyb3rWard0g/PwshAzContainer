using System.Management.Automation;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.Resources;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.Get, "AzContainerAppResource")]
    [OutputType(typeof(ContainerAppData))]
    public class GetAzContainerAppResource : PSCmdlet
    {
        [Parameter()]
        public string? Name { get; set; }

        [Parameter()]
        public string? ResourceGroupName { get; set; }

        [Parameter()]
        public string? SubscriptionId { get; set; }

        [Parameter(ParameterSetName = "ById")]
        public string ResourceId { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            // Retrieve the ARM client from session state
            if (SessionState.PSVariable.Get("AzRMClient").Value is ArmClient client)
            {
                WriteVerbose("[+] Successfully retrieved the ARM client from session state.");
                try
                {
                    ContainerAppResource containerApp;
                    List<ContainerAppData> containerAppsList = new List<ContainerAppData>();

                    if (ParameterSetName == "ById")
                    {
                        WriteVerbose($"[+] Referencing container app by Id: {ResourceId}...");
                        containerApp = client.GetContainerAppResource(new ResourceIdentifier(ResourceId));
                        containerAppsList.Add(containerApp.Data);
                    }
                    else{
                        SubscriptionResource subscription;

                        // Get the subscription
                        if (string.IsNullOrEmpty(SubscriptionId))
                        {
                            WriteVerbose("[+] Getting default subscription...");
                            subscription = client.GetDefaultSubscriptionAsync().GetAwaiter().GetResult();
                        }
                        else 
                        {
                            ResourceIdentifier subscriptionResourceId = SubscriptionResource.CreateResourceIdentifier(SubscriptionId);
                            subscription = client.GetSubscriptionResource(subscriptionResourceId);
                        } 
                        SubscriptionId = subscription.Data.SubscriptionId;
                        WriteVerbose($"[+] Using subscription: {SubscriptionId}...");
                        
                        // Get the resource groups in the subscription
                        WriteVerbose("[+] Getting all resource groups...");
                        ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

                        if (!string.IsNullOrEmpty(ResourceGroupName))
                        {
                            // Get a specific resource group if ResourceGroupName is specified
                            WriteVerbose($"[+] Getting specific resource group: {ResourceGroupName}...");
                            ResourceGroupResource resourceGroup = resourceGroups.GetAsync(ResourceGroupName).GetAwaiter().GetResult();

                            // Get the Container Apps in the resource group
                            if (!string.IsNullOrEmpty(Name))
                            {
                                containerApp = RetrieveContainerApp(resourceGroup, Name)!;
                                if (containerApp != null)
                                {
                                    WriteVerbose($"[+] Adding data from {containerApp.Data.Name} to array ...");
                                    containerAppsList.Add(containerApp.Data);
                                }
                            }
                            else
                            {
                                RetrieveContainerAppsFromResourceGroup(resourceGroup, containerAppsList);
                            }
                        }
                        else
                        {
                            // Loop through each resource group and get Container Apps within them
                            WriteVerbose("[+] Getting Azure Container Apps from all resource groups...");
                            foreach (ResourceGroupResource resourceGroup in resourceGroups)
                            {
                                // Get the Container Apps in the resource group
                                if (!string.IsNullOrEmpty(Name))
                                {
                                    containerApp = RetrieveContainerApp(resourceGroup, Name)!;
                                    if (containerApp != null)
                                    {
                                        WriteVerbose($"[+] Found Azure Container App: {Name}");
                                        WriteVerbose($"[+] Adding data from {containerApp.Data.Name} to array ...");
                                        containerAppsList.Add(containerApp.Data);
                                        break;
                                    }
                                }
                                else
                                {
                                    RetrieveContainerAppsFromResourceGroup(resourceGroup, containerAppsList);
                                }
                            }
                        }
                    }
            
                    WriteVerbose("[+] Processing results...");
                    if (containerAppsList.Count == 1)
                    {
                        WriteVerbose("[+] Returning one object...");
                        WriteObject(containerAppsList[0]);
                    }
                    else if (containerAppsList.Count > 1)
                    {
                        WriteVerbose("[+] Returning an array of container apps data...");
                        WriteObject(containerAppsList.ToArray());
                    }
                    else
                    {
                        WriteVerbose("[+] No Container Apps found.");
                    }
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