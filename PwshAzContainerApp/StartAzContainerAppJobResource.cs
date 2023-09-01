using System.Management.Automation;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using Azure.ResourceManager.Resources;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsLifecycle.Start, "AzContainerAppJobResource")]
    [OutputType(typeof(ContainerAppJobExecutionBase))]
    public class StartAzContainerAppJobResource : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Mandatory = true)]
        public string ResourceGroupName { get; set; }

        [Parameter]
        public string SubscriptionId { get; set; }

        [Parameter(Mandatory = false)]
        public JobExecutionContainer JobContainer { get; set; } // Allow passing a single JobExecutionContainer

        [Parameter(Mandatory = false)]
        public ContainerAppJobExecutionTemplate JobExecutionTemplate { get; set; } // Allow passing the entire template

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

                    WriteVerbose($"[+] Getting Azure Container App Job: {Name}");
                    ResourceIdentifier containerAppJobResourceId = ContainerAppJobResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, Name);
                    ContainerAppJobResource containerAppJob = client.GetContainerAppJobResource(containerAppJobResourceId);

                    WriteVerbose("[+] Starting Azure Container App Job...");
                    // Use the provided JobContainer or JobExecutionTemplate
                    ContainerAppJobExecutionTemplate template;

                    if (JobContainer != null)
                    {
                        template = new ContainerAppJobExecutionTemplate()
                        {
                            Containers =
                            {
                                JobContainer
                            }
                        };
                    }
                    else
                    {
                        template = JobExecutionTemplate ?? new ContainerAppJobExecutionTemplate();
                    }

                    ArmOperation<ContainerAppJobExecutionBase> lro = containerAppJob.StartAsync(WaitUntil.Completed, template: template).GetAwaiter().GetResult();
                    ContainerAppJobExecutionBase result = lro.Value;
                    WriteVerbose("[+] Azure Container App Job was executed");
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