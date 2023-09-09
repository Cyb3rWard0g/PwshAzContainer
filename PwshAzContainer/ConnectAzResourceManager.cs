using System.Management.Automation;
using Azure.Identity;
using Azure.ResourceManager;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommunications.Connect, "AzResourceManager")]
    [OutputType(typeof(ArmClient))]
    public class ConnectAzResourceManager : PSCmdlet
    {
        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            try
            {
                WriteVerbose("[+] Initializing an authenticated ARM client...");

                ArmClient client;

                var managedIdentityClientId = Environment.GetEnvironmentVariable("MANAGED_IDENTITY_CLIENT_ID");

                if (string.IsNullOrEmpty(managedIdentityClientId))
                {
                    // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet&preserve-view=true#defaultazurecredential
                    WriteVerbose("[+] Using ChainedTokenCredential: AzurePowerShellCredential -> AzureCliCredential -> ManagedIdentityCredential");
                    client = new ArmClient(new ChainedTokenCredential(new AzurePowerShellCredential(), new AzureCliCredential(), new ManagedIdentityCredential()));
                }
                else
                {
                    // Use ManagedIdentityCredential with the provided client ID
                    WriteVerbose($"[+] Using ManagedIdentityCredential with identity: {managedIdentityClientId}");
                    client = new ArmClient(new ManagedIdentityCredential(managedIdentityClientId));
                }

                if (client == null)
                {
                    WriteVerbose("[+] No ARM Client was initialized.");
                }
                else
                {
                    // Check if the ARM Client is already stored in the session state or force it
                    if (Force || SessionState.PSVariable.Get("AzRMClient") == null)
                    {
                        // Store the ARM Client in session state for later cmdlets to access
                        WriteVerbose("[+] Storing the ARM Client in session state...");
                        SessionState.PSVariable.Set("AzRMClient", client);

                        WriteVerbose("[+] Successfully initialized authenticated ARM Client.");
                    }
                    else
                    {
                        WriteVerbose("[+] ARM Client is already stored in session state.");
                    }
                }
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
    }
}