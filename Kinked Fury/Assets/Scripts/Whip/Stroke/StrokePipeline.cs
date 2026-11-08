using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
    public enum StrokeStepType
    {
        Resample, // parameter = spacing in world units
        Simplify, // parameter = tolerance in world units
        Spline    // parameter = samples per segment (rounded to an int)
    }

    [Serializable]
    public struct StrokeStep
    {
        public StrokeStepType type;
        public float parameter;

        public StrokeStep(StrokeStepType type, float parameter)
        {
            this.type = type;
            this.parameter = parameter;
        }

        public IStrokeProcessor Build()
        {
            switch (type)
            {
                case StrokeStepType.Resample: return new ResampleProcessor(parameter);
                case StrokeStepType.Simplify: return new SimplifyProcessor(parameter);
                case StrokeStepType.Spline: return new SplineProcessor(Mathf.RoundToInt(parameter));
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown stroke step type.");
            }
        }
    }

    /// <summary>A named, ordered chain of steps. Editable in the Inspector so combinations can be compared by feel.</summary>
    [Serializable]
    public class StrokePreset
    {
        public string name;
        public StrokeStep[] steps;

        public StrokePreset(string name, params StrokeStep[] steps)
        {
            this.name = name;
            this.steps = steps;
        }

        public List<IStrokeProcessor> BuildProcessors()
        {
            var processors = new List<IStrokeProcessor>();
            if (steps == null) return processors;
            foreach (StrokeStep step in steps) processors.Add(step.Build());
            return processors;
        }
    }

    public static class StrokePipeline
    {
        /// <summary>Runs the raw stroke through each processor in order and returns the final polyline.</summary>
        public static List<Vector2> Run(IReadOnlyList<Vector2> raw, IReadOnlyList<IStrokeProcessor> processors)
        {
            var current = new List<Vector2>(raw ?? (IReadOnlyList<Vector2>)Array.Empty<Vector2>());
            if (processors == null) return current;
            foreach (IStrokeProcessor processor in processors) current = processor.Process(current);
            return current;
        }
    }
}
