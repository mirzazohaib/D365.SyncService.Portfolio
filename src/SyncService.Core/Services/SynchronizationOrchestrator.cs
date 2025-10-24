// This is the heart of our application's business logic.
// Notice it only depends on interfaces from the Core layer. It has no knowledge
// of the concrete implementations in the Infrastructure layer. This is the
// essence of Clean Architecture and makes this component highly testable.

using System;
using System.Linq;
using System.Threading.Tasks;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;

namespace SyncService.Core.Services
{
    public class SynchronizationOrchestrator : ISynchronizationOrchestrator
    {
        private readonly IExternalInventoryService _externalInventoryService;
        private readonly ID365DataverseConnector _d365Connector;

        // Dependencies are injected via the constructor
        public SynchronizationOrchestrator(
            IExternalInventoryService externalInventoryService,
            ID365DataverseConnector d365Connector)
        {
            _externalInventoryService = externalInventoryService;
            _d365Connector = d365Connector;
        }

        public async Task<SyncResult> RunFullSyncAsync()
        {
            try
            {
                Console.WriteLine("CORE: Starting full inventory synchronization...");

                // 1. Get data from the external system
                var externalInventory = await _externalInventoryService.GetCurrentInventoryAsync();

                if (externalInventory == null || !externalInventory.Any())
                {
                    return SyncResult.Success(0); // No items to process
                }

                // In a real application, you would add more complex business logic here:
                // - Compare against existing D365 records to avoid unnecessary updates.
                // - Transform or validate data.
                // - Handle records that exist in D365 but not in the external system.
                Console.WriteLine($"CORE: Fetched {externalInventory.Count()} items from external source.");

                // 2. Push the data to Dynamics 365
                var success = await _d365Connector.UpdateProductInventoryBatchAsync(externalInventory);

                if (!success)
                {
                    return SyncResult.Failure("Failed to update records in Dynamics 365. See logs for details.");
                }

                Console.WriteLine("CORE: Synchronization completed successfully.");
                return SyncResult.Success(externalInventory.Count());
            }
            catch (Exception ex)
            {
                // Professional error handling: log the exception (not shown) and return a clean failure message.
                Console.WriteLine($"CORE: An unexpected error occurred: {ex.Message}");
                return SyncResult.Failure("An unexpected error occurred during synchronization.");
            }
        }
    }
}
