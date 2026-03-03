using System;
using System.Collections.Generic;

namespace Soenneker.Sets.Concurrent.SlidingWindow.Abstract;

/// <summary>
/// Represents a high-throughput, thread-safe set whose entries automatically expire after a fixed time window.
/// </summary>
/// <typeparam name="T">The element type. Must be non-nullable.</typeparam>
public interface ISlidingWindowConcurrentSet<T> : IAsyncDisposable, IDisposable where T : notnull
{
    /// <summary>
    /// Gets the number of elements currently present in the set.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a live view of values currently considered present (not a point-in-time snapshot).
    /// Enumeration may reflect concurrent adds/removes/expirations.
    /// </summary>
    IEnumerable<T> Values { get; }

    /// <summary>
    /// Attempts to add <paramref name="value"/> to the set.
    /// Returns <c>true</c> if it was newly added; <c>false</c> if it was already present (within the current window).
    /// </summary>
    bool TryAdd(T value);

    /// <summary>
    /// Determines whether <paramref name="value"/> is present in the set (within the current window).
    /// </summary>
    bool Contains(T value);

    /// <summary>
    /// Attempts to remove <paramref name="value"/> from the set.
    /// Returns <c>true</c> if removed; <c>false</c> if it was not present.
    /// </summary>
    bool TryRemove(T value);

    /// <summary>
    /// Creates a point-in-time snapshot of the current values as a new array (allocates).
    /// </summary>
    T[] ToArray();
}