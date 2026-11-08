using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Re-emits the stroke with a point every <c>spacing</c> units along its length, regardless of how fast
    /// the stroke was drawn. Does not smooth: jitter and corners are kept as they are.
    /// The first and last points of the input are always preserved.
    /// </summary>
    public class ResampleProcessor : IStrokeProcessor
    {
        private const float Epsilon = 1e-6f;
        private readonly float spacing;

        public string Name => $"Resample({spacing:0.###})";

        public ResampleProcessor(float spacing)
        {
            this.spacing = Mathf.Max(spacing, 0.001f);
        }

        public List<Vector2> Process(IReadOnlyList<Vector2> input)
        {
            var result = new List<Vector2>();
            if (input == null || input.Count == 0) return result;

            result.Add(input[0]);
            float needed = spacing; // distance still to travel before the next emitted point

            for (int i = 1; i < input.Count; i++)
            {
                Vector2 a = input[i - 1];
                Vector2 b = input[i];
                float segLength = (b - a).magnitude;
                if (segLength < Epsilon) continue;

                float travelled = 0f;
                while (segLength - travelled >= needed)
                {
                    travelled += needed;
                    result.Add(a + (b - a) * (travelled / segLength));
                    needed = spacing;
                }
                needed -= segLength - travelled;
            }

            // Keep the true end of the stroke even if it fell between two samples.
            Vector2 end = input[input.Count - 1];
            if ((result[result.Count - 1] - end).sqrMagnitude > Epsilon) result.Add(end);

            return result;
        }
    }
}
