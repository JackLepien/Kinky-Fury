using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Prototype harness for the drawing pipeline. Hold the left mouse button to draw; the raw stroke
    /// (purple) and the processed stroke (cyan) are shown together with the derived stats.
    /// Tab or number keys 1-9 switch the processing preset live and re-process the current stroke.
    /// Setup: add this component to an empty GameObject in a scene that has an orthographic Main Camera.
    /// </summary>
    public class WhipDrawingTester : MonoBehaviour
    {
        [Header("Drawing")]
        [Tooltip("Mouse samples closer together than this (world units) are ignored.")]
        [SerializeField] private float minPointDistance = 0.05f;
        [SerializeField] private WhipStatTuning tuning = new WhipStatTuning();

        [Header("Processing (Tab or 1-9 to switch)")]
        [SerializeField] private int activePreset = 3; // Developer's chosen default: "Simplify > Spline"
        [SerializeField] private List<StrokePreset> presets = new List<StrokePreset>
        {
            new StrokePreset("Resample only",
                new StrokeStep(StrokeStepType.Resample, 0.25f)),
            new StrokePreset("Simplify > Resample",
                new StrokeStep(StrokeStepType.Simplify, 0.08f),
                new StrokeStep(StrokeStepType.Resample, 0.25f)),
            new StrokePreset("Resample > Spline > Resample",
                new StrokeStep(StrokeStepType.Resample, 0.3f),
                new StrokeStep(StrokeStepType.Spline, 8f),
                new StrokeStep(StrokeStepType.Resample, 0.25f)),
            new StrokePreset("Simplify > Spline",
                new StrokeStep(StrokeStepType.Simplify, 0.08f),
                new StrokeStep(StrokeStepType.Spline, 8f)),
        };

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
            activePreset = Mathf.Clamp(activePreset, 0, Mathf.Max(presets.Count - 1, 0));
        }

        private void Update()
        {
            if (cam == null || Mouse.current == null) return;

            HandlePresetKeys();

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

        private void HandlePresetKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || presets.Count == 0) return;

            int newPreset = activePreset;
            if (keyboard.tabKey.wasPressedThisFrame) newPreset = (activePreset + 1) % presets.Count;
            for (int i = 0; i < Mathf.Min(presets.Count, 9); i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame) newPreset = i;
            }

            if (newPreset != activePreset)
            {
                activePreset = newPreset;
                if (!drawing) Reprocess();
            }
        }

        private void Reprocess()
        {
            if (accumulator == null || presets.Count == 0) return;
            List<IStrokeProcessor> processors = presets[activePreset].BuildProcessors();
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
            if (presets.Count == 0) return;
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };

            int rawCount = accumulator != null ? accumulator.Points.Count : 0;
            float rawLength = accumulator != null ? accumulator.Length : 0f;
            string capNote = accumulator != null && accumulator.IsCapped ? "  (LENGTH CAP REACHED)" : "";

            string text =
                $"Preset {activePreset + 1}/{presets.Count}: {presets[activePreset].name}   [Tab / 1-9]\n" +
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
