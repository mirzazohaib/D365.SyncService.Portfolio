// This is the heart of our application's business logic.
// Notice it only depends on interfaces from the Core layer. It has no knowledge
// of the concrete implementations in the Infrastructure layer. This is the
// essence of Clean Architecture and makes this component highly testable.

using System;
using System.Linq;
using System.Threading.Tasks;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;
using Microsoft.Extensions.Logging; // Requires Microsoft.Extensions.Logging package



namespace SyncService.Core.Services
{
    public class SynchronizationOrchestrator : ISynchronizationOrchestrator
    {
        private readonly IExternalInventoryService _externalInventoryService;
        private readonly ID365DataverseConnector _d365Connector;
        private readonly ILogger<SynchronizationOrchestrator> _logger; // Add a logger

        // Constructor

        // Dependencies are injected via the constructor
        public SynchronizationOrchestrator(
            IExternalInventoryService externalInventoryService,
            ID365DataverseConnector d365Connector, ILogger<SynchronizationOrchestrator> logger) // Add the logger
        {
            _externalInventoryService = externalInventoryService;
            _d365Connector = d365Connector;
            _logger = logger; // Initialize the logger
        }

        public async Task<SyncResult> RunFullSyncAsync()
        {
            try
            {
                _logger.LogInformation("Starting full inventory synchronization..."); // Log the start of the operation

                // 1. Get data from the external system
                var externalInventory = await _externalInventoryService.GetCurrentInventoryAsync();

                // Handle potential null result from service defensively
                if (externalInventory == null)
                {
                    _logger.LogWarning("External inventory service returned null.");
                    return SyncResult.Failure("Failed to retrieve data from external system (null response).");
                }

                var productList = externalInventory.ToList(); // Convert to List for easier handling

                if (!productList.Any())
                {
                    _logger.LogInformation("No products found in the external system to synchronize.");
                    // Return success, but indicate 0 items processed
                    return SyncResult.Success(0);
                }

                // Replaced Console.WriteLine with LogInformation using structured logging)
                _logger.LogInformation("Fetched {ProductCount} items from external source.", productList.Count);

                // 2. Push the data to Dynamics 365
                var success = await _d365Connector.UpdateProductInventoryBatchAsync(productList);

                if (!success)
                {
                    // Error details should already be logged by the D365DataverseConnector
                    // Return a generic failure message suitable for the API consumer
                    _logger.LogWarning("D365 connector reported failure during batch update.");
                    return SyncResult.Failure("Failed to update records in Dynamics 365. Check service logs for details.");
                }

                // Replaced Console.WriteLine with LogInformation
                _logger.LogInformation("Synchronization completed successfully for {ProductCount} items.", productList.Count);
                return SyncResult.Success(productList.Count);
            }
            catch (Exception ex)
            {
                // Refined Error Handling: Log the full exception.
                // Replaced Console.WriteLine with LogError
                _logger.LogError(ex, "An unexpected error occurred during the synchronization orchestration.");

                // Return a generic failure message suitable for the API consumer.
                return SyncResult.Failure("An unexpected error occurred during synchronization. Check service logs for details.");
            }
        }
    }
}