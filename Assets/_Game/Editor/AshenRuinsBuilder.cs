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
        private const string Illustrations = Art + "/Illustrations";
        private const string Prefabs = "Assets/_Game/Prefabs/AshenRuins";
        private const string NavigationPath = "Assets/_Game/Generated/AshenRuinsNavMesh.asset";

        private static readonly (string Field, string File)[] Pieces =
        {
            ("flagstone", "Flagstone"), ("scorchedFlagstone", "ScorchedFlagstone"),
            ("wall", "Wall"), ("brokenWall", "BrokenWall"), ("pillar", "Pillar"),
            ("sealedArch", "SealedArch"), ("banner", "Banner"), ("brazier", "Brazier"),
            ("crimsonBriar", "CrimsonBriar"), ("ashTree", "AshTree"), ("cliff", "Cliff"), ("ashSeal", "AshSeal"),
            ("fallenMasonry", "FallenMasonry")
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
            var basalt = LoadIllustration("CrackedBasalt", true);
            var chasm = LoadIllustration("EmberChasm", false);
            var memorial = LoadIllustration("VigilMemorial", true);
            var flame = IllustrationMaterial("RuinsFlameArt", LoadIllustration("BrazierFlame", true));
            flame.SetFloat("_Waver", 1f);
            var foliage = IllustrationMaterial("RuinsFoliageArt", LoadIllustration("EmberBriar", true));
            var modules = Pieces.Select(piece => (piece.Field, Prefab: CreatePrefab(piece.File, basalt, flame, foliage))).ToArray();
            var memorialPrefab = CreateMemorial(memorial);
            var scene = EditorSceneManager.OpenScene(Sandbox);
            var arena = scene.GetRootGameObjects().Single(root => root.name == "Arena");
            foreach (Transform child in arena.transform.Cast<Transform>().ToArray())
            {
                if (child.name is not ("Floor" or "Long wall" or "Short wall"))
                {
                    Object.DestroyImmediate(child.gameObject);
                    continue;
                }
                child.GetComponent<Renderer>().enabled = false;
                ResizeCollider(child);
            }

            var room = arena.AddComponent<AshenRuinsRoom>();
            foreach (var module in modules)
                CombatSandboxBuilder.SetReference(room, module.Field, module.Prefab);
            CombatSandboxBuilder.SetReference(room, "memorial", memorialPrefab);
            room.Rebuild(seed, (module, parent) => (GameObject)PrefabUtility.InstantiatePrefab(module, parent));

            var grout = CombatSandboxBuilder.Material("RuinsGrout", new Color(0.13f, 0.085f, 0.11f), "AshenRealm/Courtyard Lit");
            CombatSandboxBuilder.Primitive("Grout", PrimitiveType.Cube, arena.transform,
                new Vector3(0, -0.235f, 0), new Vector3(AshenRuinsRoom.Columns * 2f, 0.30f, AshenRuinsRoom.Rows * 2f),
                grout, false);
            arena.transform.Find("Grout").GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            BakeNavigation(arena.GetComponent<NavMeshSurface>());
            SetLighting(scene);
            SetBackdrop(chasm);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save " + ScenePath);

            if (!EditorBuildSettings.scenes.Any(entry => entry.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"Ashen Realm: Ashen Ruins ready at {ScenePath} with seed {seed}.");
        }

        private static void ResizeCollider(Transform child)
        {
            float width = AshenRuinsRoom.Columns * 2f;
            float depth = AshenRuinsRoom.Rows * 2f;
            switch (child.name)
            {
                case "Floor":
                    child.localScale = new Vector3(width, 0.5f, depth);
                    break;
                case "Long wall":
                    child.localPosition = new Vector3(0, 0.35f, Mathf.Sign(child.localPosition.z) * (depth / 2 - 0.25f));
                    child.localScale = new Vector3(width, 0.7f, 0.5f);
                    break;
                case "Short wall":
                    child.localPosition = new Vector3(Mathf.Sign(child.localPosition.x) * (width / 2 - 0.25f), 0.35f, 0);
                    child.localScale = new Vector3(0.5f, 0.7f, depth);
                    break;
            }
        }

        private static Texture2D LoadIllustration(string name, bool mipmaps)
        {
            string path = $"{Illustrations}/{name}.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                throw new InvalidOperationException($"Missing Ashen Ruins illustration at {path}.");
            if (importer.npotScale != TextureImporterNPOTScale.None || importer.mipmapEnabled != mipmaps ||
                importer.wrapMode != TextureWrapMode.Clamp || !importer.alphaIsTransparency)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = mipmaps;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static GameObject CreatePrefab(string name, Texture2D basalt, Material flame, Material foliage)
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
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material => Surface(material.name, name, basalt)).ToArray();
                }

                if (name == "Brazier")
                {
                    AddPainting("Painterly Flame", root.transform, flame, new Vector3(0, 1.19f, 0), 1.75f);
                    var light = new GameObject("Brazier Glow").AddComponent<Light>();
                    light.transform.SetParent(root.transform, false);
                    light.transform.localPosition = new Vector3(0, 1.64f, 0);
                    light.type = LightType.Point;
                    light.range = 5.6f;
                    light.intensity = 14f;
                    light.color = new Color(1f, 0.49f, 0.15f);
                }
                if (name == "CrimsonBriar")
                    AddPainting("Crimson shoots", root.transform, foliage, new Vector3(0, 0.03f, -0.24f), 1.24f);
                if (name == "AshTree")
                {
                    AddPainting("Lower ash crown", root.transform, foliage, new Vector3(-0.88f, 2.51f, 0.08f), 1.69f);
                    AddPainting("Higher ash crown", root.transform, foliage, new Vector3(0.60f, 3.29f, 0.37f), 2.10f);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, $"{Prefabs}/{name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateMemorial(Texture2D art)
        {
            var root = new GameObject("VigilMemorial");
            try
            {
                var material = IllustrationMaterial("RuinsMemorial", art);
                AddPainting("Painted stone figure", root.transform, material, Vector3.zero, 5.4f);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{Prefabs}/{root.name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetBackdrop(Texture2D art)
        {
            var camera = UnityEngine.Camera.main;
            var material = IllustrationMaterial("RuinsBackdrop", art);
            var quad = IllustrationQuad("Illustrated Chasm", camera.transform, material, new Vector3(0, 0, 68f), Vector3.one);
            var backdrop = camera.gameObject.AddComponent<RuinsBackdrop>();
            CombatSandboxBuilder.SetReference(backdrop, "canvas", quad);
            backdrop.Fit();
        }

        private static Material IllustrationMaterial(string label, Texture2D illustration)
        {
            var material = CombatSandboxBuilder.Material(label, Color.white, "AshenRealm/Ruins Illustration");
            material.SetTexture("_BaseMap", illustration);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform IllustrationQuad(string label, Transform parent, Material material, Vector3 center,
            Vector3 size)
        {
            CombatSandboxBuilder.Primitive(label, PrimitiveType.Quad, parent, center, size, material, false);
            var quad = parent.Find(label);
            quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return quad;
        }

        private static void AddPainting(string label, Transform parent, Material material, Vector3 foot, float height)
        {
            var texture = material.mainTexture;
            var angle = Quaternion.Euler(38, 45, 0);
            var quad = IllustrationQuad(label, parent, material, foot + angle * Vector3.up * (height * 0.44f),
                new Vector3(height * texture.width / texture.height, height, 1f));
            quad.localRotation = angle;
        }

        private static Material Surface(string name, string module, Texture2D basalt)
        {
            if (name is "RuinsPaving" or "RuinsMasonry")
            {
                var stone = CombatSandboxBuilder.Material(name, Color.white, "AshenRealm/Ruins Illustrated Stone");
                stone.SetTexture("_BaseMap", basalt);
                stone.SetFloat("_Masonry", name == "RuinsMasonry" ? 1f : 0f);
                EditorUtility.SetDirty(stone);
                return stone;
            }
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
                "RuinsFire" => new Color(1f, 0.36f, 0.055f),
                "RuinsRune" => new Color(0.95f, 0.24f, 0.18f),
                "RuinsCliff" => new Color(0.75f, 0.47f, 0.54f),
                _ => throw new ArgumentException($"Unknown Ashen Ruins surface {name}.")
            };
            bool lit = name is "RuinsPaint" or "RuinsCliff";
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
            sun.intensity = 2.25f;
            sun.transform.rotation = Quaternion.Euler(38, -53, 0);
            roots.Single(root => root.name == "Cold Sky Fill").SetActive(false);
            RenderSettings.ambientLight = new Color(0.30f, 0.21f, 0.28f);
            var camera = UnityEngine.Camera.main;
            camera.backgroundColor = new Color(0.105f, 0.035f, 0.070f);
            camera.orthographicSize = 6.5f;
            camera.farClipPlane = 100f;
            var follow = camera.GetComponent<IsometricCamera>();
            var cameraSettings = new SerializedObject(follow);
            cameraSettings.FindProperty("pitch").floatValue = 38f;
            cameraSettings.ApplyModifiedPropertiesWithoutUndo();
            follow.Place(true);
        }
    }
}
