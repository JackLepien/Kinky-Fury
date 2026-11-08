using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>Gameplay numbers derived from a drawn shape.</summary>
    public readonly struct WhipStats
    {
        public readonly float Length;         // world units along the line
        public readonly float Width;          // bounding box
        public readonly float Height;         // bounding box
        public readonly float LengthFraction; // Length / maxLength, 0..1
        public readonly float Defense;        // 0..1, driven by height
        public readonly float HitboxRadius;   // collider radius along the whip, shrinks as the whip gets longer
        public readonly float Damage;

        public WhipStats(float length, float width, float height, float lengthFraction,
                         float defense, float hitboxRadius, float damage)
        {
            Length = length;
            Width = width;
            Height = height;
            LengthFraction = lengthFraction;
            Defense = defense;
            HitboxRadius = hitboxRadius;
            Damage = damage;
        }
    }

    /// <summary>
    /// Tuning for the shape-to-stats rules. PLACEHOLDER values: the trade-off curves are a first
    /// interpretation of the pitch and are meant to be adjusted by feel.
    /// </summary>
    [Serializable]
    public class WhipStatTuning
    {
        [Tooltip("Hard cap on drawn whip length, in world units.")]
        public float maxLength = 12f;

        [Tooltip("Shape height (world units) at which defense reaches its maximum of 1.")]
        public float heightForFullDefense = 6f;

        public float baseDamage = 10f;

        [Tooltip("Fraction of damage lost at maximum length (0 = none, 1 = all).")]
        [Range(0f, 1f)] public float lengthDamagePenalty = 0.5f;

        [Tooltip("Extra damage multiplier gained as the hitbox shrinks to its minimum (0 = no bonus).")]
        [Min(0f)] public float smallHitboxDamageBonus = 1f;

        [Tooltip("Hitbox radius of a very short whip.")]
        public float maxHitboxRadius = 0.5f;

        [Tooltip("Hitbox radius of a maximum-length whip.")]
        public float minHitboxRadius = 0.1f;
    }

    /// <summary>Pure function from a processed polyline to stats, so every trade-off can be unit tested.</summary>
    public static class WhipStatCalculator
    {
        public static WhipStats Calculate(IReadOnlyList<Vector2> points, WhipStatTuning tuning)
        {
            if (points == null || points.Count < 2)
            {
                return new WhipStats(0f, 0f, 0f, 0f, 0f, tuning.maxHitboxRadius, 0f);
            }

            float length = 0f;
            Vector2 min = points[0];
            Vector2 max = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                length += (points[i] - points[i - 1]).magnitude;
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            float width = max.x - min.x;
            float height = max.y - min.y;
            float lengthFraction = tuning.maxLength > 0f ? Mathf.Clamp01(length / tuning.maxLength) : 0f;

            // Taller shapes are more defensive.
            float defense = tuning.heightForFullDefense > 0f
                ? Mathf.Clamp01(height / tuning.heightForFullDefense)
                : 0f;

            // Longer whips get a smaller hitbox...
            float hitboxRadius = Mathf.Lerp(tuning.maxHitboxRadius, tuning.minHitboxRadius, lengthFraction);
            float smallness = Mathf.InverseLerp(tuning.maxHitboxRadius, tuning.minHitboxRadius, hitboxRadius);

            // ...which boosts damage, while raw length scales it down.
            float damage = tuning.baseDamage
                           * (1f - tuning.lengthDamagePenalty * lengthFraction)
                           * (1f + tuning.smallHitboxDamageBonus * smallness);

            return new WhipStats(length, width, height, lengthFraction, defense, hitboxRadius, damage);
        }
    }
}
