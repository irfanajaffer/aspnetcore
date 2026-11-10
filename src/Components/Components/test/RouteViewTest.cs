// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Test;

public class RouteViewTest
{
    private readonly TestRenderer _renderer;
    private readonly RouteViewTestNavigationManager _navigationManager;
    private readonly RouteView _routeViewComponent;
    private readonly int _routeViewComponentId;

    public RouteViewTest()
    {
        var serviceCollection = new ServiceCollection();
        _navigationManager = new RouteViewTestNavigationManager();
        serviceCollection.AddSingleton<NavigationManager>(_navigationManager);
        var services = serviceCollection.BuildServiceProvider();
        _renderer = new TestRenderer(services);

        var componentFactory = new ComponentFactory(new DefaultComponentActivator(services), new DefaultComponentPropertyActivator(), _renderer);
        _routeViewComponent = (RouteView)componentFactory.InstantiateComponent(services, typeof(RouteView), null, null);

        _routeViewComponentId = _renderer.AssignRootComponentId(_routeViewComponent);
    }

    [Fact]
    public void ThrowsIfNoRouteDataSupplied()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = _routeViewComponent.SetParametersAsync(ParameterView.Empty);
        });

        Assert.Equal($"The {nameof(RouteView)} component requires a non-null value for the parameter {nameof(RouteView.RouteData)}.", ex.Message);
    }

    [Fact]
    public void RendersPageInsideLayoutView()
    {
        var routeParams = new Dictionary<string, object>
            {
                { nameof(ComponentWithLayout.Message), "Test message" }
            };
        var routeData = new RouteData(typeof(ComponentWithLayout), routeParams);

        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
            })));

        var batch = _renderer.Batches.Single();
        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)typeof(TestLayout), sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));

        var layoutViewComponentId = batch.GetComponentFrames<LayoutView>().Single().ComponentId;
        var layoutViewFrames = _renderer.GetCurrentRenderTreeFrames(layoutViewComponentId).AsEnumerable();
        Assert.Collection(layoutViewFrames,
            frame => AssertFrame.Component<TestLayout>(frame, subtreeLength: 2, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutComponentBase.Body), sequence: 1));

        var testLayoutComponentId = batch.GetComponentFrames<TestLayout>().Single().ComponentId;
        var testLayoutFrames = _renderer.GetCurrentRenderTreeFrames(testLayoutComponentId).AsEnumerable();
        Assert.Collection(testLayoutFrames,
            frame => AssertFrame.Text(frame, "Layout starts here", sequence: 0),
            frame => AssertFrame.Region(frame, subtreeLength: 3),
            frame => AssertFrame.Component<ComponentWithLayout>(frame, sequence: 0, subtreeLength: 2),
            frame => AssertFrame.Attribute(frame, nameof(ComponentWithLayout.Message), "Test message", sequence: 1),
            frame => AssertFrame.Text(frame, "Layout ends here", sequence: 2));

        var pageComponentId = batch.GetComponentFrames<ComponentWithLayout>().Single().ComponentId;
        var pageFrames = _renderer.GetCurrentRenderTreeFrames(pageComponentId).AsEnumerable();
        Assert.Collection(pageFrames,
            frame => AssertFrame.Text(frame, "Hello from the page with message 'Test message'", sequence: 0));

        Assert.Equal(4, batch.DiffsInOrder.Count);
    }

    [Fact]
    public void UsesDefaultLayoutIfNoneSetOnPage()
    {
        var routeData = new RouteData(typeof(ComponentWithoutLayout), new Dictionary<string, object>());

        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
                { nameof(RouteView.DefaultLayout), typeof(OtherLayout) },
            })));

        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)typeof(OtherLayout), sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));
    }

    [Fact]
    public void UsesNoLayoutIfNoneSetOnPageAndNoDefaultSet()
    {
        var routeData = new RouteData(typeof(ComponentWithoutLayout), new Dictionary<string, object>());

        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
            })));

        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)null, sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));
    }

    [Fact]
    public void PageLayoutSupersedesDefaultLayout()
    {
        var routeData = new RouteData(typeof(ComponentWithLayout), new Dictionary<string, object>());

        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
                { nameof(RouteView.DefaultLayout), typeof(OtherLayout) },
            })));

        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)typeof(TestLayout), sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));
    }

    [Fact]
    public async Task ClearsBodyWhenLayoutBecomesRoutedPage()
    {
        var childRoute = new RouteData(typeof(ComponentWithRoutableLayout), new Dictionary<string, object>());
        var parentRoute = new RouteData(typeof(RoutableLayout), new Dictionary<string, object>
        {
            [nameof(RoutableLayout.Message)] = "Parent page",
        });

        await SetRouteAsync(childRoute);
        var parentFrame = _renderer.Batches.Single().GetComponentFrames<RoutableLayout>().Single();
        Assert.NotNull(((RoutableLayout)parentFrame.Component).Body);

        await SetRouteAsync(parentRoute);

        var parentPageFrame = _renderer.Batches.Last().GetComponentFrames<RoutableLayout>().Single();
        var parentPage = (RoutableLayout)parentPageFrame.Component;
        Assert.NotEqual(parentFrame.ComponentId, parentPageFrame.ComponentId);
        Assert.Null(parentPage.Body);
        Assert.Equal("Parent page", parentPage.Message);

        await SetRouteAsync(childRoute);

        var returnedLayoutFrame = _renderer.Batches.Last().GetComponentFrames<RoutableLayout>().Single();
        Assert.NotEqual(parentPageFrame.ComponentId, returnedLayoutFrame.ComponentId);
        Assert.NotNull(((RoutableLayout)returnedLayoutFrame.Component).Body);
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task RecreatesCustomLayoutWhenItBecomesRoutedPage()
    {
        var parentRoute = new RouteData(typeof(CustomRoutableLayout), new Dictionary<string, object>());
        var childRoute = new RouteData(typeof(ComponentWithCustomRoutableLayout), new Dictionary<string, object>());

        await SetRouteAsync(childRoute);

        var layoutFrame = _renderer.Batches.Single().GetComponentFrames<CustomRoutableLayout>().Single();
        var outerLayoutId = _renderer.Batches.Single().GetComponentFrames<TestLayout>().Single().ComponentId;
        Assert.NotNull(((CustomRoutableLayout)layoutFrame.Component).Body);

        await SetRouteAsync(parentRoute);

        var pageFrame = _renderer.GetCurrentRenderTreeFrames(outerLayoutId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(CustomRoutableLayout));
        Assert.NotEqual(layoutFrame.ComponentId, pageFrame.ComponentId);
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(pageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Region(frame, subtreeLength: 2),
            frame => AssertFrame.Text(frame, "Default body", sequence: 0));
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task PreservesConstructorInitializedBodyOnDirectArrival()
    {
        await SetRouteAsync(new RouteData(typeof(InitializedBodyPage), new Dictionary<string, object>()));

        var pageFrame = _renderer.Batches.Single().GetComponentFrames<InitializedBodyPage>().Single();
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(pageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Region(frame, subtreeLength: 2),
            frame => AssertFrame.Text(frame, "Default body", sequence: 0));
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task PreservesPageInstanceWhenRouteParametersChange()
    {
        await SetRouteAsync(new RouteData(typeof(ComponentWithoutLayout), new Dictionary<string, object>
        {
            [nameof(ComponentWithoutLayout.Message)] = "First",
        }));
        var pageFrame = _renderer.Batches.Single().GetComponentFrames<ComponentWithoutLayout>().Single();
        var layoutViewId = _renderer.Batches.Single().GetComponentFrames<LayoutView>().Single().ComponentId;

        await SetRouteAsync(new RouteData(typeof(ComponentWithoutLayout), new Dictionary<string, object>
        {
            [nameof(ComponentWithoutLayout.Message)] = "Second",
        }));

        var newPageFrame = _renderer.GetCurrentRenderTreeFrames(layoutViewId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(ComponentWithoutLayout));
        Assert.Equal(pageFrame.ComponentId, newPageFrame.ComponentId);
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(newPageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Text(frame, "Hello from the page with message 'Second'", sequence: 0));
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task RendersPageWhenLayoutInvokesBodyDirectlyMoreThanOnce()
    {
        var routeData = new RouteData(typeof(InitializedBodyPage), new Dictionary<string, object>());
        await _renderer.Dispatcher.InvokeAsync(() =>
            _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(RouteView.RouteData)] = routeData,
                [nameof(RouteView.DefaultLayout)] = typeof(RepeatingBodyLayout),
            })));
        var layoutId = _renderer.Batches.Single().GetComponentFrames<RepeatingBodyLayout>().Single().ComponentId;
        var originalPages = _renderer.GetCurrentRenderTreeFrames(layoutId).AsEnumerable()
            .Where(frame => frame.ComponentType == typeof(InitializedBodyPage)).ToArray();
        Assert.Equal(2, originalPages.Length);

        await SetRouteAsync(routeData);

        Assert.Empty(_renderer.HandledExceptions);
        var currentPages = _renderer.GetCurrentRenderTreeFrames(layoutId).AsEnumerable()
            .Where(frame => frame.ComponentType == typeof(InitializedBodyPage)).ToArray();
        Assert.Equal(originalPages.Select(frame => frame.ComponentId), currentPages.Select(frame => frame.ComponentId));
        foreach (var page in currentPages)
        {
            Assert.Collection(
                _renderer.GetCurrentRenderTreeFrames(page.ComponentId).AsEnumerable(),
                frame => AssertFrame.Region(frame, subtreeLength: 2),
                frame => AssertFrame.Text(frame, "Default body", sequence: 0));
        }
    }

    private Task SetRouteAsync(RouteData routeData) => _renderer.Dispatcher.InvokeAsync(() =>
        _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(RouteView.RouteData)] = routeData,
        })));

    [Layout(typeof(TestLayout))]
    private class RoutableLayout : LayoutComponentBase
    {
        [Parameter] public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Message);
            builder.AddContent(1, Body);
        }
    }

    private class InitializedBodyPage : LayoutComponentBase
    {
        public InitializedBodyPage()
        {
            Body = builder => builder.AddContent(0, "Default body");
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }

    [Layout(typeof(TestLayout))]
    private class CustomRoutableLayout : ComponentBase
    {
        public CustomRoutableLayout()
        {
            Body = builder => builder.AddContent(0, "Default body");
        }

        [Parameter] public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }

    [Layout(typeof(CustomRoutableLayout))]
    private class ComponentWithCustomRoutableLayout : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Child page");
        }
    }

    [Layout(typeof(RoutableLayout))]
    private class ComponentWithRoutableLayout : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Child page");
        }
    }

    private class RepeatingBodyLayout : ComponentBase
    {
        [Parameter] public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Body(builder);
            Body(builder);
        }
    }

    private class RouteViewTestNavigationManager : NavigationManager
    {
        public RouteViewTestNavigationManager() =>
            Initialize("https://www.example.com/subdir/", "https://www.example.com/subdir/");

        public void NotifyLocationChanged(string uri)
        {
            Uri = uri;
            NotifyLocationChanged(false);
        }
    }

    private class ComponentWithoutLayout : AutoRenderComponent
    {
        [Parameter] public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"Hello from the page with message '{Message}'");
        }
    }

    [Layout(typeof(TestLayout))]
    private class ComponentWithLayout : AutoRenderComponent
    {
        [Parameter] public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"Hello from the page with message '{Message}'");
        }
    }

    private class TestLayout : AutoRenderComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Layout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "Layout ends here");
        }
    }

    private class OtherLayout : AutoRenderComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "OtherLayout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "OtherLayout ends here");
        }
    }
}
