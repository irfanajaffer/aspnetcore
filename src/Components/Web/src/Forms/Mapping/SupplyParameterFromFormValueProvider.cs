// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

// Provides values for [SupplyParameterFromForm] parameters on components.
// It is used in two ways:
// - By default, an instance is registered in DI, supplying values outside any FormMappingScope
// - If there is a FormMappingScope, internally it creates an instance of this to implement ICascadingValueSupplier for it
internal class SupplyParameterFromFormValueProvider : ICascadingValueSupplier
{
    private readonly IFormValueMapper? _formValueMapper;
    private readonly FormMappingContext _mappingContext;

    public FormMappingContext MappingContext => _mappingContext;

    public SupplyParameterFromFormValueProvider(IFormValueMapper? formValueMapper, string mappingScopeName)
    {
        _formValueMapper = formValueMapper;
        _mappingContext = new FormMappingContext(mappingScopeName);

        MappingScopeName = mappingScopeName;
    }

    public string MappingScopeName { get; }

    bool ICascadingValueSupplier.IsFixed => true;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a FormMappingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(FormMappingContext))
        {
            return true;
        }

        // We also supply values for [SupplyValueFromForm]
        if (_formValueMapper is not null && parameterInfo.Attribute is SupplyParameterFromFormAttribute supplyParameterFromFormAttribute)
        {
            return _formValueMapper.CanMap(parameterInfo.PropertyType, MappingScopeName, GetFormName(supplyParameterFromFormAttribute));
        }

        return false;
    }

    public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
    {
        // We supply a FormMappingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(FormMappingContext))
        {
            return _mappingContext;
        }

        // We also supply values for [SupplyValueFromForm]
        if (_formValueMapper is { } valueMapper && parameterInfo.Attribute is SupplyParameterFromFormAttribute)
        {
            return GetFormPostValue(valueMapper, _mappingContext, parameterInfo);
        }

        throw new InvalidOperationException($"Received an unexpected attribute type {parameterInfo.Attribute.GetType()}");
    }

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this

    internal static object? GetFormPostValue(IFormValueMapper formValueMapper, FormMappingContext? mappingContext, in CascadingParameterInfo parameterInfo)
    {
        Debug.Assert(mappingContext != null);

        var attribute = (SupplyParameterFromFormAttribute)parameterInfo.Attribute; // Must be a valid cast because we check in CanSupplyValue
        var parameterName = attribute.Name ?? parameterInfo.PropertyName;
        var restrictToFormName = GetFormName(attribute);
        Action<string, FormattableString, string?> errorHandler = string.IsNullOrEmpty(restrictToFormName) ?
            mappingContext.AddError :
            (name, message, value) => mappingContext.AddError(restrictToFormName, parameterName, message, value);

        var context = new FormValueMappingContext(mappingContext.MappingScopeName, restrictToFormName, parameterInfo.PropertyType, parameterName)
        {
            OnError = errorHandler,
            MapErrorToContainer = mappingContext.AttachParentValue
        };

        formValueMapper.Map(context);

        return context.Result;
    }

    // Returns the form name to match against. Handler (the new property) takes
    // precedence; we fall back to FormName for backward compatibility. If both
    // are supplied this is a configuration error and we throw.
    internal static string? GetFormName(SupplyParameterFromFormAttribute attribute)
    {
        if (!string.IsNullOrEmpty(attribute.Handler) && !string.IsNullOrEmpty(attribute.FormName))
        {
            throw new InvalidOperationException(
                $"{nameof(SupplyParameterFromFormAttribute)} cannot specify both {nameof(attribute.FormName)} and {nameof(attribute.Handler)}. " +
                $"Use {nameof(attribute.Handler)} when binding to a form that submits via a GET request; otherwise use {nameof(attribute.FormName)}.");
        }

        return attribute.Handler ?? attribute.FormName;
    }
}
