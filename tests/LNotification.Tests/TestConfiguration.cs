using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace LNotification.Tests;

internal sealed class TestConfigurationSection : IConfigurationSection
{
    private readonly Dictionary<string, TestConfigurationSection> _children
        = new(StringComparer.OrdinalIgnoreCase);

    internal TestConfigurationSection(string key, string path)
    {
        Key = key;
        Path = path;
    }

    public string Key { get; }
    public string Path { get; }
    public string? Value { get; set; }

    public string? this[string key]
    {
        get => GetSection(key).Value;
        set => GetSection(key).Value = value;
    }

    public IConfigurationSection GetSection(string key)
    {
        if (!_children.TryGetValue(key, out var section))
        {
            var path = string.IsNullOrEmpty(Path) ? key : $"{Path}:{key}";
            section = new TestConfigurationSection(key, path);
            _children[key] = section;
        }

        return section;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => _children.Values;

    public IChangeToken GetReloadToken() => NoopChangeToken.Instance;

    internal void SetPathValue(string path, string? value)
    {
        var segments = path.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var current = this;

        foreach (var segment in segments)
        {
            current = (TestConfigurationSection)current.GetSection(segment);
        }

        current.Value = value;
    }

    internal static IConfiguration FromDictionary(IDictionary<string, string?> values)
    {
        var root = new TestConfigurationSection(string.Empty, string.Empty);

        foreach (var kvp in values)
        {
            root.SetPathValue(kvp.Key, kvp.Value);
        }

        return root;
    }

    private sealed class NoopChangeToken : IChangeToken
    {
        public static readonly NoopChangeToken Instance = new();

        public bool ActiveChangeCallbacks => false;
        public bool HasChanged => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
            => NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }
}
