using System.Linq;
using Serum.Audio;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Serum.Editor.Audio
{
    public static class AudioTestSceneSetup
    {
        const string Folder = "Assets/Data/AudioTest";
        public const string ScenePath = Folder + "/AudioTest.unity";

        [MenuItem("Serum/Audio/Open Audio Test Scene")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Create();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Data", "AudioTest");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                AudioSampleSetup.CreateStarterAudioSetup();
                // The test uses a continuous 2D bed, so listening position cannot hide it.
                var ambienceObject = scene.GetRootGameObjects().First(x => x.name == "Audio Sample - Forest Ambience");
                Object.DestroyImmediate(ambienceObject.GetComponent<AmbienceEmitter>());
                var ambience = ambienceObject.GetComponent<AudioSource>();
                ambience.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambience/Forest Ambience 1.mp3");
                ambience.loop = true;
                ambience.playOnAwake = true;
                ambience.spatialBlend = 0f;
                ambience.volume = 0.12f;

                var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"), scene);
                PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                player.name = "Audio Test Player - animated in place";
                player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (var script in player.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
                foreach (var listener in player.GetComponentsInChildren<AudioListener>(true)) Object.DestroyImmediate(listener);
                foreach (var collider in player.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                var animator = player.GetComponentInChildren<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                var left = player.GetComponentsInChildren<Transform>().First(x => x.name.EndsWith("LeftFoot"));
                var right = player.GetComponentsInChildren<Transform>().First(x => x.name.EndsWith("RightFoot"));
                var source = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Player/boy@Running.fbx").OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview__"));
                var clip = Object.Instantiate(source);
                clip.name = "AudioTest_Run";
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                // Sample each foot's lowest pose to seed contact events on the copied clip.
                float leftTime = 0f, rightTime = 0f, leftY = float.MaxValue, rightY = float.MaxValue;
                for (int i = 0; i < 120; i++)
                {
                    float time = clip.length * i / 120f;
                    clip.SampleAnimation(animator.gameObject, time);
                    if (left.position.y < leftY) { leftY = left.position.y; leftTime = time; }
                    if (right.position.y < rightY) { rightY = right.position.y; rightTime = time; }
                }
                AnimationUtility.SetAnimationEvents(clip, new[] {
                    new AnimationEvent { time = leftTime, functionName = "FootstepLeft" },
                    new AnimationEvent { time = rightTime, functionName = "FootstepRight" }
                }.OrderBy(x => x.time).ToArray());
                AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(Folder + "/AudioTest_Run.anim"));
                var controller = AnimatorController.CreateAnimatorControllerAtPath(AssetDatabase.GenerateUniqueAssetPath(Folder + "/AudioTest.controller"));
                controller.AddMotion(clip);
                animator.runtimeAnimatorController = controller;
                var footsteps = animator.gameObject.AddComponent<FootstepController>();
                var data = new SerializedObject(footsteps);
                data.FindProperty("leftFoot").objectReferenceValue = left;
                data.FindProperty("rightFoot").objectReferenceValue = right;
                data.FindProperty("defaultFootstep").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoundEvent>("Assets/Data/Audio/Footstep_Default.asset");
                data.FindProperty("landingSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoundEvent>("Assets/Data/Audio/Landing_Default.asset");
                data.ApplyModifiedPropertiesWithoutUndo();
                player.transform.position = Vector3.zero;

                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Test Floor";
                floor.transform.position = new Vector3(0, -0.1f, 0);
                floor.transform.localScale = new Vector3(10, 0.2f, 10);
                var surface = new SerializedObject(floor.AddComponent<SurfaceType>());
                surface.FindProperty("footstepSound").objectReferenceValue = data.FindProperty("defaultFootstep").objectReferenceValue;
                surface.ApplyModifiedPropertiesWithoutUndo();
                var camera = new GameObject("Test Camera", typeof(Camera), typeof(AudioListener));
                camera.transform.position = new Vector3(0, 1.6f, -4);
                camera.transform.LookAt(new Vector3(0, 1, 0));
                camera.GetComponent<Camera>().backgroundColor = new Color(0.15f, 0.18f, 0.22f);
                camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                var light = new GameObject("Test Light", typeof(Light));
                light.GetComponent<Light>().type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(40, -30, 0);
                var panel = new GameObject("Audio Test Controls").AddComponent<AudioTestPanel>();
                panel.character = animator;
                panel.footsteps = footsteps;
                panel.ambience = ambience;
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("Audio test ready: " + ScenePath + ". Open it and press Play. Contact times: " + leftTime + ", " + rightTime);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
