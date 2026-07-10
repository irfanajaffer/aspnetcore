// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class HttpContextFormValueMapperTest
{
    // Cases with no scope in effect
    [InlineData(true, "some-form", "", null)]           // No form restriction
    [InlineData(true, "some-form", "", "some-form")]    // Matching form
    [InlineData(false, "some-form", "", "other-form")]  // Mismatching form
    [InlineData(false, "some-form", "x", "some-form")]  // Mismatching scope

    // Cases with scope in effect
    [InlineData(true, "[scope-name]some-form", "scope-name", null)]             // Matching scope, no form restriction
    [InlineData(true, "[scope-name]some-form", "scope-name", "some-form")]      // Matching scope, matching form
    [InlineData(false, "[scope-name]some-form", "scope-name", "other-form")]    // Matching scope, mismatching form
    [InlineData(false, "[scope-name]some-form", "other-scope", null)]           // Mismatching scope, no form restriction
    [InlineData(false, "[scope-name]some-form", "other-scope", "some-form")]    // Mismatching scope, matching form
    [InlineData(false, "[scope-name]some-form", "other-scope", "other-form")]   // Mismatching scope, mismatching form
    [InlineData(false, "[scope]", "longerstring", null)] // Show we don't try to read too many characters from the scope section

    // Invalid incoming form handler name
    [InlineData(false, "[something", "something", null)] // Unterminated scope name shouldn't match on scope
    [InlineData(false, "[something", "", "something")] // Unterminated scope name shouldn't match on form
    [InlineData(false, "something]", "something", null)]
    [InlineData(false, "something]", "", "something")]
    [InlineData(false, "[a][b]", "b", null)] // Scope name is only counted as the first bracketed item
    [Theory]
    public void CanMap_MatchesOnScopeAndFormName(bool expectedResult, string incomingFormName, string scopeName, string formNameOrNull)
    {
        var formData = new HttpContextFormDataProvider();
        formData.SetFormData(incomingFormName, new Dictionary<string, StringValues>(), new FormFileCollection());

        var mapper = new HttpContextFormValueMapper(formData, Options.Create<RazorComponentsServiceOptions>(new()));

        var canMap = mapper.CanMap(typeof(string), scopeName, formNameOrNull);
        Assert.Equal(expectedResult, canMap);
    }

    [Fact]
    public void DefaultState_IsNoneAndEmpty()
    {
        var formData = new HttpContextFormDataProvider();

        Assert.False(formData.TryGetIncomingHandlerName(out _));
        Assert.Equal(FormDataSource.None, formData.FormDataSource);
        Assert.Empty(formData.Entries);
        Assert.Empty(formData.FormFiles);
    }

    [Fact]
    public void SetFormData_MarksSourceAsFormPost()
    {
        var formData = new HttpContextFormDataProvider();
        formData.SetFormData("handler", new Dictionary<string, StringValues>(), new FormFileCollection());

        Assert.Equal(FormDataSource.FormPost, formData.FormDataSource);
        Assert.True(formData.TryGetIncomingHandlerName(out var handler));
        Assert.Equal("handler", handler);
    }

    [Fact]
    public void SetFormDataFromQuery_MarksSourceAsFormGet_AndExposesEntries()
    {
        var formData = new HttpContextFormDataProvider();
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Filter.Query"] = "hello",
            ["Filter.Page"] = "2",
            ["Filter.InStockOnly"] = "true",
        });

        formData.SetFormDataFromQuery("search", query);

        Assert.Equal(FormDataSource.FormGet, formData.FormDataSource);
        Assert.True(formData.TryGetIncomingHandlerName(out var handler));
        Assert.Equal("search", handler);
        Assert.Equal("hello", formData.Entries["Filter.Query"].ToString());
        Assert.Equal("2", formData.Entries["Filter.Page"].ToString());
        Assert.Equal("true", formData.Entries["Filter.InStockOnly"].ToString());
    }

    [Fact]
    public void SetFormDataFromQuery_EmptyQuery_StillExposesHandlerName()
    {
        var formData = new HttpContextFormDataProvider();
        formData.SetFormDataFromQuery("search", new QueryCollection());

        Assert.Equal(FormDataSource.FormGet, formData.FormDataSource);
        Assert.True(formData.TryGetIncomingHandlerName(out var handler));
        Assert.Equal("search", handler);
        Assert.Empty(formData.Entries);
    }

    [Fact]
    public void CanMap_WorksWithQueryStringPopulatedProvider()
    {
        var formData = new HttpContextFormDataProvider();
        formData.SetFormDataFromQuery("my-form", new QueryCollection());

        var mapper = new HttpContextFormValueMapper(formData, Options.Create<RazorComponentsServiceOptions>(new()));

        Assert.True(mapper.CanMap(typeof(string), "", "my-form"));
        Assert.True(mapper.CanMap(typeof(string), "", null));
        Assert.False(mapper.CanMap(typeof(string), "different-scope", "my-form"));
    }

    [Fact]
    public void Map_PopulatesComplexModelFromQueryString()
    {
        var formData = new HttpContextFormDataProvider();
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Query"] = "hello",
            ["Category"] = "Electronics",
            ["Price.Min"] = "10",
            ["Price.Max"] = "500",
            ["SortBy"] = "PriceAscending",
            ["InStockOnly"] = "true",
            ["Tags"] = new StringValues(new[] { "audio", "peripherals" }),
        });
        formData.SetFormDataFromQuery("filter", query);

        var options = Options.Create<RazorComponentsServiceOptions>(new());
        var mapper = new HttpContextFormValueMapper(formData, options);

        Assert.True(mapper.CanMap(typeof(QueryStringFormBindingTestModel), "", "filter"));

        var ctx = new FormValueMappingContext("", "filter", typeof(QueryStringFormBindingTestModel), "")
        {
            OnError = (_, message, _) => throw new InvalidOperationException(message.ToString(CultureInfo.InvariantCulture)),
        };
        mapper.Map(ctx);

        var bound = (QueryStringFormBindingTestModel)ctx.Result!;
        Assert.Equal("hello", bound.Query);
        Assert.Equal("Electronics", bound.Category);
        Assert.Equal(10m, bound.Price.Min);
        Assert.Equal(500m, bound.Price.Max);
        Assert.Equal(QueryStringFormSortOrder.PriceAscending, bound.SortBy);
        Assert.True(bound.InStockOnly);
        Assert.Equal(new[] { "audio", "peripherals" }, bound.Tags);
    }

    private class QueryStringFormBindingTestModel
    {
        public string Query { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public QueryStringFormPriceRange Price { get; set; } = new();
        public QueryStringFormSortOrder SortBy { get; set; }
        public bool InStockOnly { get; set; }
        public IList<string> Tags { get; set; } = new List<string>();
    }

    private class QueryStringFormPriceRange
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; } = decimal.MaxValue;
    }

    private enum QueryStringFormSortOrder
    {
        NameAscending = 0,
        PriceAscending = 1,
    }
}
