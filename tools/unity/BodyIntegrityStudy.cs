using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamMachine.Body;
using DreamMachine.Social;
using DreamMachine.Sim;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class BodyIntegrityStudy
{
    public static void Run()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Read-only body audit");
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var material = new Material(shader);
        var light = new GameObject("Pose inspection light", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(30f, -25f, 0f);
        var camera = new GameObject("Diagnostic camera", typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.19f, .24f, .30f);
        var target = new RenderTexture(960, 720, 24);
        camera.targetTexture = target;
        var image = new Texture2D(960, 720, TextureFormat.RGB24, false);
        var original = RenderTexture.active;
        using var rows = File.CreateText(Path.Combine(Application.persistentDataPath, "phase-c14-body-integrity.csv"));
        rows.WriteLine("individual,age,pose,visual_source,pair_a,pair_b,proxy_depth_mm,above_2mm,wrist_mapping_error_mm,private_skinned_renderers,cloth_components");
        try
        {
            var household = PopulationConfig.CreateDefault();
            foreach (var look in AppearanceCatalog.Defaults(household))
            {
                float age = look.Id == "son" ? WorldSimulation.AgeOf(LifeStage.Toddler) :
                    look.Id == "daughter" ? 0f : HumanoidAnatomy.AdultAgeDays;
                var seed = household.Find(look.Id).BirthSeed;
                var body = CharacterFactory.Build(root.transform, look, material, new HumanoidAnatomy(seed), age);
                foreach (var text in body.GetComponentsInChildren<TextMesh>()) text.gameObject.SetActive(false);
                typeof(SegmentedBody).GetMethod("ShowEarlyClothes", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(body, new object[] { true });
                var type = typeof(SegmentedBody).Assembly.GetType("DreamMachine.UnityBridge.HouseholdAvatar", throwOnError: true);
                var avatar = look.IsBaby ? null : type.GetMethod("Load").Invoke(null, new object[] { body });
                if (avatar != null) type.GetMethod("SetVisible").Invoke(avatar, new object[] { true });
                string[] poses = { "Neutral", "Sitting", "Kneeling", "Supine", "Prone", "CrossedArms", "CrossReach", "TableReach",
                    "ElbowFlexion", "KneeFlexion", "HandToFace", "SupportedGround", "BalanceLoss" };
                foreach (var pose in poses)
                {
                    Apply(body, pose);
                    if (avatar != null)
                        type.GetMethod("Follow").Invoke(avatar, new object[] { null, null, pose == "Sitting", pose == "Supine", false, 0f, .05f });
                    int renderers = body.GetComponentsInChildren<SkinnedMeshRenderer>().Length;
                    var animator = body.GetComponentInChildren<Animator>();
                    var wrist = animator == null ? null : animator.GetBoneTransform(HumanBodyBones.RightHand);
                    float mapping = wrist == null ? float.NaN : Vector3.Distance(wrist.position, body.RigPivot("rightWrist").position) * 1000f;
                    int cloth = body.GetComponentsInChildren<Cloth>().Length;
                    var proxies = Proxies(body);
                    foreach (var (a, b) in new[]
                    {
                        (BodySegment.LeftHand, BodySegment.Torso), (BodySegment.RightHand, BodySegment.Torso),
                        (BodySegment.LeftForearm, BodySegment.Torso), (BodySegment.RightForearm, BodySegment.Torso),
                        (BodySegment.LeftForearm, BodySegment.RightForearm), (BodySegment.LeftThigh, BodySegment.RightThigh),
                        (BodySegment.LeftShin, BodySegment.RightShin), (BodySegment.LeftFoot, BodySegment.RightFoot),
                        (BodySegment.RightHand, BodySegment.Head), (BodySegment.Head, BodySegment.Torso)
                    })
                    {
                        var x = proxies[a]; var y = proxies[b];
                        bool hit = Physics.ComputePenetration(x, x.transform.position, x.transform.rotation,
                            y, y.transform.position, y.transform.rotation, out _, out float depth);
                        if (!hit) depth = 0f;
                        rows.WriteLine(FormattableString.Invariant($"{look.Id},{age},{pose},{(avatar == null ? "procedural" : "skinned")},{a},{b},{1000f*depth:F3},{depth > AnatomyContacts.DiagnosticDepth(x.contactOffset,y.contactOffset)},{mapping:F3},{renderers},{cloth}"));
                    }
                    foreach (var collider in proxies.Values) Object.DestroyImmediate(collider.gameObject);
                    if ((look.Id == "father" || look.Id == "son") &&
                        (pose == "Neutral" || pose == "Sitting" || pose == "CrossReach" || pose == "Prone"))
                    {
                        var focus = Vector3.up * (body.Dimensions.Height * .52f);
                        camera.transform.position = focus + new Vector3(.77f, .24f, 1.52f) * body.Dimensions.Height;
                        camera.transform.LookAt(focus);
                        RenderPose(camera, body);
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                        image.Apply();
                        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, $"phase-c14-body-{look.Id}-{pose}.png"), image.EncodeToPNG());
                    }
                }
                Object.DestroyImmediate(body.gameObject);
            }
            Debug.Log("BODY_C14 measured 52 imposed diagnostic poses");
        }
        finally
        {
            RenderTexture.active = original;
            camera.targetTexture = null;
            Object.DestroyImmediate(light.gameObject);
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(material);
        }
    }

    static void RenderPose(Camera camera, SegmentedBody body)
    {
        var snapshots = new List<(SkinnedMeshRenderer Source, Mesh Mesh, GameObject View)>();
        try
        {
            foreach (var source in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                source.BakeMesh(mesh);
                var view = new GameObject("Pose-matched baked skin");
                view.transform.SetParent(source.transform, false);
                view.AddComponent<MeshFilter>().sharedMesh = mesh;
                view.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
                snapshots.Add((source, mesh, view));
                source.forceRenderingOff = true;
            }
            camera.Render();
        }
        finally
        {
            foreach (var (source, mesh, view) in snapshots)
            {
                source.forceRenderingOff = false;
                Object.DestroyImmediate(view);
                Object.DestroyImmediate(mesh);
            }
        }
    }

    static void Apply(SegmentedBody body, string pose)
    {
        body.ApplyGroundedGait(0f, 0f, 0f);
        foreach (string joint in new[] { "leftShoulder", "rightShoulder", "leftElbow", "rightElbow", "leftWrist", "rightWrist" })
            body.RigPivot(joint).localRotation = Quaternion.identity;
        var d = body.Dimensions;
        var pelvis = body.RigPivot("pelvis");
        var spin = pelvis.parent;
        spin.localRotation = Quaternion.identity;
        pelvis.localRotation = Quaternion.identity;
        void Set(string name, float x, float y = 0f, float z = 0f) => body.RigPivot(name).localRotation = Quaternion.Euler(x, y, z);
        void Reach(float x, float y, float z)
        {
            var shoulder = body.RigPivot("rightShoulder");
            Vector3 aim = shoulder.parent.InverseTransformDirection(new Vector3(x, y, z) * d.Height - shoulder.position);
            shoulder.localRotation = Quaternion.FromToRotation(Vector3.down, aim);
        }
        if (pose == "Sitting" || pose == "Kneeling" || pose == "KneeFlexion" || pose == "SupportedGround")
        {
            foreach (string side in new[] { "left", "right" })
            {
                Set(side + "Hip", pose == "Kneeling" ? 8 : -75);
                Set(side + "Knee", pose == "Kneeling" ? 110 : 92);
            }
            pelvis.localPosition = Vector3.up * (pose == "Kneeling" ? d.Thigh * .91f : d.Shin + d.AnkleHeight);
        }
        switch (pose)
        {
            case "Supine": pelvis.localRotation = Quaternion.Euler(-90f, 0, 0); pelvis.localPosition = Vector3.up * d.Head * .45f; break;
            case "Prone":
            case "SupportedGround": pelvis.localRotation = Quaternion.Euler(85f, 0, 0); pelvis.localPosition = Vector3.up * d.Head * .7f; break;
            case "CrossedArms": Set("leftShoulder", -22, 0, 71); Set("rightShoulder", -22, 0, -71); Set("leftElbow", -75); Set("rightElbow", -75); break;
            case "CrossReach": Reach(-.22f, .56f, .16f); Set("rightElbow", -20); break;
            case "TableReach": Reach(.12f, .44f, .31f); Set("rightElbow", -23); break;
            case "ElbowFlexion": Set("rightElbow", -139); break;
            case "HandToFace": Reach(.06f, .9f, .13f); Set("rightElbow", -60); break;
            case "BalanceLoss": spin.localRotation = Quaternion.Euler(12, 0, 30); Set("rightHip", -32); Set("rightKnee", 60); break;
        }
    }

    static Dictionary<BodySegment, CapsuleCollider> Proxies(SegmentedBody body)
    {
        var result = new Dictionary<BodySegment, CapsuleCollider>();
        var d = body.Dimensions;
        Vector3 Point(string joint) => body.RigPivot(joint).position;
        void Capsule(BodySegment key, Vector3 start, Vector3 finish, float radius)
        {
            var go = new GameObject(key.ToString());
            Vector3 delta = finish - start;
            go.transform.SetPositionAndRotation((start + finish) * .5f,
                delta.sqrMagnitude > 1e-8f ? Quaternion.FromToRotation(Vector3.up, delta) : Quaternion.identity);
            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = radius;
            collider.height = delta.magnitude + 2f * radius;
            collider.contactOffset = .001f;
            result.Add(key, collider);
        }
        Capsule(BodySegment.Torso, Vector3.Lerp(Point("pelvis"), Point("chest"), .23f),
            Vector3.Lerp(Point("pelvis"), Point("chest"), .82f), d.HipWidth * .30f);
        Capsule(BodySegment.Head, Point("head"), Point("head"), d.Head * .46f);
        for (int side = 0; side < 2; side++)
        {
            string name = side == 0 ? "left" : "right";
            Vector3 wrist = Point(name + "Wrist");
            Vector3 grip = Point(name + "Grip");
            Vector3 knee = Point(name + "Knee");
            Vector3 ankle = Point(name + "Ankle");
            Transform foot = body.RigPivot(name + "Foot");
            Capsule(side == 0 ? BodySegment.LeftForearm : BodySegment.RightForearm,
                Vector3.Lerp(Point(name + "Elbow"), wrist, .14f), Vector3.Lerp(Point(name + "Elbow"), wrist, .86f), d.ShoulderWidth * .09f);
            Capsule(side == 0 ? BodySegment.LeftHand : BodySegment.RightHand, grip, grip, d.Hand * .27f);
            Capsule(side == 0 ? BodySegment.LeftThigh : BodySegment.RightThigh,
                Vector3.Lerp(Point(name + "Hip"), knee, .17f), Vector3.Lerp(Point(name + "Hip"), knee, .83f), d.HipWidth * .24f);
            Capsule(side == 0 ? BodySegment.LeftShin : BodySegment.RightShin, Vector3.Lerp(knee, ankle, .15f),
                Vector3.Lerp(knee, ankle, .85f), d.HipWidth * .17f);
            Capsule(side == 0 ? BodySegment.LeftFoot : BodySegment.RightFoot,
                ankle - foot.forward * d.Foot * .10f, ankle + foot.forward * d.Foot * .43f, d.Foot * .20f);
        }
        return result;
    }
}
