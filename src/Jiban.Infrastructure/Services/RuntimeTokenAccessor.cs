namespace Jiban.Infrastructure.Services
{
    /// <summary>
    /// Provides a runtime implementation of <see cref="ITokenAccessor"/> for managing the current authentication token.
    /// </summary>
    public class RuntimeTokenAccessor : ITokenAccessor
    {
        /// <summary>
        /// Stores the current authentication token in memory.
        /// </summary>
        private string? _token;

        /// <summary>
        /// Gets the current authentication token, if available.
        /// </summary>
        public string? CurrentToken => _token;

        /// <summary>
        /// Sets the current authentication token.
        /// </summary>
        /// <param name="token">The token to set as current.</param>
        public void SetToken(string token)
        {
            _token = token;
        }

        /// <summary>
        /// Clears the current authentication token.
        /// </summary>
        public void ClearToken()
        {
            _token = null;
        }
    }

}
