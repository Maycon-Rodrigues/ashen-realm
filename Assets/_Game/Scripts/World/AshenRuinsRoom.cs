using System;
using UnityEngine;

namespace AshenRealm
{
    public sealed class AshenRuinsRoom : MonoBehaviour
    {
        public const int DefaultSeed = 191;
        public const int Columns = 12;
        public const int Rows = 10;

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

                for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                {
                    float x = 2f * column - 11f;
                    float z = 2f * row - 9f;
                    double scorchChance = Mathf.Abs(z) < 2f ? 0.10 : 0.29;
                    var tile = random.NextDouble() < scorchChance ? scorchedFlagstone : flagstone;
                    Place(tile, paving, new Vector3(x, 0, z), 90f * random.Next(4), instantiate);
                }

                float gateX = 4f * random.Next(-1, 2);
                Place(sealedArch, perimeter, new Vector3(gateX, 0, 9.75f), 180f, instantiate);
                Place(ashSeal, scenery, new Vector3(2f * random.Next(-1, 2), 0, 2.3f), 0f, instantiate);

                for (float x = -11f; x <= 11f; x += 2f)
                {
                    if (Mathf.Abs(x - gateX) > 1.1f)
                        Place(PickWall(random, 0.25), perimeter, new Vector3(x, 0, 9.75f), 180f, instantiate);
                    Place(PickWall(random, 0.88), perimeter, new Vector3(x, 0, -9.75f), 0f, instantiate);
                }

                for (float z = -9f; z <= 9f; z += 2f)
                {
                    Place(PickWall(random, 0.76), perimeter, new Vector3(-11.75f, 0, z), 90f, instantiate);
                    Place(PickWall(random, 0.30), perimeter, new Vector3(11.75f, 0, z), -90f, instantiate);
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 9f;
                    Place(pillar, perimeter, new Vector3(x, 0, 9.65f), 90f * random.Next(4), instantiate);
                    Place(banner, perimeter, new Vector3(x, 0, 9.14f), 180f, instantiate);
                    Place(brazier, scenery, new Vector3(gateX + side * 2.68f, 0, 8.43f), 0f, instantiate);
                    Place(brazier, scenery, new Vector3(x, 0, -8.75f), 0f, instantiate);
                    Place(crimsonBriar, scenery, new Vector3(x + random.Next(-1, 2), 0.17f, -9.70f),
                        90f * random.Next(4), instantiate);
                    Place(ashTree, depth, new Vector3(side * 9.8f, -1.05f, 10.95f), 90f * random.Next(4), instantiate);
                    Place(cliff, depth, new Vector3(side * 9.8f, -0.96f, 11.2f), 0f, instantiate);
                    Place(cliff, depth, new Vector3(side * 14.4f, -0.3f, 11.8f), 40f, instantiate);
                }

                for (float x = -10f; x <= 10f; x += 4f)
                {
                    Place(cliff, depth, new Vector3(x, -0.24f, -9.73f), 180f * random.Next(2), instantiate);
                    Place(crimsonBriar, scenery, new Vector3(x, 0.15f, 9.84f), 90f * random.Next(4), instantiate);
                }
                for (float z = -8f; z <= 8f; z += 4f)
                    Place(cliff, depth, new Vector3(-11.72f, -0.24f, z), 90f + 180f * random.Next(2), instantiate);
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

        private static void Place(GameObject module, Transform parent, Vector3 position, float yaw,
            Func<GameObject, Transform, GameObject> instantiate)
        {
            if (!module)
                throw new InvalidOperationException("Ashen Ruins has an unassigned module prefab.");
            var instance = instantiate(module, parent);
            instance.name = module.name;
            instance.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
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
