namespace AarxMaps.Tests.Routes.Helpers;

/// <summary>Inline Google Routes API response bodies, shaped after Google's reference.</summary>
internal static class RoutesFixtures {
    // -------------------- COMPUTE ROUTES --------------------
    /// <summary>A full drive route (one leg, two steps) plus a bare alternative, and a fallback.</summary>
    public const string RoutesDrive = """
        {
          "routes": [
            {
              "routeLabels": [ "DEFAULT_ROUTE" ],
              "legs": [
                {
                  "distanceMeters": 1200,
                  "duration": "300s",
                  "staticDuration": "280s",
                  "polyline": { "encodedPolyline": "leg_poly" },
                  "startLocation": { "latLng": { "latitude": 4.6097, "longitude": -74.0817 } },
                  "endLocation": { "latLng": { "latitude": 4.6533, "longitude": -74.0636 } },
                  "steps": [
                    {
                      "distanceMeters": 500,
                      "staticDuration": "120s",
                      "polyline": { "encodedPolyline": "step_poly_1" },
                      "startLocation": { "latLng": { "latitude": 4.6097, "longitude": -74.0817 } },
                      "endLocation": { "latLng": { "latitude": 4.62, "longitude": -74.08 } },
                      "navigationInstruction": { "maneuver": "DEPART", "instructions": "Head north on Cra. 7" },
                      "localizedValues": { "distance": { "text": "0.5 km" }, "staticDuration": { "text": "2 mins" } },
                      "travelMode": "DRIVE"
                    },
                    {
                      "distanceMeters": 700,
                      "staticDuration": "160.5s",
                      "navigationInstruction": { "maneuver": "TURN_SLIGHT_LEFT", "instructions": "Slight left onto Av. Jimenez" },
                      "travelMode": "DRIVE"
                    }
                  ],
                  "localizedValues": {
                    "distance": { "text": "1.2 km" }, "duration": { "text": "5 mins" }, "staticDuration": { "text": "5 mins" }
                  }
                }
              ],
              "distanceMeters": 1200,
              "duration": "300s",
              "staticDuration": "280s",
              "polyline": { "encodedPolyline": "route_poly" },
              "description": "Cra. 7",
              "warnings": [ "This route has restricted usage roads." ],
              "viewport": {
                "low": { "latitude": 4.6, "longitude": -74.09 },
                "high": { "latitude": 4.66, "longitude": -74.06 }
              },
              "travelAdvisory": { "routeRestrictionsPartiallyIgnored": true },
              "optimizedIntermediateWaypointIndex": [ 1, 0 ],
              "localizedValues": {
                "distance": { "text": "1.2 km" }, "duration": { "text": "5 mins" }, "staticDuration": { "text": "5 mins" }
              },
              "routeToken": "route-token-1"
            },
            {
              "routeLabels": [ "DEFAULT_ROUTE_ALTERNATE", "SOME_FUTURE_LABEL" ],
              "distanceMeters": 1500,
              "duration": "420s",
              "staticDuration": "400s"
            }
          ],
          "fallbackInfo": { "routingMode": "FALLBACK_TRAFFIC_UNAWARE", "reason": "LATENCY_EXCEEDED" }
        }
        """;

    /// <summary>A transit route: a walk step and a bus step with full transit details.</summary>
    public const string RoutesTransit = """
        {
          "routes": [
            {
              "legs": [
                {
                  "steps": [
                    { "distanceMeters": 300, "staticDuration": "240s", "travelMode": "WALK" },
                    {
                      "distanceMeters": 5000,
                      "staticDuration": "900s",
                      "travelMode": "TRANSIT",
                      "transitDetails": {
                        "stopDetails": {
                          "arrivalStop": { "name": "Calle 72", "location": { "latLng": { "latitude": 4.658, "longitude": -74.063 } } },
                          "arrivalTime": "2026-10-24T15:15:00Z",
                          "departureStop": { "name": "Museo del Oro", "location": { "latLng": { "latitude": 4.602, "longitude": -74.072 } } },
                          "departureTime": "2026-10-24T15:00:00.123456789Z"
                        },
                        "localizedValues": {
                          "arrivalTime": { "time": { "text": "10:15 AM" }, "timeZone": "America/Bogota" },
                          "departureTime": { "time": { "text": "10:00 AM" }, "timeZone": "America/Bogota" }
                        },
                        "headsign": "Portal Norte",
                        "headway": "300s",
                        "transitLine": {
                          "agencies": [
                            { "name": "TransMilenio", "phoneNumber": "+57 601 2203000", "uri": "https://www.transmilenio.gov.co/" },
                            { "name": "Shady Agency", "uri": "javascript:alert(1)" }
                          ],
                          "name": "Troncal Caracas",
                          "nameShort": "B74",
                          "color": "#d30000",
                          "textColor": "#ffffff",
                          "vehicle": { "name": { "text": "Bus" }, "type": "BUS", "iconUri": "//maps.gstatic.com/bus.png" }
                        },
                        "stopCount": 6,
                        "tripShortText": "B74"
                      }
                    }
                  ],
                  "stepsOverview": {
                    "multiModalSegments": [
                      { "stepEndIndex": 0, "navigationInstruction": { "instructions": "Walk to Museo del Oro" }, "travelMode": "WALK" },
                      { "stepStartIndex": 1, "stepEndIndex": 1, "navigationInstruction": { "instructions": "Bus towards Portal Norte" }, "travelMode": "TRANSIT" }
                    ]
                  }
                }
              ],
              "travelAdvisory": { "transitFare": { "currencyCode": "COP", "units": "3200", "nanos": 500000000 } },
              "localizedValues": { "transitFare": { "text": "COP 3,200.50" } }
            }
          ]
        }
        """;

    /// <summary>What Google sends when no route exists between the waypoints.</summary>
    public const string RoutesEmpty = "{}";

    // -------------------- COMPUTE ROUTE MATRIX --------------------
    /// <summary>
    /// A 2x2 matrix streamed out of order, as in Google's sample: one full element, one plain, one failed,
    /// one with no route.
    /// </summary>
    public const string Matrix2x2 = """
        [
          {
            "originIndex": 1, "destinationIndex": 1, "status": {}, "condition": "ROUTE_EXISTS",
            "distanceMeters": 2500, "duration": "400s", "staticDuration": "380s",
            "localizedValues": { "distance": { "text": "2.5 km" }, "duration": { "text": "7 mins" }, "staticDuration": { "text": "6 mins" } },
            "travelAdvisory": { "transitFare": { "currencyCode": "USD", "units": "2", "nanos": 750000000 } }
          },
          {
            "originIndex": 0, "destinationIndex": 0, "status": {}, "condition": "ROUTE_EXISTS",
            "distanceMeters": 822, "duration": "160s"
          },
          {
            "originIndex": 1, "destinationIndex": 0, "status": { "code": 3, "message": "Invalid waypoint." }
          },
          {
            "originIndex": 0, "destinationIndex": 1, "status": {}, "condition": "ROUTE_NOT_FOUND",
            "fallbackInfo": { "routingMode": "FALLBACK_TRAFFIC_UNAWARE", "reason": "SERVER_ERROR" }
          }
        ]
        """;
}
