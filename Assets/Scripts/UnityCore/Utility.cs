using UnityEngine;

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
    }
}