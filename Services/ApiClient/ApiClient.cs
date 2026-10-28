using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

/// <summary>
/// Client HTTP ke ERP.Api. Method dipecah per modul di file ApiClient.&lt;Modul&gt;.cs (partial class).
/// </summary>
public partial class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }
}
