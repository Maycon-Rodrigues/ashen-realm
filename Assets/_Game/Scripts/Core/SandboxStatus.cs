using UnityEngine;

namespace AshenRealm
{
    public sealed class SandboxStatus : MonoBehaviour
    {
        [SerializeField] private Health player;
        [SerializeField] private Health enemy;

        private void OnGUI()
        {
            if (!player || !enemy)
                return;

            GUILayout.BeginArea(new Rect(16, 16, 300, 120), GUI.skin.box);
            GUILayout.Label($"Cavaleiro  {player.CurrentHealth}/{player.MaxHealth} HP");
            GUILayout.Label($"Corrompido  {enemy.CurrentHealth}/{enemy.MaxHealth} HP");
            GUILayout.Label("WASD / Analógico esquerdo · Mover");
            GUILayout.Label("Clique esquerdo / Botão oeste · Atacar");
            if (!player.IsAlive || !enemy.IsAlive)
                GUILayout.Label(player.IsAlive ? "Inimigo derrotado." : "Cavaleiro derrotado.");
            GUILayout.EndArea();
        }
    }
}
