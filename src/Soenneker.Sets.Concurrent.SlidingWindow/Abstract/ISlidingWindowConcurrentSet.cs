using System;
using System.Collections.Generic;

namespace Soenneker.Sets.Concurrent.SlidingWindow.Abstract;

/// <summary>
/// Represents a thread-safe, bucketed set whose entries expire after an approximate sliding window.
/// </summary>
/// <typeparam name="T">The element type. Must be non-nullable.</typeparam>
public interface ISlidingWindowConcurrentSet<T> : IAsyncDisposable, IDisposable where T : notnull
{
    /// <summary>
    /// Gets the underlying dictionary count. During rotation it can briefly include entries that <see cref="Contains"/> considers expired.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a live view of values currently considered present (not a point-in-time snapshot).
    /// Enumeration may reflect concurrent adds/removes/expirations.
    /// </summary>
    IEnumerable<T> Values { get; }

    /// <summary>
    /// Attempts to add <paramref name="value"/> to the set. An existing value from an older bucket is refreshed into the current bucket.
    /// </summary>
    /// <param name="value">Value to test, add, or remove from the set.</param>
    /// <returns><see langword="true"/> if the value was absent; <see langword="false"/> if it already existed, including when its bucket was refreshed.</returns>
    bool TryAdd(T value);

    /// <summary>
    /// Determines whether <paramref name="value"/> is present in the set (within the current window).
    /// </summary>
    /// <param name="value">Value to test, add, or remove from the set.</param>
    /// <returns>true if is present in the set (within the current window); otherwise, false.</returns>
    bool Contains(T value);

    /// <summary>
    /// Attempts to remove <paramref name="value"/> from the set.
    /// Returns <c>true</c> if removed; <c>false</c> if it was not present.
    /// </summary>
    /// <param name="value">Value to test, add, or remove from the set.</param>
    /// <returns>true if the requested update was applied; otherwise, false.</returns>
    bool TryRemove(T value);

    /// <summary>
    /// Creates a point-in-time snapshot of the current values as a new array (allocates).
    /// </summary>
    /// <returns>The newly created t[].</returns>
    T[] ToArray();
}
