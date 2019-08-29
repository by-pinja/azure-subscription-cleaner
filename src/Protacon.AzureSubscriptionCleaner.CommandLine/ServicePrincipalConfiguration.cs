namespace Protacon.AzureSubscriptionCleaner.CommandLine
{
    public class ServicePrincipalConfiguration
    {
        /// <summary>
        /// Tenant ID (Guid), this can be retrieved from Azure AD
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Client ID (Guid), this can be retreived from Azure AD
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Client secret / password, this can be retrieved from Azure AD
        /// when application / service principal is created
        /// </summary>
        public string ClientSecret { get; }

        public ServicePrincipalConfiguration(string tenantId, string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new System.ArgumentException("Tenant ID is required.", nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new System.ArgumentException("Client ID is required.", nameof(clientId));
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new System.ArgumentException("Client secret is required", nameof(clientSecret));
            }

            TenantId = tenantId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }
}