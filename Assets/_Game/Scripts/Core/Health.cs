using System;
using UnityEngine;

namespace AshenRealm
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public event Action Died;

        private void Awake() => CurrentHealth = MaxHealth;

        public bool TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return false;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (!IsAlive)
                Died?.Invoke();

            return true;
        }
    }
}
