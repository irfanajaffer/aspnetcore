// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that the value of the associated property should be supplied from
/// the form data for the form with the specified name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SupplyParameterFromFormAttribute : CascadingParameterAttributeBase
{
    /// <summary>
    /// Gets or sets the name of the form value. If not specified, the property name will be used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the name of the form that provides this value. If not specified,
    /// the value will be mapped from any incoming form post within the current form
    /// mapping scope. If specified, the value will only be mapped from a form with
    /// the specified name in the current mapping scope.
    /// </summary>
    public string? FormName { get; set; }

    /// <summary>
    /// Gets or sets the name of the form handler. When set, this property binds the
    /// associated parameter to the form data that the named form handler supplies,
    /// regardless of whether the data is provided as a <c>POST</c> body (the default)
    /// or as a <c>GET</c> request's query string. The handler name must match the
    /// <c>FormName</c> of the form that should provide the data.
    /// </summary>
    /// <remarks>
    /// This is the recommended way to bind a complex model to a form whose
    /// <c>method</c> attribute is <c>get</c>. When a form is submitted with
    /// <c>method="get"</c>, the browser encodes the form fields into the URL
    /// query string, and the same complex model binding pipeline used for
    /// <c>POST</c> requests is used to populate the value.
    /// </remarks>
    public string? Handler { get; set; }

    /// <inheritdoc />
    internal override bool SingleDelivery => true;
}
