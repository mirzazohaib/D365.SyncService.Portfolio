// This is our simulated external inventory system. In a real-world scenario,
// this class would contain HttpClient logic to call a REST API.
// For our portfolio project, it simply returns a hardcoded list of products,
// demonstrating our ability to adhere to the IExternalInventoryService contract.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;

namespace SyncService.Infrastructure.Services
{
    public class MockExternalInventoryService : IExternalInventoryService
    {
        public Task<IEnumerable<ProductDto>> GetCurrentInventoryAsync()
        {
            // Simulate an API call that returns a list of products.
            // In a real application, this would involve an HttpClient request and JSON deserialization.
            Console.WriteLine("INFRASTRUCTURE: Fetching data from mock external warehouse API...");

            var mockInventory = new List<ProductDto>
            {
                new ProductDto { Sku = "PROD-001", QuantityOnHand = 150, LastModified = DateTime.UtcNow.AddHours(-2) },
                new ProductDto { Sku = "PROD-002", QuantityOnHand = 50, LastModified = DateTime.UtcNow.AddHours(-1) },
                new ProductDto { Sku = "PROD-003", QuantityOnHand = 0, LastModified = DateTime.UtcNow.AddMinutes(-30) },
                new ProductDto { Sku = "PROD-004", QuantityOnHand = 2500, LastModified = DateTime.UtcNow.AddDays(-1) }
            };

            // Task.FromResult is used to return an already completed task, simulating an async operation.
            return Task.FromResult(mockInventory.AsEnumerable());
        }
    }
}
