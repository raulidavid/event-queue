using Jiban.Nswag;

namespace Jiban.Infrastructure.HostedServices
{
    public partial class ElectronicDocHostedService
    {
        public async Task SendEmailDocumentsAsync(DocumentEmailRequest documentEmailRequest)
        {
            await _electronicDocService.SendEmailDocumentsAsync(documentEmailRequest);
        }
    }
}