using System;
using Mnema.Common.Extensions;

namespace Mnema.Tests.Common.Extensions;

public class StringExtensionsTests
{
    #region ToNormalized Tests

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("hello!@#$%", "hello!")]
    [InlineData("test-123", "test123")]
    [InlineData("test+123!", "test+123!")]
    [InlineData("hello＊world＋", "hello＊world＋")]
    [InlineData("HELLO", "hello")]
    [InlineData("TeSt123", "test123")]
    [InlineData("  hello  ", "hello")]
    [InlineData("café", "café")]
    [InlineData("日本", "日本")]
    [InlineData("hello world", "helloworld")]
    [InlineData("hello,", "hello")]
    public void ToNormalized_ReturnsExpectedResult(string? input, string expected)
    {
        Assert.Equal(expected, input.ToNormalized());
    }

    #endregion

    #region PadFloat Tests

    [Theory]
    [InlineData(null, 5, "")]
    [InlineData("", 5, "")]
    [InlineData("42", 5, "00042")]
    [InlineData("123", 2, "123")]
    [InlineData("42.5", 5, "00042.5")]
    [InlineData("3.14159", 5, "00003.14159")]
    [InlineData(".5", 5, "00000.5")]
    [InlineData("42.5", 0, "42.5")]
    public void PadFloat_ReturnsExpectedResult(string? input, int padding, string expected)
    {
        Assert.Equal(expected, input.PadFloat(padding));
    }

    #endregion

    #region RemovePrefix Tests

    [Theory]
    [InlineData("helloworld", "hello", "world")]
    [InlineData("helloworld", "goodbye", "helloworld")]
    [InlineData("hello", "", "hello")]
    [InlineData("hi", "hello", "hi")]
    [InlineData("hello", "hello", "")]
    [InlineData("Helloworld", "hello", "Helloworld")]
    public void RemovePrefix_ReturnsExpectedResult(string input, string prefix, string expected)
    {
        Assert.Equal(expected, input.RemovePrefix(prefix));
    }

    #endregion

    #region RemoveSuffix Tests

    [Theory]
    [InlineData("helloworld", "world", "hello")]
    [InlineData("helloworld", "goodbye", "helloworld")]
    [InlineData("hello", "", "hello")]
    [InlineData("hi", "hello", "hi")]
    [InlineData("hello", "hello", "")]
    [InlineData("helloWorld", "world", "helloWorld")]
    public void RemoveSuffix_ReturnsExpectedResult(string input, string suffix, string expected)
    {
        Assert.Equal(expected, input.RemoveSuffix(suffix));
    }

    #endregion

    #region AsInt Tests

    [Theory]
    [InlineData("42", 42)]
    [InlineData("-10", -10)]
    [InlineData("0", 0)]
    [InlineData("hello", 0)]
    [InlineData("42.5", 0)]
    [InlineData("", 0)]
    [InlineData("  ", 0)]
    [InlineData("42abc", 0)]
    [InlineData("abc42", 0)]
    public void AsInt_ReturnsExpectedResult(string input, int expected)
    {
        Assert.Equal(expected, input.AsInt());
    }

    #endregion

    #region OrNonEmpty Tests

    [Fact]
    public void OrNonEmpty_WithNonEmptyString_ReturnsOriginal()
    {
        Assert.Equal("first", "first".OrNonEmpty("second", "third"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OrNonEmpty_WithNullOrEmpty_ReturnsFirstNonEmpty(string? input)
    {
        Assert.Equal("second", input.OrNonEmpty("second", "third"));
    }

    [Fact]
    public void OrNonEmpty_WithAllEmpty_ReturnsEmpty()
    {
        string? s = null;
        Assert.Equal(string.Empty, s.OrNonEmpty(string.Empty, null, ""));
    }

    [Fact]
    public void OrNonEmpty_SkipsEmptyValues()
    {
        string? s = null;
        Assert.Equal("third", s.OrNonEmpty(string.Empty, null, "third", "fourth"));
    }

    [Fact]
    public void OrNonEmpty_WithNoParameters_ReturnsEmpty()
    {
        string? s = null;
        Assert.Equal(string.Empty, s.OrNonEmpty());
    }

    #endregion

    #region AsDateTime Tests

    [Theory]
    [InlineData("2024-01-15", "yyyy-MM-dd", 2024, 1, 15, 0, 0, 0)]
    [InlineData("  2024-01-15  ", "yyyy-MM-dd", 2024, 1, 15, 0, 0, 0)]
    [InlineData("15/01/2024 14:30:00", "dd/MM/yyyy HH:mm:ss", 2024, 1, 15, 14, 30, 0)]
    public void AsDateTime_WithValidFormat_ReturnsDateTime(string input, string format, int year, int month, int day,
        int hour, int minute, int second)
    {
        var result = input.AsDateTime(format);

        Assert.NotNull(result);
        Assert.Equal(new DateTime(year, month, day, hour, minute, second).ToUniversalTime(), result);
    }

    [Theory]
    [InlineData("2024-01-15", "MM/dd/yyyy")]
    [InlineData("not-a-date", "yyyy-MM-dd")]
    [InlineData("", "yyyy-MM-dd")]
    public void AsDateTime_WithInvalidInput_ReturnsNull(string input, string format)
    {
        Assert.Null(input.AsDateTime(format));
    }

    #endregion
}
