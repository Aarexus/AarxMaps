using AarxMaps.Core.Exceptions;

namespace AarxMaps.Core.Internal.Builders;

internal sealed class MapsUriBuilder {
    // -------------------- INIT --------------------
    private string? _path;
    private readonly Dictionary<string, string> _query = [];

    // -------------------- BUILD --------------------
    public Uri Build() {
        if (string.IsNullOrWhiteSpace(_path))
            throw new MapsInvalidRequestException("URL path was not set into the request.");

        var builder = new UriBuilder(_path) {
            Query = string.Join("&",
                _query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")
            )
        };

        return new Uri(builder.ToString(), UriKind.Absolute);
    }

    // -------------------- CONFIG --------------------
    public MapsUriBuilder WithPath(Uri baseUrl, string endpoint) {
        _path = $"{ClearSlash(baseUrl.AbsoluteUri, false)}/{ClearSlash(endpoint)}";
        return this;
    }

    public MapsUriBuilder AddQuery(IDictionary<string, string> queryParams) {
        foreach (var param in queryParams)
            _query[param.Key] = param.Value;

        return this;
    }

    // -------------------- INNER METHS --------------------
    private static string ClearSlash(string path, bool start = true)
        => start ? path.TrimStart('/') : path.TrimEnd('/') ;
}