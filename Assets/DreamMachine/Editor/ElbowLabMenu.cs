using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamMachine.EditorTools
{
    static class ElbowLabMenu
    {
        [MenuItem("Dream Machine/Start Elbow Laboratory")]
        static void Open() => Open(false);

        [MenuItem("Dream Machine/Start Native Elbow Laboratory")]
        static void OpenNative() => Open(true);

        static void Open(bool native)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Elbow Laboratory").AddComponent<ElbowLabView>().UseNativePhysics = native;
            EditorApplication.EnterPlaymode();
        }
    }
}
