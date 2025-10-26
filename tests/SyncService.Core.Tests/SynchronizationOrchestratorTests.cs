using Moq;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;
using SyncService.Core.Services;
using Microsoft.Extensions.Logging;


namespace SyncService.Core.Tests;

public class SynchronizationOrchestratorTests
{
    // Mocks for the dependencies of orchestrator
    private readonly Mock<IExternalInventoryService> _mockExternalInventoryService;
    private readonly Mock<ID365DataverseConnector> _mockD365Connector;

    // The actual class being tested
    private readonly SynchronizationOrchestrator _orchestrator;

    // Mock for the logger
    private readonly Mock<ILogger<SynchronizationOrchestrator>> _mockLogger; // Mock for the logger


    public SynchronizationOrchestratorTests()
    {
        // Arrange: This constructor runs before each test.
        // New mock instances created for each test to ensure they are isolated.
        _mockExternalInventoryService = new Mock<IExternalInventoryService>();
        _mockD365Connector = new Mock<ID365DataverseConnector>();
        _mockLogger = new Mock<ILogger<SynchronizationOrchestrator>>(); // Mock for the logger


        // A real instance is created of orchestrator, but pass it the MOCK dependencies.
        _orchestrator = new SynchronizationOrchestrator(
            _mockExternalInventoryService.Object,
            _mockD365Connector.Object,
            _mockLogger.Object // Pass the logger object to the orchestrator
        );
    }

    [Fact]
    public async Task RunFullSyncAsync_WhenNewProductsExist_ShouldCallUpsertAndSucceed()
    {
        // Arrange: Set up the scenario for this specific test.
        var productsFromExternalSystem = new List<ProductDto>
        {
            new ProductDto { Sku = "SKU001", QuantityOnHand = 10, LastModified = DateTime.UtcNow },
            new ProductDto { Sku = "SKU002", QuantityOnHand = 25, LastModified = DateTime.UtcNow }
        };

        // Tell mock external service to return our list of products when its method is called.
        _mockExternalInventoryService
            .Setup(s => s.GetCurrentInventoryAsync())
            .ReturnsAsync(productsFromExternalSystem);

        _mockD365Connector
            .Setup(c => c.UpdateProductInventoryBatchAsync(It.IsAny<IEnumerable<ProductDto>>()))
            .ReturnsAsync(true);

        // Act: Execute the method needs to be tested.
        var result = await _orchestrator.RunFullSyncAsync();

        // Assert: Verify that the outcome is what as expected.
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.ItemsProcessed);
        Assert.Null(result.ErrorMessage); // On success, there should be no error message.

        // Verify that orchestrator correctly called the D365 connector.
        _mockD365Connector.Verify(
            c => c.UpdateProductInventoryBatchAsync(productsFromExternalSystem),
            Times.Once
        );
    }

    [Fact]
    public async Task RunFullSyncAsync_WhenNoProductsExist_ShouldNotCallUpsertAndSucceed()
    {
        // Arrange: Set up a different scenario where the external system has no products.
        var emptyProductList = new List<ProductDto>();

        _mockExternalInventoryService
            .Setup(s => s.GetCurrentInventoryAsync())
            .ReturnsAsync(emptyProductList);

        // Act: Execute the method.
        var result = await _orchestrator.RunFullSyncAsync();

        // Assert: Verify the outcome.
        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.ItemsProcessed);
        Assert.Null(result.ErrorMessage);

        // Verify that the D365 connector's Upsert method was NEVER called,
        // because there was nothing to do. This is important for efficiency.
        _mockD365Connector.Verify(
            c => c.UpdateProductInventoryBatchAsync(It.IsAny<IEnumerable<ProductDto>>()),
            Times.Never
        );
    }

    // Optional: Add a test case for when the D365 connector fails
    [Fact]
    public async Task RunFullSyncAsync_WhenD365UpdateFails_ShouldReturnFailure()
    {
        // Arrange
         var productsFromExternalSystem = new List<ProductDto>
        {
            new ProductDto { Sku = "SKU001", QuantityOnHand = 10, LastModified = DateTime.UtcNow }
        };

         _mockExternalInventoryService
            .Setup(s => s.GetCurrentInventoryAsync())
            .ReturnsAsync(productsFromExternalSystem);

        // Simulate the D365 connector returning false (failure)
        _mockD365Connector
            .Setup(c => c.UpdateProductInventoryBatchAsync(It.IsAny<IEnumerable<ProductDto>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _orchestrator.RunFullSyncAsync();

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(0, result.ItemsProcessed); // Should be 0 on failure
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Failed to update product batch", result.ErrorMessage); // Check error message

        _mockD365Connector.Verify(
            c => c.UpdateProductInventoryBatchAsync(productsFromExternalSystem),
            Times.Once // Verify it was still called once
        );
    }
}

