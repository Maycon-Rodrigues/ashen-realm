using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AshenRealm.Tests
{
    public sealed class AshenRuinsRoomTests
    {
        [UnityTest]
        public IEnumerator SeedsRebuildTheSameRoomAndKeepTheCombatRouteOpen()
        {
            yield return SceneManager.LoadSceneAsync("AshenRuins");
            yield return null;

            var room = Object.FindAnyObjectByType<AshenRuinsRoom>();
            Assert.That(room, Is.Not.Null);
            var original = Layout(room);
            int originalSeed = room.Seed;

            room.Rebuild(-773_128);
            yield return null;
            Assert.That(Layout(room), Is.Not.EqualTo(original));
            Assert.That(room.Seed, Is.EqualTo(-773_128));
            AssertNavigation();

            room.Rebuild(originalSeed);
            yield return null;
            Assert.That(Layout(room), Is.EqualTo(original));
            AssertNavigation();
        }

        private static (string Name, Vector3 Position, Quaternion Orientation, Vector3 Scale)[] Layout(AshenRuinsRoom room)
        {
            var assembly = room.transform.Find("Assembled Modules");
            Assert.That(assembly, Is.Not.Null);
            var paving = assembly.Find("Paving");
            Assert.That(paving.childCount, Is.EqualTo(AshenRuinsRoom.Columns * AshenRuinsRoom.Rows));
            Assert.That(paving.Cast<Transform>().Select(tile => tile.localPosition).Distinct().Count(),
                Is.EqualTo(paving.childCount));
            Assert.That(assembly.Find("Perimeter").Cast<Transform>().Count(module => module.name == "SealedArch"),
                Is.EqualTo(1));

            return assembly.Cast<Transform>().SelectMany(group => group.Cast<Transform>())
                .Select(module => (module.name, module.localPosition, module.localRotation, module.localScale)).ToArray();
        }

        private static void AssertNavigation()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            var enemy = Object.FindAnyObjectByType<EnemyAI>();
            var path = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(enemy.transform.position, player.transform.position, NavMesh.AllAreas, path),
                Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(NavMesh.SamplePosition(new Vector3(0, 0, AshenRuinsRoom.Rows + 1f),
                out _, 0.3f, NavMesh.AllAreas), Is.False);
        }
    }
}
