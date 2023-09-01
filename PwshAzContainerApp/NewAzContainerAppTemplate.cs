using System.Management.Automation;
using Azure.ResourceManager.AppContainers.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerAppTemplate")]
    [OutputType(typeof(ContainerAppTemplate))]
    public class NewAzContainerAppTemplate : PSCmdlet
    {
        [Parameter]
        public string RevisionSuffix { get; set; }

        [Parameter(Mandatory = true)]
        public string ContainerImage { get; set; }

        [Parameter(Mandatory = true)]
        public string ContainerName { get; set; }

        [Parameter]
        public double ResourceCpu { get; set; } = 0.5;

        [Parameter]
        public string ResourceMemory { get; set; } = "1Gi";

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            
            var Template = new ContainerAppTemplate()
            {
                RevisionSuffix = RevisionSuffix,
                Containers =
                {
                    new ContainerAppContainer()
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
            WriteObject(Template);
        }
    }
}