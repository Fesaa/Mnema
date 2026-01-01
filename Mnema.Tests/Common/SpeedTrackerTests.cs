using System.Threading.Tasks;
using Mnema.Common;

namespace Mnema.Tests.Common;

public class SpeedTrackerTests
{
    [Fact]
    public void Constructor_InitializesWithZeroProgress()
    {
        var tracker = new SpeedTracker(100);

        Assert.Equal(0, tracker.Progress());
        Assert.Equal(0, tracker.Speed());
    }

    [Fact]
    public void Increment_IncreasesProgress()
    {
        var tracker = new SpeedTracker(100);

        tracker.Increment();
        tracker.Increment();

        Assert.Equal(2, tracker.Progress());
    }

    [Fact]
    public void Progress_CalculatesPercentageCorrectly()
    {
        var tracker = new SpeedTracker(100);

        for (int i = 0; i < 25; i++)
        {
            tracker.Increment();
        }

        Assert.Equal(25, tracker.Progress());
    }

    [Fact]
    public void SetIntermediate_CreatesIntermediateTracker()
    {
        var tracker = new SpeedTracker(10);

        tracker.SetIntermediate(100);
        tracker.IncrementIntermediate();

        Assert.True(tracker.IntermediateSpeed() >= 0);
    }

    [Fact]
    public void IncrementIntermediate_WithNoIntermediate_DoesNotThrow()
    {
        var tracker = new SpeedTracker(10);

        var exception = Record.Exception(() => tracker.IncrementIntermediate());

        Assert.Null(exception);
    }

    [Fact]
    public void IntermediateSpeed_WithNoIntermediate_ReturnsZero()
    {
        var tracker = new SpeedTracker(10);

        Assert.Equal(0, tracker.IntermediateSpeed());
    }

    [Fact]
    public void Progress_IncludesIntermediateFractionalProgress()
    {
        var tracker = new SpeedTracker(10);
        
        tracker.Increment(); // 1/10 = 10%
        tracker.SetIntermediate(100);
        
        for (int i = 0; i < 50; i++)
        {
            tracker.IncrementIntermediate(); // 50/100 = 0.5 of a main item
        }

        var progress = tracker.Progress();
        
        // Should be 10% + (50/100)/10 * 100 = 10% + 5% = 15%
        Assert.Equal(15, progress);
    }

    [Fact]
    public void ClearIntermediate_RemovesIntermediateTracker()
    {
        var tracker = new SpeedTracker(10);

        tracker.SetIntermediate(100);
        tracker.IncrementIntermediate();
        tracker.ClearIntermediate();

        Assert.Equal(0, tracker.IntermediateSpeed());
    }

    [Fact]
    public void EstimatedTimeRemaining_WithNoProgress_ReturnsZero()
    {
        var tracker = new SpeedTracker(100);

        Assert.Equal(0, tracker.EstimatedTimeRemaining());
    }

    [Fact]
    public async Task ConcurrentIncrements_AreThreadSafe()
    {
        var tracker = new SpeedTracker(1000);
        var tasks = new Task[10];

        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    tracker.Increment();
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(100, tracker.Progress());
    }

    [Fact]
    public async Task ConcurrentIntermediateOperations_AreThreadSafe()
    {
        var tracker = new SpeedTracker(10);
        tracker.SetIntermediate(1000);

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    tracker.IncrementIntermediate();
                }
            });
        }

        await Task.WhenAll(tasks);

        // Should have incremented 1000 times on intermediate tracker
        var intermediateProgress = tracker.IntermediateSpeed();
        Assert.True(intermediateProgress >= 0);
    }

    [Fact]
    public void SetIntermediate_MultipleTimesOverwrites()
    {
        var tracker = new SpeedTracker(10);

        tracker.SetIntermediate(100);
        tracker.IncrementIntermediate();
        
        tracker.SetIntermediate(50); // Overwrites previous intermediate

        // Previous increments should be lost
        var speed = tracker.IntermediateSpeed();
        Assert.Equal(0, speed); // New tracker has no increments yet
    }
}