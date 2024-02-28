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
    [Cmdlet(VerbsCommon.New, "AzContainerAppJobResource")]
    [OutputType(typeof(ContainerAppJobData))]
    public class NewAzContainerAppJobResource : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string? Name { get; set; }

        [Parameter(Mandatory = true)]
        public string? ResourceGroupName { get; set; }

        [Parameter]
        public string? SubscriptionId { get; set; }

        [Parameter(Mandatory = true)]
        public string? EnvironmentId { get; set; }


        [Parameter()]
        public List<ContainerAppRegistryCredentials>? ConfigRegistries { get; set; }

        [Parameter(Mandatory = true)]
        public ContainerAppJobTemplate? ContainerTemplate { get; set;}

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
                    WriteVerbose("[+] Getting Azure Container App Jobs...");
                    ContainerAppJobCollection collection = resourceGroupResource.GetContainerAppJobs();

                    WriteVerbose("[+] Defining Container App Job Data...");
                    string containerAppJobName = Name;
                    var appJobData = new ContainerAppJobData(new AzureLocation(Location))
                    {
                        EnvironmentId = new ResourceIdentifier(EnvironmentId),
                        Configuration = new ContainerAppJobConfiguration(ContainerAppJobTriggerType.Manual, 180)
                        {
                            ReplicaTimeout = 180,
                            ReplicaRetryLimit = 0,
                            ManualTriggerConfig = new JobConfigurationManualTriggerConfig()
                            {
                                ReplicaCompletionCount = 1,
                                Parallelism = 4,
                            },
                        },
                        Template = ContainerTemplate,
                    };

                    if (ConfigRegistries != null && ConfigRegistries.Count > 0)
                    {
                        foreach (var credential in ConfigRegistries)
                        {
                            appJobData.Configuration.Registries.Add(credential);
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
                        appJobData.Identity = managedIdentity;
                    }

                    WriteVerbose("[+] Creating Azure Container App Job...");
                    ArmOperation<ContainerAppJobResource> lro = collection.CreateOrUpdateAsync(WaitUntil.Completed, containerAppJobName, appJobData).GetAwaiter().GetResult();
                    ContainerAppJobResource result = lro.Value;
                    WriteVerbose("[+] Azure Container App Job creation Results:");
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