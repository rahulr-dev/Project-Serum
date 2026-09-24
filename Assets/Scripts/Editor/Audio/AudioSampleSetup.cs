using Serum.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Serum.Editor.Audio
{
    /// <summary>Creates the starter sound assets and a few scene objects from the project's Audio folder.</summary>
    public static class AudioSampleSetup
    {
        const string AssetFolder = "Assets/Data/Audio";

        [MenuItem("Serum/Audio/Create Starter Audio Setup")]
        public static void CreateStarterAudioSetup()
        {
            EnsureFolder("Assets/Data", "Audio");
            SoundEvent footstep = CreateOrUpdateEvent(
                AssetFolder + "/Footstep_Default.asset",
                new[]
                {
                    "Assets/Audio/General/FootSteps/BaseFootstep1.mp3",
                    "Assets/Audio/General/FootSteps/footsteps1.mp3",
                    "Assets/Audio/General/FootSteps/footsteps2.mp3"
                }, 0.72f, 1f, 15f);

            CreateOrUpdateEvent(
                AssetFolder + "/Landing_Default.asset",
                new[]
                {
                    "Assets/Audio/General/Landing/Landing1.mp3",
                    "Assets/Audio/General/Landing/Landing2.mp3",
                    "Assets/Audio/General/Landing/Landing_inTemple1.mp3",
                    "Assets/Audio/General/Landing/Landing_inTemple2.mp3"
                }, 0.85f, 1f, 18f);

            GameObject manager = Object.FindFirstObjectByType<AudioManager>()?.gameObject;
            if (manager == null)
            {
                manager = new GameObject("Audio Manager");
                manager.AddComponent<AudioManager>();
                Undo.RegisterCreatedObjectUndo(manager, "Create Audio Manager");
            }

            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null && terrain.GetComponent<SurfaceType>() == null)
            {
                SurfaceType surface = terrain.gameObject.AddComponent<SurfaceType>();
                SerializedObject serializedSurface = new(surface);
                serializedSurface.FindProperty("footstepSound").objectReferenceValue = footstep;
                serializedSurface.ApplyModifiedPropertiesWithoutUndo();
                Undo.RegisterCreatedObjectUndo(surface, "Add Default Surface Type");
            }

            GameObject ambience = GameObject.Find("Audio Sample - Forest Ambience");
            if (ambience == null)
            {
                ambience = new GameObject("Audio Sample - Forest Ambience");
                ambience.transform.position = Vector3.zero;
                AmbienceEmitter emitter = ambience.AddComponent<AmbienceEmitter>();
                SerializedObject serializedEmitter = new(emitter);
                SerializedProperty clips = serializedEmitter.FindProperty("clips");
                clips.arraySize = 2;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambience/Forest Ambience 1.mp3");
                clips.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambience/Forest Ambience 2.mp3");
                serializedEmitter.ApplyModifiedPropertiesWithoutUndo();
                Undo.RegisterCreatedObjectUndo(ambience, "Create Forest Ambience");
            }

            Selection.activeObject = footstep;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Starter audio setup created. Add FootstepController to the player, assign Footstep_Default and Landing_Default, then add FootstepLeft/FootstepRight animation events.");
        }

        static SoundEvent CreateOrUpdateEvent(string path, string[] clipPaths, float volume, float minDistance, float maxDistance)
        {
            SoundEvent soundEvent = AssetDatabase.LoadAssetAtPath<SoundEvent>(path);
            if (soundEvent == null)
            {
                soundEvent = ScriptableObject.CreateInstance<SoundEvent>();
                AssetDatabase.CreateAsset(soundEvent, path);
            }

            SerializedObject serialized = new(soundEvent);
            SerializedProperty clips = serialized.FindProperty("clips");
            clips.arraySize = clipPaths.Length;
            for (int i = 0; i < clipPaths.Length; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPaths[i]);

            serialized.FindProperty("volume").floatValue = volume;
            serialized.FindProperty("minDistance").floatValue = minDistance;
            serialized.FindProperty("maxDistance").floatValue = maxDistance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(soundEvent);
            return soundEvent;
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
