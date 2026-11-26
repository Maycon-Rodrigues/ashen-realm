using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AshenRealm
{
    public sealed class SandboxStatus : MonoBehaviour
    {
        [SerializeField] private Health player;
        [SerializeField] private Health enemy;

        private EnemyAI enemyCombat;
        private SandboxProgression progression;
        private InputAction restart;
        private bool restarting;
        private bool EncounterEnded => player && enemy && (!player.IsAlive || !enemy.IsAlive);

        private void Awake()
        {
            if (enemy)
                enemyCombat = enemy.GetComponent<EnemyAI>();
            progression = GetComponent<SandboxProgression>();
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
            if (!player || !enemy)
                return;

            GUILayout.BeginArea(new Rect(16, 16, 350, 180), GUI.skin.box);
            GUILayout.Label($"Cavaleiro  {player.CurrentHealth}/{player.MaxHealth} HP");
            GUILayout.Label($"Corrompido  {enemy.CurrentHealth}/{enemy.MaxHealth} HP");
            GUILayout.Label("WASD / Analógico esquerdo · Mover");
            GUILayout.Label("Clique esquerdo / Botão oeste · Atacar");
            if (progression)
            {
                GUILayout.Label($"Dano por golpe  {progression.Damage}");
                switch (progression.State)
                {
                    case SwordState.Locked:
                        GUILayout.Label("Derrote o Corrompido para obter a Lâmina de Cinza");
                        break;
                    case SwordState.Dropped:
                        GUILayout.Label(progression.CanCollect
                            ? "E / Botão sul · Pegar Lâmina de Cinza"
                            : "Aproxime-se da lâmina caída");
                        break;
                    case SwordState.Collected:
                        GUILayout.Label("Q / Botão norte · Equipar Lâmina de Cinza");
                        break;
                    case SwordState.Equipped:
                        GUILayout.Label("Lâmina de Cinza equipada");
                        break;
                }
            }
            if (enemyCombat && enemyCombat.IsWindingUp)
                GUILayout.Label("Golpe iminente · Saia da área marcada");
            GUILayout.EndArea();

            if (!EncounterEnded)
                return;

            var panel = new Rect((Screen.width - 280) / 2f, Screen.height - 114, 280, 98);
            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Label(player.IsAlive ? "Vitória" : "Derrota");
            GUILayout.Label(player.IsAlive ? "R / Start · Próximo encontro" : "R / Start · Tentar de novo");
            if (GUILayout.Button("Reiniciar encontro"))
                Restart();
            GUILayout.EndArea();
        }
    }
}
