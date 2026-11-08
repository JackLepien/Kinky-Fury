using UnityEngine;

namespace KinkedFury.Whip
{
    /// <summary>Placeholder target for whip testing: shows its health, flashes on hit, and resets after "dying".</summary>
    [RequireComponent(typeof(Collider2D))]
    public class TestDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 1f;
        [SerializeField] private Color hitColor = Color.white;

        private float health;
        private float respawnTimer;
        private float flashTimer;
        private float lastDamage;
        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private GUIStyle labelStyle;

        private void Awake()
        {
            health = maxHealth;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) baseColor = spriteRenderer.color;
        }

        public void TakeHit(float damage)
        {
            if (health <= 0f) return;
            health = Mathf.Max(health - damage, 0f);
            lastDamage = damage;
            flashTimer = 0.1f;
            if (health <= 0f) respawnTimer = respawnDelay;
        }

        private void Update()
        {
            if (flashTimer > 0f) flashTimer -= Time.deltaTime;
            if (spriteRenderer != null) spriteRenderer.color = flashTimer > 0f ? hitColor : baseColor;

            if (health <= 0f)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f) health = maxHealth;
            }
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
            string text = health <= 0f ? "DEFEATED" : $"{health:0}/{maxHealth:0}   (last hit {lastDamage:0.0})";
            GUI.Label(new Rect(screen.x - 100f, Screen.height - screen.y - 12f, 200f, 24f), text, labelStyle);
        }
    }
}
