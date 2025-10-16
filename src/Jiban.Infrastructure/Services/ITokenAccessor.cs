namespace Jiban.Infrastructure.Services
{
    /// <summary>
    /// Provides access and management for the current authentication token within the application context.
    /// </summary>
    public interface ITokenAccessor
    {
        /// <summary>
        /// Gets the current authentication token, if available.
        /// </summary>
        string? CurrentToken { get; }

        /// <summary>
        /// Sets the current authentication token.
        /// </summary>
        /// <param name="token">The token to set as current.</param>
        void SetToken(string token);

        /// <summary>
        /// Clears the current authentication token.
        /// </summary>
        void ClearToken();
    }
}