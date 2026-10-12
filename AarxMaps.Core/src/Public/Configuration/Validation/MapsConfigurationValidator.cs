using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Configuration.Validation;

internal sealed class MapsConfigurationValidator : IValidateOptions<MapsConfiguration> {
    public ValidateOptionsResult Validate(string? name, MapsConfiguration o) {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(o.ApiKey))
            failures.Add($"{nameof(o.ApiKey)} must be provided.");
        if (o.RequestTimeout <= TimeSpan.Zero && o.RequestTimeout != Timeout.InfiniteTimeSpan)
            failures.Add($"{nameof(o.RequestTimeout)} must be positive or infinite.");

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}