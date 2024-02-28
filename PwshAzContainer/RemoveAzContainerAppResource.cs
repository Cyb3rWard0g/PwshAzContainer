using System.Management.Automation;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.Resources;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.Remove, "AzContainerAppResource")]
    [OutputType(typeof(ContainerAppData))]
    public class RemoveAzContainerAppResource : PSCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "ByName")]
        public string? Name { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByName")]
        public string? ResourceGroupName { get; set; }

        [Parameter(ParameterSetName = "ByName")]
        public string? SubscriptionId { get; set; }

        [Parameter(ParameterSetName = "ById")]
        public string? ResourceId { get; set; }

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
                
                    if (ParameterSetName == "ById")
                    {
                        WriteVerbose($"[+] Referencing container app by Id: {ResourceId}...");
                        containerApp = client.GetContainerAppResource(new ResourceIdentifier(ResourceId));
                    }
                    else
                    {
                        // Get the subscription
                        if (string.IsNullOrEmpty(SubscriptionId))
                        {
                            WriteVerbose("[+] Getting default subscription...");
                            SubscriptionResource subscription = client.GetDefaultSubscriptionAsync().GetAwaiter().GetResult();
                            SubscriptionId = subscription.Data.SubscriptionId;
                        }
                        WriteVerbose($"[+] Using subscription: {SubscriptionId}...");

                        WriteVerbose($"[+] Getting Azure Container App: {Name}...");
                        ResourceIdentifier containerAppResourceId = ContainerAppResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, Name);
                        containerApp = client.GetContainerAppResource(containerAppResourceId);
                    }

                    WriteVerbose("[+] Deleting Azure Container App...");
                    // invoke the operation
                    containerApp.DeleteAsync(WaitUntil.Completed).GetAwaiter().GetResult();
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