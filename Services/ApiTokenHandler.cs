using System.Net.Http.Headers;

namespace ERP.Web.Services;

/// <summary>
/// Meneruskan JWT milik user yang sedang login (disimpan di cookie auth) ke setiap request API.
/// </summary>
public class ApiTokenHandler : DelegatingHandler
{
    public const string AccessTokenClaim = "access_token";

    private readonly IHttpContextAccessor _http;

    public ApiTokenHandler(IHttpContextAccessor http)
    {
        _http = http;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _http.HttpContext?.User.FindFirst(AccessTokenClaim)?.Value;

        if (!string.IsNullOrEmpty(token) && request.Headers.Authorization == null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
