using KinkedFury.Whip;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KinkedFury.EditorTools
{
    /// <summary>
    /// One-click scene setup for whip testing: placeholder player (with hand anchor and left/right movement),
    /// a test dummy, and the drawing tester, all wired together. Safe to run repeatedly: objects that already
    /// exist are brought up to date (size, hand position, movement) instead of duplicated.
    /// Changes are undoable and the scene is marked dirty but not saved.
    /// </summary>
    public static class WhipTestSetupMenu
    {
        // Placeholder sizes in world units, chosen to read clearly against a whip up to 12 units long.
        private static readonly Vector2 PlayerSize = new Vector2(1.5f, 3f);
        private static readonly Vector2 DummySize = new Vector2(2f, 4f);
        private const float GroundY = -3.5f; // bottom edge of both placeholders

        [MenuItem("Kinked Fury/Create or Update Whip Test Setup")]
        private static void CreateOrUpdate()
        {
            Sprite square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (square == null) Debug.LogWarning("Built-in square sprite not found; placeholders will be invisible.");

            WhipDrawingTester tester = Object.FindFirstObjectByType<WhipDrawingTester>();
            if (tester == null)
            {
                var testerObject = new GameObject("Whip Drawing Tester");
                Undo.RegisterCreatedObjectUndo(testerObject, "Whip Test Setup");
                tester = testerObject.AddComponent<WhipDrawingTester>();
            }

            WhipAttack attack = Object.FindFirstObjectByType<WhipAttack>();
            if (attack == null)
            {
                var player = new GameObject("Placeholder Player");
                Undo.RegisterCreatedObjectUndo(player, "Whip Test Setup");
                player.transform.position = new Vector3(-5f, 0f, 0f);
                var playerRenderer = player.AddComponent<SpriteRenderer>();
                playerRenderer.sprite = square;
                playerRenderer.color = new Color(0.45f, 0.3f, 0.6f);
                attack = player.AddComponent<WhipAttack>();

                var hand = new GameObject("Hand");
                hand.transform.SetParent(player.transform, false);
                WireAttack(attack, tester, hand.transform);
            }

            GameObject playerObject = attack.gameObject;
            Undo.RecordObject(playerObject.transform, "Whip Test Setup");
            playerObject.transform.localScale = new Vector3(PlayerSize.x, PlayerSize.y, 1f);
            playerObject.transform.position = new Vector3(
                playerObject.transform.position.x, GroundY + PlayerSize.y * 0.5f, 0f);
            if (playerObject.GetComponent<PlayerMover>() == null) Undo.AddComponent<PlayerMover>(playerObject);

            // The hand sits just outside the player's right edge, a bit above centre. These are local
            // units, so they scale with the player; WhipAttack mirrors the offset when facing left.
            Transform handTransform = playerObject.transform.Find("Hand");
            if (handTransform != null)
            {
                Undo.RecordObject(handTransform, "Whip Test Setup");
                handTransform.localPosition = new Vector3(0.55f, 0.1f, 0f);
            }

            TestDummy dummy = Object.FindFirstObjectByType<TestDummy>();
            GameObject dummyObject;
            if (dummy == null)
            {
                dummyObject = new GameObject("Test Dummy");
                Undo.RegisterCreatedObjectUndo(dummyObject, "Whip Test Setup");
                dummyObject.transform.position = new Vector3(4f, 0f, 0f);
                var dummyRenderer = dummyObject.AddComponent<SpriteRenderer>();
                dummyRenderer.sprite = square;
                dummyRenderer.color = new Color(0.55f, 0.15f, 0.2f);
                dummyObject.AddComponent<BoxCollider2D>();
                dummyObject.AddComponent<TestDummy>();
            }
            else
            {
                dummyObject = dummy.gameObject;
            }
            Undo.RecordObject(dummyObject.transform, "Whip Test Setup");
            dummyObject.transform.localScale = new Vector3(DummySize.x, DummySize.y, 1f);
            dummyObject.transform.position = new Vector3(
                dummyObject.transform.position.x, GroundY + DummySize.y * 0.5f, 0f);

            EditorSceneManager.MarkSceneDirty(playerObject.scene);
            Selection.activeGameObject = playerObject;
        }

        private static void WireAttack(WhipAttack attack, WhipDrawingTester tester, Transform hand)
        {
            var serialized = new SerializedObject(attack);
            serialized.FindProperty("shapeSource").objectReferenceValue = tester;
            serialized.FindProperty("hand").objectReferenceValue = hand;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
