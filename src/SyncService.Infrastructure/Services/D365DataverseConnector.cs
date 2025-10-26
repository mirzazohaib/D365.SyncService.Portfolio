using Microsoft.Extensions.Configuration; // Requires Microsoft.Extensions.Configuration.Abstractions package
using Microsoft.Extensions.Options; // Requires Microsoft.Extensions.Options package
using Microsoft.Identity.Client;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;
using SyncService.Infrastructure.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json; // Required for GetFromJsonAsync, PostAsJsonAsync etc.
using System.Text.Json; // Required for JsonSerializerOptions
using Microsoft.Extensions.Logging; // Required for ILogger, Requires Microsoft.Extensions.Logging package


namespace SyncService.Infrastructure.Services
{
    /// Implements the ID365DataverseConnector interface to interact with the D365 Web API.
    public class D365DataverseConnector : ID365DataverseConnector
    {
        private readonly HttpClient _httpClient;
        private readonly D365Config _d365Config;
        private readonly IConfidentialClientApplication _msalClient;
        private readonly ILogger<D365DataverseConnector> _logger; // Add a logger

        // Constructor

        // Using IOptions<D365Config> to access strongly-typed configuration.
        // Using IHttpClientFactory for managing HttpClient instances.
        // Requires Microsoft.Extensions.Http package for HttpClientFactory.
        public D365DataverseConnector(IOptions<D365Config> d365ConfigOptions, IHttpClientFactory httpClientFactory, ILogger<D365DataverseConnector> logger) // Add the logger
        {
            _d365Config = d365ConfigOptions.Value; // Get the configured D365 settings
            _httpClient = httpClientFactory.CreateClient("D365Client"); // Use a named client
            _logger = logger; // Logger injected


            // Validate required configuration
            if (string.IsNullOrWhiteSpace(_d365Config.EnvironmentUrl) ||
                string.IsNullOrWhiteSpace(_d365Config.ClientId) ||
                string.IsNullOrWhiteSpace(_d365Config.ClientSecret) ||
                string.IsNullOrWhiteSpace(_d365Config.TenantId))
            {
                // Log error before throwing
                _logger.LogError("D365 configuration is missing required values in appsettings.");
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
                _logger.LogInformation("Attempting to acquire D365 authentication token.");
                // Acquire token using client credentials flow
                authResult = await _msalClient.AcquireTokenForClient(scopes).ExecuteAsync();
                _logger.LogInformation("Successfully acquired D365 authentication token.");
            }
            catch (MsalServiceException ex)
            {
                // Log the specific error from MSAL
                _logger.LogError(ex, "Failed to acquire D365 authentication token due to MSAL service exception.");
                throw new InvalidOperationException($"Failed to acquire D365 authentication token: {ex.Message}", ex);
            }

            if (authResult == null || string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                _logger.LogError("Failed to acquire D365 authentication token (result was null or empty).");
                throw new InvalidOperationException("Failed to acquire D365 authentication token (result was null or empty).");
            }

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_d365Config.ApiUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            _httpClient.DefaultRequestHeaders.Accept.Clear(); // Clear accept headers before adding
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Remove("OData-MaxVersion"); // Remove before adding
            _httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            _httpClient.DefaultRequestHeaders.Remove("OData-Version"); // Remove before adding
            _httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
            _httpClient.DefaultRequestHeaders.Remove("Prefer"); // Remove before adding
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

            if (_httpClient.Timeout == TimeSpan.FromSeconds(100))
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(60);
            }
            _logger.LogInformation("HttpClient configured successfully for D365 API calls.");
        }

        /// <summary>
        /// Attempts to update or create a batch of Product records in D365 Dataverse using the Web API.
        /// </summary>
        public async Task<bool> UpdateProductInventoryBatchAsync(IEnumerable<ProductDto> products)
        {
            // IMPORTANT: Replace with the actual Logical names from your D365 environment
            string entitySetName = "cr62d_productinventories"; // Plural Logical name of your table
            string keyFieldName = "cr62d_sku"; // Logical name of the SKU field (your alternate key)
            string quantityFieldName = "cr62d_quantityonhand"; // Logical name of the Quantity field
            string lastModFieldName = "cr62d_lastmodifiedexternal"; // Logical name of the Last Modified field

            bool allSucceeded = true;

            try
            {
                await InitializeHttpClientAsync();
                _logger.LogInformation("Starting D365 product inventory batch update for {ProductCount} items.", products.Count());

                foreach (var product in products)
                {
                    var payload = new Dictionary<string, object>
                    {
                        { quantityFieldName, product.QuantityOnHand },
                        { lastModFieldName, product.LastModified.ToUniversalTime() }
                    };

                    string requestUri = $"{entitySetName}({keyFieldName}='{Uri.EscapeDataString(product.Sku)}')";
                    _logger.LogInformation("Sending PATCH request for SKU {Sku} to URI {RequestUri}", product.Sku, requestUri);

                    try
                    {
                        HttpResponseMessage response = await _httpClient.PatchAsJsonAsync(requestUri, payload);

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            // Log using structured logging
                            _logger.LogError("Failed to update SKU {Sku}. Status: {StatusCode}. Reason: {ErrorContent}",
                                             product.Sku, response.StatusCode, errorContent);
                            allSucceeded = false;
                            break; // Stop on first failure
                        }
                        else
                        {
                            // Log success using structured logging
                            _logger.LogInformation("Successfully updated/created SKU {Sku}. Status: {StatusCode}",
                                                   product.Sku, response.StatusCode);
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        // Log using structured logging and pass exception
                        _logger.LogError(httpEx, "HTTP request failed for SKU {Sku}", product.Sku);
                        allSucceeded = false;
                        break;
                    }
                    catch (JsonException jsonEx)
                    {
                        // Log using structured logging and pass exception
                        _logger.LogError(jsonEx, "JSON serialization failed for SKU {Sku}", product.Sku);
                        allSucceeded = false;
                        break;
                    }
                } // End foreach loop

                if(allSucceeded)
                {
                    _logger.LogInformation("D365 product inventory batch update completed successfully.");
                }
                else
                {
                     _logger.LogWarning("D365 product inventory batch update completed with one or more failures.");
                }
            }
            catch (InvalidOperationException authEx)
            {
                // Error already logged in InitializeHttpClientAsync
                _logger.LogError(authEx, "Authentication failed during batch update initialization.");
                return false; // Overall failure
            }
            catch (Exception ex)
            {
                // Log using structured logging and pass exception
                _logger.LogError(ex, "An unexpected error occurred during the D365 update batch.");
                return false; // Indicate overall failure
            }

            return allSucceeded;
        }
    }
}
