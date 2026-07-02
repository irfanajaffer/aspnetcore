// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components;

public class ComponentStateLoggingTests
{
    // Event IDs from Renderer.Log.cs
    private const int SkippingCascadingUpdateOnDisposedComponentEventId = 7;
    private const int StoppedSingleDeliveryCascadingParametersEventId = 8;
    private const int SkippingRenderOnDisposedComponentEventId = 9;
    private const int SupplyingCombinedParametersEventId = 10;

    #region Helper Classes and Interfaces

    private class SimpleTestComponent : IComponent
    {
        public RenderHandle RenderHandle { get; private set; }

        public void Attach(RenderHandle renderHandle) => RenderHandle = renderHandle;
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    // Capturing logger for verifying log output
    private class CapturingLogger : ILogger
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }

    private record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }

    private class CapturingLoggerFactory : ILoggerFactory
    {
        public CapturingLogger Logger { get; } = new();
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => Logger;
        public void Dispose() { }
    }

    private class TestRendererWithLogger : TestRenderer
    {
        public TestRendererWithLogger(CapturingLoggerFactory loggerFactory) : base(loggerFactory)
        {
        }
    }

    #endregion

    [Fact]
    public async Task RenderIntoBatch_WhenDisposed_SkipsRenderAndLogs()
    {
        // Arrange
        var loggerFactory = new CapturingLoggerFactory();
        var renderer = new TestRendererWithLogger(loggerFactory);
        var component = new SimpleTestComponent();
        var componentState = new ComponentState(renderer, 123, component, null);
        var batchBuilder = new RenderBatchBuilder();

        // Act: dispose then attempt to render
        await componentState.DisposeAsync();
        componentState.RenderIntoBatch(batchBuilder, builder => builder.AddMarkupContent(0, "hello"), out var ex);

        // Assert
        Assert.Null(ex);
        Assert.Equal(0, batchBuilder.UpdatedComponentDiffs.Count);

        var logEntry = Assert.Single(loggerFactory.Logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.Level);
        Assert.Equal(SkippingRenderOnDisposedComponentEventId, logEntry.EventId);
        Assert.Contains("Skipping render for component 123 (SimpleTestComponent) because it was disposed before rendering", logEntry.Message);
    }

    [Fact]
    public async Task NotifyCascadingValueChanged_WhenDisposed_SkipsUpdateAndLogs()
    {
        // Arrange
        var loggerFactory = new CapturingLoggerFactory();
        var renderer = new TestRendererWithLogger(loggerFactory);
        var component = new SimpleTestComponent();
        var componentState = new ComponentState(renderer, 124, component, null);

        // Act: dispose then notify
        await componentState.DisposeAsync();
        componentState.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);

        // Assert: should not throw, and should log
        var logEntry = Assert.Single(loggerFactory.Logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.Level);
        Assert.Equal(SkippingCascadingUpdateOnDisposedComponentEventId, logEntry.EventId);
        Assert.Contains("Skipping cascading parameter update for component 124 (SimpleTestComponent): component was already disposed", logEntry.Message);
    }

    [Fact]
    public void SetDirectParameters_WithSingleDeliveryParam_StopsSupplyingAndLogs()
    {
        // Arrange: component with single-delivery cascading parameters
        var loggerFactory = new CapturingLoggerFactory();
        var renderer = new TestRendererWithLogger(loggerFactory);
        var parentComponent = new SimpleTestComponent();
        var parentState = new ComponentState(renderer, 1, parentComponent, null);

        var supplier = new SupplyParameterWithSingleDeliveryComponent(isFixed: true);
        var supplierState = new ComponentState(renderer, 2, supplier, parentState);

        var consumer = new SingleDeliveryConsumerComponent();
        var consumerState = new ComponentState(renderer, 3, consumer, supplierState);

        // Act: SetDirectParameters triggers the single-delivery teardown and parameter supply
        consumerState.SetDirectParameters(ParameterView.Empty);

        // Assert: Two logs are expected: one for stopping single-delivery, one for supplying params.
        Assert.Equal(2, loggerFactory.Logger.Entries.Count);

        var stoppedLog = loggerFactory.Logger.Entries.First(e => e.EventId.Id == StoppedSingleDeliveryCascadingParametersEventId);
        Assert.Equal(LogLevel.Debug, stoppedLog.Level);
        Assert.Contains("Stopped supplying single-delivery cascading parameters to component 3 (SingleDeliveryConsumerComponent)", stoppedLog.Message);

        var suppliedLog = loggerFactory.Logger.Entries.First(e => e.EventId.Id == SupplyingCombinedParametersEventId);
        Assert.Equal(LogLevel.Trace, suppliedLog.Level);
        Assert.Contains("Supplying combined parameters to component 3 (SingleDeliveryConsumerComponent)", suppliedLog.Message);
    }

    [Fact]
    public void SetDirectParameters_WithNoSingleDeliveryParam_OnlyLogsSupplyingParameters()
    {
        // Arrange
        var loggerFactory = new CapturingLoggerFactory();
        var renderer = new TestRendererWithLogger(loggerFactory);
        var parentComponent = new SimpleTestComponent();
        var parentState = new ComponentState(renderer, 1, parentComponent, null);

        var childComponent = new SimpleTestComponent();
        var childState = new ComponentState(renderer, 2, childComponent, parentState);

        // Act: SetDirectParameters triggers parameter supply
        childState.SetDirectParameters(ParameterView.Empty);

        // Assert: Only the trace log for supplying parameters should be present.
        var logEntry = Assert.Single(loggerFactory.Logger.Entries);
        Assert.Equal(LogLevel.Trace, logEntry.Level);
        Assert.Equal(SupplyingCombinedParametersEventId, logEntry.EventId);
        Assert.Contains("Supplying combined parameters to component 2 (SimpleTestComponent)", logEntry.Message);
    }

    [Fact]
    public async Task FullLifecycle_LogsCorrectEventsInSequence()
    {
        // Arrange
        var loggerFactory = new CapturingLoggerFactory();
        var renderer = new TestRendererWithLogger(loggerFactory);
        var parentComponent = new SimpleTestComponent();
        var parentState = new ComponentState(renderer, 1, parentComponent, null);

        var supplier = new SupplyParameterWithSingleDeliveryComponent(isFixed: true);
        var supplierState = new ComponentState(renderer, 2, supplier, parentState);

        var consumer = new SingleDeliveryConsumerComponent();
        var consumerState = new ComponentState(renderer, 3, consumer, supplierState);
        var batchBuilder = new RenderBatchBuilder();

        // Act: initial parameter supply (triggers single-delivery teardown path)
        consumerState.SetDirectParameters(ParameterView.Empty);

        // Act: dispose consumer then try to notify (should no-op via disposed check)
        await consumerState.DisposeAsync();
        consumerState.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);

        // Act: dispose then try to render (should no-op via disposed check)
        consumerState.RenderIntoBatch(batchBuilder, builder => builder.AddMarkupContent(0, "hello"), out _);

        // Assert: all paths completed without throwing and logged correctly
        Assert.Equal(0, batchBuilder.UpdatedComponentDiffs.Count);
        var logs = loggerFactory.Logger.Entries.ToArray();
        Assert.Equal(4, logs.Length);
        Assert.Equal(SupplyingCombinedParametersEventId, logs[0].EventId);
        Assert.Equal(StoppedSingleDeliveryCascadingParametersEventId, logs[1].EventId);
        Assert.Equal(SkippingCascadingUpdateOnDisposedComponentEventId, logs[2].EventId);
        Assert.Equal(SkippingRenderOnDisposedComponentEventId, logs[3].EventId);
    }

    #endregion

    #region Test component types for single-delivery cascading parameter testing

    private class SupplyParameterWithSingleDeliveryAttribute : CascadingParameterAttributeBase
    {
        internal override bool SingleDelivery => true;
    }

    private class SupplyParameterWithSingleDeliveryComponent : ComponentBase, ICascadingValueSupplier
    {
        public bool IsFixed { get; }

        public SupplyParameterWithSingleDeliveryComponent(bool isFixed) => IsFixed = isFixed;

        public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
            => parameterInfo.Attribute is SupplyParameterWithSingleDeliveryAttribute;

        public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();

        public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();

        public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();
    }

    private class SingleDeliveryConsumerComponent : IComponent
    {
        public RenderHandle RenderHandle { get; private set; }

        [CascadingParameter]
        [SupplyParameterWithSingleDelivery]
        public string? CascadingValue { get; set; }

        public void Attach(RenderHandle renderHandle) => RenderHandle = renderHandle;
        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.TryGetValue<string>(nameof(CascadingValue), out var value);
            CascadingValue = value ?? string.Empty;
            return Task.CompletedTask;
        }
    }

    #endregion
}
