
namespace BlueHeron.Collections.Trie.Tests;

/// <summary>
/// Container for details on the duration of a test.
/// Values are calculated in microseconds.
/// </summary>
internal sealed class BenchMarkResult
{
    /// <summary>
    /// The average duration of the test.
    /// </summary>
    public double AverageDuration {get; private set;}

    /// <summary>
    /// The maximum duration of the test.
    /// </summary>
    public double MaxDuration { get; private set; } = double.MinValue;

    /// <summary>
    /// The median duration of the test. This is a crude calculation that assumes that the number of tests is large and the values are randomly distributed.
    /// </summary>
    public double MedianDuration { get; private set; }

    /// <summary>
    /// The minimum duration of the test.
    /// </summary>
    public double MinDuration { get; private set; } = double.MaxValue;

    /// <summary>
    /// The number of tests performed.
    /// </summary>
    public double NumTests { get; private set; }

    /// <summary>
    /// Updates this <see cref="BenchMarkResult"/> with the given duration of a test.
    /// </summary>
    /// <param name="duration">A <see cref="TimeSpan"/> representing the duration of a test</param>
    public void AddResult(TimeSpan duration)
    {
        NumTests++;
        AverageDuration += (duration.TotalMicroseconds - AverageDuration) / NumTests; // don't store total. Its value may overflow.
        MaxDuration = Math.Max(MaxDuration, duration.TotalMicroseconds);
        MinDuration = Math.Min(MaxDuration, duration.TotalMicroseconds);
        var currentMedianEstimate = (MaxDuration + MinDuration) / 2; // if new duration is closer to the current estimated median, store it
        if (Math.Abs(MedianDuration - currentMedianEstimate) > Math.Abs(duration.TotalMicroseconds - currentMedianEstimate))
        {
            MedianDuration = duration.TotalMicroseconds;
        }
    }
}