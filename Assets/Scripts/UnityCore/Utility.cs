using UnityEngine;
using UnityEngine.UI;

namespace UnityCore {
    public static class Utility
    {
        public static GradientColorKey[] InvertGradientKeysInplace(GradientColorKey[] keys) {
            for (int i = 0; i < keys.Length; i++)
                keys[i].time = 1 - keys[i].time;

            return keys;
        }

        public static GradientAlphaKey[] InvertGradientKeysInplace(GradientAlphaKey[] keys) {
            for (int i = 0; i < keys.Length; i++)
                keys[i].time = 1 - keys[i].time;

            return keys;
        }

        public static Keyframe[] InvertAnimationCurveKeysInplace(Keyframe[] keys) {
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time = 1 - keys[i].time;
                (keys[i].inTangent, keys[i].outTangent) = (-keys[i].outTangent, -keys[i].inTangent);
                (keys[i].inWeight, keys[i].outWeight) = (keys[i].outWeight, keys[i].inWeight);
                switch (keys[i].weightedMode)
                {
                    case WeightedMode.In:
                        keys[i].weightedMode = WeightedMode.Out;
                        break;
                    case WeightedMode.Out:
                        keys[i].weightedMode = WeightedMode.In;
                        break;
                }
            }

            return keys;
        }

        /// <summary>
        /// Creates a static game object with mesh filter and mesh renderer, assigning object's parent, mesh and
        /// material. Lighting-related renderer features are disabled.
        /// 
        /// Note: calling with `makeStatic = true` might cause mesh combining that might render meshes incorrectly
        /// </summary>
        public static GameObject MakeFlatMesh(Mesh mesh, Material mat, Transform parent, string objectName = "FlatMesh", bool makeStatic = false) {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.parent = parent;
            gameObject.isStatic = makeStatic;
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            return gameObject;
        }

        public static void TintAllGraphics(GameObject parent, Color color) {
            Graphic[] graphics = parent.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics) {
                graphic.color = color;
            }
        }
    }
}