using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Ramer-Douglas-Peucker simplification: drops points that lie within <c>tolerance</c> of the line
    /// between their neighbours, so straight runs collapse and corners survive.
    /// Output spacing is uneven; follow with a ResampleProcessor if even spacing is needed.
    /// </summary>
    public class SimplifyProcessor : IStrokeProcessor
    {
        private readonly float tolerance;

        public string Name => $"Simplify({tolerance:0.###})";

        public SimplifyProcessor(float tolerance)
        {
            this.tolerance = Mathf.Max(tolerance, 0f);
        }

        public List<Vector2> Process(IReadOnlyList<Vector2> input)
        {
            var result = new List<Vector2>();
            if (input == null || input.Count == 0) return result;
            if (input.Count <= 2)
            {
                for (int i = 0; i < input.Count; i++) result.Add(input[i]);
                return result;
            }

            int n = input.Count;
            var keep = new bool[n];
            keep[0] = true;
            keep[n - 1] = true;

            // Explicit stack instead of recursion so long strokes can't overflow the call stack.
            var stack = new Stack<(int start, int end)>();
            stack.Push((0, n - 1));

            while (stack.Count > 0)
            {
                (int start, int end) = stack.Pop();
                float maxDistance = -1f;
                int maxIndex = -1;

                for (int i = start + 1; i < end; i++)
                {
                    float d = DistanceToSegment(input[i], input[start], input[end]);
                    if (d > maxDistance)
                    {
                        maxDistance = d;
                        maxIndex = i;
                    }
                }

                if (maxIndex >= 0 && maxDistance > tolerance)
                {
                    keep[maxIndex] = true;
                    stack.Push((start, maxIndex));
                    stack.Push((maxIndex, end));
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (keep[i]) result.Add(input[i]);
            }
            return result;
        }

        // Distance to the segment (not the infinite line), so closed loops where start == end still work.
        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared < 1e-12f) return (p - a).magnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
            return (p - (a + ab * t)).magnitude;
        }
    }
}
