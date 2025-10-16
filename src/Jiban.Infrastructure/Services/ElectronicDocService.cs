using Jiban.Nswag;

namespace Jiban.Infrastructure.Services
{
    /// <summary>
    /// Service for processing electronic documents, handling authentication and account transfers.
    /// </summary>
    public class ElectronicDocService : IElectronicDocService
    {
        private readonly IAccountClient _accountClient;
        private readonly ITokenAccessor _tokenAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElectronicDocService"/> class.
        /// </summary>
        /// <param name="accountClient">Client for account-related operations.</param>
        /// <param name="tokenAccessor">Accessor for managing authentication tokens.</param>
        public ElectronicDocService(IAccountClient accountClient, ITokenAccessor tokenAccessor)
        {
            _accountClient = accountClient;
            _tokenAccessor = tokenAccessor;
        }

        /// <summary>
        /// Processes electronic documents for a given identification, handling authentication and account transfer.
        /// </summary>
        /// <param name="identificacion">The identification of the user or entity.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        public async Task ProcessElectronicDocumentsAsync(string identificacion, CancellationToken cancellationToken)
        {
            // Assume token is already set by the caller (e.g., the queue/hosted service).
            // If needed, you could validate it's present:
            if (string.IsNullOrWhiteSpace(_tokenAccessor.CurrentToken))
                throw new InvalidOperationException("JWT token not set in ITokenAccessor.");

            // Initiate account transfer using the provided payload.
            await _accountClient.AccountTransferCreateAsync(new AccountTransferCreateRequest
            {
                // Populate payload properties as required.
            }, cancellationToken);
        }
    }
}