using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Prototype harness for the drawing pipeline. Hold the left mouse button to draw; the raw stroke
    /// (purple) and the processed stroke (cyan) are shown together with the derived stats.
    /// The stroke is processed with Simplify > Spline; both are tunable in the Inspector.
    /// Setup: add this component to an empty GameObject in a scene that has an orthographic Main Camera.
    /// </summary>
    public class WhipDrawingTester : MonoBehaviour
    {
        [Header("Drawing")]
        [Tooltip("Mouse samples closer together than this (world units) are ignored.")]
        [SerializeField] private float minPointDistance = 0.05f;
        [SerializeField] private WhipStatTuning tuning = new WhipStatTuning();

        [Header("Processing (Simplify > Spline)")]
        [Tooltip("Ramer-Douglas-Peucker tolerance in world units: how far a point may sit from the simplified line before it is kept. Higher = fewer, sharper corners.")]
        [SerializeField] private float simplifyTolerance = 0.08f;
        [Tooltip("Smoothing samples inserted per segment of the simplified line. Higher = smoother curves.")]
        [SerializeField, Min(1)] private int splineSamplesPerSegment = 8;

        private Camera cam;
        private StrokeAccumulator accumulator;
        private LineRenderer rawLine;
        private LineRenderer processedLine;
        private List<Vector2> processed = new List<Vector2>();
        private WhipStats stats;
        private bool drawing;
        private GUIStyle labelStyle;

        private void Start()
        {
            cam = Camera.main;
            if (cam == null) Debug.LogError("WhipDrawingTester needs a camera tagged MainCamera.", this);

            accumulator = new StrokeAccumulator(tuning.maxLength, minPointDistance);
            rawLine = CreateLine("Raw Stroke", new Color(0.65f, 0.4f, 0.95f, 0.55f), 0.06f, 0);
            processedLine = CreateLine("Processed Stroke", new Color(0f, 0.9f, 1f, 1f), 0.08f, 1);
        }

        private void Update()
        {
            if (cam == null || Mouse.current == null) return;

            Vector2 mouseWorld = cam.ScreenToWorldPoint(
                new Vector3(Mouse.current.position.x.ReadValue(),
                            Mouse.current.position.y.ReadValue(),
                            Mathf.Abs(cam.transform.position.z)));

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Rebuilt each stroke so Inspector edits to the cap and spacing take effect immediately.
                accumulator = new StrokeAccumulator(tuning.maxLength, minPointDistance);
                accumulator.Begin(mouseWorld);
                drawing = true;
                processed.Clear();
                stats = WhipStatCalculator.Calculate(processed, tuning);
                SetLine(processedLine, processed);
            }
            else if (drawing && Mouse.current.leftButton.isPressed)
            {
                accumulator.Add(mouseWorld);
            }

            if (drawing && Mouse.current.leftButton.wasReleasedThisFrame) drawing = false;

            if (accumulator != null) SetLine(rawLine, accumulator.Points);
            if (!drawing && accumulator != null && accumulator.Points.Count > 0 && processed.Count == 0)
            {
                Reprocess();
            }
        }

        /// <summary>The most recently processed stroke as a saved whip shape, or false if nothing usable is drawn.</summary>
        public bool TryGetShape(out WhipShape shape)
        {
            shape = drawing ? null : WhipShape.FromPoints(processed, stats);
            return shape != null;
        }

        private void Reprocess()
        {
            if (accumulator == null) return;
            // Simplify removes hand jitter and keeps the real corners; the spline then rounds them into a smooth path.
            var processors = new List<IStrokeProcessor>
            {
                new SimplifyProcessor(simplifyTolerance),
                new SplineProcessor(splineSamplesPerSegment),
            };
            processed = StrokePipeline.Run(accumulator.Points, processors);
            stats = WhipStatCalculator.Calculate(processed, tuning);
            SetLine(processedLine, processed);
        }

        private LineRenderer CreateLine(string lineName, Color color, float width, int sortingOrder)
        {
            var lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = sortingOrder;
            line.positionCount = 0;
            return line;
        }

        private static void SetLine(LineRenderer line, IReadOnlyList<Vector2> points)
        {
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
        }

        private void OnGUI()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };

            int rawCount = accumulator != null ? accumulator.Points.Count : 0;
            float rawLength = accumulator != null ? accumulator.Length : 0f;
            string capNote = accumulator != null && accumulator.IsCapped ? "  (LENGTH CAP REACHED)" : "";

            string text =
                $"Raw points: {rawCount}   Processed points: {processed.Count}\n" +
                $"Drawn length: {rawLength:0.00} / {tuning.maxLength:0.0}{capNote}\n" +
                $"Processed length: {stats.Length:0.00}   Width: {stats.Width:0.00}   Height: {stats.Height:0.00}\n" +
                $"Defense: {stats.Defense:0.00}   Hitbox radius: {stats.HitboxRadius:0.00}   Damage: {stats.Damage:0.0}";

            GUI.Label(new Rect(12, 8, 1000, 130), text, labelStyle);
        }

        private void OnDrawGizmos()
        {
            // Processed points as dots, so point density and spacing are visible.
            Gizmos.color = Color.white;
            for (int i = 0; i < processed.Count; i++) Gizmos.DrawSphere(processed[i], 0.03f);
        }
    }
}
