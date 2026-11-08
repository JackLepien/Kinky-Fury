using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>Pure helpers for playing a whip shape out: how much is visible, where the hit circles go, and mirroring.</summary>
    public static class WhipSweep
    {
        /// <summary>
        /// The part of the line that has been drawn out so far. <paramref name="fraction"/> is the share of
        /// total length revealed (0 = just the hand, 1 = whole whip). The tip is interpolated along its segment
        /// so it moves smoothly instead of jumping from point to point.
        /// </summary>
        public static List<Vector2> VisiblePrefix(IReadOnlyList<Vector2> points, float fraction)
        {
            var visible = new List<Vector2>();
            if (points == null || points.Count == 0) return visible;

            visible.Add(points[0]);
            if (points.Count == 1) return visible;

            float total = 0f;
            for (int i = 1; i < points.Count; i++) total += (points[i] - points[i - 1]).magnitude;

            float target = total * Mathf.Clamp01(fraction);
            float travelled = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                float segment = (points[i] - points[i - 1]).magnitude;
                if (travelled + segment >= target)
                {
                    float t = segment > 0f ? (target - travelled) / segment : 0f;
                    Vector2 tip = Vector2.Lerp(points[i - 1], points[i], t);
                    if ((tip - visible[visible.Count - 1]).sqrMagnitude > 1e-10f) visible.Add(tip);
                    return visible;
                }
                travelled += segment;
                visible.Add(points[i]);
            }
            return visible;
        }

        /// <summary>
        /// Centres for the chain of hit circles along the line. Spacing is 1.5x the radius, so neighbouring
        /// circles always overlap and nothing can slip between them, while keeping the circle count modest.
        /// </summary>
        public static List<Vector2> CircleCenters(IReadOnlyList<Vector2> points, float radius)
        {
            float spacing = Mathf.Max(radius, 0.001f) * 1.5f;
            return new ResampleProcessor(spacing).Process(points);
        }

        /// <summary>Anchors a local whip point at the hand, flipping horizontally when facing left (facing = -1).</summary>
        public static Vector2 ToWorld(Vector2 local, Vector2 hand, int facing)
        {
            return hand + new Vector2(local.x * (facing < 0 ? -1f : 1f), local.y);
        }
    }
}
