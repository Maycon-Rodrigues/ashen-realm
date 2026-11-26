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

        public static void Play()
        {
            Generate();
            EditorApplication.delayCall += EditorApplication.EnterPlaymode;
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

            UpgradeCombatPresentation();
            UpgradeSandboxProgression();
            bool artChanged = CombatSandboxArt.Apply();
            bool knightChanged = CombatSandboxKnight.Apply();
            if ((artChanged || knightChanged) && !EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath))
                throw new IOException("Could not save the art pass in " + ScenePath);

            if (!EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log("Ashen Realm: CombatSandbox ready at " + ScenePath);
        }

        private static void UpgradeSandboxProgression()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var hud = roots.Single(root => root.TryGetComponent<SandboxStatus>(out _));
            if (hud.TryGetComponent<SandboxProgression>(out _))
                return;

            var player = roots.Select(root => root.GetComponent<PlayerCombat>()).Single(combat => combat);
            var enemy = roots.Single(root => root.TryGetComponent<EnemyAI>(out _));
            var blade = player.transform.Find("Blade Pivot/Sword blade").GetComponent<Renderer>();
            var reward = Material("AshenBlade", new Color(0.16f, 0.84f, 0.72f), "Universal Render Pipeline/Unlit");
            var drop = new GameObject("Ashen Blade Drop").transform;
            drop.SetParent(hud.transform, false);
            var sword = new GameObject("Loot Sword").transform;
            sword.SetParent(drop, false);
            sword.localRotation = Quaternion.Euler(-25f, 0f, -24f);
            Primitive("Blade", PrimitiveType.Cube, sword, new Vector3(0f, 0.48f, 0f),
                new Vector3(0.15f, 0.80f, 0.11f), reward, false);
            Primitive("Crossguard", PrimitiveType.Cube, sword, new Vector3(0f, 0.10f, 0f),
                new Vector3(0.39f, 0.10f, 0.10f), reward, false);
            Primitive("Grip", PrimitiveType.Cube, sword, new Vector3(0f, -0.04f, 0f),
                new Vector3(0.11f, 0.24f, 0.10f), reward, false);
            drop.gameObject.SetActive(false);

            var progression = hud.AddComponent<SandboxProgression>();
            SetReference(progression, "player", player);
            SetReference(progression, "playerHealth", player.GetComponent<Health>());
            SetReference(progression, "enemy", enemy.GetComponent<Health>());
            SetReference(progression, "drop", drop);
            SetReference(progression, "blade", blade);
            SetReference(progression, "upgradedBlade", reward);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save the progression setup in " + ScenePath);
        }

        private static void UpgradeCombatPresentation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var knight = roots.Single(root => root.TryGetComponent<PlayerCombat>(out _));
            if (knight.TryGetComponent<CombatFeedback>(out _))
                return;

            var grunt = roots.Single(root => root.TryGetComponent<EnemyAI>(out _));
            var border = Material("RuinStone", new Color(0.18f, 0.22f, 0.22f));
            var blade = Material("BladeSteel", new Color(0.59f, 0.66f, 0.67f));
            var pivot = new GameObject("Blade Pivot").transform;
            pivot.SetParent(knight.transform, false);
            pivot.localPosition = new Vector3(0.46f, 1.02f, 0.07f);
            Primitive("Sword blade", PrimitiveType.Cube, pivot, new Vector3(0, 0, 0.48f),
                new Vector3(0.10f, 0.07f, 0.79f), blade, false);
            Primitive("Crossguard", PrimitiveType.Cube, pivot, new Vector3(0, 0, 0.08f),
                new Vector3(0.29f, 0.1f, 0.075f), border, false);

            var warningObject = new GameObject("Attack Warning");
            warningObject.transform.SetParent(grunt.transform, false);
            warningObject.transform.localPosition = Vector3.up * 0.04f;
            var warning = warningObject.AddComponent<LineRenderer>();
            warning.useWorldSpace = false;
            warning.loop = true;
            warning.sortingOrder = 1;
            warning.shadowCastingMode = ShadowCastingMode.Off;
            warning.receiveShadows = false;
            warning.sharedMaterial = Material("AttackWarning", new Color(1f, 0.33f, 0.11f), "Universal Render Pipeline/Unlit");
            warning.enabled = false;

            var swing = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/Swing.wav");
            var hit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/Hit.wav");
            if (!swing || !hit)
                throw new InvalidOperationException("Generate both combat cues with Tools/Audio/generate-combat-cues.py.");

            AddCombatFeedback(knight, pivot, null, swing, hit);
            AddCombatFeedback(grunt, grunt.transform.Find("Body"), warning, swing, hit);

            var arena = roots.Single(root => root.name == "Arena");
            var piers = arena.transform.Cast<Transform>().Where(child => child.name == "Broken pier").ToArray();
            AddDecoration(arena.transform, piers[0], "Altar", 0.64f, border);
            AddDecoration(arena.transform, piers[2], "Rock", 0.60f, border);
            AddDecoration(arena.transform, piers[5], "DeadTree", 0.79f,
                Material("WitheredBark", new Color(0.24f, 0.16f, 0.11f)));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save the combat presentation in " + ScenePath);
        }

        private static void AddCombatFeedback(GameObject actor, Transform pivot, LineRenderer warning, AudioClip swing, AudioClip hit)
        {
            var feedback = actor.AddComponent<CombatFeedback>();
            SetReference(feedback, "body", actor.transform.Find("Body").GetComponent<Renderer>());
            SetReference(feedback, "pose", pivot);
            SetReference(feedback, "warning", warning);
            SetReference(feedback, "swing", swing);
            SetReference(feedback, "hit", hit);
        }

        private static void AddDecoration(Transform parent, Transform pier, string modelName, float scale, Material material)
        {
            const string folder = "Assets/_Game/Art/Arena";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{modelName}.fbx");
            if (!model)
                throw new InvalidOperationException($"Missing arena model {modelName} in {folder}.");

            var prop = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            prop.transform.localPosition = Vector3.ProjectOnPlane(pier.localPosition, Vector3.up);
            prop.transform.localScale = Vector3.one * scale;
            foreach (var renderer in prop.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();

            // Reuse the pier's baked collision footprint for the new visuals.
            pier.GetComponent<Renderer>().enabled = false;
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

        internal static void SetReference(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static Material Material(string name, Color color, string shaderName = "Universal Render Pipeline/Lit")
        {
            string path = Output + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing)
                return existing;
            var shader = Shader.Find(shaderName);
            if (!shader)
                throw new InvalidOperationException($"Shader {shaderName} is missing.");

            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Smoothness"))
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

        internal static void Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
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
