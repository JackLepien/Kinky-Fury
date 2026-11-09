using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinkedFury.Whip
{
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
