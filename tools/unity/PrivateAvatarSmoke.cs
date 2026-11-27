using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamMachine.Body;
using DreamMachine.Sim;
using DreamMachine.Social;
using DreamMachine.UnityBridge;
using UniVRM10;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class PrivateAvatarSmoke
{
    const string Folder = "Assets/DreamMachineLocalAvatars/";
    const string PreviewScene = Folder + "MerlinPreview.unity";
    const string SessionKey = "Dreamer.PrivateAvatarSmoke";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int _frames;

    static PrivateAvatarSmoke()
    {
        if (SessionState.GetInt(SessionKey, 0) != 0) EditorApplication.playModeStateChanged += OnPlay;
    }

    public static void Mature() => Start(1);
    public static void Newborn() => Start(2);
    public static void Growth() => Start(3);
    public static void Toddler() => Start(4);

    static void Start(int mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke")
            throw new InvalidOperationException("Use a scratch project with the isolated product name.");
        foreach (var model in new[] { "Merlin", "Marie", "Arthur", "Alice" })
        {
            string assetPath = Folder + model + ".vrm";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) throw new InvalidOperationException("VRM import failed after shader refresh for " + model);
            Inspect(prefab, model);
        }
        string path = Path.Combine(Application.persistentDataPath, "world.checkpoint");
        Directory.CreateDirectory(Application.persistentDataPath);
        var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 37);
        if (mode == 1)
            foreach (var learner in world.Children) learner.Body.AgeDays = HumanoidAnatomy.AdultAgeDays;
        if (mode == 3)
            foreach (var learner in world.Children) learner.Body.AgeDays = HumanoidAnatomy.AdultAgeDays - .005f;
        if (mode == 4)
            foreach (var learner in world.Children) learner.Body.AgeDays = .84f;
        File.WriteAllBytes(path, world.CaptureCheckpoint());
        SessionState.SetInt(SessionKey, mode);
        EditorSceneManager.OpenScene("Assets/Scenes/DreamMachine.unity");
        EditorApplication.EnterPlaymode();
    }

    static void OnPlay(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            var app = Object.FindAnyObjectByType<DreamMachineApp>() ?? new GameObject("DreamMachine").AddComponent<DreamMachineApp>();
            Read<TimeGovernor>(app, "_governor").Paused = true;
            EditorApplication.update += VerifyLiving;
        }
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.EraseInt(SessionKey);
            EditorApplication.Exit(0);
        }
    }

    static T Read<T>(DreamMachineApp app, string field) => (T)typeof(DreamMachineApp).GetField(field, Hidden).GetValue(app);
    static void Invoke(DreamMachineApp app, string method, params object[] args) => typeof(DreamMachineApp).GetMethod(method, Hidden).Invoke(app, args);

    static void VerifyLiving()
    {
        if (++_frames < 10) return;
        EditorApplication.update -= VerifyLiving;
        try
        {
            int mode = SessionState.GetInt(SessionKey, 0);
            bool mature = mode == 1, growth = mode == 3;
            var app = Object.FindAnyObjectByType<DreamMachineApp>();
            var world = Read<WorldSimulation>(app, "_sim");
            var control = WorldSimulation.RestoreCheckpoint(world.CaptureCheckpoint());
            var bodies = Object.FindObjectsByType<SegmentedBody>(FindObjectsSortMode.None);
            if (world.TickCount != 0 || bodies.Length != 4) throw new InvalidOperationException("Incorrect startup fixture.");
            if (!growth) Capture(Camera.main, "household-private-" + (mature ? "mature-standing" : mode == 4 ? "toddler" : "newborn"));
            foreach (var body in bodies)
            {
                bool bound = body.GetComponentInChildren<Vrm10Instance>() != null;
                bool teacher = body.PersonId == "father" || body.PersonId == "mother";
                if (bound != (mature || teacher)) throw new InvalidOperationException("Wrong age or identity binding for " + body.PersonId);
                if (!teacher && (mode == 2 || mode == 4))
                {
                    var torso = body.RigPivot("torsoGeometry");
                    var infant = torso.Find("Infant sleeper").gameObject;
                    var toddler = torso.Find("Toddler overalls").gameObject;
                    if (infant.activeSelf != (mode == 2) || toddler.activeSelf != (mode == 4))
                        throw new InvalidOperationException("Wrong early-development garment for " + body.PersonId);
                    Debug.Log(FormattableString.Invariant($"PRIVATE_GARMENT {body.PersonId} stage={(mode == 2 ? "infant" : "toddler")} anatomy_days={body.SampledDevelopmentDays:F3} height={body.Dimensions.Height:F3}"));
                    if (body.PersonId == "daughter")
                        CaptureDevelopment(Camera.main, body, mode == 2 ? "infant" : "toddler");
                }
                if (!bound) continue;
                var instance = body.GetComponentInChildren<Vrm10Instance>();
                Inspect(instance.gameObject, body.PersonId);
                var bone = instance.GetComponentInChildren<Animator>();
                Vector2 bounds = MeasureHeight(instance);
                float expected = body.Dimensions.Height;
                if (Mathf.Abs(bounds.x) > .035f || Mathf.Abs(bounds.y - expected) > .06f)
                    throw new InvalidOperationException($"{body.PersonId} ground {bounds.x:F4} and height {bounds.y:F4} differ from floor and {expected:F4}.");
                Vector3 Thigh(HumanBodyBones hip, HumanBodyBones knee) =>
                    bone.GetBoneTransform(knee).position - bone.GetBoneTransform(hip).position;
                if (Vector3.Dot(instance.transform.forward, Thigh(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg)) <= 0f ||
                    Vector3.Dot(instance.transform.forward, Thigh(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg)) <= 0f)
                    throw new InvalidOperationException("A standing knee is oriented backward for " + body.PersonId);
                Debug.Log(FormattableString.Invariant($"PRIVATE_STANDING {body.PersonId} floor={bounds.x:F4} height={bounds.y:F4} expected={expected:F4} seed={body.AnatomySeed}"));
            }
            Invoke(app, "ToggleResearchPresentation");
            if (bodies.Any(body => body.GetComponentInChildren<Vrm10Instance>() != null ||
                !body.RigPivot("pelvis").GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled)))
                throw new InvalidOperationException("Research mode did not restore procedural rendering.");
            Invoke(app, "ToggleResearchPresentation");

            var merlin = bodies.Single(body => body.PersonId == "father");
            var model = merlin.GetComponentInChildren<Vrm10Instance>();
            var animator = model.GetComponentInChildren<Animator>();
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var palm = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            var lid = Read<Transform>(app, "_lid");
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Vector3 startFoot = model.transform.InverseTransformPoint(left.position);
            Quaternion startHead = Quaternion.Inverse(model.transform.rotation) * head.rotation;
            Vector3 startHand = model.transform.InverseTransformPoint(wrist.position);
            int moving = 0, motion = 0, looking = 0, blinking = 0, interacting = 0, sleeping = 0;
            long poseTime = 0;
            int lidSamples = 0;
            int headOverBed = 0;
            float minimumLidError = float.PositiveInfinity;
            for (int i = 0; i < 1240; i++)
            {
                world.Tick();
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                Invoke(app, "StepActors", WorldSimulation.Dt);
                poseTime += System.Diagnostics.Stopwatch.GetTimestamp() - start;
                var person = world.Home.ReadPerson(0);
                if (Vector3.Distance(model.transform.position, new Vector3(person.X, model.transform.position.y, person.Z)) > .0001f)
                    throw new InvalidOperationException("Avatar moved away from Merlin's authoritative root.");
                if (merlin.Walking)
                {
                    moving++;
                    if ((model.transform.InverseTransformPoint(left.position) - startFoot).sqrMagnitude > .0001f)
                        motion++;
                }
                if ((person.Item >= 0 || person.Person >= 0) && person.Activity != HomeActivity.Sleep &&
                    Quaternion.Angle(startHead, Quaternion.Inverse(model.transform.rotation) * head.rotation) > 4f &&
                    Mathf.Abs(model.Runtime.LookAt.Pitch) > 1f) looking++;
                if (model.Runtime.Expression.GetWeight(ExpressionKey.Blink) > .3f) blinking++;
                if (person.Phase == HomePhase.Act && (person.Activity == HomeActivity.Open || person.Activity == HomeActivity.Close))
                {
                    minimumLidError = Mathf.Min(minimumLidError,
                        Vector3.Distance(palm.position, lid.TransformPoint(new Vector3(0f, .037f, .27f))));
                    if (++lidSamples == 12 && mature) Capture(Camera.main, "household-private-lid");
                }
                if (person.Phase == HomePhase.Act && person.Activity != HomeActivity.Rest && person.Activity != HomeActivity.Sleep &&
                    person.Activity != HomeActivity.Inspect && (model.transform.InverseTransformPoint(wrist.position) - startHand).sqrMagnitude > .012f)
                {
                    if (++interacting == 10 && !growth) Capture(Camera.main, "household-private-" + (mature ? "mature-reach" : "merlin-reach"));
                }
                if (person.Phase == HomePhase.Act && person.Activity == HomeActivity.Sleep &&
                    Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.Hips).up, Vector3.up) < .5f)
                {
                    sleeping++;
                    if (head.position.z < model.transform.position.z - .20f) headOverBed++;
                    if (sleeping == 30 && mature) Capture(Camera.main, "household-private-sleep");
                }
            }
            control.RunTicks(1240);
            if (!world.CaptureCheckpoint().SequenceEqual(control.CaptureCheckpoint()))
                throw new InvalidOperationException("Binding or displaying avatars changed the simulation.");
            if (growth && bodies.Count(body => body.GetComponentInChildren<Vrm10Instance>() != null) != 4)
                throw new InvalidOperationException("Growth did not activate the mature learner visuals.");
            if (moving == 0 || motion == 0 || looking == 0 || blinking == 0 || interacting == 0 || sleeping == 0)
                throw new InvalidOperationException("Missing pose, gait, gaze, expression, reach or sleep evidence.");
            double poseMs = poseTime * 1000.0 / System.Diagnostics.Stopwatch.Frequency / 1240.0;
            Debug.Log(FormattableString.Invariant($"PRIVATE_LIVING mode={mode} ticks=1240 moving={moving} gait={motion} gaze={looking} blink={blinking} reach={interacting} sleep={sleeping} head_over_bed={headOverBed} exact=true view_ms={poseMs:F4} lid_samples={lidSamples} lid_error_m={minimumLidError:F4}"));
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static Vector2 MeasureHeight(Vrm10Instance model)
    {
        var mesh = new Mesh();
        var bounds = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        foreach (var part in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            part.BakeMesh(mesh, useScale: true);
            foreach (var vertex in mesh.vertices)
            {
                float height = part.transform.TransformPoint(vertex).y;
                bounds.x = Mathf.Min(bounds.x, height);
                bounds.y = Mathf.Max(bounds.y, height);
            }
        }
        Object.Destroy(mesh);
        return bounds;
    }

    static void Capture(Camera camera, string name)
    {
        var target = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            string tag = Environment.GetEnvironmentVariable("DREAMER_AVATAR_CAPTURE_TAG");
            string file = string.IsNullOrEmpty(tag) ? name : name + "-" + tag;
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, file + ".png"), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.Destroy(target);
            Object.Destroy(image);
        }
    }

    static void CaptureDevelopment(Camera camera, SegmentedBody body, string stage)
    {
        Transform view = camera.transform;
        Vector3 savedPosition = view.position;
        Quaternion savedRotation = view.rotation;
        Vector3 middle = body.transform.position + Vector3.up * body.Dimensions.Height * .53f;
        try
        {
            view.position = middle + new Vector3(.48f, .27f, 1.17f);
            view.LookAt(middle);
            Capture(camera, "household-private-" + stage + "-detail");
        }
        finally
        {
            view.SetPositionAndRotation(savedPosition, savedRotation);
        }
    }

    [MenuItem("Dream Machine/Private Avatar Preview")]
    public static void Preview()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Merlin.vrm");
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Resources/DreamerLocalAvatars/Merlin.vrm");
        if (prefab == null) throw new InvalidOperationException("The private Merlin VRM has not imported.");
        var original = SceneManager.GetActiveScene();
        var preview = SceneManager.GetSceneByPath(PreviewScene);
        if (!preview.isLoaded)
        {
            preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(preview);
            var subject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            subject.name = "Merlin private preview";
            subject.transform.position = new Vector3(0f, 0f, 80f);
            foreach (var child in subject.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 30;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Preview ground";
            floor.layer = 30;
            floor.transform.position = new Vector3(0f, -.04f, 80f);
            floor.transform.localScale = new Vector3(4f, .08f, 4f);

            var key = new GameObject("Preview key").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.5f;
            key.cullingMask = 1 << 30;
            key.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

            var camera = new GameObject("Merlin preview camera").AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.15f, .21f, .27f);
            camera.orthographic = true;
            camera.orthographicSize = 1.08f;
            var center = new Vector3(0f, .96f, 80f);
            camera.transform.position = center + new Vector3(.3f, .15f, 3.2f);
            camera.transform.LookAt(center);
            EditorSceneManager.SaveScene(preview, PreviewScene);
            SceneManager.SetActiveScene(original);
        }
        Inspect(prefab, "Merlin");
    }

    internal static void Inspect(GameObject prefab, string label)
    {
        var animator = prefab.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
            throw new InvalidOperationException(label + " did not import a valid Unity humanoid.");
        var skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var materials = skins.SelectMany(skin => skin.sharedMaterials).Distinct().ToArray();
        if (skins.Length == 0 || skins.Any(skin => skin.sharedMesh == null || skin.bones.Length == 0) ||
            materials.Length == 0 || materials.Any(m => m == null || !m.shader.isSupported || m.mainTexture == null))
            throw new InvalidOperationException(label + " lacks skinned or supported textured rendering.");
        int mapped = 0;
        for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            if (animator.GetBoneTransform((HumanBodyBones)i) != null) mapped++;
        Debug.Log($"PRIVATE_AVATAR {label} skins={skins.Length} vertices={skins.Sum(skin => skin.sharedMesh.vertexCount)} " +
            $"materials={materials.Length} humanoid={mapped} shaders={string.Join(",", materials.Select(m => m.shader.name).Distinct())}");
    }
}
