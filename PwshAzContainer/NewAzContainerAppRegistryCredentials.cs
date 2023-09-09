using System.Management.Automation;
using Azure.ResourceManager.AppContainers.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerAppRegistryCredentials")]
    [OutputType(typeof(ContainerAppRegistryCredentials))]
    public class NewAzContainerAppRegistryCredentials : PSCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "Identity")]
        public string Identity { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "UserPassword")]
        public string Username { get; set; }

         [Parameter(Mandatory = true, ParameterSetName = "UserPassword")]
        public string? PasswordSecretReference { get; set; }

        [Parameter(Mandatory = true)]
        public string Server { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            
            var registryCreds = new ContainerAppRegistryCredentials()
            {
                Server = Server,
            };

            if (ParameterSetName == "Identity")
            {
                registryCreds.Identity = Identity;
            }
            else {
                registryCreds.Username = Username;
                registryCreds.PasswordSecretRef = PasswordSecretReference;
            }

            WriteObject(registryCreds);
        }
    }
}