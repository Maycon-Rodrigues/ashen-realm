using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AshenRealm.Editor
{
    internal static class CombatSandboxKnight
    {
        private const string ModelPath = "Assets/_Game/Art/Characters/FallenKnightSkinned.fbx";
        private const string ActorVisual = "Fallen Knight Painted Rig";

        public static bool Apply()
        {
            var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            var actor = roots.Single(root => root.TryGetComponent<PlayerCombat>(out _));
            if (actor.transform.Find(ActorVisual))
                return false;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!asset)
                throw new InvalidOperationException("Generate the skinned Fallen Knight with Tools/Blender/scripts/fallen_knight.py.");

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(asset, actor.transform);
            visual.name = ActorVisual;
            var body = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!body || body.bones.Length == 0 || body.sharedMesh.colors32.Length != body.sharedMesh.vertexCount)
                throw new InvalidOperationException("The Fallen Knight FBX must include a skin and vertex paint.");

            var grip = visual.GetComponentsInChildren<Transform>().Single(bone => bone.name == "SwordGrip");
            if (actor.transform.InverseTransformPoint(grip.position).z < 0f)
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            body.sharedMaterials = body.sharedMaterials.Select(ConvertMaterial).ToArray();
            var animator = visual.GetComponent<Animator>();
            if (animator)
                animator.enabled = false;

            var weapon = actor.transform.Find("Blade Pivot");
            weapon.SetPositionAndRotation(grip.position, actor.transform.rotation);
            weapon.Find("AshenSword").localScale = new Vector3(0.74f, 1.05f, 0.90f);
            var motion = actor.AddComponent<FallenKnightMotion>();
            CombatSandboxBuilder.SetReference(motion, "model", visual.transform);
            CombatSandboxBuilder.SetReference(motion, "weapon", weapon);

            var feedback = actor.GetComponent<CombatFeedback>();
            CombatSandboxBuilder.SetReference(feedback, "body", body);
            CombatSandboxBuilder.SetReference(feedback, "pose", null);

            UnityEngine.Object.DestroyImmediate(actor.GetComponent<ArenaCharacterMotion>());
            UnityEngine.Object.DestroyImmediate(actor.transform.Find("FallenKnight").gameObject);
            Debug.Log($"Ashen Realm: painted Fallen Knight imported with {body.bones.Length} skin bones.");
            return true;
        }

        private static Material ConvertMaterial(Material imported)
        {
            Color color = imported.name switch
            {
                "KnightPlate" => new Color(0.29f, 0.34f, 0.40f),
                "KnightEdge" => new Color(0.43f, 0.48f, 0.50f),
                "KnightLeather" => new Color(0.20f, 0.13f, 0.095f),
                "KnightCloth" => new Color(0.16f, 0.39f, 0.33f),
                "KnightHem" => new Color(0.39f, 0.42f, 0.24f),
                "KnightBronze" => new Color(0.67f, 0.46f, 0.25f),
                "KnightVisor" => new Color(0.24f, 0.71f, 0.61f),
                _ => throw new InvalidOperationException($"Unexpected knight surface {imported.name}.")
            };
            string shader = imported.name == "KnightVisor" ? "Universal Render Pipeline/Unlit" : "AshenRealm/Courtyard Lit";
            var material = CombatSandboxBuilder.Material(imported.name, color, shader);
            if (material.HasProperty("_VertexPaint"))
            {
                bool metal = imported.name is "KnightPlate" or "KnightEdge" or "KnightBronze";
                material.SetFloat("_VertexPaint", 0.72f);
                material.SetFloat("_Rim", metal ? 0.26f : 0.065f);
                material.SetFloat("_Polish", metal ? 0.23f : 0.025f);
                material.SetFloat("_Grain", metal ? 0.23f : 0.42f);
                EditorUtility.SetDirty(material);
            }
            return material;
        }
    }
}
