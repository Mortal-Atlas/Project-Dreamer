using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamMachine.EditorTools
{
    static class ElbowLabMenu
    {
        [MenuItem("Dream Machine/Start Lower-Body Laboratory")]
        static void OpenLowerBody()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Lower-body laboratory").AddComponent<LowerBodyLabView>();
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Dream Machine/Start Elbow Laboratory")]
        static void Open() => Open(false);

        [MenuItem("Dream Machine/Start Native Elbow Laboratory")]
        static void OpenNative() => Open(true);

        [MenuItem("Dream Machine/Start Predictive Elbow Laboratory")]
        static void OpenPredictive() => Open(true, true);

        [MenuItem("Dream Machine/Start Force-Aware Elbow Laboratory")]
        static void OpenForceAware() => Open(true, false, true);

        static void Open(bool native, bool predictive = false, bool force = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var view = new GameObject("Elbow Laboratory").AddComponent<ElbowLabView>();
            view.UseNativePhysics = native;
            view.UsePredictiveMotor = predictive;
            view.UseForceMotor = force;
            EditorApplication.EnterPlaymode();
        }
    }
}
