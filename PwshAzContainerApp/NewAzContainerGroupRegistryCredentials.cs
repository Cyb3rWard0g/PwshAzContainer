using System.Management.Automation;
using Azure.ResourceManager.ContainerInstance.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerGroupRegistryCredentials")]
    [OutputType(typeof(ContainerGroupImageRegistryCredential))]
    public class NewAzContainerGroupRegistryCredentials : PSCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "Identity")]
        public string Identity { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "UserPassword")]
        public string Username { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "UserPassword")]
        public string? Password { get; set; }

        [Parameter(Mandatory = true)]
        public string Server { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            
            var registryCreds = new ContainerGroupImageRegistryCredential(Server);

            if (ParameterSetName == "Identity")
            {
                registryCreds.Identity = Identity;
            }
            else {
                registryCreds.Username = Username;
                registryCreds.Password = Password;
            }

            WriteObject(registryCreds);
        }
    }
}