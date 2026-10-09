using AarxMaps.Core.Internal.Handlers;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models;
using AarxMaps.Core.Internal.Factories;
using AarxMaps.Core.Internal.Json;

namespace AarxMaps.Core.Internal.Http;

internal sealed class MapsApiClient : IMapsApiClient {
    // -------------------- INIT --------------------
    private readonly HttpClient _httpClient;
    private readonly RequestFactory _reqFactory;

    public MapsApiClient(HttpClient httpClient, RequestFactory requestFactory) {
        _httpClient = httpClient;
        _reqFactory = requestFactory;
    }

    // -------------------- METHS --------------------
    public async Task<T> SendAsync<T>(MapsApiRequest apiRequest, CancellationToken ct) {
        // Response Oowned entirely here (consumes & disposes)
        using var response = await CreateAndSendAsync(apiRequest, HttpCompletionOption.ResponseContentRead, ct)
            .ConfigureAwait(false);

        await ExceptionHandler.EnsureSuccessAsync(response, ct).ConfigureAwait(false);
        
        return await DeserializeOrThrowAsync<T>(response, ct).ConfigureAwait(false);
    }

    public async Task<StreamResponse> SendStreamAsync(MapsApiRequest apiRequest, CancellationToken ct) {
        HttpResponseMessage? response = null;
        try {
            response = await CreateAndSendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            await ExceptionHandler.EnsureSuccessAsync(response, ct).ConfigureAwait(false);

            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

            // Ownership transfered
            return new StreamResponse(stream, response);
        }
        catch {
            response?.Dispose();
            throw;
        }
    }

    // -------------------- INNER METHS --------------------
    private async Task<HttpResponseMessage> CreateAndSendAsync(
        MapsApiRequest apiRequest, HttpCompletionOption option, CancellationToken ct
    ) {
        using var request = _reqFactory.CreateRequest(apiRequest);
        try {
            return await _httpClient.SendAsync(request, option, ct).ConfigureAwait(false);   
        }
         catch (HttpRequestException ex) {
            throw new MapsApiException("Network error while calling the Maps API.", inner: ex);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested) {
            throw new MapsApiException("The Maps API request timed out.", inner: ex);
        }
    }

    private static async Task<T> DeserializeOrThrowAsync<T>(HttpResponseMessage response, CancellationToken ct) {
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json))
            throw new MapsApiException("The Maps API returned an empty response body.");

        return MapsJsonSerializer.Deserialize<T>(json)
            ?? throw new MapsApiException($"The response deserialized to null for {typeof(T).Name}.");
    }
}