using System.Threading;
using System.Threading.Tasks;
using Take.Blip.Builder;

namespace Builder.Flow.Tests.Interface
{
    public class MemoryStateManager : IStateManager
    {
        public string State;
        public string PreviousStateId;

        public MemoryStateManager()
        {
            State = string.Empty;
            PreviousStateId = string.Empty;
        }

        public async Task DeleteStateIdAsync(IContext context, CancellationToken cancellationToken)
        {
            State = string.Empty;
        }

        public async Task<string> GetPreviousStateIdAsync(IContext context, CancellationToken cancellationToken)
        {
            return PreviousStateId;
        }

        public async Task<string> GetStateIdAsync(IContext context, CancellationToken cancellationToken)
        {
            return State;
        }

        public async Task SetPreviousStateIdAsync(IContext context, string previousStateId, CancellationToken cancellationToken)
        {
            PreviousStateId = previousStateId;
        }

        public async Task SetStateIdAsync(IContext context, string stateId, CancellationToken cancellationToken)
        {
            State = stateId;
        }
    }
}
