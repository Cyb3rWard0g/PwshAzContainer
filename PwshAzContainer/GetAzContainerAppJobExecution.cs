using System.Management.Automation;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.Resources;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.Get, "AzContainerAppJobExecution")]
    [OutputType(typeof(string))]
    public class AzContainerAppJobExecution : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string JobName { get; set; }

        [Parameter(Mandatory = true)]
        public string ResourceGroupName { get; set; }

        [Parameter]
        public string? SubscriptionId { get; set; }

        [Parameter]
        public string? ExecutionName { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            // Retrieve the ARM client from session state
            
            if (SessionState.PSVariable.Get("AzRMClient").Value is ArmClient client)
            {
                WriteVerbose("[+] Successfully retrieved the ARM client from session state.");
                try
                {
                    // Get the subscription
                    if (string.IsNullOrEmpty(SubscriptionId))
                    {
                        WriteVerbose("[+] Getting default subscription...");
                        SubscriptionResource subscription = client.GetDefaultSubscriptionAsync().GetAwaiter().GetResult();
                        SubscriptionId = subscription.Data.SubscriptionId;
                    }
                    WriteVerbose($"[+] Using subscription: {SubscriptionId}...");
                    
                    WriteVerbose($"[+] Getting Azure Container App Job: {JobName}...");
                    ResourceIdentifier containerAppJobResourceId = ContainerAppJobResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, JobName);
                    ContainerAppJobResource containerAppJob = client.GetContainerAppJobResource(containerAppJobResourceId);
                    
                    // get the collection of this ContainerAppJobExecutionResource
                    ContainerAppJobExecutionCollection collection = containerAppJob.GetContainerAppJobExecutions();

                    // Get Access Token
                    var managedIdentityClientId = Environment.GetEnvironmentVariable("MANAGED_IDENTITY_CLIENT_ID");
                    AccessToken token;
                    if (string.IsNullOrEmpty(managedIdentityClientId))
                    {
                        // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet&preserve-view=true#defaultazurecredential
                        WriteVerbose("[+] Using ChainedTokenCredential: AzurePowerShellCredential -> AzureCliCredential -> ManagedIdentityCredential");
                        ChainedTokenCredential credential = new(new AzurePowerShellCredential(), new AzureCliCredential(), new ManagedIdentityCredential());
                        token = credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" })).GetAwaiter().GetResult();
                    }
                    else
                    {
                        // Use ManagedIdentityCredential with the provided client ID
                        WriteVerbose($"[+] Using ManagedIdentityCredential with identity: {managedIdentityClientId}");
                        ManagedIdentityCredential credential = new (managedIdentityClientId);
                        token = credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" })).GetAwaiter().GetResult();
                    }
                    
                    if(!string.IsNullOrEmpty(ExecutionName))
                    {
                        ContainerAppJobExecutionResource executionResource = collection.GetAsync(ExecutionName).GetAwaiter().GetResult();
                        var executionResourceData = executionResource.Get().Value.Data;

                        try
                        {
                            var executionDetails = RetrieveExecutionDetails(executionResourceData.Id, token);
                            WriteObject(executionDetails);
                        }
                        catch (HttpRequestException ex)
                        {
                            WriteError(new ErrorRecord(ex, "ApiCallFailed", ErrorCategory.OperationStopped, null));
                        }
                    }
                    else
                    {
                        List<string> containerAppJobExecutionsList = new List<string>();
                        foreach (ContainerAppJobExecutionResource item in collection)
                        {
                            var executionResourceData = item.Get().Value.Data;
                            try
                            {
                                var executionDetails = RetrieveExecutionDetails(executionResourceData.Id, token);
                                if (executionDetails != null)
                                {
                                    containerAppJobExecutionsList.Add(executionDetails);
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                WriteError(new ErrorRecord(ex, "ApiCallFailed", ErrorCategory.OperationStopped, null));
                            }
                        };
                        WriteObject(containerAppJobExecutionsList);
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
            else
            {
                WriteVerbose("[+] ARM client not found in session state.");
                ThrowTerminatingError(new ErrorRecord(
                    new PSInvalidOperationException("ARM client not found in session state."),
                    "ARMClientNotFound", ErrorCategory.ResourceUnavailable, null));
            }
        }

        // Method to retrieve execution details from the API using HttpClient
        private string? RetrieveExecutionDetails(string? executionId, AccessToken token)
        {
            var apiUrl = "https://management.azure.com";
            var apiVersion = "2023-04-01-preview";
            var executionDetailsUrl = $"{apiUrl}{executionId}?api-version={apiVersion}";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            // Make the API call to retrieve execution details
            WriteVerbose($"[+] Retrieving execution details for {executionId}..");
            var executionDetailsResponse = httpClient.GetAsync(executionDetailsUrl).GetAwaiter().GetResult();
            if (executionDetailsResponse.IsSuccessStatusCode)
            { 
                var jsonContent = executionDetailsResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                // Deserialize the JSON content
                var document = JsonDocument.Parse(jsonContent);
                // Serialize it with indentation for a pretty JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // This setting ensures the JSON is formatted with indentation
                };
                var prettyJson = JsonSerializer.Serialize(document.RootElement, options);
                return prettyJson;
            }
            else
            {
                // Handle the case where the API call was not successful
                throw new HttpRequestException("Failed to retrieve execution details from the API.");
            }
        }
    }
}