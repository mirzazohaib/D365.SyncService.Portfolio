// This is our simulated D365 Dataverse connector. In a real-world project,
// this class would use the official Microsoft.Xrm.Sdk or DataverseClient
// to perform CRUD (Create, Read, Update, Delete) operations.
// Our mock simply logs the action and returns success, fulfilling the contract.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;

namespace SyncService.Infrastructure.Services
{
    public class MockD365DataverseConnector : ID365DataverseConnector
    {
        public Task<bool> UpdateProductInventoryBatchAsync(IEnumerable<ProductDto> products)
        {
            // Simulate connecting to the Dataverse and sending a batch update request.
            // In a real application, this would involve building an EntityCollection
            // and using an ExecuteMultipleRequest for bulk operations.
            Console.WriteLine($"INFRASTRUCTURE: Sending batch update to D365 Dataverse for {products.Count()} products...");

            foreach (var product in products)
            {
                Console.WriteLine($"  - Updating SKU: {product.Sku}, New Quantity: {product.QuantityOnHand}");
            }

            // Simulate a successful operation. We could add logic here to simulate failures.
            return Task.FromResult(true);
        }
    }
}
