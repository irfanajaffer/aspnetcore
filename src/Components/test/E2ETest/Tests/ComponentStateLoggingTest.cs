// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class ComponentStateLoggingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    // Event IDs from Renderer.Log.cs
    private const int SkippingCascadingUpdateOnDisposedComponentEventId = 7;
    private const int SkippingRenderOnDisposedComponentEventId = 9;

    public ComponentStateLoggingTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void EndToEnd_DisposedComponentActions_AreLoggedToBrowserConsole()
    {
        // Arrange
        Navigate(ServerPathBase);
        var appElement = Browser.MountTestComponent<DisposedComponentLoggingCases>();
        var toggleChildButton = appElement.FindElement(By.Id("toggle-child"));
        var triggerCascadingUpdateButton = appElement.FindElement(By.Id("trigger-cascading-update"));

        // Act 1: Dispose the child component by toggling it off
        toggleChildButton.Click();

        // Act 2: Trigger a cascading parameter update on the (now disposed) child
        triggerCascadingUpdateButton.Click();

        // Assert
        var browserLogs = Browser.GetBrowserLogs(LogLevel.Debug);

        // Verify that a render was skipped on the disposed component
        Assert.Contains(browserLogs, log =>
            log.LogLevel >= LogLevel.Debug &&
            log.Message.Contains($"[{SkippingRenderOnDisposedComponentEventId}]") &&
            log.Message.Contains("Skipping render for component"));

        // Verify that a cascading value update was skipped on the disposed component
        Assert.Contains(browserLogs, log =>
            log.LogLevel >= LogLevel.Debug &&
            log.Message.Contains($"[{SkippingCascadingUpdateOnDisposedComponentEventId}]") &&
            log.Message.Contains("Skipping cascading parameter update for component"));
    }
}