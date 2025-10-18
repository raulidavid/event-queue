using Jiban.Infrastructure.Nswag;

namespace Jiban.Infrastructure.Services
{
    /// <summary>
    /// Service for processing electronic documents, handling authentication and account transfers.
    /// </summary>
    public class ElectronicDocService : IElectronicDocService
    {
        private readonly ISriClient _sriClient;
        private readonly ITokenAccessor _tokenAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElectronicDocService"/> class.
        /// </summary>
        /// <param name="sriClient">Client for account-related operations.</param>
        /// <param name="tokenAccessor">Accessor for managing authentication tokens.</param>
        public ElectronicDocService(ISriClient sriClient, ITokenAccessor tokenAccessor)
        {
            _sriClient = sriClient;
            _tokenAccessor = tokenAccessor;
        }

        public async Task SendEmailDocumentsAsync(DocumentEmailRequest documentEmailRequest)
        {
            // Assume token is already set by the caller (e.g., the queue/hosted service).
            // If needed, you could validate it's present:
            if (string.IsNullOrWhiteSpace(_tokenAccessor.CurrentToken))
                throw new InvalidOperationException("JWT token not set in ITokenAccessor.");

            await _sriClient.SendEmailDocumentAsync(documentEmailRequest);
        }
    }
}