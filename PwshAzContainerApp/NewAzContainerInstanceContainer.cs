using System.Management.Automation;
using Azure.ResourceManager.ContainerInstance.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerInstanceContainer")]
    [OutputType(typeof(ContainerInstanceContainer))]
    public class NewAzContainerInstanceContainer : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Mandatory = true)]
        public string Image { get; set; }

        [Parameter]
        public double MemoryInGB { get; set; } = 3;

        [Parameter]
        public double Cpu { get; set; } = 2;

        [Parameter]
        public List<string>? Command { get; set; }

        [Parameter]
        public List<ContainerPort>? Ports { get; set; }

        [Parameter]
        public PSObject[]? ContainerEnv { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            
            var ContainerInstance = new ContainerInstanceContainer(Name,Image,new ContainerResourceRequirements(
                new ContainerResourceRequestsContent(MemoryInGB,Cpu)
                {
                    MemoryInGB = MemoryInGB,
                    Cpu = Cpu
                })
            )
            {
                Image = Image
            };

            // Setting commands to execute
            if (Command != null && Command.Count > 0 )
            {
                foreach (var cmd in Command)
                {
                    ContainerInstance.Command.Add(cmd);
                }
            }

            // Setting environment variables
            if (ContainerEnv != null && ContainerEnv.Length > 0)
            {     
                foreach (var customEnvVar in ContainerEnv)
                {
                    var varName = customEnvVar.Properties["Name"].Value.ToString();
                    ContainerEnvironmentVariable envVar = new ContainerEnvironmentVariable(varName);
                    if (customEnvVar.Properties["Value"] != null)
                    {
                        envVar.Value = customEnvVar.Properties["Value"].Value.ToString();
                    }
                    else if (customEnvVar.Properties["SecureValue"] != null) {
                        envVar.SecureValue = customEnvVar.Properties["SecureValue"].Value.ToString();
                    }
                    ContainerInstance.EnvironmentVariables.Add(envVar);
                }
            }

            // Setting Ports to expose
            if (Ports != null && Ports.Count > 0 )
            {
                foreach (var port in Ports)
                {
                    ContainerInstance.Ports.Add(port);
                }
            }

            WriteObject(ContainerInstance);
        }
    }
}

