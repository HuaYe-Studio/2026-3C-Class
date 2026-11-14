using System.IO;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace ThreeCDemo
{
    public static class BuildTrainingLab
    {
        private const string Root = "Assets/ThreeCDemo";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Gameplay 3C/Create Training Lab")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SampleScene";

            Material floor = Material("Deck", 0x20374a);
            Material edge = Material("Structure", 0x142232);
            Material panel = Material("Panels", 0x365366);
            Material stripe = Material("Guide", 0x436878);
            Material teal = Material("Signal", 0x29dacf, true);
            Material orange = Material("Target", 0xff7b3d);
            Material pulseMaterial = Material("Pulse", 0xffffff);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.54f, .66f, .78f);
            RenderSettings.ambientEquatorColor = new Color(.27f, .37f, .51f);
            RenderSettings.ambientGroundColor = new Color(.18f, .22f, .32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.09f, .15f, .23f);
            RenderSettings.fogDensity = .024f;

            var sun = new GameObject("Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1, .93f, .85f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);

            Transform arena = new GameObject("Training Deck").transform;
            Box(arena, "Foundation", V(0, -.9f, 0), V(24, 1.4f, 24), edge, true);
            Box(arena, "Arena floor", V(0, -.18f, 0), V(22, .36f, 22), floor, true);
            Box(arena, "Distant ground", V(0, -2, 0), V(120, 1, 120), edge);
            for (int i = -10; i <= 10; i += 2)
            {
                Box(arena, "Deck seam", V(i, .006f, 0), V(.018f, .01f, 21), stripe);
                Box(arena, "Deck seam", V(0, .007f, i), V(21, .01f, .018f), stripe);
            }
            for (int i = -1; i <= 1; i += 2)
            {
                Box(arena, "Perimeter", V(i * 10.7f, .27f, 0), V(.5f, .54f, 22), panel, true);
                Box(arena, "Perimeter", V(0, .27f, i * 10.7f), V(22, .54f, .5f), panel, true);
                Box(arena, "Perimeter marker", V(i * 10.69f, .55f, 0), V(.07f, .025f, 20), teal);
                Box(arena, "Perimeter marker", V(0, .55f, i * 10.69f), V(20, .025f, .07f), teal);
                for (int z = -8; z <= 8; z += 8)
                {
                    Box(arena, "Pylon", V(i * 9.6f, .8f, z), V(.75f, 1.6f, .75f), edge, true);
                    Box(arena, "Pylon cap", V(i * 9.6f, 1.62f, z), V(.78f, .09f, .78f), teal);
                }
            }
            for (int z = -4; z <= 0; z++)
                Box(arena, "Approach marker", V(0, .018f, z), V(.13f, .015f, .4f), teal);
            for (int i = 0; i < 9; i++)
            {
                float x = -22 + i * 5.5f;
                float h = 2.6f + (i % 3) * 1.25f;
                Box(arena, "Backdrop block", V(x, h / 2 - 1, 17 + i % 2 * 3), V(3.8f, h, 3), panel);
            }

            Transform dummyRoot = new GameObject("Ground Training Target").transform;
            dummyRoot.position = V(0, 0, 2);
            Ring(dummyRoot, "Target perimeter", V(0, .026f, 0), 1.45f, .045f, orange);
            Ring(dummyRoot, "Target inner ring", V(0, .028f, 0), .87f, .025f, stripe);
            var targetCollider = dummyRoot.gameObject.AddComponent<BoxCollider>();
            targetCollider.center = V(0, .83f, 0);
            targetCollider.size = V(.48f, 1.25f, .5f);
            var dummy = dummyRoot.gameObject.AddComponent<TrainingDummy>();

            Transform player = new GameObject("Player").transform;
            player.position = V(0, 0, -4.8f);
            var capsule = player.gameObject.AddComponent<CharacterController>();
            capsule.center = V(0, .94f, 0);
            capsule.height = 1.8f;
            capsule.radius = .36f;
            capsule.stepOffset = 0;
            capsule.minMoveDistance = 0;
            var reader = player.gameObject.AddComponent<PlayerInputReader>();
            Assign(reader, "actions", CreateActions());
            Box(player, "Direction marker", V(0, .045f, .61f), V(.13f, .05f, .35f), teal);

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.gameObject.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            var brain = camera.gameObject.AddComponent<CinemachineBrain>();
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, .65f);

            Transform aim = new GameObject("Camera Look Target").transform;
            aim.SetParent(player, false);
            aim.localPosition = V(0, 1.05f, 0);
            var follow = new GameObject("VCam - Follow").AddComponent<CinemachineVirtualCamera>();
            follow.Priority = 10;
            follow.Follow = player;
            follow.LookAt = aim;
            follow.m_Lens.FieldOfView = 49;
            var body = follow.AddCinemachineComponent<CinemachineTransposer>();
            body.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
            body.m_FollowOffset = V(3.8f, 5.2f, -7.5f);
            body.m_XDamping = .3f;
            body.m_YDamping = .25f;
            body.m_ZDamping = .3f;
            var composer = follow.AddCinemachineComponent<CinemachineComposer>();
            composer.m_ScreenX = .49f;
            composer.m_ScreenY = .61f;
            composer.m_DeadZoneWidth = 0;
            composer.m_DeadZoneHeight = 0;
            follow.transform.SetPositionAndRotation(player.position + body.m_FollowOffset,
                Quaternion.LookRotation(aim.position - player.position - body.m_FollowOffset));
            camera.transform.SetPositionAndRotation(follow.transform.position, follow.transform.rotation);

            var overview = new GameObject("VCam - Overview").AddComponent<CinemachineVirtualCamera>();
            overview.Priority = 5;
            overview.m_Lens.FieldOfView = 43;
            overview.transform.position = V(0, 24, -12);
            overview.transform.LookAt(V(0, 0, 0));

            Transform pulse = Ring(null, "Attack range effect", V(0, .04f, 0), .5f, .035f, pulseMaterial);
            pulse.gameObject.SetActive(false);
            var controller = player.gameObject.AddComponent<PlayerController>();
            Assign(controller, "target", dummy);
            Assign(controller, "followCamera", follow);
            Assign(controller, "overviewCamera", overview);
            Assign(controller, "pulse", pulse);
            Assign(controller, "pulseRenderer", pulse.GetComponent<Renderer>());
            InstallModels(controller, dummy);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Gameplay 3C/Update Training Models")]
        public static void UpdateModels()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Open SampleScene in Edit Mode first.");

            var player = Object.FindObjectOfType<PlayerController>();
            var dummy = Object.FindObjectOfType<TrainingDummy>();
            InstallModels(player, dummy);
            RemoveParts(player.transform, "Torso", "Shoulder housing", "Left boot", "Right boot",
                "Helmet", "Forward visor", "Back light");
            RemoveParts(dummy.transform, "Target pedestal", "Target impact plate", "Target center", "Target beacon");
            var frame = dummy.transform.Find("Target frame");
            if (frame != null)
            {
                var collider = dummy.gameObject.AddComponent<BoxCollider>();
                collider.center = frame.localPosition;
                collider.size = frame.localScale;
                Object.DestroyImmediate(frame.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveParts(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                var part = root.Find(name);
                if (part != null) Object.DestroyImmediate(part.gameObject);
            }
        }

        private static void InstallModels(PlayerController player, TrainingDummy dummy)
        {
            Material("Ceramic", 0xc6e1e8);
            Assign(player, "visor", InstallModel(player.transform, "TrainingRobot", "Visor_FX", 1));
            Assign(dummy, "indicator", InstallModel(dummy.transform, "GroundTarget", "ImpactCore_FX", -1));
        }

        private static Renderer InstallModel(Transform root, string model, string indicator, int frontSign)
        {
            string path = Root + "/Models/" + model + ".fbx";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new System.IO.FileNotFoundException(path);
            RemoveParts(root, model);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
            instance.name = model;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            Renderer effect = null;
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i].name;
                    var mapped = AssetDatabase.LoadAssetAtPath<Material>(Root + "/" + materialName + ".mat");
                    if (mapped == null) throw new System.InvalidOperationException("Missing material " + materialName);
                    materials[i] = mapped;
                }
                renderer.sharedMaterials = materials;
                if (renderer.name == indicator) effect = renderer;
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Debug.Log($"{model}/{renderer.name}: {mesh.triangles.Length / 3} triangles, {renderer.bounds.size}");
            }
            if (effect == null) throw new System.InvalidOperationException("Missing effect mesh " + indicator);
            if (root.InverseTransformPoint(effect.bounds.center).z * frontSign < 0)
                instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            return effect;
        }

        private static InputActionAsset CreateActions()
        {
            const string path = Root + "/Training.inputactions";
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Player");
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            map.AddAction("Skill", InputActionType.Button, "<Keyboard>/e");
            map.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            map.AddAction("Dodge", InputActionType.Button, "<Keyboard>/leftShift");
            map.AddAction("ChargeAttack", InputActionType.Button, "<Keyboard>/q");
            map.AddAction("Camera", InputActionType.Button, "<Keyboard>/c");
            map.AddAction("Reset", InputActionType.Button, "<Keyboard>/r");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, asset.ToJson());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            Object.DestroyImmediate(asset);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        private static void Assign(Object component, string name, Object value)
        {
            var data = new SerializedObject(component);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private static Renderer Box(Transform parent, string name, Vector3 pos, Vector3 size, Material material, bool solid = false)
            => Part(parent, name, PrimitiveType.Cube, pos, size, material, solid);

        private static Renderer Part(Transform parent, string name, PrimitiveType type, Vector3 pos,
            Vector3 size, Material material, bool solid = false)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = pos;
            part.transform.localScale = size;
            if (!solid) Object.DestroyImmediate(part.GetComponent<Collider>());
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Transform Ring(Transform parent, string name, Vector3 pos, float radius, float thickness, Material material)
        {
            string path = Root + "/" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                const int sides = 64;
                var vertices = new Vector3[(sides + 1) * 2];
                var indices = new int[sides * 6];
                for (int i = 0; i <= sides; i++)
                {
                    float angle = i * (2 * Mathf.PI / sides);
                    Vector3 direction = V(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    vertices[2 * i] = direction * radius;
                    vertices[2 * i + 1] = direction * (radius - thickness);
                    if (i == sides) continue;
                    int t = i * 6;
                    int v = i * 2;
                    indices[t] = v;
                    indices[t + 1] = v + 1;
                    indices[t + 2] = v + 2;
                    indices[t + 3] = v + 1;
                    indices[t + 4] = v + 3;
                    indices[t + 5] = v + 2;
                }
                mesh = new Mesh { name = name, vertices = vertices, triangles = indices };
                mesh.RecalculateNormals();
                AssetDatabase.CreateAsset(mesh, path);
            }
            var ring = new GameObject(name);
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = pos;
            ring.AddComponent<MeshFilter>().sharedMesh = mesh;
            ring.AddComponent<MeshRenderer>().sharedMaterial = material;
            return ring.transform;
        }

        private static Material Material(string name, uint rgb, bool emissive = false)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            var color = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .26f);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", (Color)color * .5f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
