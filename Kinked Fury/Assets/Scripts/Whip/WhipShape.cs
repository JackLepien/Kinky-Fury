using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>
    /// A finished, saved whip: the processed line re-anchored so its first point is the origin
    /// (the player's hand), plus the stats derived when it was drawn. Immutable snapshot, so redrawing
    /// or switching presets never changes a swing already in progress.
    /// </summary>
    public sealed class WhipShape
    {
        public IReadOnlyList<Vector2> LocalPoints { get; }
        public WhipStats Stats { get; }

        private WhipShape(List<Vector2> localPoints, WhipStats stats)
        {
            LocalPoints = localPoints;
            Stats = stats;
        }

        /// <summary>Returns null when the stroke is too short to be a whip (fewer than 2 points or zero length).</summary>
        public static WhipShape FromPoints(IReadOnlyList<Vector2> points, WhipStats stats)
        {
            if (points == null || points.Count < 2 || stats.Length <= 0f) return null;

            var local = new List<Vector2>(points.Count);
            Vector2 origin = points[0];
            for (int i = 0; i < points.Count; i++) local.Add(points[i] - origin);
            return new WhipShape(local, stats);
        }
    }
}
