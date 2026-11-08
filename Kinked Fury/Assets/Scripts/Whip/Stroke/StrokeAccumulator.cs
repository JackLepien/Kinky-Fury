using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Collects raw points while the player draws and enforces the hard length cap.
    /// Plain C# so the cap behaviour is unit tested independently of mouse input.
    /// </summary>
    public class StrokeAccumulator
    {
        private readonly List<Vector2> points = new List<Vector2>();
        private readonly float maxLength;
        private readonly float minPointDistance;

        public IReadOnlyList<Vector2> Points => points;
        public float Length { get; private set; }
        public bool IsCapped { get; private set; }

        public StrokeAccumulator(float maxLength, float minPointDistance)
        {
            this.maxLength = Mathf.Max(maxLength, 0f);
            this.minPointDistance = Mathf.Max(minPointDistance, 0f);
        }

        public void Begin(Vector2 start)
        {
            points.Clear();
            Length = 0f;
            IsCapped = false;
            points.Add(start);
        }

        /// <summary>Adds a point, clamping it onto the cap if it would exceed the maximum length. Ignored once capped.</summary>
        public void Add(Vector2 point)
        {
            if (IsCapped || points.Count == 0) return;

            Vector2 last = points[points.Count - 1];
            Vector2 delta = point - last;
            float distance = delta.magnitude;
            if (distance < minPointDistance || distance <= 0f) return;

            float remaining = maxLength - Length;
            if (distance >= remaining)
            {
                // Truncate this segment so the stroke ends exactly at the cap.
                point = last + delta * (remaining / distance);
                distance = remaining;
                IsCapped = true;
            }

            Length += distance;
            points.Add(point);
        }
    }
}
