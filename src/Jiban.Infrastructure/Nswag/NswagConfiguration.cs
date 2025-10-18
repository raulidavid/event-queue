using Jiban.BaseCode.PermissionsCode;
using Microsoft.Extensions.Configuration;

namespace Jiban.Infrastructure.Nswag
{
    /// <summary>
    /// Provides configuration used for NSwag-generated API calls.
    /// Contains the NSwag API base URL and the <see cref="HttpClient"/> used to make requests.
    /// </summary>
    public class NswagConfiguration
    {
        /// <summary>
        /// Gets or sets the <see cref="HttpClient"/> used to send requests to NSwag APIs.
        /// </summary>
        /// <value>An initialized <see cref="HttpClient"/>, typically provided by dependency injection.</value>
        public HttpClient _httpClient { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="NswagConfiguration"/>.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> to use for API requests.</param>
        public NswagConfiguration(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
    }
}
