using System.Text.Json;
using AarxMaps.Core.Abstractions;
using AarxMaps.Places.Internal.Services.Details.DTOs;
using AarxMaps.Places.Services.Details;

namespace AarxMaps.Places.Internal.Services.Details;

internal sealed class DetailsService : IDetailsService {
    // -------------------- INIT --------------------
    private static readonly PhotoOptions _defPhotoOptions = new PhotoOptionsBuilder().Build();

    private readonly IMapsApiClient _apiClient;
    public DetailsService(IMapsApiClient apiClient) => _apiClient = apiClient;

    // -------------------- METHS --------------------
    public async Task<PlaceDetails> GetDetailsAsync(DetailsRequest detRequest, CancellationToken ct = default) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(detRequest);

        // REQUEST
        var request = DetailsFactory.CreateDetailsRequest(detRequest);
        var response = await _apiClient.SendAsync<PlaceDetailsDto>(request, ct)
            .ConfigureAwait(false);

        return DetailsMapper.ToPlaceDetails(detRequest.PlaceId, response);
    }

    public async Task<JsonDocument> GetDetailsRawAsync(
        DetailsRequest detRequest, IEnumerable<string> fields, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(detRequest);

        // REQUEST
        var request = DetailsFactory.CreateRawDetailsRequest(detRequest, fields);
        return await _apiClient.SendAsync<JsonDocument>(request, ct)
            .ConfigureAwait(false);
    }

    public async Task<PlacePhotos> GetPhotosAsync(
        DetailsRequest detRequest, PhotoOptions? photoOptions = null, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(detRequest);
        var options = photoOptions ?? _defPhotoOptions;

        // PHOTO NAMES
        var request = DetailsFactory.CreatePhotosRequest(detRequest);
        var response = await _apiClient.SendAsync<PlaceDetailsDto>(request, ct)
            .ConfigureAwait(false);

        // PARALLEL MEDIA REQUESTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var tasks = new List<Task<PlacePhotoInfo?>>(options.MaxPhotos);
        foreach (var photo in response.Photos) {
            if (tasks.Count == options.MaxPhotos) break;
            if (string.IsNullOrWhiteSpace(photo.Name)) continue;

            // Task.WhenAll below completes every task before the using disposes cts
#pragma warning disable CA2025
            tasks.Add(GetPhotoInfoAsync(photo.Name, photo, options, cts));
#pragma warning restore CA2025
        }
        var photos = await Task.WhenAll(tasks).ConfigureAwait(false);

        return new PlacePhotos() { PlaceId = detRequest.PlaceId, Photos = [.. photos.OfType<PlacePhotoInfo>()] };
    }

    public async Task<PlaceReviews> GetReviewsAsync(DetailsRequest detRequest, CancellationToken ct = default) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(detRequest);

        // REQUEST
        var request = DetailsFactory.CreateReviewsRequest(detRequest);
        var response = await _apiClient.SendAsync<PlaceDetailsDto>(request, ct)
            .ConfigureAwait(false);

        return DetailsMapper.ToPlaceReviews(detRequest.PlaceId, response);
    }

    // -------------------- INNER METHS --------------------
    private async Task<PlacePhotoInfo?> GetPhotoInfoAsync(
        string photoName, PhotoDto photo, PhotoOptions options, CancellationTokenSource cts
    ) {
        try {
            var request = DetailsFactory.CreatePhotoMediaRequest(photoName, options);
            var media = await _apiClient.SendAsync<PhotoMediaDto>(request, cts.Token)
                .ConfigureAwait(false);

            return DetailsMapper.ToPlacePhotoInfo(photoName, photo, media);
        }
        // One failed photo fails the whole call, so stop the remaining (billed) requests
        catch (Exception ex) when (ex is not OperationCanceledException) {
            await cts.CancelAsync().ConfigureAwait(false);
            throw;
        }
    }
}
