namespace AarxMaps.Tests.Places.Helpers;

/// <summary>Inline Google Places (New) response bodies, shaped after Google's reference.</summary>
internal static class PlacesFixtures {
    // -------------------- AUTOCOMPLETE --------------------
    public const string AutocompleteSingle = """
        {
          "suggestions": [
            {
              "placePrediction": {
                "placeId": "ChIJ-place-1",
                "text": { "text": "Bogota, Colombia", "matches": [ { "endOffset": 6 } ] },
                "types": [ "locality", "political" ]
              }
            }
          ]
        }
        """;

    // -------------------- SEARCHING --------------------
    public const string SearchTextPage = """
        {
          "places": [
            {
              "id": "ChIJ-pizza-1",
              "displayName": { "text": "Pizza Uno", "languageCode": "es" },
              "types": [ "pizza_restaurant", "restaurant" ]
            },
            {
              "id": "ChIJ-pizza-2",
              "displayName": { "text": "Pizza Dos", "languageCode": "es" },
              "types": [ "restaurant" ]
            }
          ],
          "nextPageToken": "page-2-token"
        }
        """;

    public const string SearchNearbyPage = """
        {
          "places": [
            {
              "id": "ChIJ-cafe-1",
              "displayName": { "text": "Cafe Uno", "languageCode": "es" },
              "types": [ "cafe", "food" ]
            }
          ]
        }
        """;

    // -------------------- DETAILS --------------------
    /// <summary>Every field in the GetDetailsAsync mask, plus a few Google sends that we ignore.</summary>
    public const string DetailsFull = """
        {
          "displayName": { "text": "Museo del Oro", "languageCode": "es" },
          "types": [ "museum", "tourist_attraction" ],
          "formattedAddress": "Cra. 6 #15-88, Bogota, Colombia",
          "plusCode": { "globalCode": "67P7MV5R+QH", "compoundCode": "MV5R+QH Bogota" },
          "location": { "latitude": 4.6019, "longitude": -74.0721 },
          "postalAddress": {
            "regionCode": "CO", "postalCode": "110321", "administrativeArea": "Bogota",
            "locality": "Bogota", "addressLines": [ "Cra. 6 #15-88" ]
          },
          "timeZone": { "id": "America/Bogota", "version": "2024a" },
          "nationalPhoneNumber": "(601) 3431212",
          "internationalPhoneNumber": "+57 601 3431212",
          "priceLevel": "PRICE_LEVEL_INEXPENSIVE",
          "rating": 4.8,
          "userRatingCount": 41234,
          "regularOpeningHours": {
            "openNow": true,
            "weekdayDescriptions": [ "Monday: Closed", "Tuesday: 9:00 AM - 6:00 PM" ]
          },
          "googleMapsUri": "https://maps.google.com/?cid=123",
          "websiteUri": "https://www.banrepcultural.org/museo-del-oro",
          "googleMapsLinks": {
            "directionsUri": "https://www.google.com/maps/dir/?cid=123",
            "placeUri": "https://maps.google.com/?cid=123&place",
            "writeAReviewUri": "https://www.google.com/maps/write-review?cid=123",
            "reviewsUri": "https://www.google.com/maps/reviews?cid=123",
            "photosUri": "https://www.google.com/maps/photos?cid=123"
          },
          "paymentOptions": { "acceptsCreditCards": true, "acceptsDebitCards": true, "acceptsCashOnly": false, "acceptsNfc": true },
          "parkingOptions": { "freeParkingLot": false, "freeStreetParking": true }
        }
        """;

    /// <summary>Every field in the GetReviewsAsync mask: one full review and one rating-only review.</summary>
    public const string ReviewsFull = """
        {
          "displayName": { "text": "Museo del Oro", "languageCode": "es" },
          "reviewSummary": {
            "text": { "text": "Visitors praise the gold collection.", "languageCode": "en-US" },
            "flagContentUri": "https://www.google.com/local/review/rap/report?postId=summary",
            "disclosureText": { "text": "Summarized with Gemini", "languageCode": "en-US" },
            "reviewsUri": "https://www.google.com/maps/reviews?cid=123&summary"
          },
          "reviews": [
            {
              "name": "places/ChIJ-museo/reviews/1",
              "relativePublishTimeDescription": "a month ago",
              "rating": 5,
              "text": { "text": "Amazing collection!", "languageCode": "en" },
              "originalText": { "text": "Amazing collection!", "languageCode": "en" },
              "authorAttribution": {
                "displayName": "Ana", "uri": "https://www.google.com/maps/contrib/1", "photoUri": "https://lh3.googleusercontent.com/a"
              },
              "publishTime": "2025-01-04T20:12:37.937458542Z"
            },
            {
              "name": "places/ChIJ-museo/reviews/2",
              "rating": 4,
              "authorAttribution": { "displayName": "Luis" },
              "publishTime": "2024-03-08T17:31:41.718981Z"
            }
          ],
          "googleMapsLinks": { "reviewsUri": "https://www.google.com/maps/reviews?cid=123" }
        }
        """;

    /// <summary>The GetPhotosAsync names call: two photos, the first with two authors.</summary>
    public const string PhotosFull = """
        {
          "photos": [
            {
              "name": "places/ChIJ-museo/photos/p1",
              "widthPx": 4032,
              "heightPx": 3024,
              "authorAttributions": [
                { "displayName": "Ana", "uri": "https://www.google.com/maps/contrib/1", "photoUri": "https://lh3.googleusercontent.com/a" },
                { "displayName": "Second author" }
              ],
              "flagContentUri": "https://www.google.com/local/imagery/report/?cb_client=maps_api_places",
              "googleMapsUri": "https://www.google.com/maps/place//data=photo-p1"
            },
            {
              "name": "places/ChIJ-museo/photos/p2",
              "widthPx": 800,
              "heightPx": 600,
              "authorAttributions": []
            }
          ]
        }
        """;

    /// <summary>A Place Photos media response with skipHttpRedirect=true.</summary>
    public const string PhotoMedia = """
        { "name": "places/ChIJ-museo/photos/p1/media", "photoUri": "https://lh3.googleusercontent.com/place-photo" }
        """;
}
