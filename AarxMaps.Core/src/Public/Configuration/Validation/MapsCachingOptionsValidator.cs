using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Configuration.Validation;

internal sealed class MapsCachingOptionsValidator : IValidateOptions<MapsCachingOptions> {
    public ValidateOptionsResult Validate(string? name, MapsCachingOptions o) {
        var failures = new List<string>(3);

        if (string.IsNullOrWhiteSpace(o.Prefix))
            failures.Add($"{nameof(o.Prefix)} must not be empty.");
        if (o.AbsoluteExpiration <= TimeSpan.Zero)
            failures.Add($"{nameof(o.AbsoluteExpiration)} must be positive.");
        if (o.SlidingExpiration is { } s && (s <= TimeSpan.Zero || s > o.AbsoluteExpiration))
            failures.Add($"{nameof(o.SlidingExpiration)} must be positive and not exceed {nameof(o.AbsoluteExpiration)}.");

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}