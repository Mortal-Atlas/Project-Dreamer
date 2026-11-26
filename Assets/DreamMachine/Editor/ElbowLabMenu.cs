using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamMachine.EditorTools
{
    static class ElbowLabMenu
    {
        [MenuItem("Dream Machine/Start Elbow Laboratory")]
        static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Elbow Laboratory").AddComponent<ElbowLabView>();
            EditorApplication.EnterPlaymode();
        }
    }
}
