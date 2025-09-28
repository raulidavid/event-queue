using System;
using System.Collections.Generic;
using System.Text;

namespace Jiban.Infrastructure.Services
{
    public interface IElectronicDocService
    {
        Task ProcessElectronicDocumentsAsync(string identificacion, CancellationToken cancellationToken);
    }
}