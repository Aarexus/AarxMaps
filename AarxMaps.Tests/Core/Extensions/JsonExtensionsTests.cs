using AarxMaps.Core.Extensions;
using FluentAssertions;
using System.Text.Json;

namespace AarxMaps.Tests.Core.Extensions;

[Trait("Package", "Core")]
[Trait("Module", "Extensions")]
[Trait("Type", "JsonExtensions")]
public class JsonExtensionsTests {
    /// <summary>Parses and CLONES the root so the element outlives the (disposed) document.</summary>
    private static JsonElement Root(string json) {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static JsonElement Prop(string literal) => Root($$"""{"v":{{literal}}}""");

    // ======================================================================
    // GetObject
    // ======================================================================
    [Fact]
    public void GetObject_PropertyIsAnObject_ReturnsIt() {
        var result = Root("""{"a":{"b":1}}""").GetObject("a");

        result.Should().NotBeNull();
        result!.Value.GetProperty("b").GetInt32().Should().Be(1);
    }

    [Fact]
    public void GetObject_EmptyObject_ReturnsAnEmptyObjectNotNull()
        => Root("""{"a":{}}""").GetObject("a").Should().NotBeNull();

    [Theory]
    [InlineData("""{"a":null}""")]
    [InlineData("""{"a":1}""")]
    [InlineData("""{"a":"s"}""")]
    [InlineData("""{"a":true}""")]
    [InlineData("""{"a":[]}""")]
    [InlineData("""{}""")]
    public void GetObject_MissingOrNotAnObject_ReturnsNull(string json)
        => Root(json).GetObject("a").Should().BeNull();

    [Fact]
    public void GetObject_PropertyNameIsCaseSensitive()
        => Root("""{"Location":{}}""").GetObject("location").Should().BeNull();

    // ======================================================================
    // GetInt
    // ======================================================================
    [Theory]
    [InlineData("0", 0)]
    [InlineData("42", 42)]
    [InlineData("-7", -7)]
    [InlineData("2147483647", int.MaxValue)]
    [InlineData("-2147483648", int.MinValue)]
    public void GetInt_IntegerNumber_ReturnsValue(string literal, int expected)
        => Prop(literal).GetInt("v").Should().Be(expected);

    [Theory]
    [InlineData("""{"v":null}""")]
    [InlineData("""{"v":"5"}""")]
    [InlineData("""{"v":true}""")]
    [InlineData("""{"v":{}}""")]
    [InlineData("""{"v":[1]}""")]
    [InlineData("""{}""")]
    public void GetInt_MissingOrNotANumber_ReturnsNull(string json)
        => Root(json).GetInt("v").Should().BeNull();

    [Theory]
    [InlineData("1.5")]
    [InlineData("100.0")]
    [InlineData("1e2")]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    [InlineData("99999999999999999999")]
    [Trait("Finding", "int-number-not-representable")]
    public void GetInt_NumberNotRepresentableAsInt32_DoesNotThrow(string literal) {
        var element = Prop(literal);

        var act = () => element.GetInt("v");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    [InlineData("99999999999999999999")]
    [Trait("Finding", "int-number-not-representable")]
    public void GetInt_NumberOutsideInt32Range_ReturnsNull(string literal)
        => Prop(literal).GetInt("v").Should().BeNull();

    // ======================================================================
    // GetFloat / GetDoubleValue
    // ======================================================================
    [Theory]
    [InlineData("0", 0f)]
    [InlineData("1.5", 1.5f)]
    [InlineData("-2.25", -2.25f)]
    [InlineData("3", 3f)]
    [InlineData("1e3", 1000f)]
    [InlineData("0.1", 0.1f)]
    public void GetFloat_AnyNumber_ReturnsSingle(string literal, float expected)
        => Prop(literal).GetFloat("v").Should().Be(expected);

    [Theory]
    [InlineData("0", 0d)]
    [InlineData("1.5", 1.5d)]
    [InlineData("-2.25", -2.25d)]
    [InlineData("3", 3d)]
    [InlineData("1e3", 1000d)]
    [InlineData("0.1", 0.1d)]
    [InlineData("1e300", 1e300d)]
    [InlineData("12345678901", 12345678901d)]
    public void GetDoubleValue_AnyNumber_ReturnsDouble(string literal, double expected)
        => Prop(literal).GetDoubleValue("v").Should().Be(expected);

    [Theory]
    [InlineData("""{"v":null}""")]
    [InlineData("""{"v":"1.5"}""")]
    [InlineData("""{"v":false}""")]
    [InlineData("""{"v":{}}""")]
    [InlineData("""{"v":[1.5]}""")]
    [InlineData("""{}""")]
    public void GetFloatAndGetDoubleValue_MissingOrNotANumber_ReturnNull(string json) {
        var element = Root(json);

        element.GetFloat("v").Should().BeNull();
        element.GetDoubleValue("v").Should().BeNull();
    }

    // ======================================================================
    // GetBool
    // ======================================================================
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void GetBool_BooleanLiteral_ReturnsValue(string literal, bool expected)
        => Prop(literal).GetBool("v").Should().Be(expected);

    [Theory]
    [InlineData("""{"v":null}""")]
    [InlineData("""{"v":"true"}""")]
    [InlineData("""{"v":1}""")]
    [InlineData("""{"v":0}""")]
    [InlineData("""{"v":{}}""")]
    [InlineData("""{"v":[true]}""")]
    [InlineData("""{}""")]
    public void GetBool_MissingOrNotABoolean_ReturnsNull(string json)
        => Root(json).GetBool("v").Should().BeNull();

    // ======================================================================
    // GetStringValue
    // ======================================================================
    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"\"", "")]                      // empty string is a value, not null
    [InlineData("\"Medellín 東京 😀\"", "Medellín 東京 😀")]
    [InlineData("\"caf\\u00e9\"", "café")]        // escapes are decoded
    [InlineData("\"a\\\"b\"", "a\"b")]
    public void GetStringValue_StringProperty_ReturnsDecodedValue(string literal, string expected)
        => Prop(literal).GetStringValue("v").Should().Be(expected);

    [Theory]
    [InlineData("""{"v":null}""")]
    [InlineData("""{"v":1}""")]
    [InlineData("""{"v":true}""")]
    [InlineData("""{"v":{}}""")]
    [InlineData("""{"v":["s"]}""")]
    [InlineData("""{}""")]
    public void GetStringValue_MissingOrNotAString_ReturnsNull(string json)
        => Root(json).GetStringValue("v").Should().BeNull();

    // ======================================================================
    // GetArray
    // ======================================================================
    [Fact]
    public void GetArray_ArrayProperty_EnumeratesItemsInOrderWhateverTheirKinds() {
        var items = Root("""{"v":[1,"two",{"three":3},null,[4]]}""").GetArray("v").ToList();

        items.Select(i => i.ValueKind).Should().Equal(
            JsonValueKind.Number, JsonValueKind.String, JsonValueKind.Object, JsonValueKind.Null, JsonValueKind.Array);
        items[1].GetString().Should().Be("two");
    }

    [Fact]
    public void GetArray_EmptyArray_YieldsNothing()
        => Root("""{"v":[]}""").GetArray("v").Any().Should().BeFalse();

    [Theory]
    [InlineData("""{"v":null}""")]
    [InlineData("""{"v":1}""")]
    [InlineData("""{"v":"s"}""")]
    [InlineData("""{"v":true}""")]
    [InlineData("""{"v":{}}""")]
    [InlineData("""{}""")]
    public void GetArray_MissingOrNotAnArray_YieldsEmptyWithoutThrowing(string json) {
        var element = Root(json);

        var act = () => element.GetArray("v").ToList();

        act.Should().NotThrow().Which.Should().BeEmpty();
    }

    [Fact]
    public void GetArray_CanBeUsedInForeach() {
        var count = 0;
        foreach (var _ in Root("""{"v":[1,2,3]}""").GetArray("v")) count++;
        foreach (var _ in Root("{}").GetArray("v")) count++;

        count.Should().Be(3);
    }

    // ======================================================================
    // GetArrayStringValues
    // ======================================================================
    [Fact]
    public void GetArrayStringValues_Strings_ReturnedInOrderIncludingDuplicates() {
        var result = Root("""{"v":["a","b","a","c"]}""").GetArrayStringValues("v");
        result.Should().Equal("a", "b", "a", "c");
    }

    [Fact]
    public void GetArrayStringValues_NullEmptyAndWhitespaceStrings_AreDropped() {
        var result = Root("""{"v":["a",null,"","   ","\t\n","b"]}""").GetArrayStringValues("v");
        result.Should().Equal("a", "b");
    }

    [Fact]
    public void GetArrayStringValues_ValuesAreReturnedUntrimmed()
        => Root("""{"v":[" a "]}""").GetArrayStringValues("v").Should().Equal(" a ");

    [Theory]
    [InlineData("""{"v":[]}""")]
    [InlineData("""{"v":[null,"",""]}""")]
    [InlineData("""{"v":null}""")]
    [InlineData("""{"v":"a"}""")]
    [InlineData("""{"v":{}}""")]
    [InlineData("""{}""")]
    public void GetArrayStringValues_NothingUsable_ReturnsEmptyList(string json) {
        var result = Root(json).GetArrayStringValues("v");

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void GetArrayStringValues_Result_IsAnIndependentSnapshot() {
        var root = Root("""{"v":["a","b"]}""");

        var first = root.GetArrayStringValues("v");
        var second = root.GetArrayStringValues("v");

        first.Should().Equal(second).And.NotBeSameAs(second);
    }

    [Fact]
    public void GetArrayStringValues_NonStringElements_AreSkippedNotThrown() {
        var root = Root("""{"v":["a",1,{"b":2},true,[],"c"]}""");

        var act = () => root.GetArrayStringValues("v");

        act.Should().NotThrow().Which.Should().Equal("a", "c");
    }

    // ======================================================================
    // Receiver that is not an object
    // ======================================================================

    public static TheoryData<string, string> ReceiverCases() {
        var data = new TheoryData<string, string>();
        var receivers = new[] { "[1]", "\"s\"", "1", "true", "null", "undefined" };
        var methods = new[] {
            nameof(JsonExtensions.GetObject), nameof(JsonExtensions.GetInt), nameof(JsonExtensions.GetFloat),
            nameof(JsonExtensions.GetDoubleValue), nameof(JsonExtensions.GetBool), nameof(JsonExtensions.GetStringValue),
            nameof(JsonExtensions.GetArray), nameof(JsonExtensions.GetArrayStringValues)
        };
        foreach (var r in receivers)
            foreach (var m in methods)
                data.Add(r, m);
        return data;
    }

    private static JsonElement Receiver(string kind) => kind == "undefined" ? default : Root(kind);
    private static object? Invoke(string method, JsonElement e) => method switch {
        nameof(JsonExtensions.GetObject) => e.GetObject("v"),
        nameof(JsonExtensions.GetInt) => e.GetInt("v"),
        nameof(JsonExtensions.GetFloat) => e.GetFloat("v"),
        nameof(JsonExtensions.GetDoubleValue) => e.GetDoubleValue("v"),
        nameof(JsonExtensions.GetBool) => e.GetBool("v"),
        nameof(JsonExtensions.GetStringValue) => e.GetStringValue("v"),
        nameof(JsonExtensions.GetArray) => e.GetArray("v"),
        nameof(JsonExtensions.GetArrayStringValues) => e.GetArrayStringValues("v"),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
    private static bool IsNullOrEmpty(object? result) => result switch {
        null => true,
        IReadOnlyList<string> list => list.Count == 0,
        IEnumerable<JsonElement> sequence => !sequence.Any(),
        _ => false
    };

    [Theory]
    [MemberData(nameof(ReceiverCases))]
    public void Getters_OnANonObjectElement_ReturnNullOrEmptyWithoutThrowing(string receiverKind, string method) {
        var element = Receiver(receiverKind);

        var act = () => Invoke(method, element);

        var result = act.Should().NotThrow().Which;
        IsNullOrEmpty(result).Should().BeTrue();
    }
}