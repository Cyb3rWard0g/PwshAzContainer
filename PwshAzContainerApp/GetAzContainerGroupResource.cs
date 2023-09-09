using System.Management.Automation;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.Resources;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.Get, "AzContainerGroupResource")]
    [OutputType(typeof(ContainerGroupResource))]
    public class GetAzContainerGroupResource : PSCmdlet
    {
        [Parameter()]
        public string? Name { get; set; }

        [Parameter()]
        public string? ResourceGroupName { get; set; }

        [Parameter()]
        public string SubscriptionId { get; set; }

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
                    ContainerGroupResource ContainerGroup;
                    List<ContainerGroupData> ContainerGroupsList = new List<ContainerGroupData>();

                    if (ParameterSetName == "ById")
                    {
                        WriteVerbose($"[+] Referencing Container Group by Id: {ResourceId}...");
                        ContainerGroup = client.GetContainerGroupResource(new ResourceIdentifier(ResourceId));
                        ContainerGroupsList.Add(ContainerGroup.Data);
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

                            // Get the container groups in the resource group
                            if (!string.IsNullOrEmpty(Name))
                            {
                                ContainerGroup = RetrieveContainerGroup(resourceGroup, Name);
                                if (ContainerGroup != null)
                                {
                                    WriteVerbose($"[+] Adding data from {ContainerGroup.Data.Name} to array ...");
                                    ContainerGroupsList.Add(ContainerGroup.Data);
                                }
                            }
                            else
                            {
                                RetrieveContainerGroupsFromResourceGroup(resourceGroup, ContainerGroupsList);
                            }
                        }
                        else
                        {
                            // Loop through each resource group and get container groups within them
                            WriteVerbose("[+] Getting Azure container groups from all resource groups...");
                            foreach (ResourceGroupResource resourceGroup in resourceGroups)
                            {
                                // Get the container groups in the resource group
                                if (!string.IsNullOrEmpty(Name))
                                {
                                    ContainerGroup = RetrieveContainerGroup(resourceGroup, Name);
                                    if (ContainerGroup != null)
                                    {
                                        WriteVerbose($"[+] Found Azure Container Group: {Name}");
                                        WriteVerbose($"[+] Adding data from {ContainerGroup.Data.Name} to array ...");
                                        ContainerGroupsList.Add(ContainerGroup.Data);
                                        break;
                                    }
                                }
                                else
                                {
                                    RetrieveContainerGroupsFromResourceGroup(resourceGroup, ContainerGroupsList);
                                }
                            }
                        }
                    }
            
                    WriteVerbose("[+] Processing results...");
                    if (ContainerGroupsList.Count == 1)
                    {
                        WriteVerbose("[+] Returning one object...");
                        WriteObject(ContainerGroupsList[0]);
                    }
                    else if (ContainerGroupsList.Count > 1)
                    {
                        WriteVerbose("[+] Returning an array of container groups data...");
                        WriteObject(ContainerGroupsList.ToArray());
                    }
                    else
                    {
                        WriteVerbose("[+] No container groups found.");
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

        private void RetrieveContainerGroupsFromResourceGroup(ResourceGroupResource resourceGroup, List<ContainerGroupData> ContainerGroupsList)
        {
            WriteVerbose($"[+] Getting Container Group resources from {resourceGroup.Id}...");
            // Get the container groups in the resource group
            ContainerGroupCollection collection = resourceGroup.GetContainerGroups();

            // Loop through each Container Group
            foreach (ContainerGroupResource ContainerGroup in collection)
            {
                // Add the Container Group data to the list
                WriteVerbose($"[+] Adding data from {ContainerGroup.Data.Name} to array ...");
                ContainerGroupsList.Add(ContainerGroup.Data);
            }
        }

        private ContainerGroupResource? RetrieveContainerGroup(ResourceGroupResource resourceGroup, string Name)
        {
            // Get the container groups in the resource group
            ContainerGroupCollection collection = resourceGroup.GetContainerGroups();

            if (collection.ExistsAsync(Name).GetAwaiter().GetResult())
            {
                WriteVerbose($"[+] Azure Container Group {Name} found in {resourceGroup.Id}");
                return collection.GetAsync(Name).GetAwaiter().GetResult();
            }
            else
            {
                 WriteVerbose($"[+] Azure Container Group {Name} does not exist in {resourceGroup.Id}!");
                // Return null when the app doesn't exist
                return null;
            }
        }
    }
}