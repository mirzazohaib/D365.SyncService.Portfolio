using Microsoft.Extensions.Configuration; // Requires Microsoft.Extensions.Configuration.Abstractions package
using Microsoft.Extensions.Options; // Requires Microsoft.Extensions.Options package
using Microsoft.Identity.Client;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;
using SyncService.Infrastructure.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json; // Required for GetFromJsonAsync, PostAsJsonAsync etc.

namespace SyncService.Infrastructure.Services
{
    /// Implements the ID365DataverseConnector interface to interact with the D365 Web API.
    public class D365DataverseConnector : ID365DataverseConnector
    {
        private readonly HttpClient _httpClient;
        private readonly D365Config _d365Config;
        private readonly IConfidentialClientApplication _msalClient;

        // Using IOptions<D365Config> to access strongly-typed configuration.
        // Using IHttpClientFactory for managing HttpClient instances.
        // Requires Microsoft.Extensions.Http package for HttpClientFactory.
        public D365DataverseConnector(IOptions<D365Config> d365ConfigOptions, IHttpClientFactory httpClientFactory)
        {
            _d365Config = d365ConfigOptions.Value; // Get the configured D365 settings
            _httpClient = httpClientFactory.CreateClient("D365Client"); // Use a named client

            // Validate required configuration
            if (string.IsNullOrWhiteSpace(_d365Config.EnvironmentUrl) ||
                string.IsNullOrWhiteSpace(_d365Config.ClientId) ||
                string.IsNullOrWhiteSpace(_d365Config.ClientSecret) ||
                string.IsNullOrWhiteSpace(_d365Config.TenantId))
            {
                throw new InvalidOperationException("D365 configuration is missing required values in appsettings.");
            }

            // Initialize the MSAL client application for authentication
            _msalClient = ConfidentialClientApplicationBuilder
                .Create(_d365Config.ClientId)
                .WithClientSecret(_d365Config.ClientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_d365Config.TenantId}"))
                .Build();
        }

        /// Authenticates using MSAL and configures the HttpClient instance.
        /// Should be called before making any API requests.
        private async Task InitializeHttpClientAsync()
        {
            // Define the scope required to access the D365 API
            // The scope is typically your EnvironmentUrl + "/.default"
            string[] scopes = new[] { $"{_d365Config.EnvironmentUrl?.TrimEnd('/')}/.default" };

            AuthenticationResult? authResult = null;
            try
            {
                // Acquire token using client credentials flow
                authResult = await _msalClient.AcquireTokenForClient(scopes).ExecuteAsync();
            }
            catch (MsalServiceException ex)
            {
                // Handle token acquisition failure (log error, throw specific exception)
                // For simplicity, we re-throw, but real apps should log details.
                // Consider adding logging here later
                throw new InvalidOperationException($"Failed to acquire D365 authentication token: {ex.Message}", ex);
            }

            if (authResult == null || string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                throw new InvalidOperationException("Failed to acquire D365 authentication token (result was null or empty).");
            }

            // Set the base address for the HttpClient if not already set by factory
            if (_httpClient.BaseAddress == null)
            {
                 _httpClient.BaseAddress = new Uri(_d365Config.ApiUrl); // Use the calculated API URL
            }

            // Clear default headers and add the Authorization header with the acquired token
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

            // Ensure other standard headers are present (some might be set by factory)
            if(!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            }
             if(!_httpClient.DefaultRequestHeaders.Contains("OData-MaxVersion"))
            {
                _httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            }
            if(!_httpClient.DefaultRequestHeaders.Contains("OData-Version"))
            {
                _httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
            }

            // Optional: Configure timeout if not set by factory
             if (_httpClient.Timeout == TimeSpan.FromSeconds(100)) // Default timeout is often 100s
             {
                 _httpClient.Timeout = TimeSpan.FromSeconds(60); // Example: 60 second timeout
             }
        }

        /// Attempts to update or create a batch of Product records in D365 Dataverse using the Web API.
        /// Sends individual PATCH requests for each product for simplicity.
        public async Task<bool> UpdateProductInventoryBatchAsync(IEnumerable<ProductDto> products)
        {
            await InitializeHttpClientAsync(); // Ensure HttpClient is configured with a valid token

            // IMPORTANT: Replace with the actual Logical names from your D365 environment
            string entitySetName = "cr62d_productinventories"; // Plural Logical name of your table
            string keyFieldName = "cr62d_sku"; // Logical name of the SKU field (your alternate key)
            string quantityFieldName = "cr62d_quantityonhand"; // Logical name of the Quantity field
            string lastModFieldName = "cr62d_lastmodifiedexternal"; // Logical name of the Last Modified field

            bool allSucceeded = true; // Track overall success

            foreach (var product in products)
            {
                // Construct the D365 payload. Keys must be the Logical names of the columns.
                var payload = new Dictionary<string, object>
                {
                    { keyFieldName, product.Sku }, // Include the key field itself in the payload
                    { quantityFieldName, product.QuantityOnHand },
                    { lastModFieldName, product.LastModified }
                };

                // Construct the URI for the PATCH request using the alternate key
                // Format: /entitysetname(keyfieldname='keyvalue')
                string requestUri = $"{entitySetName}({keyFieldName}='{Uri.EscapeDataString(product.Sku)}')";

                try
                {
                    // Send PATCH request. PATCH performs an Upsert when using an alternate key.
                    HttpResponseMessage response = await _httpClient.PatchAsJsonAsync(requestUri, payload);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Log the failure details
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"INFRA: Failed to update SKU {product.Sku}. Status: {response.StatusCode}. Reason: {errorContent}");
                        // Log.Error("Failed to update SKU {Sku}. Status: {StatusCode}. Reason: {ErrorContent}",
                        //     product.Sku, response.StatusCode, errorContent);

                        allSucceeded = false; // Mark the overall operation as failed
                        // Depending on requirements, you might continue processing others or break here.
                        // continue;
                        break; // Stop on first failure for simplicity
                    }
                    else
                    {
                         // Optionally log success or process the returned record representation if needed
                        Console.WriteLine($"INFRA: Successfully updated/created SKU {product.Sku}. Status: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"INFRA: HTTP request failed for SKU {product.Sku}: {httpEx.Message}");
                    // Log.Error(httpEx, "HTTP request failed for SKU {Sku}", product.Sku);
                    allSucceeded = false;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"INFRA: An unexpected error occurred processing SKU {product.Sku}: {ex.Message}");
                    // Log.Error(ex, "Unexpected error processing SKU {Sku}", product.Sku);
                    allSucceeded = false;
                    break;
                }
            }

            return allSucceeded;
        }
    }
}

