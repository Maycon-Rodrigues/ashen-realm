using System;
using UnityEngine;

namespace AshenRealm
{
    public sealed class AshenRuinsRoom : MonoBehaviour
    {
        public const int DefaultSeed = 191;
        public const int Columns = 11;
        public const int Rows = 7;

        [SerializeField] private int seed = DefaultSeed;
        [SerializeField] private GameObject flagstone;
        [SerializeField] private GameObject scorchedFlagstone;
        [SerializeField] private GameObject wall;
        [SerializeField] private GameObject brokenWall;
        [SerializeField] private GameObject pillar;
        [SerializeField] private GameObject sealedArch;
        [SerializeField] private GameObject banner;
        [SerializeField] private GameObject brazier;
        [SerializeField] private GameObject crimsonBriar;
        [SerializeField] private GameObject ashTree;
        [SerializeField] private GameObject cliff;
        [SerializeField] private GameObject ashSeal;
        [SerializeField] private GameObject fallenMasonry;
        [SerializeField] private GameObject memorial;
        [SerializeField] private Transform assembly;

        public int Seed => seed;

        private void Awake()
        {
            if (!assembly)
                Rebuild(seed);
        }

        public void Rebuild(int roomSeed) => Rebuild(roomSeed, (module, parent) => Instantiate(module, parent));

        public void Rebuild(int roomSeed, Func<GameObject, Transform, GameObject> instantiate)
        {
            var random = new System.Random(roomSeed);
            var previous = assembly;
            var next = new GameObject("Assembled Modules").transform;
            next.SetParent(transform, false);

            try
            {
                var paving = Group("Paving", next);
                var perimeter = Group("Perimeter", next);
                var scenery = Group("Scenery", next);
                var depth = Group("Cliff Face", next);

                const float edgeX = Columns - 0.25f;
                const float edgeZ = Rows - 0.25f;
                for (int row = 0; row < Rows; row++)
                {
                    for (int column = 0; column < Columns; column++)
                    {
                        float x = 2f * column - Columns + 1f;
                        float z = 2f * row - Rows + 1f;
                        bool scorched = random.NextDouble() < (Mathf.Abs(z) > 3f ? 0.43 : 0.12);
                        Place(scorched ? scorchedFlagstone : flagstone, paving, new Vector3(x, 0, z), 0f, instantiate);
                    }
                }

                float gateX = 1f + 4f * random.Next(-1, 2);
                Place(sealedArch, perimeter, new Vector3(gateX, 0, edgeZ), 180f, instantiate)
                    .localScale = new Vector3(1.03f, 1.15f, 1f);
                Place(ashSeal, scenery, new Vector3(0.6f * random.Next(-2, 3), 0, 1.15f), 18f, instantiate)
                    .localScale = Vector3.one * 1.32f;

                for (float x = -Columns + 1f; x < Columns; x += 2f)
                {
                    if (Mathf.Abs(x - gateX) > 1.1f)
                    {
                        var farWall = Place(PickWall(random, 0.23), perimeter, new Vector3(x, 0, edgeZ), 180f, instantiate);
                        farWall.localScale = new Vector3(1, random.Next(8, 16) * 0.1f, 1);
                    }
                    Place(PickWall(random, 0.91), perimeter, new Vector3(x, 0, -edgeZ), 0f, instantiate);
                }

                for (float z = -Rows + 1f; z < Rows; z += 2f)
                {
                    Place(PickWall(random, 0.84), perimeter, new Vector3(-edgeX, 0, z), 90f, instantiate);
                    Place(PickWall(random, 0.24), perimeter, new Vector3(edgeX, 0, z), -90f, instantiate);
                }

                float memorialX = gateX > 0 ? -5.9f : 6.4f;
                Place(memorial, perimeter, new Vector3(memorialX, 0.09f, edgeZ - 1.26f), 0, instantiate);
                Place(fallenMasonry, scenery, new Vector3(memorialX - 1.15f, 0.04f, edgeZ - 0.82f), 21, instantiate);
                Place(fallenMasonry, scenery, new Vector3(8.4f, 0.04f, -edgeZ + 0.68f), -35, instantiate);

                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 8.72f;
                    Place(pillar, perimeter, new Vector3(x, 0, edgeZ), 90f * random.Next(4), instantiate)
                        .localScale = new Vector3(1.32f, side > 0 ? 1.30f : 1.59f, 1.20f);
                    Place(banner, perimeter, new Vector3(x, 0.74f, edgeZ - 0.54f), 180, instantiate);
                    Place(brazier, scenery, new Vector3(gateX + side * 2.64f, 0, edgeZ - 1.20f), 0, instantiate);
                    Place(brazier, scenery, new Vector3(x, 0, -edgeZ + 0.76f), 0, instantiate);
                    Place(crimsonBriar, scenery, new Vector3(x + random.Next(-1, 2), 0.18f, -edgeZ + 0.25f),
                        0f, instantiate).localScale = Vector3.one * 1.4f;
                    Place(ashTree, depth, new Vector3(side * 10.1f, -0.6f, edgeZ + 1.1f), 0f,
                        instantiate).localScale = Vector3.one * 1.42f;
                    Place(cliff, depth, new Vector3(side * 10.0f, -0.64f, edgeZ + 0.9f), 0, instantiate);
                }

                for (float x = -Columns + 1f; x < Columns; x += 4f)
                    Place(cliff, depth, new Vector3(x, -0.18f, -edgeZ), 180f * random.Next(2), instantiate);
                for (float z = -Rows + 1f; z < Rows; z += 4f)
                    Place(cliff, depth, new Vector3(-edgeX, -0.19f, z), 90f + 180f * random.Next(2), instantiate);
                for (float x = -Columns + 1f; x < Columns; x += 2f)
                {
                    if (Mathf.Abs(x - gateX) > 2.1f)
                        Place(crimsonBriar, scenery, new Vector3(x + random.Next(-2, 3) * 0.12f, 0.16f, edgeZ - 0.43f), 0f, instantiate);
                }
            }
            catch
            {
                Remove(next);
                throw;
            }

            if (previous)
                Remove(previous);
            assembly = next;
            seed = roomSeed;
        }

        private GameObject PickWall(System.Random random, double chanceOfRuin) =>
            random.NextDouble() < chanceOfRuin ? brokenWall : wall;

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static Transform Place(GameObject module, Transform parent, Vector3 position, float yaw,
            Func<GameObject, Transform, GameObject> instantiate)
        {
            if (!module)
                throw new InvalidOperationException("Ashen Ruins has an unassigned module prefab.");
            var instance = instantiate(module, parent);
            instance.name = module.name;
            instance.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return instance.transform;
        }

        private static void Remove(Transform group)
        {
            if (Application.isPlaying)
            {
                group.gameObject.SetActive(false);
                Destroy(group.gameObject);
            }
            else
            {
                DestroyImmediate(group.gameObject);
            }
        }
    }
}
