using AarxMaps.Routes.Internal.Common;
using AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;
using AarxMaps.Routes.Services.DistanceMatrix;

namespace AarxMaps.Routes.Internal.Services.DistanceMatrix;

internal static class DistanceMatrixMapper {
    // -------------------- METHS --------------------
    internal static RouteMatrixElement ToElement(RouteMatrixElementDto dto)
        => new () {
            OriginIndex = dto.OriginIndex,
            DestinationIndex = dto.DestinationIndex,
            Condition = WireEnum<RouteMatrixElementCondition>.FromWire(dto.Condition),
            // Successful elements send "status": {}, which carries no error
            Status = dto.Status is { Code: not 0 } status
                ? new RouteMatrixElementStatus() { Code = status.Code, Message = status.Message }
                : null,
            DistanceMeters = dto.DistanceMeters,
            Duration = CommonMapper.ToDuration(dto.Duration),
            StaticDuration = CommonMapper.ToDuration(dto.StaticDuration),
            TravelAdvisory = CommonMapper.ToTravelAdvisory(dto.TravelAdvisory),
            FallbackInfo = CommonMapper.ToFallbackInfo(dto.FallbackInfo),
            LocalizedValues = CommonMapper.ToLocalizedValues(dto.LocalizedValues)
        };
}
