using System;
using System.Threading;

namespace Mnema.Common;

/// <summary>
/// Tracks progress and speed for work items
/// </summary>
public class SpeedTracker(int maxItem)
{
    private readonly Lock _lock = new ();
    private readonly Lock _intermediateLock = new ();

    private DateTime LastCheck { get; set; } = DateTime.UtcNow;
    private DateTime StartTime { get; set; } = DateTime.UtcNow;

    private int _cur;
    // For tracking intermediate progress of current work item
    private SpeedTracker? _intermediate;

    /// <summary>
    /// Records one instance of work is finished
    /// </summary>
    public void Increment()
    {
        lock (_lock)
        {
            _cur++;
            LastCheck = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Increments the intermediate tracker if it exists
    /// </summary>
    public void IncrementIntermediate()
    {
        lock (_intermediateLock)
        {
            _intermediate?.Increment();
        }
    }

    /// <summary>
    /// Returns the completion percentage (0-100)
    /// If an intermediate tracker exists, includes its fractional progress
    /// </summary>
    public double Progress()
    {
        int cur;
        lock (_lock)
        {
            cur = _cur;
        }

        if (maxItem == 0)
        {
            return 0;
        }

        double progress = cur;
        double intermediateProgress = 0.0;

        // Add fractional progress from intermediate tracker
        lock (_intermediateLock)
        {
            if (_intermediate != null)
            {
                intermediateProgress = _intermediate.Progress() / maxItem;
            }
        }

        return progress / maxItem * 100 + intermediateProgress;
    }

    /// <summary>
    /// Returns items per second
    /// </summary>
    public double Speed()
    {
        lock (_lock)
        {
            var elapsed = (DateTime.UtcNow - StartTime).TotalSeconds;
            if (elapsed == 0)
            {
                return 0;
            }
            return _cur / elapsed;
        }
    }

    /// <summary>
    /// Returns the speed of the intermediate tracker if it exists
    /// </summary>
    public double IntermediateSpeed()
    {
        lock (_intermediateLock)
        {
            return _intermediate?.Speed() ?? 0;
        }
    }

    /// <summary>
    /// Sets the intermediate progress tracker for the current work item
    /// </summary>
    /// <param name="maxItem">Maximum items for the intermediate tracker</param>
    public void SetIntermediate(int maxItem)
    {
        lock (_intermediateLock)
        {
            _intermediate = new SpeedTracker(maxItem);
        }
    }

    /// <summary>
    /// Removes the intermediate tracker (call when work item completes)
    /// </summary>
    public void ClearIntermediate()
    {
        lock (_intermediateLock)
        {
            _intermediate = null;
        }
    }

    /// <summary>
    /// Calculates estimated time remaining in seconds
    /// </summary>
    public double EstimatedTimeRemaining()
    {
        if (_cur == 0)
        {
            return 0;
        }

        lock (_lock)
        {
            var speed = Speed();
            if (speed == 0)
            {
                return 0;
            }
            
            return (maxItem - _cur) / speed;
        }
    }
}