// This is the starting point for the Web API, which will expose the single
// endpoint used to trigger the synchronization process.

using Microsoft.AspNetCore.Mvc;
using SyncService.Core.Interfaces;
using SyncService.Core.Models;
using System.Threading.Tasks;

namespace SyncService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly ISynchronizationOrchestrator _orchestrator;

        // The ISynchronizationOrchestrator is injected via the constructor by the DI container.
        public SyncController(ISynchronizationOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        /// Triggers a full synchronization of product inventory from the external system to D365.
        /// The result of the synchronization operation
        [HttpPost("trigger")] // Use "trigger" as the route
        [ProducesResponseType(typeof(SyncResult), 200)]
        [ProducesResponseType(typeof(SyncResult), 500)]
        public async Task<IActionResult> TriggerSync()
        {
            var result = await _orchestrator.RunFullSyncAsync();

            if (result.IsSuccessful)
            {
                // On success, return an HTTP 200 OK with the SyncResult object.
                return Ok(result);
            }

            // On failure, return an HTTP 500 Internal Server Error with the SyncResult object,
            // which contains the error details.
            return StatusCode(500, result);
        }
    }
}