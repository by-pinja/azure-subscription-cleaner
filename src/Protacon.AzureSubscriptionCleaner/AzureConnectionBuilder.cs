using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Protacon.AzureSubscriptionCleaner
{
    public static class AzureConnectionBuilder
    {

        public static IAzure BuildConnection()
        {
            var msiInformation = new MSILoginInformation(MSIResourceType.AppService);

            var credentials = SdkContext.AzureCredentialsFactory.FromMSI(msiInformation, AzureEnvironment.AzureGlobalCloud);
            return Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
        }

        public static IAzure BuildServicePrincipalConnection(ServicePrincipalConfiguration servicePrincipal)
        {
            var credentials = SdkContext
                .AzureCredentialsFactory
                .FromServicePrincipal(servicePrincipal.ClientId, servicePrincipal.ClientSecret, servicePrincipal.TenantId, AzureEnvironment.AzureGlobalCloud);
            return Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();
        }
    }
}