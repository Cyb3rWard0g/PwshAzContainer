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
        public string? Name { get; set; }

        [Parameter(Mandatory = true)]
        public string? ResourceGroupName { get; set; }

        [Parameter]
        public string? SubscriptionId { get; set; }

        [Parameter]
        public ContainerAppJobExecutionTemplate? JobExecutionTemplate { get; set; }

        [Parameter]
        public List<string>? ContainerCommand { get; set; }

        [Parameter]
        public PSObject[]? ContainerEnv { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            // Retrieve the ARM client from session state
            if (SessionState.PSVariable.Get("AzRMClient").Value is ArmClient client)
            {
                WriteVerbose("[+] Successfully retrieved the ARM client from session state.");
                try
                {
                    ContainerAppJobExecutionTemplate Template = new ContainerAppJobExecutionTemplate();

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
                    
                    ContainerAppJobData containerData = containerAppJob.Get().Value.Data;

                    WriteVerbose("[+] Processing input..");
                    if (JobExecutionTemplate != null)
                    {
                        WriteVerbose("[+] Starting Job with new execution template..");
                        if(JobExecutionTemplate.Containers[0].Env != null && containerData.Template.Containers[0].Env != null)
                        {
                            WriteVerbose("[+] Retrieving existing environment variables..");
                            foreach (var existingVar in containerData.Template.Containers[0].Env)
                            {
                                JobExecutionTemplate.Containers[0].Env.Add(existingVar);
                            }
                        }
                        Template = JobExecutionTemplate;
                    }
                    else
                    {
                        if(ContainerCommand != null && ContainerCommand.Count > 0)
                        {
                            WriteVerbose("[+] Starting Job with new commands..");
                            Template = new ContainerAppJobExecutionTemplate()
                            {
                                Containers =
                                {
                                    new JobExecutionContainer()
                                    {
                                        Image = containerData.Template.Containers[0].Image,
                                        Name = containerData.Template.Containers[0].Name,
                                        Resources = new AppContainerResources()
                                        {
                                            Cpu = containerData.Template.Containers[0].Resources.Cpu,
                                            Memory = containerData.Template.Containers[0].Resources.Memory,
                                        },
                                    }
                                },
                            };
                            WriteVerbose("[+] Adding new commands to execution template..");
                            foreach (var newCmd in ContainerCommand)
                            {
                                Template.Containers[0].Command.Add(newCmd);
                            }
                        }

                        // Setting environment variables
                        if(ContainerEnv != null && containerData.Template.Containers[0].Env != null)
                        {
                            WriteVerbose("[+] Retrieving existing environment variables..");
                            foreach (var existingVar in containerData.Template.Containers[0].Env)
                            {
                                Template.Containers[0].Env.Add(existingVar);
                            }
                        }

                        if (ContainerEnv != null && ContainerEnv.Length > 0)
                        {     
                            WriteVerbose("[+] Adding new environment variables to template..");
                            foreach (var customEnvVar in ContainerEnv)
                            {
                                ContainerAppEnvironmentVariable envVar = new()
                                {
                                    Name = customEnvVar.Properties["Name"].Value.ToString()
                                };

                                if (customEnvVar.Properties["Value"] != null)
                                {
                                    envVar.Value = customEnvVar.Properties["Value"].Value.ToString();
                                }
                                else if (customEnvVar.Properties["SecretRef"] != null) {
                                    envVar.SecretRef = customEnvVar.Properties["SecretRef"].Value.ToString();
                                }
                                Template.Containers[0].Env.Add(envVar);
                            }
                        }
                    }
                    WriteVerbose("[+] Starting Azure Container App Job...");
                    ArmOperation<ContainerAppJobExecutionBase> lro = containerAppJob.StartAsync(WaitUntil.Completed, Template).GetAwaiter().GetResult();
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