using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenRealm.Editor
{
    internal static class CombatSandboxArt
    {
        private const string ModelFolder = "Assets/_Game/Art/VerticalSlice";
        private static readonly Dictionary<string, Color> Palette = new()
        {
            ["Iron"] = new Color(0.11f, 0.20f, 0.27f),
            ["Steel"] = new Color(0.28f, 0.49f, 0.66f),
            ["Brass"] = new Color(0.72f, 0.52f, 0.27f),
            ["Leather"] = new Color(0.26f, 0.15f, 0.10f),
            ["Spirit"] = new Color(0.15f, 0.82f, 0.73f),
            ["Mantle"] = new Color(0.09f, 0.34f, 0.28f),
            ["Carapace"] = new Color(0.42f, 0.17f, 0.16f),
            ["Hide"] = new Color(0.64f, 0.27f, 0.16f),
            ["Bone"] = new Color(0.83f, 0.67f, 0.45f),
            ["Ember"] = new Color(1f, 0.40f, 0.19f),
            ["Stone"] = new Color(0.31f, 0.33f, 0.34f),
            ["Slate"] = new Color(0.25f, 0.28f, 0.31f),
            ["Moss"] = new Color(0.22f, 0.29f, 0.26f),
            ["Path"] = new Color(0.36f, 0.36f, 0.32f)
        };

        public static bool Apply()
        {
            var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            var arena = roots.Single(root => root.name == "Arena").transform;
            if (arena.Find("AshGate"))
                return false;

            var player = roots.Single(root => root.TryGetComponent<PlayerCombat>(out _));
            var enemy = roots.Single(root => root.TryGetComponent<EnemyAI>(out _));
            var progression = roots.Select(root => root.GetComponent<SandboxProgression>()).Single(component => component);
            BuildActor(player, "FallenKnight", 1f, false);
            BuildActor(enemy, "CorruptedGrunt", 0.94f, true);

            var pivot = player.transform.Find("Blade Pivot");
            Vector3 pivotPosition = pivot.localPosition;
            var shield = FindPart(player.transform.Find("FallenKnight"), "Shield").bounds.center;
            pivotPosition.x = -Mathf.Sign(player.transform.InverseTransformPoint(shield).x) * Mathf.Abs(pivotPosition.x);
            pivot.localPosition = pivotPosition;
            pivot.Find("Sword blade").gameObject.SetActive(false);
            pivot.Find("Crossguard").gameObject.SetActive(false);
            var weapon = InstantiateModel("AshenSword", pivot);
            weapon.localRotation = Quaternion.Euler(90f, 0f, 0f);
            weapon.localPosition = Vector3.back * 0.1f;
            CombatSandboxBuilder.SetReference(progression, "blade", FindPart(weapon, "Blade"));

            var drop = progression.transform.Find("Ashen Blade Drop");
            drop.Find("Loot Sword").gameObject.SetActive(false);
            var pickup = InstantiateModel("AshenSword", drop);
            pickup.localRotation = Quaternion.Euler(-25f, 0f, -24f);
            FindPart(pickup, "Blade").sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Generated/AshenBlade.mat");

            var warning = enemy.transform.Find("Attack Warning");
            warning.localPosition = Vector3.up * 0.102f;

            var paving = InstantiateModel("ArenaPaving", arena);
            paving.localPosition = Vector3.up * 0.003f;
            arena.Find("Floor").GetComponent<Renderer>().sharedMaterial = Surface("Slate");
            DressWalls(arena);
            BuildGate(arena);

            var sun = roots.Single(root => root.name == "Dusk Light").GetComponent<Light>();
            sun.color = new Color(1f, 0.88f, 0.73f);
            sun.intensity = 2.4f;
            sun.transform.rotation = Quaternion.Euler(52f, 43f, 0f);
            RenderSettings.sun = sun;
            var fill = new GameObject("Cold Sky Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.58f, 0.76f, 1f);
            fill.intensity = 1.15f;
            fill.transform.rotation = Quaternion.Euler(48f, -127f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.60f, 0.68f, 0.72f);
            return true;
        }

        private static void BuildActor(GameObject actor, string modelName, float scale, bool isEnemy)
        {
            var visual = InstantiateModel(modelName, actor.transform);
            visual.localScale = Vector3.one * scale;
            string facingPart = isEnemy ? "FacingEyeA" : "FacingVisor";
            var face = FindPart(visual, facingPart);
            if (actor.transform.InverseTransformPoint(face.bounds.center).z < 0f)
                visual.localRotation = Quaternion.Euler(0f, 180f, 0f);

            actor.transform.Find("Body").GetComponent<Renderer>().enabled = false;
            actor.transform.Find("Facing marker").GetComponent<Renderer>().enabled = false;

            var motion = actor.AddComponent<ArenaCharacterMotion>();
            CombatSandboxBuilder.SetReference(motion, "visual", visual);
            CombatSandboxBuilder.SetReference(motion, "leftLeg", LegPivot(visual, "LeftLeg"));
            CombatSandboxBuilder.SetReference(motion, "rightLeg", LegPivot(visual, "RightLeg"));

            var feedback = actor.GetComponent<CombatFeedback>();
            CombatSandboxBuilder.SetReference(feedback, "body", FindPart(visual, "Chest"));
            if (isEnemy)
                CombatSandboxBuilder.SetReference(feedback, "pose", visual);
        }

        private static Transform LegPivot(Transform visual, string label)
        {
            var parts = visual.GetComponentsInChildren<MeshRenderer>()
                .Where(renderer => PartName(renderer).StartsWith(label, StringComparison.Ordinal)).ToArray();

            // Keep the imported hierarchy intact. Each leg renders from duplicate mesh references under its pivot.
            var thigh = parts.Single(renderer => PartName(renderer) == label + "Thigh");
            var pivot = new GameObject(label).transform;
            pivot.SetParent(visual, false);
            var hip = thigh.bounds.center;
            hip.y = thigh.bounds.max.y;
            pivot.localPosition = visual.InverseTransformPoint(hip);

            foreach (var part in parts)
            {
                var articulated = new GameObject(PartName(part));
                articulated.transform.SetPositionAndRotation(part.transform.position, part.transform.rotation);
                articulated.transform.localScale = part.transform.lossyScale;
                articulated.transform.SetParent(pivot, true);
                articulated.AddComponent<MeshFilter>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
                articulated.AddComponent<MeshRenderer>().sharedMaterials = part.sharedMaterials;
                part.enabled = false;
            }

            return pivot;
        }

        private static void DressWalls(Transform arena)
        {
            var stone = Surface("Stone");
            for (int side = -1; side <= 1; side += 2)
            {
                for (int index = -5; index <= 5; index++)
                {
                    float x = index * 2.06f;
                    CombatSandboxBuilder.Primitive("Wall coping", PrimitiveType.Cube, arena,
                        new Vector3(x, 0.74f, side * 9.75f), new Vector3(1.92f, 0.12f, 0.62f),
                        index % 3 == 0 ? Surface("Path") : stone, false);
                }
            }
        }

        private static void BuildGate(Transform arena)
        {
            var gate = InstantiateModel("AshGate", arena);
            gate.localPosition = new Vector3(11.08f, 0f, 0f);
            gate.localRotation = Quaternion.Euler(0f, -90f, 0f);

            CombatSandboxBuilder.Primitive("Beyond the gate", PrimitiveType.Cube, arena,
                new Vector3(13.6f, -0.20f, 0f), new Vector3(4.0f, 0.40f, 4.35f), Surface("Slate"), false);
            for (int step = 0; step < 3; step++)
            {
                CombatSandboxBuilder.Primitive("Ruined approach step", PrimitiveType.Cube, arena,
                    new Vector3(12.3f + step * 0.88f, 0.02f + step * 0.11f, 0f),
                    new Vector3(0.84f, 0.15f + step * 0.22f, 2.58f), Surface("Path"), false);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                CombatSandboxBuilder.Primitive("Broken landing wall", PrimitiveType.Cube, arena,
                    new Vector3(13.45f, 0.28f, side * 1.92f), new Vector3(3.3f, 0.6f, 0.49f), Surface("Stone"), false);
                var flame = new GameObject("Gate Ember").transform;
                flame.SetParent(gate, false);
                flame.localPosition = new Vector3(side * 1.47f, 1.98f, 0.60f);
                CombatSandboxBuilder.Primitive("Ember Facet", PrimitiveType.Cube, flame,
                    Vector3.zero, new Vector3(0.13f, 0.30f, 0.12f), Surface("Ember"), false);
                flame.localRotation = Quaternion.Euler(0f, 0f, side * 15f);
                var light = flame.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.49f, 0.20f);
                light.range = 3.1f;
                light.intensity = 2.2f;
            }
        }

        private static Transform InstantiateModel(string name, Transform parent)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{name}.fbx");
            if (!model)
                throw new InvalidOperationException($"Missing {name}.fbx. Run Tools/Blender/scripts/vertical_slice.py.");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = name;
            var renderers = instance.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
                renderer.sharedMaterial = Surface(renderer.name.Split('.')[0]);

            if (renderers.Length == 0)
                throw new InvalidOperationException($"The imported model {name}.fbx contains no renderers.");
            return instance.transform;
        }

        private static string PartName(Renderer renderer)
        {
            var names = renderer.name.Split('.');
            return names.Length > 1 ? names[1] : names[0];
        }

        private static MeshRenderer FindPart(Transform model, string name) =>
            model.GetComponentsInChildren<MeshRenderer>().Single(renderer => PartName(renderer) == name);

        private static Material Surface(string name)
        {
            string shader = name is "Ember" or "Spirit"
                ? "Universal Render Pipeline/Unlit"
                : "Universal Render Pipeline/Lit";
            Color color = Palette[name];
            var material = CombatSandboxBuilder.Material("Art" + name, color, shader);
            if (material.color != color)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
            }
            // A restrained colored fill keeps silhouettes readable on the shaded side.
            if (name is "Steel" or "Mantle" or "Carapace" or "Hide")
            {
                var emission = color * 0.56f;
                if (material.GetColor("_EmissionColor") != emission)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    EditorUtility.SetDirty(material);
                }
            }
            return material;
        }
    }
}
