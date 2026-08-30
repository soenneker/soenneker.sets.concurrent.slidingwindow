[![](https://img.shields.io/nuget/v/soenneker.sets.concurrent.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.sets.concurrent.slidingwindow/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.sets.concurrent.slidingwindow/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.sets.concurrent.slidingwindow/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.sets.concurrent.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.sets.concurrent.slidingwindow/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.sets.concurrent.slidingwindow/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.sets.concurrent.slidingwindow/actions/workflows/codeql.yml)

# Soenneker.Sets.Concurrent.SlidingWindow

A concurrent set that refreshes values into time buckets and expires them with one internal periodic rotation task.

## Installation

```bash
dotnet add package Soenneker.Sets.Concurrent.SlidingWindow
```

## Usage

```csharp
using Soenneker.Sets.Concurrent.SlidingWindow;

await using var recentIds = new SlidingWindowConcurrentSet<string>(
    window: TimeSpan.FromMinutes(5),
    rotationInterval: TimeSpan.FromSeconds(30),
    capacityHint: 10_000,
    comparer: StringComparer.Ordinal);

if (!recentIds.TryAdd(messageId))
{
    // The ID was already present. This call may also refresh its bucket.
    return;
}

bool isRecent = recentIds.Contains(messageId);
bool removed = recentIds.TryRemove(messageId);
string[] snapshot = recentIds.ToArray();
```

`TryAdd` returns `true` only when the value was absent from the dictionary. If the value is already present from an earlier bucket, it returns `false` and refreshes that value into the current bucket. Repeated add attempts can therefore extend retention even though they report a duplicate. A duplicate within the same bucket returns `false` without adding another bucket record.

## Expiration precision

The constructor creates `max(2, ceil(window / rotationInterval))` buckets. Entries expire when their last bucket rotates out, so expiration is quantized rather than exact.

With a five-minute window and a 30-second rotation interval, an unrefreshed entry normally remains for roughly 4.5 to 5 minutes depending on where within the current slice it was added. Timer scheduling delays can extend that further. When `rotationInterval` is greater than or equal to `window`, the two-bucket minimum means retention is roughly one to two rotation intervals.

Choose an interval small enough for the expiration precision your use case needs, while remembering that every interval runs a bucket cleanup pass.

## Collection views

`Contains` checks both the dictionary and the recorded bucket age. `Count`, `Values`, and `ToArray` read the underlying concurrent dictionary. At a rotation boundary—or if the timer pump is delayed—they can briefly include an entry that `Contains` already considers expired.

`Values` is a live concurrent view and may change during enumeration. `ToArray` allocates a snapshot of keys observed during that call. None of these operations provides a transaction with concurrent adds, refreshes, removals, or expiration.

`capacityHint` controls only the dictionary's initial capacity. It does not bound the number of live values or queued bucket records. Frequently refreshing values creates stale bucket records that remain until their buckets rotate.

## Disposal

Each set owns a `PeriodicTimer`, cancellation source, and rotation task. Prefer asynchronous disposal when practical because it cancels and awaits the pump:

```csharp
await recentIds.DisposeAsync();
```

`Dispose` and `DisposeAsync` are idempotent. After disposal, `TryAdd`, `Contains`, and `TryRemove` throw `ObjectDisposedException`.

This collection is suitable for approximate recent-value tracking and deduplication hints. It is not a durable idempotency store, exact rate limiter, security token revocation list, or TTL cache with per-entry expiration guarantees.
