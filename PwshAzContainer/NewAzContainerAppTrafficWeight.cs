using System.Management.Automation;
using Azure.ResourceManager.AppContainers.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerAppTrafficWeight")]
    [OutputType(typeof(ContainerAppRevisionTrafficWeight))]
    public class NewAzContainerAppTrafficWeight : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string RevisionName { get; set; }

        [Parameter]
        public int Weight { get; set; } = 0;

        [Parameter]
        public string Label { get; set; } = "";

        [Parameter(ParameterSetName = "LatestRevision")]
        public bool IsLatestRevision { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            
            var Traffic = new List<ContainerAppRevisionTrafficWeight>();
            
            var TrafficWeight = new ContainerAppRevisionTrafficWeight(){
                RevisionName = RevisionName,
                Weight = Weight,
                Label = Label,
            };

            if (ParameterSetName == "LatestRevision")
            {
                TrafficWeight.IsLatestRevision = IsLatestRevision;
            }

            Traffic.Add(TrafficWeight);

            WriteObject(Traffic);
        }
    }
}