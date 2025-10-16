namespace Jiban.Infrastructure.Services
{
    public interface IElectronicDocService
    {
        Task ProcessElectronicDocumentsAsync(string identificacion, CancellationToken cancellationToken);
    }
}