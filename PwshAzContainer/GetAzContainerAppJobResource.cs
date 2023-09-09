using System.Management.Automation;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.Resources;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.Get, "AzContainerAppJobResource")]
    [OutputType(typeof(ContainerAppJobData))]
    public class AzContainerAppJobResource : PSCmdlet
    {
        [Parameter]
        public string? Name { get; set; }

        [Parameter]
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
                    ContainerAppJobResource? containerAppJob;
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
                    
                    // Initialize a list to store Container App jobs data
                    var containerAppJobsList = new List<ContainerAppJobData>();

                    if (!string.IsNullOrEmpty(ResourceGroupName))
                    {
                        // Get a specific resource group if ResourceGroupName is specified
                        WriteVerbose($"[+] Getting specific resource group: {ResourceGroupName}...");
                        ResourceGroupResource resourceGroup = resourceGroups.GetAsync(ResourceGroupName).GetAwaiter().GetResult();

                        // Get the Container App Jobs in the resource group
                        if (!string.IsNullOrEmpty(Name))
                        {
                            containerAppJob = RetrieveContainerAppJob(resourceGroup, Name);
                            if (containerAppJob != null)
                            {
                                WriteVerbose($"[+] Adding data from {containerAppJob.Data.Name} to array ...");
                                containerAppJobsList.Add(containerAppJob.Data);
                            }
                        }
                        else
                        {
                            RetrieveContainerAppJobsFromResourceGroup(resourceGroup, containerAppJobsList);
                        }
                    }
                    else
                    {
                        // Loop through each resource group and get Container Apps within them
                        WriteVerbose("[+] Getting Azure Container App Jobs from all resource groups...");
                        foreach (ResourceGroupResource resourceGroup in resourceGroups)
                        {
                            // Get the Container App Jobs in the resource group
                            if (!string.IsNullOrEmpty(Name))
                            {
                                containerAppJob = RetrieveContainerAppJob(resourceGroup, Name);
                                if (containerAppJob != null)
                                {
                                    WriteVerbose($"[+] Found Azure Container App Job: {Name}");
                                    WriteVerbose($"[+] Adding data from {containerAppJob.Data.Name} to array ...");
                                    containerAppJobsList.Add(containerAppJob.Data);
                                    break;
                                }
                            }
                            else
                            {
                                RetrieveContainerAppJobsFromResourceGroup(resourceGroup, containerAppJobsList);
                            }
                        }
                    }

                    WriteVerbose("[+] Processing results...");
                    if (containerAppJobsList.Count == 1)
                    {
                        WriteVerbose("[+] Returning one object...");
                        WriteObject(containerAppJobsList[0]);
                    }
                    else if (containerAppJobsList.Count > 1)
                    {
                        WriteVerbose("[+] Returning an array of container app jobs data...");
                        WriteObject(containerAppJobsList.ToArray());
                    }
                    else
                    {
                        WriteVerbose("[+] No Container Apps Jobs found.");
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

        private void RetrieveContainerAppJobsFromResourceGroup(ResourceGroupResource resourceGroup, List<ContainerAppJobData> containerAppsList)
        {
            WriteVerbose($"[+] Getting container app job resources from {resourceGroup.Id}...");
            // Get the Container Apps in the resource group
            ContainerAppJobCollection collection = resourceGroup.GetContainerAppJobs();

            // Loop through each Container App
            foreach (ContainerAppJobResource containerAppJob in collection)
            {
                // Add the Container App data to the list
                WriteVerbose($"[+] Adding data from {containerAppJob.Data.Name} to array ...");
                containerAppsList.Add(containerAppJob.Data);
            }
        }

        private ContainerAppJobResource? RetrieveContainerAppJob(ResourceGroupResource resourceGroup, string Name)
        {
            // Get the Container Apps in the resource group
            ContainerAppJobCollection collection = resourceGroup.GetContainerAppJobs();

            if (collection.ExistsAsync(Name).GetAwaiter().GetResult())
            {
                WriteVerbose($"[+] Azure Container App Job {Name} found in {resourceGroup.Id}");
                return collection.GetAsync(Name).GetAwaiter().GetResult();
            }
            else
            {
                 WriteVerbose($"[+] Azure Container App Job {Name} does not exist in {resourceGroup.Id}!");
                // Return null when the app doesn't exist
                return null;
            }
        }
    }
}