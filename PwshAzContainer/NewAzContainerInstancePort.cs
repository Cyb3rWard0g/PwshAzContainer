using System.Management.Automation;
using Azure.ResourceManager.ContainerInstance.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerInstancePort")]
    [OutputType(typeof(ContainerPort))]
    public class NewAzContainerInstancePort : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public int Port { get; set; }

        [Parameter]
        [ValidateSet("Tcp","Udp")]
        public ContainerNetworkProtocol Protocol { get; set; } = "Tcp";

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var InstancePort = new ContainerPort(Port)
            {
                Port = Port,
                Protocol = Protocol
            };

            WriteObject(InstancePort);
        }
    }
}