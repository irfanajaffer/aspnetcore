// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

// Regression coverage for the SSR-only deselect-all multi-InputSelect scenario.
// The existing InputSelectMultiple_EditForm_DeselectingAll_DoesNotThrow test in
// FormsTest.cs runs under Blazor Server (InteractiveServer) and Blazor WebAssembly,
// where the empty selection is delivered as an empty array by the JS interop or
// circuit. In pure static SSR, no JS interop is involved: the multi-InputSelect is
// rendered as a <select multiple> inside an <EditForm>, and on POST the bound
// [SupplyParameterFromForm] property is deserialized from the form data. When all
// options are deselected, the form posts no value for the multi-select key, and the
// bound array must be received as an empty array (not null) so that re-rendering the
// page does not throw a NullReferenceException on accessing .Length.
public class InputSelectMultipleDeselectAllTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    public InputSelectMultipleDeselectAllTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void InputSelectMultiple_EditForm_DeselectingAll_Ssr_DeserializesAsEmptyArray()
    {
        Navigate($"{ServerPathBase}/forms/input-select-multiple-deselect-all");

        // Sanity check: the page rendered through static SSR as a multi-select
        // exposing all four city options.
        var citiesSelect = new SelectElement(Browser.FindElement(By.Id("cities")));
        Browser.True(() => citiesSelect.IsMultiple);
        Browser.Equal(4, () => citiesSelect.Options.Count);

        // Drive the browser into the starting state for the issue scenario: a
        // multi-select with two options selected. Note: in static SSR the bound
        // value is not pre-applied to the <option> elements' `selected`
        // attribute (InputSelect only emits a single `value` attribute on the
        // <select>, which doesn't preselect for multi-select), so we drive the
        // initial selection through the DOM like a user would.
        citiesSelect.SelectByText("San Francisco");
        citiesSelect.SelectByText("Tokyo");
        Browser.Equal(2, () => citiesSelect.AllSelectedOptions.Count);

        // Deselect all options and submit. In SSR this produces a POST whose
        // form data contains no entry for the multi-select, so
        // [SupplyParameterFromForm] must resolve SelectedCities to an empty
        // array, not null. If it resolves to null, re-rendering the page would
        // NRE on SelectedCities.Length (or fall back to -1 via the
        // `?? -1` in the markup).
        citiesSelect.DeselectByText("San Francisco");
        citiesSelect.DeselectByText("Tokyo");
        Browser.Equal(0, () => citiesSelect.AllSelectedOptions.Count);

        Browser.FindElement(By.Id("send")).Click();

        Browser.Equal("Submitted. Length: 0", () => Browser.FindElement(By.Id("result")).Text);
        Browser.Equal("Submitted. IsNull: False", () => Browser.FindElement(By.Id("result-null")).Text);
    }
}
