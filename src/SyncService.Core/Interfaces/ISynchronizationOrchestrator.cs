// This interface defines the main business logic service. It will be implemented
// in the Core layer and injected into the API Controller.

using System.Threading.Tasks;
using SyncService.Core.Models;

namespace SyncService.Core.Interfaces
{
    public interface ISynchronizationOrchestrator
    {
        Task<SyncResult> RunFullSyncAsync();
    }
}
