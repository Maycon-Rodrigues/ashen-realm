using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AshenRealm.Editor
{
    internal static class AshenRuinsBuilder
    {
        private const string Sandbox = "Assets/_Game/Generated/CombatSandbox.unity";
        internal const string ScenePath = "Assets/_Game/Generated/AshenRuins.unity";
        private const string Art = "Assets/_Game/Art/AshenRuins";
        private const string Prefabs = "Assets/_Game/Prefabs/AshenRuins";
        private const string NavigationPath = "Assets/_Game/Generated/AshenRuinsNavMesh.asset";

        private static readonly (string Field, string File)[] Pieces =
        {
            ("flagstone", "Flagstone"), ("scorchedFlagstone", "ScorchedFlagstone"),
            ("wall", "Wall"), ("brokenWall", "BrokenWall"), ("pillar", "Pillar"),
            ("sealedArch", "SealedArch"), ("banner", "Banner"), ("brazier", "Brazier"),
            ("crimsonBriar", "CrimsonBriar"), ("ashTree", "AshTree"), ("cliff", "Cliff"), ("ashSeal", "AshSeal")
        };

        [MenuItem("Ashen Realm/Generate Ashen Ruins")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            GenerateRoom(ReadSeed());
        }

        public static void Play()
        {
            if (!File.Exists(ScenePath))
                GenerateRoom(ReadSeed());
            else
                EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.delayCall += EditorApplication.EnterPlaymode;
        }

        private static int ReadSeed()
        {
            string value = Environment.GetEnvironmentVariable("ASHEN_RUINS_SEED");
            return string.IsNullOrEmpty(value) ? AshenRuinsRoom.DefaultSeed : int.Parse(value, CultureInfo.InvariantCulture);
        }

        internal static void GenerateRoom(int seed)
        {
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            var modules = Pieces.Select(piece => (piece.Field, Prefab: CreatePrefab(piece.File))).ToArray();
            var scene = EditorSceneManager.OpenScene(Sandbox);
            var arena = scene.GetRootGameObjects().Single(root => root.name == "Arena");
            foreach (Transform child in arena.transform.Cast<Transform>().ToArray())
            {
                if (child.name is "Floor" or "Long wall" or "Short wall")
                    child.GetComponent<Renderer>().enabled = false;
                else
                    Object.DestroyImmediate(child.gameObject);
            }

            var room = arena.AddComponent<AshenRuinsRoom>();
            foreach (var module in modules)
                CombatSandboxBuilder.SetReference(room, module.Field, module.Prefab);
            room.Rebuild(seed, (module, parent) => (GameObject)PrefabUtility.InstantiatePrefab(module, parent));

            var grout = CombatSandboxBuilder.Material("RuinsGrout", new Color(0.13f, 0.085f, 0.11f), "AshenRealm/Courtyard Lit");
            CombatSandboxBuilder.Primitive("Grout", PrimitiveType.Cube, arena.transform,
                new Vector3(0, -0.235f, 0), new Vector3(24, 0.30f, 20), grout, false);
            arena.transform.Find("Grout").GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var embers = CombatSandboxBuilder.Material("RuinsAbyss", Color.white, "AshenRealm/Ruins Embers");
            CombatSandboxBuilder.Primitive("Ash Below", PrimitiveType.Quad, arena.transform,
                new Vector3(0, -8.3f, 0), new Vector3(120, 120, 1), embers, false);
            var abyss = arena.transform.Find("Ash Below");
            abyss.localRotation = Quaternion.Euler(90, 0, 0);
            abyss.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            BakeNavigation(arena.GetComponent<NavMeshSurface>());
            SetLighting(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save " + ScenePath);

            if (!EditorBuildSettings.scenes.Any(entry => entry.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"Ashen Realm: Ashen Ruins ready at {ScenePath} with seed {seed}.");
        }

        private static GameObject CreatePrefab(string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/{name}.fbx");
            if (!model)
                throw new InvalidOperationException($"Missing Ashen Ruins module {name}. Generate the FBX kit in Blender.");

            var root = new GameObject(name);
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.GetComponent<MeshFilter>().sharedMesh.HasVertexAttribute(VertexAttribute.Color))
                        throw new InvalidOperationException($"The Ashen Ruins module {name} is missing its painted vertex colors.");
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material => Surface(material.name, name)).ToArray();
                }

                if (name == "Brazier")
                {
                    var light = new GameObject("Brazier Glow").AddComponent<Light>();
                    light.transform.SetParent(root.transform, false);
                    light.transform.localPosition = new Vector3(0, 1.64f, 0);
                    light.type = LightType.Point;
                    light.range = 5.8f;
                    light.intensity = 10f;
                    light.color = new Color(1f, 0.43f, 0.15f);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, $"{Prefabs}/{name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Material Surface(string name, string module)
        {
            if (name == "RuinsCoal")
            {
                var veil = CombatSandboxBuilder.Material("RuinsVeil", Color.white, "AshenRealm/Ruins Embers");
                veil.SetFloat("_Veil", 1f);
                EditorUtility.SetDirty(veil);
                return veil;
            }
            if (module == "AshSeal")
                return CombatSandboxBuilder.Material("RuinsFloorSeal", new Color(0.57f, 0.16f, 0.20f),
                    "Universal Render Pipeline/Unlit");

            Color tint = name switch
            {
                "RuinsPaint" => Color.white,
                "RuinsCore" => new Color(1f, 0.84f, 0.27f),
                "RuinsFire" => new Color(1f, 0.36f, 0.055f),
                "RuinsRune" => new Color(0.95f, 0.24f, 0.18f),
                _ => throw new ArgumentException($"Unknown Ashen Ruins surface {name}.")
            };
            bool lit = name == "RuinsPaint";
            var surface = CombatSandboxBuilder.Material(name, tint,
                lit ? "AshenRealm/Courtyard Lit" : "Universal Render Pipeline/Unlit");
            if (lit)
            {
                surface.SetFloat("_VertexPaint", 1f);
                surface.SetFloat("_Grain", 0.49f);
                surface.SetFloat("_Rim", 0.025f);
                EditorUtility.SetDirty(surface);
            }
            return surface;
        }

        private static void BakeNavigation(NavMeshSurface navigation)
        {
            navigation.RemoveData();
            navigation.navMeshData = null;
            navigation.BuildNavMesh();
            var baked = navigation.navMeshData;
            if (!baked || !NavMesh.SamplePosition(Vector3.zero, out _, 0.5f, NavMesh.AllAreas))
                throw new InvalidOperationException("The Ashen Ruins NavMesh does not cover the arena center.");

            var stored = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath);
            if (!stored)
                AssetDatabase.CreateAsset(baked, NavigationPath);
            else
            {
                navigation.RemoveData();
                EditorUtility.CopySerialized(baked, stored);
                navigation.navMeshData = stored;
                navigation.AddData();
                Object.DestroyImmediate(baked);
            }
        }

        private static void SetLighting(UnityEngine.SceneManagement.Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var sun = roots.Single(root => root.name == "Dusk Light").GetComponent<Light>();
            sun.color = new Color(1f, 0.66f, 0.54f);
            sun.intensity = 2.15f;
            sun.transform.rotation = Quaternion.Euler(43, -45, 0);
            roots.Single(root => root.name == "Cold Sky Fill").SetActive(false);
            RenderSettings.ambientLight = new Color(0.30f, 0.21f, 0.28f);
            var camera = UnityEngine.Camera.main;
            camera.backgroundColor = new Color(0.105f, 0.035f, 0.070f);
            camera.orthographicSize = 7.4f;
        }
    }
}
