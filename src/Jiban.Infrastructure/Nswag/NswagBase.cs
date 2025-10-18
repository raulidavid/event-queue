namespace Jiban.Infrastructure.Nswag
{
    public class NswagBase
    {
        protected readonly NswagConfiguration _nswagConfiguration;
        protected readonly HttpClient _httpClient;

        public NswagBase(NswagConfiguration nswagConfiguration)
        {
            _nswagConfiguration = nswagConfiguration;
            _httpClient = _nswagConfiguration._httpClient;
        }
    }
}