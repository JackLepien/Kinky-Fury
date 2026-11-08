using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>
    /// One swappable step in the stroke-processing pipeline. Takes a polyline and returns a new polyline;
    /// never mutates the input. Implementations are plain C# (no scene access) so they can be unit tested.
    /// </summary>
    public interface IStrokeProcessor
    {
        string Name { get; }
        List<Vector2> Process(IReadOnlyList<Vector2> input);
    }
}
