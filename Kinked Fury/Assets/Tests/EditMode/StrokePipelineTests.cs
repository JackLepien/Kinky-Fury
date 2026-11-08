using System.Collections.Generic;
using KinkedFury.Whip;
using NUnit.Framework;
using UnityEngine;

namespace KinkedFury.Tests
{
    /// <summary>Extreme-input tests for the stroke processors, pipeline, length cap, and stat rules.</summary>
    public class StrokePipelineTests
    {
        // ---------- test shapes ----------

        private static List<Vector2> Line(Vector2 from, Vector2 to, int steps)
        {
            var points = new List<Vector2>();
            for (int i = 0; i <= steps; i++) points.Add(Vector2.Lerp(from, to, i / (float)steps));
            return points;
        }

        private static List<Vector2> Spiral(int count)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * 0.25f;
                float radius = 0.05f + i * 0.01f;
                points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return points;
        }

        // Right-angle zigzag with extra collinear points along each run.
        private static List<Vector2> Zigzag()
        {
            var points = Line(new Vector2(0, 0), new Vector2(5, 0), 10);
            points.AddRange(Line(new Vector2(5, 0), new Vector2(5, 5), 10).GetRange(1, 10));
            points.AddRange(Line(new Vector2(5, 5), new Vector2(10, 5), 10).GetRange(1, 10));
            return points;
        }

        private static List<Vector2> Jagged(int count)
        {
            var random = new System.Random(42);
            var points = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                points.Add(new Vector2(i * 0.1f, (float)(random.NextDouble() * 2.0 - 1.0)));
            }
            return points;
        }

        private static float PolylineLength(IReadOnlyList<Vector2> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++) length += (points[i] - points[i - 1]).magnitude;
            return length;
        }

        private static void AssertAllFinite(IReadOnlyList<Vector2> points)
        {
            foreach (Vector2 p in points)
            {
                Assert.IsFalse(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y),
                    $"Non-finite point {p}");
            }
        }

        // ---------- degenerate input, every processor ----------

        private static IEnumerable<IStrokeProcessor> AllProcessors()
        {
            yield return new ResampleProcessor(0.25f);
            yield return new SimplifyProcessor(0.08f);
            yield return new SplineProcessor(8);
        }

        [Test]
        public void EveryProcessor_HandlesEmptyInput()
        {
            foreach (IStrokeProcessor processor in AllProcessors())
            {
                Assert.AreEqual(0, processor.Process(new List<Vector2>()).Count, processor.Name);
            }
        }

        [Test]
        public void EveryProcessor_HandlesSinglePoint()
        {
            foreach (IStrokeProcessor processor in AllProcessors())
            {
                List<Vector2> result = processor.Process(new List<Vector2> { new Vector2(1, 2) });
                Assert.AreEqual(1, result.Count, processor.Name);
                Assert.AreEqual(new Vector2(1, 2), result[0], processor.Name);
            }
        }

        [Test]
        public void EveryProcessor_HandlesZeroLengthStroke()
        {
            var stationary = new List<Vector2>();
            for (int i = 0; i < 20; i++) stationary.Add(new Vector2(3, 3));

            foreach (IStrokeProcessor processor in AllProcessors())
            {
                List<Vector2> result = processor.Process(stationary);
                AssertAllFinite(result);
                Assert.Greater(result.Count, 0, processor.Name);
                Assert.AreEqual(0f, PolylineLength(result), 1e-3f, processor.Name);
            }
        }

        [Test]
        public void EveryProcessor_DoesNotMutateInput()
        {
            List<Vector2> input = Spiral(40);
            var copy = new List<Vector2>(input);
            foreach (IStrokeProcessor processor in AllProcessors()) processor.Process(input);
            CollectionAssert.AreEqual(copy, input);
        }

        // ---------- resample ----------

        [Test]
        public void Resample_StraightLine_IsEvenlySpacedAndKeepsEndpoints()
        {
            List<Vector2> input = Line(Vector2.zero, new Vector2(10, 0), 7);
            List<Vector2> result = new ResampleProcessor(1f).Process(input);

            Assert.AreEqual(11, result.Count);
            Assert.AreEqual(input[0], result[0]);
            Assert.AreEqual(input[input.Count - 1].x, result[result.Count - 1].x, 1e-4f);
            for (int i = 1; i < result.Count; i++)
            {
                Assert.AreEqual(1f, (result[i] - result[i - 1]).magnitude, 1e-3f);
            }
        }

        [Test]
        public void Resample_Spiral_IsFiniteAndPreservesLengthRoughly()
        {
            List<Vector2> input = Spiral(300);
            List<Vector2> result = new ResampleProcessor(0.1f).Process(input);

            AssertAllFinite(result);
            Assert.AreEqual(PolylineLength(input), PolylineLength(result), PolylineLength(input) * 0.02f);
        }

        // ---------- simplify ----------

        [Test]
        public void Simplify_Zigzag_KeepsRightAngleCorners()
        {
            List<Vector2> result = new SimplifyProcessor(0.05f).Process(Zigzag());

            Assert.AreEqual(4, result.Count, "Expected start, two corners, end.");
            Assert.AreEqual(new Vector2(5, 0), result[1]);
            Assert.AreEqual(new Vector2(5, 5), result[2]);
        }

        [Test]
        public void Simplify_WobblyStraightLine_CollapsesToTwoPoints()
        {
            var wobbly = new List<Vector2>();
            for (int i = 0; i <= 50; i++) wobbly.Add(new Vector2(i * 0.2f, (i % 2 == 0 ? 0.01f : -0.01f)));

            Assert.AreEqual(2, new SimplifyProcessor(0.05f).Process(wobbly).Count);
        }

        [Test]
        public void Simplify_ClosedLoop_DoesNotCollapseToAPoint()
        {
            var loop = new List<Vector2>();
            for (int i = 0; i <= 32; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                loop.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            }
            loop[loop.Count - 1] = loop[0]; // exactly closed

            Assert.Greater(new SimplifyProcessor(0.05f).Process(loop).Count, 4);
        }

        [Test]
        public void Simplify_LongStroke_DoesNotOverflowStack()
        {
            Assert.DoesNotThrow(() => new SimplifyProcessor(0.0001f).Process(Jagged(50000)));
        }

        // ---------- spline ----------

        [Test]
        public void Spline_PassesThroughEveryInputPoint()
        {
            var input = new List<Vector2> { new Vector2(0, 0), new Vector2(3, 0), new Vector2(3, 4), new Vector2(7, 4) };
            List<Vector2> result = new SplineProcessor(8).Process(input);

            Assert.AreEqual((input.Count - 1) * 8 + 1, result.Count);
            for (int i = 0; i < input.Count; i++)
            {
                Assert.AreEqual(input[i].x, result[i * 8].x, 1e-3f);
                Assert.AreEqual(input[i].y, result[i * 8].y, 1e-3f);
            }
        }

        [Test]
        public void Spline_DuplicatePoints_DoNotProduceNaN()
        {
            var input = new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1), new Vector2(2, 0)
            };
            AssertAllFinite(new SplineProcessor(8).Process(input));
        }

        // ---------- pipeline ----------

        [Test]
        public void Pipeline_AllPresetChains_SurviveEveryExtremeShape()
        {
            var shapes = new Dictionary<string, List<Vector2>>
            {
                { "spiral", Spiral(400) },
                { "zigzag", Zigzag() },
                { "jagged", Jagged(500) },
                { "single point", new List<Vector2> { Vector2.one } },
                { "empty", new List<Vector2>() },
            };
            var chains = new List<IStrokeProcessor[]>
            {
                new IStrokeProcessor[] { new ResampleProcessor(0.25f) },
                new IStrokeProcessor[] { new SimplifyProcessor(0.08f), new ResampleProcessor(0.25f) },
                new IStrokeProcessor[] { new ResampleProcessor(0.3f), new SplineProcessor(8), new ResampleProcessor(0.25f) },
                new IStrokeProcessor[] { new SimplifyProcessor(0.08f), new SplineProcessor(8) },
            };

            foreach (var shape in shapes)
            {
                foreach (IStrokeProcessor[] chain in chains)
                {
                    List<Vector2> result = null;
                    Assert.DoesNotThrow(() => result = StrokePipeline.Run(shape.Value, chain), shape.Key);
                    AssertAllFinite(result);
                    if (shape.Value.Count > 0)
                    {
                        Assert.AreEqual(shape.Value[0], result[0], $"{shape.Key}: start point preserved");
                    }
                }
            }
        }

        // ---------- length cap ----------

        [Test]
        public void Accumulator_StopsAtHardLengthCap()
        {
            var accumulator = new StrokeAccumulator(maxLength: 5f, minPointDistance: 0.05f);
            accumulator.Begin(Vector2.zero);
            for (int i = 1; i <= 200; i++) accumulator.Add(new Vector2(i * 0.1f, 0));

            Assert.IsTrue(accumulator.IsCapped);
            Assert.AreEqual(5f, accumulator.Length, 1e-4f);
            Assert.AreEqual(5f, PolylineLength(accumulator.Points), 1e-3f);
        }

        [Test]
        public void Accumulator_IgnoresPointsAfterCap()
        {
            var accumulator = new StrokeAccumulator(1f, 0.05f);
            accumulator.Begin(Vector2.zero);
            accumulator.Add(new Vector2(10, 0)); // jumps far past the cap in one sample
            int countAtCap = accumulator.Points.Count;
            accumulator.Add(new Vector2(10, 10));

            Assert.AreEqual(1f, accumulator.Length, 1e-4f);
            Assert.AreEqual(countAtCap, accumulator.Points.Count);
        }

        [Test]
        public void Accumulator_ZeroMovement_AddsNothing()
        {
            var accumulator = new StrokeAccumulator(10f, 0.05f);
            accumulator.Begin(Vector2.one);
            for (int i = 0; i < 20; i++) accumulator.Add(Vector2.one);

            Assert.AreEqual(1, accumulator.Points.Count);
            Assert.AreEqual(0f, accumulator.Length);
        }

        // ---------- stat rules ----------

        [Test]
        public void Stats_TallerShapeOfSameLength_HasMoreDefense()
        {
            var tuning = new WhipStatTuning();
            WhipStats flat = WhipStatCalculator.Calculate(Line(Vector2.zero, new Vector2(4, 0), 8), tuning);
            WhipStats tall = WhipStatCalculator.Calculate(Line(Vector2.zero, new Vector2(0, 4), 8), tuning);

            Assert.AreEqual(flat.Length, tall.Length, 1e-4f);
            Assert.Greater(tall.Defense, flat.Defense);
        }

        [Test]
        public void Stats_LongerWhip_HasSmallerHitbox()
        {
            var tuning = new WhipStatTuning();
            WhipStats shortWhip = WhipStatCalculator.Calculate(Line(Vector2.zero, new Vector2(2, 0), 4), tuning);
            WhipStats longWhip = WhipStatCalculator.Calculate(Line(Vector2.zero, new Vector2(10, 0), 20), tuning);

            Assert.Less(longWhip.HitboxRadius, shortWhip.HitboxRadius);
        }

        [Test]
        public void Stats_DamageLengthPenalty_LowersDamageWhenHitboxBonusIsOff()
        {
            var tuning = new WhipStatTuning { smallHitboxDamageBonus = 0f };
            WhipStats shortWhip = WhipStatCalculator.Calculate(Line(Vector2.zero, new Vector2(2, 0), 4), tuning);
            WhipStats longWhip = WhipStatCalculator.Calculate(Line(Vector2.zero, new Vector2(10, 0), 20), tuning);

            Assert.Less(longWhip.Damage, shortWhip.Damage);
        }

        [Test]
        public void Stats_DegenerateInput_IsZeroNotNaN()
        {
            var tuning = new WhipStatTuning();
            foreach (var points in new[] { new List<Vector2>(), new List<Vector2> { Vector2.one } })
            {
                WhipStats stats = WhipStatCalculator.Calculate(points, tuning);
                Assert.AreEqual(0f, stats.Length);
                Assert.AreEqual(0f, stats.Damage);
                Assert.AreEqual(0f, stats.Defense);
            }
        }

        [Test]
        public void Stats_StrokeAtLengthCap_HasLengthFractionOne()
        {
            var tuning = new WhipStatTuning { maxLength = 12f };
            WhipStats stats = WhipStatCalculator.Calculate(Line(Vector2.zero, new Vector2(12, 0), 24), tuning);

            Assert.AreEqual(1f, stats.LengthFraction, 1e-4f);
        }

        // ---------- whip shape, sweep, circle chain, mirroring ----------

        [Test]
        public void Shape_IsAnchoredAtOrigin_AndRejectsDegenerateStrokes()
        {
            var tuning = new WhipStatTuning();
            List<Vector2> drawn = Line(new Vector2(4, 7), new Vector2(8, 7), 4);
            WhipShape shape = WhipShape.FromPoints(drawn, WhipStatCalculator.Calculate(drawn, tuning));

            Assert.IsNotNull(shape);
            Assert.AreEqual(Vector2.zero, shape.LocalPoints[0]);
            Assert.AreEqual(4f, shape.LocalPoints[shape.LocalPoints.Count - 1].x, 1e-4f);

            Assert.IsNull(WhipShape.FromPoints(new List<Vector2>(), WhipStatCalculator.Calculate(new List<Vector2>(), tuning)));
            var still = new List<Vector2> { Vector2.one, Vector2.one };
            Assert.IsNull(WhipShape.FromPoints(still, WhipStatCalculator.Calculate(still, tuning)));
        }

        [Test]
        public void Sweep_RevealsTheRequestedShareOfLength()
        {
            List<Vector2> line = Line(Vector2.zero, new Vector2(10, 0), 5);

            Assert.AreEqual(1, WhipSweep.VisiblePrefix(line, 0f).Count);
            Assert.AreEqual(5f, PolylineLength(WhipSweep.VisiblePrefix(line, 0.5f)), 1e-3f);
            Assert.AreEqual(10f, PolylineLength(WhipSweep.VisiblePrefix(line, 1f)), 1e-3f);
            Assert.AreEqual(10f, PolylineLength(WhipSweep.VisiblePrefix(line, 5f)), 1e-3f, "fraction is clamped");
        }

        [Test]
        public void Sweep_TipFollowsBends_NotTheStraightChord()
        {
            // Right angle: 5 right, then 5 up. Half the length ends exactly at the corner.
            var corner = new List<Vector2> { Vector2.zero, new Vector2(5, 0), new Vector2(5, 5) };
            List<Vector2> half = WhipSweep.VisiblePrefix(corner, 0.5f);
            Assert.AreEqual(new Vector2(5, 0).x, half[half.Count - 1].x, 1e-3f);
            Assert.AreEqual(0f, half[half.Count - 1].y, 1e-3f);

            List<Vector2> threeQuarters = WhipSweep.VisiblePrefix(corner, 0.75f);
            Vector2 tip = threeQuarters[threeQuarters.Count - 1];
            Assert.AreEqual(5f, tip.x, 1e-3f);
            Assert.AreEqual(2.5f, tip.y, 1e-3f);
        }

        [Test]
        public void CircleChain_LeavesNoGapsAlongASpiral()
        {
            List<Vector2> spiral = Spiral(300);
            float radius = 0.1f;
            List<Vector2> centers = WhipSweep.CircleCenters(spiral, radius);

            // Every point of the drawn line must sit inside at least one circle.
            foreach (Vector2 p in spiral)
            {
                float nearest = float.MaxValue;
                foreach (Vector2 c in centers) nearest = Mathf.Min(nearest, (p - c).magnitude);
                Assert.LessOrEqual(nearest, radius, $"gap near {p}");
            }
        }

        [Test]
        public void CircleChain_NeighboursAlwaysOverlap()
        {
            List<Vector2> centers = WhipSweep.CircleCenters(Line(Vector2.zero, new Vector2(12, 0), 50), 0.1f);
            for (int i = 1; i < centers.Count; i++)
            {
                Assert.Less((centers[i] - centers[i - 1]).magnitude, 2f * 0.1f);
            }
        }

        [Test]
        public void ToWorld_MirrorsHorizontallyWhenFacingLeft_AndKeepsHeight()
        {
            Vector2 hand = new Vector2(10, 3);
            Vector2 local = new Vector2(4, 2);

            Assert.AreEqual(new Vector2(14, 5), WhipSweep.ToWorld(local, hand, 1));
            Assert.AreEqual(new Vector2(6, 5), WhipSweep.ToWorld(local, hand, -1));
        }

        [Test]
        public void ToWorld_FirstPointIsAlwaysTheHand()
        {
            Vector2 hand = new Vector2(-2, 1);
            Assert.AreEqual(hand, WhipSweep.ToWorld(Vector2.zero, hand, 1));
            Assert.AreEqual(hand, WhipSweep.ToWorld(Vector2.zero, hand, -1));
        }
    }
}
