using System.Management.Automation;
using Azure.ResourceManager.AppContainers.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerAppJobExecutionTemplate")]
    [OutputType(typeof(ContainerAppJobExecutionTemplate))]
    public class NewAzContainerAppJobExecutionTemplate : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ContainerImage { get; set; }

        [Parameter(Mandatory = true)]
        public string ContainerName { get; set; }

        [Parameter]
        public List<string>? ContainerCommand { get; set; }

        [Parameter]
        public PSObject[]? ContainerEnv { get; set; }

        [Parameter]
        public double ResourceCpu { get; set; } = 1.5;

        [Parameter]
        public string ResourceMemory { get; set; } = "3Gi";

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            
            var ExecutionTemplate = new ContainerAppJobExecutionTemplate()
            {
                Containers =
                {
                    new JobExecutionContainer()
                    {
                        Image = ContainerImage,
                        Name = ContainerName,
                        Resources = new AppContainerResources()
                        {
                            Cpu = ResourceCpu,
                            Memory = ResourceMemory,
                        },
                    }
                },
            };

            // Setting commands to execute
            if (ContainerCommand != null && ContainerCommand.Count > 0 )
            {
                foreach (var command in ContainerCommand)
                {
                    ExecutionTemplate.Containers[0].Command.Add(command);
                }
            }

            // Setting environment variables
            if (ContainerEnv != null && ContainerEnv.Length > 0)
            {     
                foreach (var customEnvVar in ContainerEnv)
                {
                    ContainerAppEnvironmentVariable envVar = new ContainerAppEnvironmentVariable();
                    envVar.Name = customEnvVar.Properties["Name"].Value.ToString();

                    if (customEnvVar.Properties["Value"] != null)
                    {
                        envVar.Value = customEnvVar.Properties["Value"].Value.ToString();
                    }
                    else if (customEnvVar.Properties["SecretRef"] != null) {
                        envVar.SecretRef = customEnvVar.Properties["SecretRef"].Value.ToString();
                    }
                    ExecutionTemplate.Containers[0].Env.Add(envVar);
                }
            }
            WriteObject(ExecutionTemplate);
        }
    }
}