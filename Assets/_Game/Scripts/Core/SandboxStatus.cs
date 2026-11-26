using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AshenRealm
{
    public sealed class SandboxStatus : MonoBehaviour
    {
        [SerializeField] private Health player;
        [SerializeField] private Health enemy;

        private static readonly Color Panel = new(0.046f, 0.062f, 0.067f, 0.94f);
        private static readonly Color Border = new(0.25f, 0.27f, 0.24f, 0.92f);
        private static readonly Color Paper = new(0.94f, 0.90f, 0.81f);
        private static readonly Color Muted = new(0.70f, 0.73f, 0.69f);
        private static readonly Color Vitality = new(0.70f, 0.23f, 0.18f);
        private static readonly Color AshenBlade = new(0.42f, 0.82f, 0.71f);
        private static readonly Color Warning = new(0.93f, 0.56f, 0.30f);

        private EnemyAI enemyCombat;
        private SandboxProgression progression;
        private UnityEngine.Camera worldCamera;
        private InputAction restart;
        private GUIStyle small;
        private GUIStyle centered;
        private GUIStyle title;
        private float startedAt;
        private bool restarting;

        private bool EncounterEnded => player && enemy && (!player.IsAlive || !enemy.IsAlive);

        private void Awake()
        {
            if (enemy)
                enemyCombat = enemy.GetComponent<EnemyAI>();
            progression = GetComponent<SandboxProgression>();
            worldCamera = UnityEngine.Camera.main;
            startedAt = Time.unscaledTime;
            restart = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
            restart.AddBinding("<Gamepad>/start");
        }

        private void OnEnable() => restart.Enable();
        private void OnDisable() => restart.Disable();
        private void OnDestroy() => restart.Dispose();

        private void Update()
        {
            if (restart.WasPressedThisFrame())
                Restart();
        }

        private void Restart()
        {
            if (!EncounterEnded || restarting)
                return;

            restarting = true;
            var sword = progression ? progression.State : SwordState.Locked;
            var reload = SceneManager.LoadSceneAsync(gameObject.scene.buildIndex);
            if (sword is SwordState.Collected or SwordState.Equipped)
                reload.completed += _ => FindAnyObjectByType<SandboxProgression>()?.Restore(sword);
        }

        private void OnGUI()
        {
            if (!player || !enemy || !worldCamera)
                return;

            if (small == null)
            {
                small = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    padding = new RectOffset(0, 0, 0, 0)
                };
                centered = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                title = new GUIStyle(centered) { fontSize = 20 };
            }

            float scale = Mathf.Clamp(Screen.height / 820f, 0.64f, 1.4f);
            Vector2 viewport = new Vector2(Screen.width, Screen.height) / scale;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = previous * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            try
            {
                var gamepad = RecentGamepad();
                DrawPlayer(viewport);
                DrawEnemy(viewport, scale);
                DrawLoot(viewport, scale, gamepad);
                if (EncounterEnded)
                    DrawOutcome(viewport, gamepad);
                else if (Time.unscaledTime - startedAt < 5f && viewport.x > 600f)
                {
                    string hint = gamepad != null
                        ? $"Analógico · Mover   /   {gamepad.buttonWest.displayName} · Atacar"
                        : "WASD · Mover   /   Clique · Atacar";
                    Text(new Rect(viewport.x - 240, viewport.y - 44, 216, 22), hint, centered, Muted);
                }
            }
            finally
            {
                GUI.matrix = previous;
            }
        }

        private void DrawPlayer(Vector2 viewport)
        {
            var panel = new Rect(24, viewport.y - 73, 218, 49);
            Frame(panel);
            Text(new Rect(panel.x + 10, panel.y + 4, 110, 19), $"{player.CurrentHealth} / {player.MaxHealth}", small, Paper);
            if (progression)
                Text(new Rect(panel.xMax - 77, panel.y + 4, 66, 19), $"{progression.Damage} dano", small,
                    progression.State == SwordState.Equipped ? AshenBlade : Muted);
            Bar(new Rect(panel.x + 10, panel.y + 27, panel.width - 20, 10), player, Vitality);
        }

        private void DrawEnemy(Vector2 viewport, float scale)
        {
            if (!enemy.IsAlive || !player.IsAlive || !enemyCombat)
                return;

            Vector3 separation = Vector3.ProjectOnPlane(enemy.transform.position - player.transform.position, Vector3.up);
            float radius = enemyCombat.DetectionRadius;
            if (separation.sqrMagnitude > radius * radius ||
                !Project(enemy.transform.position + Vector3.up * 2.05f, viewport, scale, out Vector2 point))
                return;

            var rect = new Rect(point.x - 36, point.y - 12, 72, 7);
            Fill(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), Panel);
            Bar(rect, enemy, enemyCombat.IsWindingUp ? Warning : Vitality);
        }

        private void DrawLoot(Vector2 viewport, float scale, Gamepad gamepad)
        {
            if (!player.IsAlive || !progression)
                return;

            switch (progression.State)
            {
                case SwordState.Dropped:
                    string collectKey = gamepad?.buttonSouth.displayName ?? "E";
                    Prompt(progression.PickupPoint + Vector3.up * 0.65f, viewport, scale,
                        progression.CanCollect ? $"{collectKey}   Pegar Lâmina de Cinza" : "Lâmina de Cinza");
                    break;
                case SwordState.Collected:
                    string equipKey = gamepad?.buttonNorth.displayName ?? "Q";
                    Prompt(player.transform.position + Vector3.up * 2.02f, viewport, scale,
                        $"{equipKey}   Equipar Lâmina de Cinza");
                    break;
            }
        }

        private void DrawOutcome(Vector2 viewport, Gamepad gamepad)
        {
            float bottom = viewport.x < 720f ? 86f : 24f;
            var panel = new Rect((viewport.x - 220) * 0.5f, viewport.y - bottom - 70f, 220, 70);
            Frame(panel);
            Text(new Rect(panel.x + 10, panel.y + 5, 200, 25), player.IsAlive ? "Vitória" : "Derrota", title, Paper);
            var button = new Rect(panel.x + 10, panel.y + 37, panel.width - 20, 23);
            Fill(button, button.Contains(Event.current.mousePosition) ? Border : new Color(0.13f, 0.17f, 0.17f));
            if (GUI.Button(button, GUIContent.none, GUIStyle.none))
                Restart();
            string action = player.IsAlive ? "Próximo encontro" : "Tentar de novo";
            string key = gamepad?.startButton.displayName ?? "R";
            Text(button, $"{action}   ·   {key}", centered, Paper);
        }

        private static Gamepad RecentGamepad()
        {
            var pad = Gamepad.current;
            if (pad == null || pad.lastUpdateTime <= (Keyboard.current?.lastUpdateTime ?? 0)
                || pad.lastUpdateTime <= (Mouse.current?.lastUpdateTime ?? 0))
                return null;

            return pad;
        }

        private void Prompt(Vector3 worldPoint, Vector2 viewport, float scale, string text)
        {
            if (!Project(worldPoint, viewport, scale, out Vector2 point))
                return;

            float width = centered.CalcSize(new GUIContent(text)).x + 24f;
            float x = Mathf.Clamp(point.x - width * 0.5f, 8f, Mathf.Max(8f, viewport.x - width - 8f));
            var label = new Rect(x, Mathf.Max(8f, point.y - 28f), width, 25);
            Frame(label);
            Text(label, text, centered, AshenBlade);
        }

        private bool Project(Vector3 worldPoint, Vector2 viewport, float scale, out Vector2 point)
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(worldPoint);
            point = new Vector2(screen.x / scale, viewport.y - screen.y / scale);
            return screen.z > 0f && point.x > 0f && point.x < viewport.x && point.y > 0f && point.y < viewport.y;
        }

        private static void Bar(Rect rect, Health health, Color color)
        {
            Fill(rect, new Color(0.17f, 0.15f, 0.14f, 0.96f));
            rect.width *= Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
            Fill(rect, color);
        }

        private static void Frame(Rect rect)
        {
            Fill(rect, Border);
            Fill(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), Panel);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Text(Rect rect, string text, GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }
    }
}
