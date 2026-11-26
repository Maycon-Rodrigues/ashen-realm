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
        public IEnumerator LootChangesDamageInTheNextEncounter()
        {
            var settings = InputSystem.settings;
            var backgroundBehavior = settings.backgroundBehavior;
            bool runInBackground = Application.runInBackground;
#if UNITY_EDITOR
            var editorInputBehavior = settings.editorInputBehaviorInPlayMode;
            settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;

            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
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

                Assert.That(movement.GetComponent<FallenKnightMotion>(), Is.Not.Null);
                var knight = movement.transform.Find("Fallen Knight Painted Rig");
                Assert.That(knight, Is.Not.Null);
                var skin = knight.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.That(skin, Is.Not.Null);
                Assert.That(skin.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color), Is.True);
                var thigh = System.Array.Find(skin.bones, bone => bone.name == "Thigh.L");
                Assert.That(thigh, Is.Not.Null);
                Quaternion thighRest = thigh.localRotation;
                var grip = System.Array.Find(knight.GetComponentsInChildren<Transform>(), bone => bone.name == "SwordGrip");
                var blade = movement.transform.Find("Blade Pivot");
                Assert.That(grip, Is.Not.Null);
                Assert.That(blade, Is.Not.Null);

                var playerHealth = movement.GetComponent<Health>();
                var combat = movement.GetComponent<PlayerCombat>();
                var enemyHealth = enemy.GetComponent<Health>();
                var strikingArm = enemy.transform.Find("CorruptedGrunt/StrikeArm");
                Assert.That(strikingArm, Is.Not.Null);
                var restArm = strikingArm.localRotation;
                Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(45));
                var originalPlayer = movement.transform.position;
                var originalCamera = camera.transform.position;
                var originalEnemy = enemy.transform.position;
                yield return new WaitForSeconds(0.2f);
                Assert.That(Vector3.Distance(originalEnemy, enemy.transform.position), Is.LessThan(0.05f));

                Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                float walkUntil = Time.time + 0.85f;
                bool sawStep = false;
                while (Time.time < walkUntil)
                {
                    sawStep |= Quaternion.Angle(thighRest, thigh.localRotation) > 8f;
                    yield return null;
                }
                Assert.That(sawStep, Is.True, "The player skin must animate while moving.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Vector3 displacement = movement.transform.position - originalPlayer;
                Assert.That(displacement.magnitude, Is.GreaterThan(2f));
                Assert.That(Vector3.Dot(displacement.normalized, (forward + right).normalized), Is.GreaterThan(0.95f));
                Assert.That(Vector3.Distance(originalCamera, camera.transform.position), Is.GreaterThan(1f));

                float deadline = Time.time + 4f;
                while (!enemy.IsWindingUp && Time.time < deadline)
                    yield return null;
                Assert.That(enemy.IsWindingUp, Is.True);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(playerHealth.MaxHealth));
                var warning = enemy.GetComponentInChildren<LineRenderer>();
                yield return null;
                Assert.That(warning.enabled, Is.True);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.A));
                float retreatUntil = Time.time + 0.35f;
                bool sawArmWindup = false;
                while (Time.time < retreatUntil)
                {
                    sawArmWindup |= Quaternion.Angle(restArm, strikingArm.localRotation) > 10f;
                    yield return null;
                }
                Assert.That(sawArmWindup, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                deadline = Time.time + 2f;
                while (enemy.IsWindingUp && Time.time < deadline)
                    yield return null;
                Assert.That(enemy.IsWindingUp, Is.False);
                yield return null;
                Assert.That(warning.enabled, Is.False);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(playerHealth.MaxHealth), "Retreat must avoid the announced strike.");

                deadline = Time.time + 5f;
                while (playerHealth.CurrentHealth == playerHealth.MaxHealth && Time.time < deadline)
                    yield return null;
                Assert.That(Vector3.Distance(originalEnemy, enemy.transform.position), Is.GreaterThan(1f));
                Assert.That(playerHealth.CurrentHealth, Is.LessThan(playerHealth.MaxHealth));

                var extraHurtbox = new GameObject("Additional Enemy Collider") { layer = enemy.gameObject.layer };
                extraHurtbox.transform.SetParent(enemy.transform, false);
                extraHurtbox.transform.localPosition = Vector3.up * 0.9f;
                extraHurtbox.AddComponent<SphereCollider>().radius = 0.35f;
                Physics.SyncTransforms();

                int deaths = 0;
                enemyHealth.Died += () => deaths++;
                for (int strike = 1; strike <= 3; strike++)
                {
                    Quaternion bladeRest = blade.localRotation;
                    Vector3 towards = enemy.transform.position - movement.transform.position;
                    movement.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(towards, Vector3.up));
                    if (strike == 2)
                        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                    else
                        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    yield return null;
                    yield return null;
                    Assert.That(combat.WindupProgress, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(45 - (strike - 1) * 20));
                    Assert.That(combat.TryAttack(), Is.False);
                    yield return new WaitForSeconds(0.22f);
                    Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(Mathf.Max(0, 45 - strike * 20)));
                    Assert.That(Quaternion.Angle(bladeRest, blade.localRotation), Is.GreaterThan(15f));
                    Assert.That(Vector3.Distance(blade.position, grip.position), Is.LessThan(0.025f));
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    InputSystem.QueueStateEvent(gamepad, new GamepadState());
                    yield return new WaitForSeconds(0.55f);
                }

                Assert.That(deaths, Is.EqualTo(1));
                Assert.That(enemy.gameObject.activeSelf, Is.False);
                Assert.That(enemyHealth.TakeDamage(20), Is.False);

                var progression = Object.FindAnyObjectByType<SandboxProgression>();
                Assert.That(progression.State, Is.EqualTo(SwordState.Dropped));
                Assert.That(progression.Damage, Is.EqualTo(20));
                Assert.That(progression.CanCollect, Is.True);

                var beforeStick = movement.transform.position;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.left });
                yield return new WaitForSeconds(0.3f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                Assert.That(Vector3.Dot(movement.transform.position - beforeStick, -right), Is.GreaterThan(0.5f));

                MovePlayer(movement, originalPlayer);
                Assert.That(progression.CanCollect, Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, Key.Q));
                yield return null;
                yield return null;
                Assert.That(progression.State, Is.EqualTo(SwordState.Dropped));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;

                MovePlayer(movement, beforeStick);
                Assert.That(progression.CanCollect, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                yield return null;
                Assert.That(progression.State, Is.EqualTo(SwordState.Collected));
                Assert.That(progression.Damage, Is.EqualTo(20));
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
                yield return null;
                yield return null;
                Assert.That(progression.State, Is.EqualTo(SwordState.Equipped));
                Assert.That(progression.Damage, Is.EqualTo(25));
                InputSystem.QueueStateEvent(gamepad, new GamepadState());

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                yield return WaitForRestart(playerHealth);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());

                movement = Object.FindAnyObjectByType<PlayerController>();
                combat = movement.GetComponent<PlayerCombat>();
                enemy = Object.FindAnyObjectByType<EnemyAI>();
                enemyHealth = enemy.GetComponent<Health>();
                progression = Object.FindAnyObjectByType<SandboxProgression>();
                Assert.That(progression.State, Is.EqualTo(SwordState.Equipped));
                Assert.That(progression.Damage, Is.EqualTo(25));
                Assert.That(movement.transform.Find("Blade Pivot/AshenSword/Steel.Blade").GetComponent<Renderer>()
                    .sharedMaterial.name, Is.EqualTo("AshenBlade"));

                MovePlayer(movement, enemy.transform.position - Vector3.right * 1.5f);
                movement.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Assert.That(combat.TryAttack(), Is.True);
                yield return new WaitForSeconds(0.48f);
                Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(20));
                Assert.That(combat.TryAttack(), Is.True);
                yield return new WaitForSeconds(0.22f);
                Assert.That(enemyHealth.IsAlive, Is.False);
                Assert.That(progression.State, Is.EqualTo(SwordState.Equipped), "The reward should be granted once.");

                var restartedPlayer = movement.GetComponent<Health>();
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return WaitForRestart(restartedPlayer);
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;

                restartedPlayer = Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>();
                restartedPlayer.TakeDamage(restartedPlayer.MaxHealth);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return WaitForRestart(restartedPlayer);
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                Assert.That(Object.FindAnyObjectByType<SandboxProgression>().Damage, Is.EqualTo(25));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(gamepad);
                Application.runInBackground = runInBackground;
                settings.backgroundBehavior = backgroundBehavior;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = editorInputBehavior;
#endif
            }
        }

        private static void MovePlayer(PlayerController player, Vector3 position)
        {
            var motor = player.GetComponent<CharacterController>();
            motor.enabled = false;
            player.transform.position = position;
            motor.enabled = true;
            Physics.SyncTransforms();
        }

        private static IEnumerator WaitForRestart(Health previousPlayer)
        {
            float deadline = Time.time + 5f;
            while (previousPlayer && Time.time < deadline)
                yield return null;

            Assert.That((bool)previousPlayer, Is.False, "The current encounter must unload.");
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>();
            var enemy = Object.FindAnyObjectByType<EnemyAI>().GetComponent<Health>();
            Assert.That(player.CurrentHealth, Is.EqualTo(player.MaxHealth));
            Assert.That(enemy.CurrentHealth, Is.EqualTo(enemy.MaxHealth));
        }
    }
}
