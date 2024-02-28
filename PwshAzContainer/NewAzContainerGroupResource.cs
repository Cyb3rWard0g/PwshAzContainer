using System.Management.Automation;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Models;

namespace PwshAzContainerApp
{
    [Cmdlet(VerbsCommon.New, "AzContainerGroupResource")]
    [OutputType(typeof(ContainerGroupData))]
    public class NewAzContainerGroupResource : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string? Name { get; set; }

        [Parameter(Mandatory = true)]
        public string? ResourceGroupName { get; set; }

        [Parameter]
        public string? SubscriptionId { get; set; }

        [Parameter]
        [ValidateSet("Linux","Windows")]
        public ContainerInstanceOperatingSystemType OsType { get; set; } = "Linux";

        [Parameter]
        [ValidateSet("Always","OnFailure","Never")]
        public ContainerGroupRestartPolicy RestartPolicy { get; set; } = "Always";

        [Parameter]
        [ValidateSet("Standard","Confidential","Dedicated")]
        public ContainerGroupSku Sku { get; set; } = "Standard";

        [Parameter]
        [ValidateSet("Public","Private")]
        public ContainerGroupIPAddressType IpAddressType { get; set; } = "Public";

        [Parameter]
        public List<ContainerGroupPort>? Ports { get; set; }

        [Parameter()]
        public List<ContainerGroupImageRegistryCredential>? ImageRegistryCredential { get; set; }

        [Parameter(Mandatory = true)]
        public ContainerInstanceContainer? ContainerInstance { get; set;}

        [Parameter()]
        public string Location { get; set; } = "East US";

        [Parameter()]
        public string? Identity { get; set; }

        [Parameter()]
        [ValidateSet("NoReuse","ResourceGroupReuse","SubscriptionReuse","TenantReuse","Unsecure")]
        public DnsNameLabelReusePolicy? DnsNameLabelReusePolicy { get; set; } = "NoReuse";

        [Parameter()]
        public string? DnsNameLabel { get; set; }

        [Parameter()]
        public string? SubnetId { get; set; }

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

                    WriteVerbose($"[+] Getting Resource Group: {ResourceGroupName}");
                    ResourceIdentifier resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName);
                    ResourceGroupResource resourceGroupResource = client.GetResourceGroupResource(resourceGroupResourceId);

                    // get the collection of this ContainerGroupResource
                    WriteVerbose("[+] Getting Azure Container Groups...");
                    ContainerGroupCollection collection = resourceGroupResource.GetContainerGroups();

                    WriteVerbose("[+] Defining Container Group Data...");
                    WriteVerbose($"[+] Container Group Name: {Name}");
                    string ContainerGroupName = Name;
                    var groupData = new ContainerGroupData(new AzureLocation(Location), new ContainerInstanceContainer[]
                        {ContainerInstance},
                        OsType
                    )
                    {
                        OSType = OsType,
                        RestartPolicy = RestartPolicy,
                        IPAddress = new ContainerGroupIPAddress(
                            new ContainerGroupPort[]{},
                            IpAddressType
                        )
                    };

                    // Conainer Group Image Registry Credential
                    if (ImageRegistryCredential != null && ImageRegistryCredential.Count > 0)
                    {
                        WriteVerbose("[+] Adding image registry credentials...");
                        foreach (var credential in ImageRegistryCredential)
                        {
                            groupData.ImageRegistryCredentials.Add(credential);
                        }
                    }

                    // Identity
                    if(!string.IsNullOrEmpty(Identity))
                    {
                        ManagedServiceIdentity managedIdentity;

                        if(Identity == "system")
                        {
                            managedIdentity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned);
                        }
                        else
                        {
                            managedIdentity = new ManagedServiceIdentity(ManagedServiceIdentityType.UserAssigned);
                            managedIdentity.UserAssignedIdentities.Add(new ResourceIdentifier(Identity), new UserAssignedIdentity());
                        }
                        groupData.Identity = managedIdentity;
                    }

                    // Group Ports
                    if (Ports != null && Ports.Count > 0)
                    {
                        WriteVerbose("[+] Adding Group Ports...");
                        foreach (var port in Ports)
                        {
                            WriteVerbose($"[+] Port {port.Port} over {port.Protocol}...");
                            groupData.IPAddress.Ports.Add(port);
                        }
                    }

                    // SubnetId for Private IP Address types
                    if (IpAddressType.ToString() == "Private" && SubnetId != null )
                    {
                        WriteVerbose($"[+] Azure local subnet: {SubnetId}");
                        groupData.SubnetIds.Add(new ContainerGroupSubnetId(new ResourceIdentifier(SubnetId)));
                    }

                    if (IpAddressType.ToString() == "Public")
                    {
                        string dnsPrefix;
                        if (!string.IsNullOrEmpty(DnsNameLabel))
                        {
                            dnsPrefix = DnsNameLabel;
                            groupData.IPAddress.DnsNameLabel = DnsNameLabel;
                        }
                        else
                        {
                            dnsPrefix = ContainerGroupName;
                            groupData.IPAddress.DnsNameLabel = ContainerGroupName;
                        }
                        WriteVerbose($"[+] DnsNameLabel: {dnsPrefix}");
                        
                        var policy = DnsNameLabelReusePolicy.ToString();
                        WriteVerbose($"[+] DnsNameLabelReusePolicy: {policy}");
                        groupData.IPAddress.AutoGeneratedDomainNameLabelScope = DnsNameLabelReusePolicy;
                    }

                    WriteVerbose("[+] Creating Azure Container Group...");
                    ArmOperation<ContainerGroupResource> lro = collection.CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, groupData).GetAwaiter().GetResult();
                    ContainerGroupResource result = lro.Value;
                    WriteVerbose("[+] Azure Container Group creation Results:");
                    WriteObject(result);
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
    }
}