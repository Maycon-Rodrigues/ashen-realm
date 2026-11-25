using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AshenRealm.Editor
{
    public static class CombatSandboxBuilder
    {
        private const string Output = "Assets/_Game/Generated";
        private const string ScenePath = Output + "/CombatSandbox.unity";

        [MenuItem("Ashen Realm/Open or Generate Combat Sandbox")]
        public static void OpenOrGenerate()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                Generate();
        }

        public static void Generate()
        {
            Directory.CreateDirectory(Output);
            AssetDatabase.Refresh();
            if (PlayerSettings.productName == "Stock")
                PlayerSettings.productName = "Ashen Realm";
            ConfigureInputAndRendering();

            if (!File.Exists(ScenePath))
                CreateScene();
            else
                EditorSceneManager.OpenScene(ScenePath);

            if (!EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log("Ashen Realm: CombatSandbox ready at " + ScenePath);
        }

        private static void ConfigureInputAndRendering()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0)
                throw new InvalidOperationException("Unity Player Settings are missing. Run the Unity bootstrap.");

            var playerSettings = new SerializedObject(settings[0]);
            var input = playerSettings.FindProperty("activeInputHandler");
            if (input == null)
                throw new InvalidOperationException("Set Active Input Handling to Input System Package (New) in Player Settings.");

            if (input.intValue == 0)
            {
                input.intValue = 1;
                playerSettings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                Debug.Log("Ashen Realm: restart Unity to activate the Input System before entering Play Mode.");
            }

            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                return;
            if (GraphicsSettings.defaultRenderPipeline)
                throw new InvalidOperationException("Assign a URP asset in Graphics Settings before generating the sandbox.");

            string path = Output + "/SandboxURP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (!pipeline)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
                AssetDatabase.CreateAsset(renderer, Output + "/SandboxRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, path);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
        }

        private static void CreateScene()
        {
            int enemyLayer = EnsureLayer("Enemy");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = Material("AshStone", new Color(0.29f, 0.27f, 0.24f));
            var border = Material("RuinStone", new Color(0.18f, 0.22f, 0.22f));
            var steel = Material("KnightSteel", new Color(0.28f, 0.48f, 0.52f));
            var rust = Material("GruntRust", new Color(0.65f, 0.25f, 0.15f));
            var arena = new GameObject("Arena");
            Primitive("Floor", PrimitiveType.Cube, arena.transform, new Vector3(0, -0.25f, 0),
                new Vector3(24, 0.5f, 20), floor, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Long wall", PrimitiveType.Cube, arena.transform, new Vector3(0, 0.35f, side * 9.75f),
                    new Vector3(24, 0.7f, 0.5f), border, true);
                Primitive("Short wall", PrimitiveType.Cube, arena.transform, new Vector3(side * 11.75f, 0.35f, 0),
                    new Vector3(0.5f, 0.7f, 20), border, true);
                for (int index = -1; index <= 1; index++)
                    Primitive("Broken pier", PrimitiveType.Cube, arena.transform,
                        new Vector3(index * 6, 0.7f, side * 7.6f),
                        new Vector3(1.1f, 1.4f + index * 0.24f, 1.1f), border, true);
            }

            var navigation = arena.AddComponent<NavMeshSurface>();
            navigation.collectObjects = CollectObjects.Children;
            navigation.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navigation.BuildNavMesh();
            if (!navigation.navMeshData)
                throw new InvalidOperationException("Arena NavMesh could not be baked.");

            navigation.navMeshData.name = "CombatSandboxNavMesh";
            AssetDatabase.CreateAsset(navigation.navMeshData, Output + "/CombatSandboxNavMesh.asset");
            if (!NavMesh.SamplePosition(Vector3.zero, out _, 0.5f, NavMesh.AllAreas))
                throw new InvalidOperationException("The baked NavMesh does not cover the arena center.");

            var player = new GameObject("Fallen Knight");
            player.transform.SetPositionAndRotation(new Vector3(-5.5f, 0, 0), Quaternion.LookRotation(Vector3.right));
            var controller = player.AddComponent<PlayerController>();
            var motor = player.GetComponent<CharacterController>();
            motor.height = 1.8f;
            motor.radius = 0.37f;
            motor.center = Vector3.up * 0.9f;
            motor.minMoveDistance = 0;
            ActorVisual(player.transform, steel, border);
            var combat = new SerializedObject(player.AddComponent<PlayerCombat>());
            combat.FindProperty("targets").intValue = 1 << enemyLayer;
            combat.ApplyModifiedPropertiesWithoutUndo();

            var enemy = new GameObject("Corrupted Grunt") { layer = enemyLayer };
            enemy.transform.position = new Vector3(5, 0, 0);
            var ai = enemy.AddComponent<EnemyAI>();
            var agent = enemy.GetComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.speed = 3.25f;
            agent.angularSpeed = 480f;
            agent.updateRotation = false;
            var body = enemy.GetComponent<CapsuleCollider>();
            body.radius = 0.4f;
            body.height = 1.8f;
            body.center = Vector3.up * 0.9f;
            var enemyHealth = enemy.GetComponent<Health>();
            var healthSettings = new SerializedObject(enemyHealth);
            healthSettings.FindProperty("maxHealth").intValue = 45;
            healthSettings.ApplyModifiedPropertiesWithoutUndo();
            SetReference(ai, "target", player.GetComponent<Health>());
            ActorVisual(enemy.transform, rust, border);

            var cameraObject = new GameObject("Isometric View") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.10f);
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<IsometricCamera>();
            SetReference(follow, "target", player.transform);
            SetReference(controller, "view", cameraObject.transform);
            follow.Place(true);

            var hud = new GameObject("Combat Status").AddComponent<SandboxStatus>();
            SetReference(hud, "player", player.GetComponent<Health>());
            SetReference(hud, "enemy", enemyHealth);

            var light = new GameObject("Dusk Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.85f, 0.73f);
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(49, -36, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.44f, 0.49f, 0.53f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save " + ScenePath);
        }

        private static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0)
                return existing;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = settings.FindProperty("layers");
            for (int index = 8; index < layers.arraySize; index++)
            {
                var layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                    continue;
                layer.stringValue = name;
                settings.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }

            throw new InvalidOperationException("A user layer is required for Enemy.");
        }

        private static void SetReference(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material Material(string name, Color color)
        {
            string path = Output + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing)
                return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader)
                throw new InvalidOperationException("URP Lit shader is missing.");

            var material = new Material(shader) { color = color };
            material.SetFloat("_Smoothness", 0.12f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ActorVisual(Transform root, Material body, Material trim)
        {
            Primitive("Body", PrimitiveType.Capsule, root, Vector3.up * 0.91f,
                new Vector3(0.74f, 0.88f, 0.74f), body, false);
            Primitive("Facing marker", PrimitiveType.Cube, root, new Vector3(0, 1.2f, 0.35f),
                new Vector3(0.4f, 0.22f, 0.24f), trim, false);
        }

        private static void Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool collides)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            if (!collides)
                UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        public static void ValidateBlenderAsset()
        {
            const string path = "Assets/_Game/Art/Generated/Rock.fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!model || !model.GetComponentsInChildren<MeshFilter>().Any(filter =>
                filter.sharedMesh && filter.sharedMesh.vertexCount >= 3))
                throw new InvalidOperationException("Unity could not import a rock mesh from " + path);
            Debug.Log("Ashen Realm: imported Rock.fbx with mesh geometry.");
        }
    }
}
