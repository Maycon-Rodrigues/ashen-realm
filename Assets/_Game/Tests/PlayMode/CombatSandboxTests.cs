using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AshenRealm.Tests
{
    public sealed class CombatSandboxTests
    {
        [UnityTest]
        public IEnumerator PlayerMovesAndDefeatsChasingEnemy()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return SceneManager.LoadSceneAsync("CombatSandbox");
                yield return null;

                var movement = Object.FindAnyObjectByType<PlayerController>();
                var enemy = Object.FindAnyObjectByType<EnemyAI>();
                var camera = Object.FindAnyObjectByType<IsometricCamera>();
                Assert.That(movement, Is.Not.Null);
                Assert.That(enemy, Is.Not.Null);
                Assert.That(camera, Is.Not.Null);
                Assert.That(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);

                var playerHealth = movement.GetComponent<Health>();
                var enemyHealth = enemy.GetComponent<Health>();
                Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(45));
                var originalPlayer = movement.transform.position;
                var originalCamera = camera.transform.position;
                var originalEnemy = enemy.transform.position;
                yield return new WaitForSeconds(0.2f);
                Assert.That(Vector3.Distance(originalEnemy, enemy.transform.position), Is.LessThan(0.05f));

                Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                yield return new WaitForSeconds(0.85f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Vector3 displacement = movement.transform.position - originalPlayer;
                Assert.That(displacement.magnitude, Is.GreaterThan(2f));
                Assert.That(Vector3.Dot(displacement.normalized, (forward + right).normalized), Is.GreaterThan(0.95f));
                Assert.That(Vector3.Distance(originalCamera, camera.transform.position), Is.GreaterThan(1f));

                float deadline = Time.time + 4f;
                while (playerHealth.CurrentHealth == playerHealth.MaxHealth && Time.time < deadline)
                    yield return null;
                Assert.That(Vector3.Distance(originalEnemy, enemy.transform.position), Is.GreaterThan(1f));
                Assert.That(playerHealth.CurrentHealth, Is.LessThan(playerHealth.MaxHealth));

                int deaths = 0;
                enemyHealth.Died += () => deaths++;
                for (int strike = 1; strike <= 3; strike++)
                {
                    Vector3 towards = enemy.transform.position - movement.transform.position;
                    movement.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(towards, Vector3.up));
                    InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    yield return null;
                    yield return null;
                    Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(Mathf.Max(0, 45 - strike * 20)));
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    yield return new WaitForSeconds(0.55f);
                }

                Assert.That(deaths, Is.EqualTo(1));
                Assert.That(enemy.gameObject.activeSelf, Is.False);
                Assert.That(enemyHealth.TakeDamage(20), Is.False);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
            }
        }
    }
}
