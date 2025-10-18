using Jiban.Infrastructure.Nswag;

namespace Jiban.Infrastructure.Services
{
    /// <summary>
    /// Provides functionality for sending electronic documents (for example, SRI documents)
    /// to recipients via email.
    /// </summary>
    public interface IElectronicDocService
    {
        /// <summary>
        /// Sends one or more documents described by <paramref name="documentEmailRequest"/> to the
        /// target email address asynchronously.
        /// </summary>
        /// <param name="documentEmailRequest">
        /// Request object containing the document identifier, document type and recipient email.
        /// See <see cref="DocumentEmailRequest"/> for details.
        /// </param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        /// <remarks>
        /// Implementations should validate the request and surface errors (for example, network
        /// or validation failures) by throwing appropriate exceptions or returning error results
        /// according to the calling convention used in the project.
        /// </remarks>
        Task SendEmailDocumentsAsync(DocumentEmailRequest documentEmailRequest);
    }
}