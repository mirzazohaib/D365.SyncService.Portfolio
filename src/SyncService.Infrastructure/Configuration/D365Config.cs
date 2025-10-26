namespace SyncService.Infrastructure.Configuration
{
    public class D365Config
    {
        public string EnvironmentUrl { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "v9.2"; // Default D365 API version
        // Calculated property for the full API base URL
        public string ApiUrl => $"{EnvironmentUrl?.TrimEnd('/')}/api/data/{ApiVersion}/";
    }
}
