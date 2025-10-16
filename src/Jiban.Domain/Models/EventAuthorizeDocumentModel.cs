namespace Jiban.Domain.Models;

/// <summary>
/// Represents the data required to authorize an event document.
/// </summary>
public class EventAuthorizeDocumentModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the user making the request.
    /// This property is used to associate the request with a specific user,
    /// enabling user-level authorization, auditing, and tracking.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public int IdSolicitud { get; set; }

    /// <summary>
    /// Gets or sets the request detail ID.
    /// </summary>
    public int IdSolicitudDetalle { get; set; }

    /// <summary>
    /// Gets or sets the request type ID.
    /// </summary>
    public int IdTipoSolicitud { get; set; }

    /// <summary>
    /// Gets or sets the identification string.
    /// </summary>
    public string Identificacion { get; set; }
}