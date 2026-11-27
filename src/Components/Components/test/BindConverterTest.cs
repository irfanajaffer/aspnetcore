// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Components;

// This is some basic coverage, it's not in depth because there are many many APIs here
// and they mostly call through to CoreFx. We don't want to test the globalization details
// of .NET in detail where we can avoid it.
//
// Instead there's a sampling of things that have somewhat unique behavior or semantics.
public class BindConverterTest
{
    [Fact]
    public void FormatValue_Bool()
    {
        // Arrange
        var value = true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Bool_Generic()
    {
        // Arrange
        var value = true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue<bool>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBool()
    {
        // Arrange
        var value = (bool?)true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBool_Generic()
    {
        // Arrange
        var value = true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue<bool?>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBoolNull()
    {
        // Arrange
        var value = (bool?)null;
        var expected = (bool?)null;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBoolNull_Generic()
    {
        // Arrange
        var value = (bool?)null;
        var expected = (bool?)null;

        // Act
        var actual = BindConverter.FormatValue<bool?>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Int()
    {
        // Arrange
        var value = 17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Int_Generic()
    {
        // Arrange
        var value = 17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue<int>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableInt()
    {
        // Arrange
        var value = (int?)17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableInt_Generic()
    {
        // Arrange
        var value = 17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue<int?>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateTime()
    {
        // Arrange
        var value = DateTime.Now;
        var expected = value.ToString(CultureInfo.CurrentCulture);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateTime_Format()
    {
        // Arrange
        var value = DateTime.Now;
        var expected = value.ToString("MM-yyyy", CultureInfo.InvariantCulture);

        // Act
        var actual = BindConverter.FormatValue(value, "MM-yyyy", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateOnly()
    {
        // Arrange
        var value = DateOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString(CultureInfo.CurrentCulture);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateOnly_Format()
    {
        // Arrange
        var value = DateOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString("MM-yyyy", CultureInfo.InvariantCulture);

        // Act
        var actual = BindConverter.FormatValue(value, "MM-yyyy", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_TimeOnly()
    {
        // Arrange
        var value = TimeOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString(CultureInfo.CurrentCulture);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_TimeOnly_Format()
    {
        // Arrange
        var value = TimeOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString("HH:mm", CultureInfo.InvariantCulture);

        // Act
        var actual = BindConverter.FormatValue(value, "HH:mm", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Enum()
    {
        // Arrange
        var value = SomeLetters.A;
        var expected = value.ToString();

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Enum_OutOfRange()
    {
        // Arrange
        var value = SomeLetters.A + 3;
        var expected = value.ToString();

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("1e-6", 1e-6)]
    [InlineData("2E-06", 2E-06)]
    [InlineData("3.5e10", 3.5e10)]
    [InlineData("123+", 123d)]
    [InlineData("123.45-", -123.45d)]
    [InlineData("1,234", 1234d)]
    [InlineData("-1,234.56", -1234.56d)]
    public void TryConvertToDouble_HandlesSpecialFormats(string input, double expected)
    {
        var culture = input.Contains(',') ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.InvariantCulture;
        var result = BindConverter.TryConvertTo<double>(
            input,
            culture,
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Fact]
    public void FormatValue_NullableEnum()
    {
        // Arrange
        var value = (SomeLetters?)null;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void FormatValue_TypeConverter()
    {
        // Arrange
        var value = new Person()
        {
            Name = "Glenn",
            Age = 47,
        };

        var expected = JsonSerializer.Serialize(value);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TryConvertToNullableDouble_AllowsEmptyValue(object input)
    {
        var result = BindConverter.TryConvertTo<double?>(
            (string)input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Null(value);
    }

    [Theory]
    [InlineData("2E")]
    [InlineData("2E-")]
    [InlineData("e10")]
    public void TryConvertToDouble_RejectsInvalidScientificNotation(
        string input)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Fact]
    public void TryConvertTo_Guid_Valid()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var incomingValue = expected.ToString();

        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.Equal(expected, actual);
        Assert.True(successfullyConverted);
    }

    [Theory]
    [InlineData("invalidguid")]
    [InlineData("")]
    [InlineData(null)]
    public void TryConvertTo_Guid_Invalid(string incomingValue)
    {
        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.False(successfullyConverted);
        Assert.Equal(Guid.Empty, actual);
    }

    [Fact]
    public void TryConvertTo_NullableGuid_Valid()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var incomingValue = expected.ToString();

        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableGuid_ValidEmptyOrNull(string incomingValue)
    {
        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableGuid__Invalid()
    {
        // Arrange
        var value = "invalidguid";

        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid?>(value, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Q")]
    public void ConvertToEnumDynamicCodeSafe_ParsesDefinedValues(string text)
    {
        var success = BindConverter.ConvertToEnumDynamicCodeSafe<SomeLetters>(
            text,
            CultureInfo.InvariantCulture,
            out var actual);

        Assert.True(success);
        Assert.Equal(Enum.Parse<SomeLetters>(text), actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Z")]
    [InlineData("42")]
    public void ConvertToEnumDynamicCodeSafe_MatchesDynamicCodePath(string text)
    {
        var expectedSuccess = BindConverter.TryConvertTo<SomeLetters>(
            text,
            CultureInfo.InvariantCulture,
            out var expected);
        var actualSuccess = BindConverter.ConvertToEnumDynamicCodeSafe<SomeLetters>(
            text,
            CultureInfo.InvariantCulture,
            out var actual);

        Assert.Equal(expectedSuccess, actualSuccess);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConvertToNullableEnumDynamicCodeSafe_ParsesDefinedValue()
    {
        var success = BindConverter.ConvertToNullableEnumDynamicCodeSafe<SomeLetters?>(
            "C",
            CultureInfo.InvariantCulture,
            out var actual);

        Assert.True(success);
        Assert.Equal(SomeLetters.C, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Z")]
    public void ConvertToNullableEnumDynamicCodeSafe_HandlesEmptyAndInvalidValues(string text)
    {
        var success = BindConverter.ConvertToNullableEnumDynamicCodeSafe<SomeLetters?>(
            text,
            CultureInfo.InvariantCulture,
            out var actual);

        Assert.Equal(text.Length == 0, success);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("-42")]
    [InlineData("4294967296")]
    [InlineData("not a number")]
    public void TryConvertTo_TypeConverter_InvalidValueReturnsFalse(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<uint>(incomingValue, CultureInfo.InvariantCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(uint), actual);
    }

    [Fact]
    public void MakeArrayTypeConverterDynamicCodeSafe_MatchesDynamicCodePath()
    {
        var intParser = BindConverter.ParserDelegateCache.MakeArrayTypeConverterDynamicCodeSafe<int[]>();
        Assert.True(intParser(new[] { "1", "2", "3" }, CultureInfo.InvariantCulture, out var ints));
        Assert.Equal(new[] { 1, 2, 3 }, ints);

        var nullableIntParser = BindConverter.ParserDelegateCache.MakeArrayTypeConverterDynamicCodeSafe<int?[]>();
        Assert.True(nullableIntParser(new[] { "1", "", "3" }, CultureInfo.InvariantCulture, out var nullableInts));
        Assert.Equal(new int?[] { 1, null, 3 }, nullableInts);

        var enumParser = BindConverter.ParserDelegateCache.MakeArrayTypeConverterDynamicCodeSafe<SomeLetters[]>();
        Assert.True(enumParser(new[] { "A", "Q" }, CultureInfo.InvariantCulture, out var enums));
        Assert.Equal(new[] { SomeLetters.A, SomeLetters.Q }, enums);

        var nullableEnumParser = BindConverter.ParserDelegateCache.MakeArrayTypeConverterDynamicCodeSafe<SomeLetters?[]>();
        Assert.True(nullableEnumParser(new[] { "A", "", "Q" }, CultureInfo.InvariantCulture, out var nullableEnums));
        Assert.Equal(new SomeLetters?[] { SomeLetters.A, null, SomeLetters.Q }, nullableEnums);

        var customValueParser = BindConverter.ParserDelegateCache.MakeArrayTypeConverterDynamicCodeSafe<CustomStruct[]>();
        Assert.True(customValueParser(new[] { "10", "20" }, CultureInfo.InvariantCulture, out var structs));
        Assert.Equal(new[] { new CustomStruct(10), new CustomStruct(20) }, structs);

        var jaggedParser = BindConverter.ParserDelegateCache.MakeArrayTypeConverterDynamicCodeSafe<int[][]>();
        Assert.True(jaggedParser(new[] { new[] { "1", "2" }, new[] { "3" } }, CultureInfo.InvariantCulture, out var jagged));
        Assert.Equal(new[] { new[] { 1, 2 }, new[] { 3 } }, jagged);

        Assert.False(intParser(new[] { "1", "not-an-int" }, CultureInfo.InvariantCulture, out var invalidInts));
        Assert.Null(invalidInts);
    }

    [Fact]
    public void MakeArrayFormatterDynamicCodeSafe_MatchesDynamicCodePath()
    {
        var intFormatter = BindConverter.FormatterDelegateCache.MakeArrayFormatterDynamicCodeSafe<int[]>();
        Assert.Equal("[\"1\", \"2\", \"3\"]", intFormatter(new[] { 1, 2, 3 }, CultureInfo.InvariantCulture));
        Assert.Equal("[]", intFormatter(Array.Empty<int>(), CultureInfo.InvariantCulture));

        var nullableIntFormatter = BindConverter.FormatterDelegateCache.MakeArrayFormatterDynamicCodeSafe<int?[]>();
        Assert.Equal("[\"1\", \"\", \"3\"]", nullableIntFormatter(new int?[] { 1, null, 3 }, CultureInfo.InvariantCulture));

        var enumFormatter = BindConverter.FormatterDelegateCache.MakeArrayFormatterDynamicCodeSafe<SomeLetters[]>();
        Assert.Equal("[\"A\", \"Q\"]", enumFormatter(new[] { SomeLetters.A, SomeLetters.Q }, CultureInfo.InvariantCulture));

        var customValueFormatter = BindConverter.FormatterDelegateCache.MakeArrayFormatterDynamicCodeSafe<CustomStruct[]>();
        Assert.Equal("[\"10\", \"20\"]", customValueFormatter(new[] { new CustomStruct(10), new CustomStruct(20) }, CultureInfo.InvariantCulture));

        var jaggedFormatter = BindConverter.FormatterDelegateCache.MakeArrayFormatterDynamicCodeSafe<int[][]>();
        Assert.Equal("[\"[\\u00221\\u0022, \\u00222\\u0022]\", \"[\\u00223\\u0022]\"]", jaggedFormatter(new[] { new[] { 1, 2 }, new[] { 3 } }, CultureInfo.InvariantCulture));
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ArrayConversionAndFormatting_WorksWhenDynamicCodeNotSupported()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", "false");

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            Assert.False(RuntimeFeature.IsDynamicCodeSupported);

            Assert.True(BindConverter.TryConvertTo<int[]>(new[] { "1", "2", "3" }, CultureInfo.InvariantCulture, out var ints));
            Assert.Equal(new[] { 1, 2, 3 }, ints);
            Assert.Equal("[\"1\", \"2\", \"3\"]", BindConverter.FormatValue(ints, CultureInfo.InvariantCulture));

            Assert.True(BindConverter.TryConvertTo<SomeLetters[]>(new[] { "A", "Q" }, CultureInfo.InvariantCulture, out var enums));
            Assert.Equal(new[] { SomeLetters.A, SomeLetters.Q }, enums);
            Assert.Equal("[\"A\", \"Q\"]", BindConverter.FormatValue(enums, CultureInfo.InvariantCulture));

            Assert.True(BindConverter.TryConvertTo<CustomStruct[]>(new[] { "7", "8" }, CultureInfo.InvariantCulture, out var structs));
            Assert.Equal(new[] { new CustomStruct(7), new CustomStruct(8) }, structs);
            Assert.Equal("[\"7\", \"8\"]", BindConverter.FormatValue(structs, CultureInfo.InvariantCulture));
        }, options);
    }

    private enum SomeLetters
    {
        A,
        B,
        C,
        Q,
    }

    [TypeConverter(typeof(CustomStructConverter))]
    private readonly record struct CustomStruct(int Id);

    private sealed class CustomStructConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => value is string text ? new CustomStruct(int.Parse(text, CultureInfo.InvariantCulture)) : base.ConvertFrom(context, culture, value);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => destinationType == typeof(string) && value is CustomStruct s ? s.Id.ToString(CultureInfo.InvariantCulture) : base.ConvertTo(context, culture, value, destinationType);
    }

    [TypeConverter(typeof(PersonConverter))]
    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }

    private class PersonConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                return JsonSerializer.Deserialize<Person>(text);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return JsonSerializer.Serialize((Person)value);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}