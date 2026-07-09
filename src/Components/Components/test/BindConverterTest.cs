// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

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

    // The TryConvertTo overloads for unsigned integer types (uint, ushort, ulong, byte) all share
    // the same conversion path. These theories cover each scenario across all of those types so
    // the reader can see at a glance which types are exercised, and a regression in any one type
    // is caught by the same shared row. The 'typeKey' column identifies the destination type and
    // the test dispatches via a switch expression to call the strongly-typed generic overload.
    [Theory]
    [InlineData("uint", "42", 42u)]
    [InlineData("ushort", "42", (ushort)42)]
    [InlineData("ulong", "42", 42ul)]
    [InlineData("byte", "42", (byte)42)]
    public void TryConvertTo_UnsignedInteger_ValidValue(string typeKey, string incomingValue, object expected)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: false, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("uint", "-42")]
    [InlineData("ushort", "-42")]
    [InlineData("ulong", "-42")]
    [InlineData("byte", "-42")]
    public void TryConvertTo_UnsignedInteger_NegativeValueReturnsFalse(string typeKey, string incomingValue)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: false, out var actual);

        Assert.False(successfullyConverted);
        // 'actual' is boxed to object; promote to decimal so every unsigned integer type (including
        // ulong, which doesn't fit in long) can be compared uniformly against zero.
        Assert.Equal(0m, Convert.ToDecimal(actual, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("uint", "0", 0u)]
    [InlineData("ushort", "0", (ushort)0)]
    [InlineData("ulong", "0", 0ul)]
    [InlineData("byte", "0", (byte)0)]
    public void TryConvertTo_UnsignedInteger_ZeroValue(string typeKey, string incomingValue, object expected)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: false, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("uint")]
    [InlineData("ushort")]
    [InlineData("ulong")]
    [InlineData("byte")]
    public void TryConvertTo_UnsignedInteger_MaxValue(string typeKey)
    {
        var (incomingValue, expected) = typeKey switch
        {
            "uint" => (uint.MaxValue.ToString(CultureInfo.InvariantCulture), (object)uint.MaxValue),
            "ushort" => (ushort.MaxValue.ToString(CultureInfo.InvariantCulture), (object)ushort.MaxValue),
            "ulong" => (ulong.MaxValue.ToString(CultureInfo.InvariantCulture), (object)ulong.MaxValue),
            "byte" => (byte.MaxValue.ToString(CultureInfo.InvariantCulture), (object)byte.MaxValue),
            _ => throw new ArgumentException($"Unsupported type key '{typeKey}'", nameof(typeKey)),
        };

        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: false, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("uint", "4294967296")] // uint.MaxValue + 1
    [InlineData("ushort", "65536")] // ushort.MaxValue + 1
    [InlineData("ulong", "18446744073709551616")] // ulong.MaxValue + 1
    [InlineData("byte", "256")] // byte.MaxValue + 1
    public void TryConvertTo_UnsignedInteger_OverflowReturnsFalse(string typeKey, string incomingValue)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: false, out var actual);

        Assert.False(successfullyConverted);
        // 'actual' is boxed to object; promote to decimal so every unsigned integer type can be
        // compared uniformly against zero.
        Assert.Equal(0m, Convert.ToDecimal(actual, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("uint", "")]
    [InlineData("ushort", "")]
    [InlineData("ulong", "")]
    [InlineData("byte", "")]
    public void TryConvertTo_UnsignedInteger_EmptyOrNullReturnsFalse(string typeKey, string incomingValue)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: false, out var actual);

        Assert.False(successfullyConverted);
        // 'actual' is boxed to object; promote to decimal so every unsigned integer type can be
        // compared uniformly against zero.
        Assert.Equal(0m, Convert.ToDecimal(actual, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("uint")]
    [InlineData("ushort")]
    [InlineData("ulong")]
    [InlineData("byte")]
    public void TryConvertTo_UnsignedInteger_InvalidStringReturnsFalse(string typeKey)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, "not a number", isNullable: false, out var actual);

        Assert.False(successfullyConverted);
        // 'actual' is boxed to object; promote to decimal so every unsigned integer type can be
        // compared uniformly against zero.
        Assert.Equal(0m, Convert.ToDecimal(actual, CultureInfo.InvariantCulture));
    }

    // Same matrix as above, but for the Nullable<T> variants of the unsigned types. On failure the
    // out parameter is null (not default(T)), so the assertions differ from the non-nullable case.
    [Theory]
    [InlineData("uint", "42", 42u)]
    [InlineData("ushort", "42", (ushort)42)]
    [InlineData("ulong", "42", 42ul)]
    [InlineData("byte", "42", (byte)42)]
    public void TryConvertTo_NullableUnsignedInteger_ValidValue(string typeKey, string incomingValue, object expected)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: true, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("uint", "-42")]
    [InlineData("ushort", "-42")]
    [InlineData("ulong", "-42")]
    [InlineData("byte", "-42")]
    public void TryConvertTo_NullableUnsignedInteger_NegativeValueReturnsFalse(string typeKey, string incomingValue)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: true, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("uint", null)]
    [InlineData("uint", "")]
    [InlineData("ushort", null)]
    [InlineData("ushort", "")]
    [InlineData("ulong", null)]
    [InlineData("ulong", "")]
    [InlineData("byte", null)]
    [InlineData("byte", "")]
    public void TryConvertTo_NullableUnsignedInteger_ValidEmptyOrNull(string typeKey, string incomingValue)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: true, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("uint", "4294967296")]
    [InlineData("ushort", "65536")]
    [InlineData("ulong", "18446744073709551616")]
    [InlineData("byte", "256")]
    public void TryConvertTo_NullableUnsignedInteger_OverflowReturnsFalse(string typeKey, string incomingValue)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, incomingValue, isNullable: true, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("uint")]
    [InlineData("ushort")]
    [InlineData("ulong")]
    [InlineData("byte")]
    public void TryConvertTo_NullableUnsignedInteger_InvalidStringReturnsFalse(string typeKey)
    {
        var successfullyConverted = TryConvertUnsigned(typeKey, "not a number", isNullable: true, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    // Dispatcher: routes the shared test matrix above to the appropriate BindConverter.TryConvertTo<T>
    // overload. Each branch calls the strongly-typed TryConvertTo<T> overload and boxes the resulting
    // value into 'actual' (or null on failure for the nullable variant). The helper returns true iff
    // the conversion succeeded.
    private static bool TryConvertUnsigned(string typeKey, string incomingValue, bool isNullable, out object actual)
    {
        switch (typeKey)
        {
            case "uint":
                {
                    if (isNullable)
                    {
                        if (BindConverter.TryConvertTo<uint?>(incomingValue, CultureInfo.CurrentCulture, out var n))
                        {
                            // For null/empty input the parser succeeds with a null T?; surface that
                            // as actual == null so the test asserts against a true null reference.
                            actual = n.HasValue ? (object)n.Value : null;
                            return true;
                        }
                        actual = null;
                        return false;
                    }
                    if (BindConverter.TryConvertTo<uint>(incomingValue, CultureInfo.CurrentCulture, out var v))
                    {
                        actual = v;
                        return true;
                    }
                    actual = 0;
                    return false;
                }
            case "ushort":
                {
                    if (isNullable)
                    {
                        if (BindConverter.TryConvertTo<ushort?>(incomingValue, CultureInfo.CurrentCulture, out var n))
                        {
                            actual = n.HasValue ? (object)n.Value : null;
                            return true;
                        }
                        actual = null;
                        return false;
                    }
                    if (BindConverter.TryConvertTo<ushort>(incomingValue, CultureInfo.CurrentCulture, out var v))
                    {
                        actual = v;
                        return true;
                    }
                    actual = (ushort)0;
                    return false;
                }
            case "ulong":
                {
                    if (isNullable)
                    {
                        if (BindConverter.TryConvertTo<ulong?>(incomingValue, CultureInfo.CurrentCulture, out var n))
                        {
                            actual = n.HasValue ? (object)n.Value : null;
                            return true;
                        }
                        actual = null;
                        return false;
                    }
                    if (BindConverter.TryConvertTo<ulong>(incomingValue, CultureInfo.CurrentCulture, out var v))
                    {
                        actual = v;
                        return true;
                    }
                    actual = 0ul;
                    return false;
                }
            case "byte":
                {
                    if (isNullable)
                    {
                        if (BindConverter.TryConvertTo<byte?>(incomingValue, CultureInfo.CurrentCulture, out var n))
                        {
                            actual = n.HasValue ? (object)n.Value : null;
                            return true;
                        }
                        actual = null;
                        return false;
                    }
                    if (BindConverter.TryConvertTo<byte>(incomingValue, CultureInfo.CurrentCulture, out var v))
                    {
                        actual = v;
                        return true;
                    }
                    actual = (byte)0;
                    return false;
                }
            default:
                throw new ArgumentException($"Unsupported type key '{typeKey}'", nameof(typeKey));
        }
    }

    [Theory]
    [InlineData("42", (sbyte)42)]
    [InlineData("-42", (sbyte)-42)]
    [InlineData("127", sbyte.MaxValue)] // sbyte.MaxValue
    [InlineData("-128", sbyte.MinValue)] // sbyte.MinValue
    public void TryConvertTo_SByte_ValidValue(string incomingValue, sbyte expected)
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("128")] // sbyte.MaxValue + 1
    [InlineData("-129")] // sbyte.MinValue - 1
    [InlineData("not a number")]
    public void TryConvertTo_SByte_InvalidValueReturnsFalse(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(sbyte), actual);
    }

    [Theory]
    [InlineData("-42", (sbyte)-42)]
    [InlineData("42", (sbyte)42)]
    public void TryConvertTo_NullableSByte_ValidValue(string incomingValue, sbyte expected)
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableSByte_ValidEmptyOrNull(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("128")] // sbyte.MaxValue + 1
    [InlineData("-129")] // sbyte.MinValue - 1
    [InlineData("not a number")]
    public void TryConvertTo_NullableSByte_InvalidValueReturnsFalse(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_TypeConverter_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<Person>("not valid json", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    private enum SomeLetters
    {
        A,
        B,
        C,
        Q,
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
