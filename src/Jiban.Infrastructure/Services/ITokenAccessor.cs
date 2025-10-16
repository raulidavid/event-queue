namespace Jiban.Infrastructure.Services
{
    public interface ITokenAccessor
    {
        string? CurrentToken { get; }
        void SetToken(string token);
        void ClearToken();
    }
}