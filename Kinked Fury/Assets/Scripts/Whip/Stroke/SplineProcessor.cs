using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Centripetal Catmull-Rom spline through the input points, sampled <c>samplesPerSegment</c> times
    /// between each pair. The curve passes through every input point, so corners become rounded bends
    /// rather than vanishing. Centripetal parameterisation avoids overshoot on unevenly spaced input.
    /// </summary>
    public class SplineProcessor : IStrokeProcessor
    {
        private const float MinKnotGap = 1e-3f; // keeps duplicate points from producing divide-by-zero
        private readonly int samplesPerSegment;

        public string Name => $"Spline({samplesPerSegment})";

        public SplineProcessor(int samplesPerSegment)
        {
            this.samplesPerSegment = Mathf.Max(samplesPerSegment, 1);
        }

        public List<Vector2> Process(IReadOnlyList<Vector2> input)
        {
            var result = new List<Vector2>();
            if (input == null || input.Count == 0) return result;
            if (input.Count < 3)
            {
                for (int i = 0; i < input.Count; i++) result.Add(input[i]);
                return result;
            }

            int last = input.Count - 1;
            for (int i = 0; i < last; i++)
            {
                Vector2 p1 = input[i];
                Vector2 p2 = input[i + 1];
                // At the ends there is no neighbour: mirror the adjacent point so the curve continues straight.
                Vector2 p0 = i > 0 ? input[i - 1] : p1 + (p1 - p2);
                Vector2 p3 = i + 1 < last ? input[i + 2] : p2 + (p2 - p1);

                for (int s = 0; s < samplesPerSegment; s++)
                {
                    result.Add(Evaluate(p0, p1, p2, p3, s / (float)samplesPerSegment));
                }
            }
            result.Add(input[last]);
            return result;
        }

        // Barry-Goldman pyramid evaluation of a centripetal Catmull-Rom segment from p1 to p2, u in [0,1).
        private static Vector2 Evaluate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
        {
            float t0 = 0f;
            float t1 = t0 + KnotGap(p0, p1);
            float t2 = t1 + KnotGap(p1, p2);
            float t3 = t2 + KnotGap(p2, p3);
            float t = Mathf.Lerp(t1, t2, u);

            Vector2 a1 = Lerp(p0, p1, t0, t1, t);
            Vector2 a2 = Lerp(p1, p2, t1, t2, t);
            Vector2 a3 = Lerp(p2, p3, t2, t3, t);
            Vector2 b1 = Lerp(a1, a2, t0, t2, t);
            Vector2 b2 = Lerp(a2, a3, t1, t3, t);
            return Lerp(b1, b2, t1, t2, t);
        }

        private static float KnotGap(Vector2 a, Vector2 b)
        {
            // Centripetal: knot spacing is the square root of the distance (distance^0.5).
            return Mathf.Max(Mathf.Sqrt((b - a).magnitude), MinKnotGap);
        }

        private static Vector2 Lerp(Vector2 a, Vector2 b, float ta, float tb, float t)
        {
            float w = (t - ta) / (tb - ta);
            return a * (1f - w) + b * w;
        }
    }
}
