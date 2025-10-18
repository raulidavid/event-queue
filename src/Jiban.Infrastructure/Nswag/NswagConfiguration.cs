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
        /// Gets or sets the NSwag API base URL.
        /// </summary>
        /// <value>
        /// The base URL read from configuration key "NswagBaseUrl". May be absolute or relative.
        /// </value>
        public string _baseUrl { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HttpClient"/> used to send requests to NSwag APIs.
        /// </summary>
        /// <value>An initialized <see cref="HttpClient"/>, typically provided by dependency injection.</value>
        public HttpClient _httpClient { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="NswagConfiguration"/>.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> to use for API requests.</param>
        /// <param name="configuration">Application configuration used to read the NSwag base URL (key "NswagBaseUrl").</param>
        /// <remarks>
        /// The constructor reads the "NswagBaseUrl" configuration value and assigns it to <see cref="_baseUrl"/>.
        /// If the configuration key is missing or invalid an exception is thrown.
        /// </remarks>
        public NswagConfiguration(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            var baseUrl = configuration[JibanConstants.NswagBaseUrl];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException($"Configuration value '{JibanConstants.NswagBaseUrl}' is missing or empty.");
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException($"Configuration value '{JibanConstants.NswagBaseUrl}' is not a valid absolute URI: '{baseUrl}'.");
            }

            _httpClient.BaseAddress = baseUri;
            _baseUrl = baseUrl;
        }
    }
}
