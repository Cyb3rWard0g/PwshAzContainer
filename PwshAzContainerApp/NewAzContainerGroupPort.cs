using System.Management.Automation;
using Azure.ResourceManager.ContainerInstance.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerGroupPort")]
    [OutputType(typeof(ContainerGroupPort))]
    public class NewAzContainerGroupPort : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public int Port { get; set; }

        [Parameter]
        [ValidateSet("Tcp","Udp")]
        public ContainerGroupNetworkProtocol Protocol { get; set; } = "Tcp";

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var GroupPort = new ContainerGroupPort(Port)
            {
                Port = Port,
                Protocol = Protocol
            };

            WriteObject(GroupPort);
        }
    }
}