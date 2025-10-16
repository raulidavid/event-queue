namespace Jiban.Infrastructure.Services
{
    public class RuntimeTokenAccessor : ITokenAccessor
    {
        private string? _token;

        public string? CurrentToken => _token;

        public void SetToken(string token)
        {
            _token = token;
        }

        public void ClearToken()
        {
            _token = null;
        }
    }

}
