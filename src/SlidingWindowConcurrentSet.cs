using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Atomics.Longs;
using Soenneker.Atomics.ValueBools;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Sets.Concurrent.SlidingWindow.Abstract;

namespace Soenneker.Sets.Concurrent.SlidingWindow;

/// <inheritdoc cref="ISlidingWindowConcurrentSet{T}"/>
public sealed class SlidingWindowConcurrentSet<T> : ISlidingWindowConcurrentSet<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, long> _index;
    private readonly ConcurrentQueue<T>[] _buckets;

    private readonly int _bucketCount;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    private AtomicLong _currentBucketId;
    private readonly Task _pump;

    private ValueAtomicBool _disposed;

    public SlidingWindowConcurrentSet(TimeSpan window, TimeSpan rotationInterval, int capacityHint = 0, IEqualityComparer<T>? comparer = null)
    {
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window));

        if (rotationInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(rotationInterval));

        _bucketCount = checked((int)Math.Ceiling(window.TotalMilliseconds / rotationInterval.TotalMilliseconds));
        if (_bucketCount < 2)
            _bucketCount = 2;

        _buckets = new ConcurrentQueue<T>[_bucketCount];
        for (var i = 0; i < _bucketCount; i++)
            _buckets[i] = new ConcurrentQueue<T>();

        int concurrencyLevel = Math.Max(2, Environment.ProcessorCount);

        _index = capacityHint > 0
            ? new ConcurrentDictionary<T, long>(concurrencyLevel, capacityHint, comparer)
            : new ConcurrentDictionary<T, long>(concurrencyLevel, 31, comparer);

        _timer = new PeriodicTimer(rotationInterval);
        _pump = Task.Run(Pump);
    }

    public int Count => _index.Count;

    public IEnumerable<T> Values => _index.Keys;

    public bool TryAdd(T value)
    {
        ThrowIfDisposed();

        long currentId = _currentBucketId.Read();

        bool existed = true;
        bool shouldEnqueue = false;

        _index.AddOrUpdate(
            value,
            _ =>
            {
                existed = false;
                shouldEnqueue = true;
                return currentId;
            },
            (_, prev) =>
            {
                existed = true;

                // Already touched in this same slice -> don't enqueue again.
                if (prev == currentId)
                    return prev;

                shouldEnqueue = true;
                return currentId;
            });

        if (shouldEnqueue)
        {
            int slot = (int)(currentId % _bucketCount);
            _buckets[slot].Enqueue(value);
        }

        return !existed;
    }

    public bool Contains(T value)
    {
        ThrowIfDisposed();

        if (!_index.TryGetValue(value, out long lastId))
            return false;

        long currentId = _currentBucketId.Read();
        return (currentId - lastId) < _bucketCount;
    }

    public bool TryRemove(T value)
    {
        ThrowIfDisposed();
        return _index.TryRemove(value, out _);
    }

    public T[] ToArray() => _index.Keys.ToArray();

    private async Task Pump()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token).NoSync())
            {
                Rotate();
            }
        }
        catch (OperationCanceledException)
        {
            // expected on dispose
        }
    }

    private void Rotate()
    {
        // Move to the next slice id (this is the *current* slice id after the increment)
        long current = _currentBucketId.Increment();

        // This slot is being reused; it corresponds to the slice that fell out of the window.
        long expiring = current - _bucketCount;
        int slot = (int)(current % _bucketCount);

        ConcurrentQueue<T> queue = _buckets[slot];

        while (queue.TryDequeue(out T? value))
        {
            // Only remove if it still points to the expiring slice.
            if (_index.TryGetValue(value, out long lastId) && lastId == expiring)
                _index.TryRemove(value, out _);
        }

        // Drop internal segments quickly
        _buckets[slot] = new ConcurrentQueue<T>();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed.Value)
            throw new ObjectDisposedException(nameof(SlidingWindowConcurrentSet<T>));
    }

    public void Dispose()
    {
        if (!_disposed.TrySetTrue())
            return;

        _cts.Cancel();
        _timer.Dispose();
        _cts.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        await _cts.CancelAsync().NoSync();
        _timer.Dispose();

        try
        {
            await _pump.NoSync();
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Dispose();
    }
}