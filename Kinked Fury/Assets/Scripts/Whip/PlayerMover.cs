using UnityEngine;
using UnityEngine.InputSystem;

namespace KinkedFury.Whip
{
    /// <summary>
    /// Placeholder left/right movement (A/D or arrow keys) for whip testing. Also tracks which way the player
    /// faces, which WhipAttack uses to mirror the whip. No gravity or collision yet: the real controller
    /// replaces this when the platformer movement is built.
    /// </summary>
    public class PlayerMover : MonoBehaviour
    {
        [Tooltip("Horizontal speed in world units per second.")]
        [SerializeField] private float moveSpeed = 7f;

        /// <summary>1 = facing right, -1 = facing left. Keeps its last value while standing still.</summary>
        public int Facing { get; private set; } = 1;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            float direction = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction -= 1f;
            if (direction == 0f) return; // standing still, or both keys held

            Facing = direction > 0f ? 1 : -1;
            transform.Translate(direction * moveSpeed * Time.deltaTime, 0f, 0f, Space.World);
        }
    }
}
