[![](https://img.shields.io/nuget/v/soenneker.sets.concurrent.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.sets.concurrent.slidingwindow/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.sets.concurrent.slidingwindow/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.sets.concurrent.slidingwindow/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.sets.concurrent.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.sets.concurrent.slidingwindow/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Sets.Concurrent.SlidingWindow

### A high-throughput, thread-safe set whose bucketed entries automatically expire after a fixed time window.

`Soenneker.Sets.Concurrent.SlidingWindow` provides a **concurrent sliding-window set** for .NET.
Items added to the set automatically expire after a configurable time window without requiring manual cleanup.

The implementation is optimized for **high-concurrency workloads** and avoids expensive per-item timers by using a **bucketed time-slice rotation system**.

This makes it ideal for **deduplication, rate limiting, and recent activity tracking**.

---

# Installation

```bash
dotnet add package Soenneker.Sets.Concurrent.SlidingWindow
```

---

# Why this library exists

Many systems need to track items that should only exist for a **limited period of time**, such as:

* recently processed messages
* request IDs
* phone numbers
* event IDs
* authentication tokens

Traditional options have downsides:

| Approach                 | Problem                                    |
| ------------------------ | ------------------------------------------ |
| `ConcurrentDictionary`   | Requires manual expiration                 |
| `MemoryCache`            | Heavy and feature-rich for simple tracking |
| Per-item timers          | Extremely expensive at scale               |
| Background cleanup scans | High CPU cost                              |

`SlidingWindowConcurrentSet` solves this by using **bucketed time slices** where items automatically expire when their time window passes.

---

# Key Features

✔ High-throughput concurrent operations

✔ Automatic expiration of entries

✔ Sliding window time-based retention

✔ Lock-minimized design

✔ Low allocation footprint

✔ No per-item timers

✔ Safe for heavy multi-threaded workloads

---

# Example

```csharp
using Soenneker.Sets.Concurrent.SlidingWindow;

var set = new SlidingWindowConcurrentSet<string>(
    window: TimeSpan.FromMinutes(5),
    rotationInterval: TimeSpan.FromSeconds(30)
);

set.TryAdd("alpha");

bool exists = set.Contains("alpha");

await Task.Delay(TimeSpan.FromMinutes(6));

bool expired = set.Contains("alpha"); // false
```

---

# Configuration

```csharp
var set = new SlidingWindowConcurrentSet<string>(
    window: TimeSpan.FromMinutes(10),
    rotationInterval: TimeSpan.FromSeconds(15)
);
```

| Parameter          | Description                                       |
| ------------------ | ------------------------------------------------- |
| `window`           | Total time items remain valid                     |
| `rotationInterval` | Time slice size used for bucket rotation          |
| `capacityHint`     | Initial capacity hint for the internal dictionary |
| `comparer`         | Optional equality comparer                        |

The window is internally divided into **time buckets**:

```
window / rotationInterval = number of buckets
```

Example:

```
window = 5 minutes
rotationInterval = 30 seconds
bucket count = 10
```

Each rotation advances the window and expires the oldest bucket.

---

# How it works

The set maintains:

* a **ConcurrentDictionary** for fast lookup
* a **ring buffer of queues** representing time buckets

When an item is added:

1. It is assigned the **current time bucket**
2. The value is queued in that bucket
3. The dictionary records the bucket index

A background rotation process periodically:

1. Advances the active bucket
2. Processes the expired bucket
3. Removes items whose last activity falls outside the sliding window

This avoids scanning the entire collection.

---

# Performance Characteristics

| Operation   | Complexity                             |
| ----------- | -------------------------------------- |
| `TryAdd`    | O(1)                                   |
| `Contains`  | O(1)                                   |
| `TryRemove` | O(1)                                   |
| Expiration  | O(n) only for items in expiring bucket |

The design ensures:

* predictable memory usage
* minimal locking
* bounded cleanup work

---

# Thread Safety

All operations are **thread-safe**.

The set is designed for **high-concurrency environments** and does not require external synchronization.

---

# Disposal

The set uses an internal **rotation task** driven by `PeriodicTimer`.

When the set is no longer needed, it should be disposed:

```csharp
set.Dispose();
```

or

```csharp
await set.DisposeAsync();
```

This stops the internal rotation loop.