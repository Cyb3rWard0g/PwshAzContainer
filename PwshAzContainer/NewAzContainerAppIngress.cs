using System.Management.Automation;
using Azure.ResourceManager.AppContainers.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerAppIngress")]
    [OutputType(typeof(ContainerAppIngressConfiguration))]
    public class NewAzContainerAppIngress : PSCmdlet
    {
        [Parameter]
        public bool External { get; set; } = false;

        [Parameter(Mandatory = true)]
        public int TargetPort { get; set; }

        [Parameter]
        public int ExposedPort { get; set; } = 0;

        [Parameter]
        [ValidateSet("Auto","Http","Http2","Tcp")]
        public string Transport { get; set; } = "Auto";

        [Parameter]
        public List<ContainerAppRevisionTrafficWeight> Traffic { get; set; } = new List<ContainerAppRevisionTrafficWeight>();
        
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var Ingress = new ContainerAppIngressConfiguration()
            {
                External = External,
                TargetPort = TargetPort,
                ExposedPort = ExposedPort,
                Transport = Transport,
            };

            foreach (var trafficWeight in Traffic)
            {
                Ingress.Traffic.Add(trafficWeight);
            }
            WriteObject(Ingress);
        }
    }
}