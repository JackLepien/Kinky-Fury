using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Plays the most recently drawn whip shape out from the player's hand: the whip sweeps out to its tip,
    /// holds briefly, then vanishes. Hits are found by testing a chain of overlapping circles along the visible
    /// part of the whip each frame, so spirals and tight bends register as reliably as straight lines.
    /// Placeholder controls: F or right mouse button swings. Facing comes from a PlayerMover on the same object
    /// if there is one; otherwise A/D (or arrow keys) just set facing.
    /// </summary>
    public class WhipAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WhipDrawingTester shapeSource;
        [Tooltip("The whip's first point is anchored here.")]
        [SerializeField] private Transform hand;
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("Feel")]
        [Tooltip("How fast the tip travels while extending, in world units per second.")]
        [SerializeField] private float sweepSpeed = 50f;
        [Tooltip("Shortest allowed sweep duration, so very short whips don't look instant.")]
        [SerializeField] private float minSweepTime = 0.05f;
        [Tooltip("How long the fully extended whip stays out (and keeps hitting) before disappearing.")]
        [SerializeField] private float holdTime = 0.12f;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private Color lineColor = new Color(0f, 0.9f, 1f, 1f);

        private readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        private readonly Collider2D[] overlapBuffer = new Collider2D[16];
        private List<Vector2> worldCenters = new List<Vector2>();
        private WhipShape shape;
        private LineRenderer line;
        private PlayerMover mover;    // optional; supplies facing when present
        private bool swinging;
        private float elapsed;
        private float sweepTime;
        private int facing = 1;       // live facing, 1 = right, -1 = left
        private int swingFacing = 1;  // facing locked at the start of the swing
        private GUIStyle labelStyle;

        private void Start()
        {
            mover = GetComponent<PlayerMover>();
            var lineObject = new GameObject("Whip Visual");
            lineObject.transform.SetParent(transform, false);
            line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = 5;
            line.positionCount = 0;

            if (hand == null) Debug.LogError("WhipAttack needs a hand Transform.", this);
            if (shapeSource == null) Debug.LogError("WhipAttack needs a WhipDrawingTester to take shapes from.", this);
        }

        private void Update()
        {
            ReadInput();
            if (swinging) AdvanceSwing(Time.deltaTime);
        }

        private void ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (mover != null)
            {
                facing = mover.Facing; // the mover owns facing when present
            }
            else if (keyboard != null)
            {
                if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) facing = -1;
                if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) facing = 1;
            }

            bool attackPressed = (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                                 || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
            if (attackPressed && !swinging) StartSwing();
        }

        private void StartSwing()
        {
            if (hand == null || shapeSource == null) return;
            if (!shapeSource.TryGetShape(out WhipShape current)) return; // nothing drawn yet

            shape = current;
            swingFacing = facing;
            elapsed = 0f;
            sweepTime = Mathf.Max(shape.Stats.Length / Mathf.Max(sweepSpeed, 0.01f), minSweepTime);
            hitThisSwing.Clear();
            swinging = true;
        }

        private void AdvanceSwing(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed >= sweepTime + holdTime)
            {
                EndSwing();
                return;
            }

            float fraction = Mathf.Clamp01(elapsed / sweepTime);
            List<Vector2> visibleLocal = WhipSweep.VisiblePrefix(shape.LocalPoints, fraction);

            // Draw the visible line, re-anchored every frame so it follows the hand.
            // The hand's offset from the player is mirrored too, so a left-facing whip starts on the left side.
            Vector2 handOffset = hand.position - transform.position;
            if (swingFacing < 0) handOffset.x = -handOffset.x;
            Vector2 handPosition = (Vector2)transform.position + handOffset;
            line.positionCount = visibleLocal.Count;
            for (int i = 0; i < visibleLocal.Count; i++)
            {
                line.SetPosition(i, WhipSweep.ToWorld(visibleLocal[i], handPosition, swingFacing));
            }

            // Hit circles along the visible line.
            float radius = shape.Stats.HitboxRadius;
            List<Vector2> localCenters = WhipSweep.CircleCenters(visibleLocal, radius);
            worldCenters = localCenters;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = true };
            for (int c = 0; c < localCenters.Count; c++)
            {
                Vector2 center = WhipSweep.ToWorld(localCenters[c], handPosition, swingFacing);
                worldCenters[c] = center;
                int count = Physics2D.OverlapCircle(center, radius, filter, overlapBuffer);
                for (int i = 0; i < count; i++)
                {
                    IDamageable target = overlapBuffer[i].GetComponentInParent<IDamageable>();
                    // Each target is hit at most once per swing.
                    if (target != null && hitThisSwing.Add(target)) target.TakeHit(shape.Stats.Damage);
                }
            }
        }

        private void EndSwing()
        {
            swinging = false;
            line.positionCount = 0;
            worldCenters.Clear();
        }

        private void OnGUI()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };
            string facingText = facing > 0 ? "right" : "left";
            GUI.Label(new Rect(12, Screen.height - 32, 900, 26),
                $"Hold LMB: draw   |   F or RMB: swing   |   A/D: move (facing {facingText})   |   Tab / 1-9: preset", labelStyle);
        }

        private void OnDrawGizmos()
        {
            // Show the hit-circle chain while swinging.
            if (!swinging || shape == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            foreach (Vector2 center in worldCenters) Gizmos.DrawWireSphere(center, shape.Stats.HitboxRadius);
        }
    }
}
