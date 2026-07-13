// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpContextFormDataProvider
{
    private static readonly IFormFileCollection _emptyFormFiles = new FormFileCollection();

    private string? _incomingHandlerName;
    private IReadOnlyDictionary<string, StringValues>? _entries;
    private IFormFileCollection? _formFiles;
    private FormDataSource _formDataSource;

    public string? IncomingHandlerName => _incomingHandlerName;

    public IReadOnlyDictionary<string, StringValues> Entries => _entries ?? ReadOnlyDictionary<string, StringValues>.Empty;

    public IFormFileCollection FormFiles => _formFiles ?? _emptyFormFiles;

    /// <summary>
    /// Gets the source of the form data currently stored in this provider.
    /// </summary>
    internal FormDataSource FormDataSource => _formDataSource;

    public void SetFormData(string incomingHandlerName, IReadOnlyDictionary<string, StringValues> form, IFormFileCollection formFiles)
    {
        // Don't overwrite form data from the query string with an empty form from the body.
        // This can happen if something calls ReadFormAsync() on a GET request.
        if (_formDataSource == FormDataSource.FormGet && form.Count == 0 && formFiles.Count == 0)
        {
            return;
        }

        _incomingHandlerName = incomingHandlerName;
        _entries = form;
        _formFiles = formFiles;
        if (form.Count > 0 || formFiles.Count > 0)
        {
            _formDataSource = FormDataSource.FormPost;
        }
    }

    /// <summary>
    /// Sets the form data on this provider from a GET request's query string.
    /// This is used to support binding complex models to forms whose
    /// <c>method</c> attribute is <c>get</c>.
    /// </summary>
    /// <param name="incomingHandlerName">The form handler name (typically derived from the <c>_handler</c> query parameter).</param>
    /// <param name="query">The query collection from the HTTP request.</param>
    public void SetFormDataFromQuery(string incomingHandlerName, IQueryCollection query)
    {
        _incomingHandlerName = incomingHandlerName;
        // Preserve duplicate query-string keys so form binding can round-trip the full
        // collection, matching the behavior of IFormCollection for POST bodies.
        _entries = new QueryCollectionReadOnlyDictionary(query);
        _formFiles = _emptyFormFiles;
        _formDataSource = FormDataSource.FormGet;
    }

    public bool TryGetIncomingHandlerName([NotNullWhen(true)] out string? incomingHandlerName)
    {
        incomingHandlerName = _incomingHandlerName;
        return incomingHandlerName is not null;
    }

    /// <summary>
    /// Wraps an <see cref="IQueryCollection"/> as an <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// so that the same model binding pipeline used for POST form bodies can also
    /// consume query string entries.
    /// </summary>
    private sealed class QueryCollectionReadOnlyDictionary : IReadOnlyDictionary<string, StringValues>
    {
        private readonly IQueryCollection _query;

        public QueryCollectionReadOnlyDictionary(IQueryCollection query)
        {
            _query = query;
        }

        public StringValues this[string key] => _query[key];

        public IEnumerable<string> Keys => _query.Keys;

        public IEnumerable<StringValues> Values
        {
            get
            {
                foreach (var key in _query.Keys)
                {
                    yield return _query[key];
                }
            }
        }

        public int Count => _query.Count;

        public bool ContainsKey(string key) => _query.ContainsKey(key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out StringValues value) =>
            _query.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _query.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

/// <summary>
/// Indicates the source of the form data that <see cref="HttpContextFormDataProvider"/> is currently storing.
/// </summary>
internal enum FormDataSource
{
    /// <summary>
    /// No form data has been set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Form data was supplied via a <c>POST</c> request body.
    /// </summary>
    FormPost = 1,

    /// <summary>
    /// Form data was supplied via a <c>GET</c> request's query string.
    /// </summary>
    FormGet = 2,
}
