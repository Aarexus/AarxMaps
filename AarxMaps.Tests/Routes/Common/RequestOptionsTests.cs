using AarxMaps.Routes.Common.Models;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Common;

[Trait("Package", "Routes")]
[Trait("Module", "Common")]
[Trait("Type", "RequestOptions")]
public class RequestOptionsTests {
    // ===================== route modifiers =====================
    [Fact]
    public void RouteModifiers_Default_AvoidsNothingAndHasNoEmissionType() {
        var modifiers = new RouteModifiers();

        modifiers.Should().BeEquivalentTo(new {
            AvoidTolls = false, AvoidHighways = false, AvoidFerries = false, AvoidIndoor = false, EmissionType = (VehicleEmissionType?)null
        });
    }

    [Fact]
    public void RouteModifiers_EveryArgument_IsKept() {
        var modifiers = new RouteModifiers(avoidTolls: true, avoidHighways: true, avoidFerries: true, avoidIndoor: true,
            emissionType: VehicleEmissionType.Electric);

        modifiers.Should().BeEquivalentTo(new {
            AvoidTolls = true, AvoidHighways = true, AvoidFerries = true, AvoidIndoor = true, EmissionType = VehicleEmissionType.Electric
        });
    }

    // ===================== transit preferences =====================
    [Fact]
    public void TransitPreferences_Default_AllowsEveryModeAndHasNoPreference() {
        var preferences = new TransitPreferences();

        preferences.AllowedTravelModes.Should().BeEmpty();
        preferences.RoutingPreference.Should().BeNull();
    }

    [Fact]
    public void TransitPreferences_CallerListChangedAfterwards_DoesNotChangeThePreferences() {
        var modes = new List<TransitTravelMode> { TransitTravelMode.Bus };
        var preferences = new TransitPreferences(modes, TransitRoutingPreference.LessWalking);

        modes.Add(TransitTravelMode.Subway);

        preferences.AllowedTravelModes.Should().Equal(TransitTravelMode.Bus);
        preferences.RoutingPreference.Should().Be(TransitRoutingPreference.LessWalking);
    }
}
