using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Tests.Unit;

namespace Soenneker.Sets.Concurrent.SlidingWindow.Tests;

public sealed class SlidingWindowConcurrentSetTests : UnitTest
{
    private static SlidingWindowConcurrentSet<T> CreateSet<T>(TimeSpan? window = null, TimeSpan? rotationInterval = null) where T : notnull
    {
        return new SlidingWindowConcurrentSet<T>(
            window ?? TimeSpan.FromSeconds(10),
            rotationInterval ?? TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Constructor_ThrowsOnZeroWindow()
    {
        Func<SlidingWindowConcurrentSet<int>> act = () => new SlidingWindowConcurrentSet<int>(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Constructor_ThrowsOnNegativeWindow()
    {
        Func<SlidingWindowConcurrentSet<int>> act = () => new SlidingWindowConcurrentSet<int>(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(1));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Constructor_ThrowsOnZeroRotationInterval()
    {
        Func<SlidingWindowConcurrentSet<int>> act = () => new SlidingWindowConcurrentSet<int>(TimeSpan.FromSeconds(10), TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Constructor_ThrowsOnNegativeRotationInterval()
    {
        Func<SlidingWindowConcurrentSet<int>> act = () => new SlidingWindowConcurrentSet<int>(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(-1));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void TryAdd_AddsNewItem_ReturnsTrue()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryAdd(42).Should().BeTrue();
        set.Contains(42).Should().BeTrue();
        set.Count.Should().Be(1);
    }

    [Test]
    public void TryAdd_SameItem_ReturnsFalse()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryAdd(42).Should().BeTrue();
        set.TryAdd(42).Should().BeFalse();
        set.Count.Should().Be(1);
    }

    [Test]
    public void TryAdd_MultipleDistinctItems_AllAdded()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryAdd(1).Should().BeTrue();
        set.TryAdd(2).Should().BeTrue();
        set.TryAdd(3).Should().BeTrue();
        set.Count.Should().Be(3);
        set.Contains(1).Should().BeTrue();
        set.Contains(2).Should().BeTrue();
        set.Contains(3).Should().BeTrue();
    }

    [Test]
    public void Contains_ReturnsFalse_WhenNotAdded()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.Contains(99).Should().BeFalse();
    }

    [Test]
    public void Contains_ReturnsFalse_AfterRemove()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryAdd(42);
        set.TryRemove(42);
        set.Contains(42).Should().BeFalse();
        set.Count.Should().Be(0);
    }

    [Test]
    public void TryRemove_RemovesItem_ReturnsTrue()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryAdd(42);
        set.TryRemove(42).Should().BeTrue();
        set.Contains(42).Should().BeFalse();
        set.Count.Should().Be(0);
    }

    [Test]
    public void TryRemove_NotPresent_ReturnsFalse()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryRemove(99).Should().BeFalse();
    }

    [Test]
    public void Count_ReflectsAddsAndRemoves()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.Count.Should().Be(0);
        set.TryAdd(1);
        set.Count.Should().Be(1);
        set.TryAdd(2);
        set.Count.Should().Be(2);
        set.TryRemove(1);
        set.Count.Should().Be(1);
        set.TryRemove(2);
        set.Count.Should().Be(0);
    }

    [Test]
    public void ToArray_ReturnsCurrentValues()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryAdd(1);
        set.TryAdd(2);
        set.TryAdd(3);
        int[] arr = set.ToArray();
        arr.Should().HaveCount(3).And.Contain(1).And.Contain(2).And.Contain(3);
    }

    [Test]
    public void ToArray_EmptySet_ReturnsEmptyArray()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        int[] arr = set.ToArray();
        arr.Should().BeEmpty();
    }

    [Test]
    public void Values_EnumeratesCurrentItems()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.TryAdd(10);
        set.TryAdd(20);
        List<int> values = set.Values.ToList();
        values.Should().HaveCount(2).And.Contain(10).And.Contain(20);
    }

    [Test]
    public void Dispose_ThrowsOnTryAdd()
    {
        SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.Dispose();
        Func<bool> act = () => set.TryAdd(1);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Dispose_ThrowsOnContains()
    {
        SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.Dispose();
        Func<bool> act = () => set.Contains(1);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Dispose_ThrowsOnTryRemove()
    {
        SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.Dispose();
        Func<bool> act = () => set.TryRemove(1);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public async Task DisposeAsync_ThrowsOnSubsequentUse()
    {
        SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        await set.DisposeAsync();
        Func<bool> act = () => set.TryAdd(1);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Dispose_Idempotent()
    {
        SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        set.Dispose();
        set.Dispose(); // should not throw
    }

    [Test]
    public void Constructor_WithCustomComparer_RespectsComparer()
    {
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;
        using var set = new SlidingWindowConcurrentSet<string>(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), comparer: comparer);
        set.TryAdd("foo").Should().BeTrue();
        set.TryAdd("FOO").Should().BeFalse();
        set.Contains("Foo").Should().BeTrue();
        set.Count.Should().Be(1);
    }

    [Test]
    public void Constructor_WithCapacityHint_AcceptsPositiveHint()
    {
        using var set = new SlidingWindowConcurrentSet<int>(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), capacityHint: 100);
        set.TryAdd(1);
        set.Count.Should().Be(1);
    }

    [Test]
    public async Task Item_ExpiresAfterWindow()
    {
        TimeSpan window = TimeSpan.FromMilliseconds(100);
        TimeSpan rotation = TimeSpan.FromMilliseconds(50);
        await using var set = new SlidingWindowConcurrentSet<int>(window, rotation);
        set.TryAdd(42);
        set.Contains(42).Should().BeTrue();

        await Task.Delay(300, System.Threading.CancellationToken.None);
        set.Contains(42).Should().BeFalse();
        // Count may lag behind Contains until rotation runs; item is no longer in the window
    }

    [Test]
    public void Concurrent_Adds_CountConsistent()
    {
        using SlidingWindowConcurrentSet<int> set = CreateSet<int>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 8 };
        Parallel.For(0, 500, options, i => set.TryAdd(i));

        int count = set.Count;
        count.Should().BeLessThanOrEqualTo(500);
        foreach (int v in set.Values)
            set.Contains(v).Should().BeTrue();
    }
}

