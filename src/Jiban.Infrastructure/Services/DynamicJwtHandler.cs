using System.Net.Http.Headers;

namespace Jiban.Infrastructure.Services
{
    public class DynamicJwtHandler : DelegatingHandler
    {
        private readonly ITokenAccessor _tokenAccessor;

        public DynamicJwtHandler(ITokenAccessor tokenAccessor)
        {
            _tokenAccessor = tokenAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _tokenAccessor.CurrentToken;

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
