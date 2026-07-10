// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms.PostHandling;

public class SupplyParameterFromFormTest
{
    [Fact]
    public async Task FindCascadingParameters_HandlesSupplyParameterFromFormValues()
    {
        var renderer = CreateRendererWithFormValueModelBinder();
        var formComponent = new FormParametersComponent();

        var componentId = renderer.AssignRootComponentId(formComponent);
        await renderer.RenderRootComponentAsync(componentId);
        var formComponentState = renderer.GetComponentState(formComponent);

        var result = CascadingParameterState.FindCascadingParameters(formComponentState, out _);

        var supplier = Assert.Single(result);
        Assert.IsType<SupplyParameterFromFormValueProvider>(supplier.ValueSupplier);
    }

    [Fact]
    public async Task FindCascadingParameters_HandlesSupplyParameterFromFormValues_WithMappingScopeName()
    {
        var renderer = CreateRendererWithFormValueModelBinder();
        var formMappingScope = new FormMappingScope
        {
            Name = "scope-name",
            FormValueModelBinder = new TestFormModelValueBinder("[scope-name]handler-name"),
            ChildContent = modelBindingContext => builder =>
            {
                builder.OpenComponent<FormParametersComponentWithName>(0);
                builder.CloseComponent();
            }
        };

        var componentId = renderer.AssignRootComponentId(formMappingScope);
        await renderer.RenderRootComponentAsync(componentId);
        var formComponentState = renderer.Batches.Single()
            .GetComponentFrames<FormParametersComponentWithName>().Single()
            .ComponentState;

        var result = CascadingParameterState.FindCascadingParameters(formComponentState, out _);

        var supplier = Assert.Single(result);
        Assert.Equal(formMappingScope, supplier.ValueSupplier);
    }

    [Fact]
    public async Task FindCascadingParameters_HandlesSupplyParameterFromFormValues_WithHandler()
    {
        var renderer = CreateRendererWithFormValueModelBinder();
        var formComponent = new FormParametersComponentWithHandler();

        var componentId = renderer.AssignRootComponentId(formComponent);
        await renderer.RenderRootComponentAsync(componentId);
        var formComponentState = renderer.GetComponentState(formComponent);

        var result = CascadingParameterState.FindCascadingParameters(formComponentState, out _);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindCascadingParameters_HandlesSupplyParameterFromFormValues_WithHandler_WhenBinderMatches()
    {
        var renderer = CreateRendererWithFormValueModelBinder(handlerName: "handler-name");
        var formComponent = new FormParametersComponentWithHandler();

        var componentId = renderer.AssignRootComponentId(formComponent);
        await renderer.RenderRootComponentAsync(componentId);
        var formComponentState = renderer.GetComponentState(formComponent);

        var result = CascadingParameterState.FindCascadingParameters(formComponentState, out _);

        var supplier = Assert.Single(result);
        Assert.IsType<SupplyParameterFromFormValueProvider>(supplier.ValueSupplier);
    }

    [Fact]
    public void GetFormName_ThrowsWhenBothHandlerAndFormNameAreSet()
    {
        var attribute = new SupplyParameterFromFormAttribute
        {
            FormName = "form-name",
            Handler = "handler-name",
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SupplyParameterFromFormValueProvider.GetFormName(attribute));
        Assert.Contains("FormName", ex.Message);
        Assert.Contains("Handler", ex.Message);
    }

    [Fact]
    public void GetFormName_ReturnsHandlerWhenOnlyHandlerIsSet()
    {
        var attribute = new SupplyParameterFromFormAttribute { Handler = "handler-name" };
        Assert.Equal("handler-name", SupplyParameterFromFormValueProvider.GetFormName(attribute));
    }

    [Fact]
    public void GetFormName_ReturnsFormNameWhenOnlyFormNameIsSet()
    {
        var attribute = new SupplyParameterFromFormAttribute { FormName = "form-name" };
        Assert.Equal("form-name", SupplyParameterFromFormValueProvider.GetFormName(attribute));
    }

    [Fact]
    public void GetFormName_ReturnsNullWhenNeitherIsSet()
    {
        var attribute = new SupplyParameterFromFormAttribute();
        Assert.Null(SupplyParameterFromFormValueProvider.GetFormName(attribute));
    }

    static TestRenderer CreateRendererWithFormValueModelBinder(string handlerName = "")
    {
        var services = new ServiceCollection();
        var valueBinder = new TestFormModelValueBinder(handlerName);
        services.AddSingleton<IFormValueMapper>(valueBinder);
        services.AddSingleton<ICascadingValueSupplier>(_ => new SupplyParameterFromFormValueProvider(
            valueBinder, mappingScopeName: ""));
        return new TestRenderer(services.BuildServiceProvider());
    }

    class FormParametersComponent : TestComponentBase
    {
        [SupplyParameterFromForm] public string FormParameter { get; set; }
    }

    class FormParametersComponentWithName : TestComponentBase
    {
        [SupplyParameterFromForm(FormName = "handler-name")] public string FormParameter { get; set; }
    }

    class FormParametersComponentWithHandler : TestComponentBase
    {
        [SupplyParameterFromForm(Handler = "handler-name")] public string FormParameter { get; set; }
    }

    class TestFormModelValueBinder(string IncomingScopeQualifiedFormName = "") : IFormValueMapper
    {
        public void Map(FormValueMappingContext context) { }

        public bool CanMap(Type valueType, string mappingScopeName, string formName)
        {
            if (string.IsNullOrEmpty(mappingScopeName))
            {
                return IncomingScopeQualifiedFormName == (formName ?? string.Empty);
            }
            else
            {
                return IncomingScopeQualifiedFormName == $"[{mappingScopeName}]{formName ?? string.Empty}";
            }
        }
    }

    class TestComponentBase : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
            => Task.CompletedTask;
    }
}
