using System;
using Character;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    /// <summary>
    /// Stores the most recently activated checkpoint and restores the player there
    /// whenever that scene is loaded again.
    /// </summary>
    public sealed class GameProgressManager : MonoBehaviour
    {
        const string SaveKeyPrefix = "Serum.GameProgress.Checkpoint.";

        public static GameProgressManager Instance { get; private set; }

        /// <summary>Raised after Save() has written the player's location.</summary>
        public static event Action OnLocationSaved;

        [Header("Runtime References")]
        [SerializeField] SideScrollerController player;

        [Header("Saved Progress")]
        [SerializeField] Vector3 savedLocation;
        [SerializeField] string savedScene;

        public bool HasCheckpoint => savedLocation != Vector3.zero;
        public string CheckpointScene => savedScene;
        public Vector3 CheckpointPosition => savedLocation;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSavedProgress();
            FindPlayer();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void Start()
        {
            FindPlayer();
            MovePlayerToSavedLocation();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Saves a world-space location in the active scene as the latest checkpoint.</summary>
        public void SaveCheckpoint(Vector3 position)
        {
            if (position == Vector3.zero)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                Debug.LogWarning("GameProgressManager: unable to save a checkpoint without an active scene.", this);
                return;
            }

            savedScene = activeScene.name;
            savedLocation = position;
            PlayerPrefs.SetString(SaveKeyPrefix + "Scene", savedScene);
            PlayerPrefs.SetFloat(SaveKeyPrefix + "X", savedLocation.x);
            PlayerPrefs.SetFloat(SaveKeyPrefix + "Y", savedLocation.y);
            PlayerPrefs.SetFloat(SaveKeyPrefix + "Z", savedLocation.z);
            PlayerPrefs.Save();

        }

        /// <summary>Saves the current player location. Intended for existing trigger/event code.</summary>
        public void Save()
        {
            FindPlayer();
            if (player == null)
            {
                Debug.LogWarning("GameProgressManager: no SideScrollerController was found to save.", this);
                return;
            }

            SaveCheckpoint(player.transform.position);
            OnLocationSaved?.Invoke();
        }

        public void ClearCheckpoint()
        {
            ClearSavedProgress();
        }

        public static void ClearSavedProgress()
        {
            PlayerPrefs.DeleteKey(SaveKeyPrefix + "Scene");
            PlayerPrefs.DeleteKey(SaveKeyPrefix + "X");
            PlayerPrefs.DeleteKey(SaveKeyPrefix + "Y");
            PlayerPrefs.DeleteKey(SaveKeyPrefix + "Z");
            PlayerPrefs.Save();

            if (Instance != null)
            {
                Instance.savedLocation = Vector3.zero;
                Instance.savedScene = string.Empty;
            }
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindPlayer();
            MovePlayerToSavedLocation();
        }

        public void MovePlayerToSavedLocation()
        {
            if (!HasCheckpoint || CheckpointScene != SceneManager.GetActiveScene().name)
                return;

            FindPlayer();
            if (player == null)
            {
                Debug.LogWarning("GameProgressManager: no SideScrollerController was found to restore.", this);
                return;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
                controller.enabled = false;

            player.transform.position = CheckpointPosition;

            if (controllerWasEnabled)
                controller.enabled = true;
        }

        void LoadSavedProgress()
        {
            savedScene = PlayerPrefs.GetString(SaveKeyPrefix + "Scene", string.Empty);
            savedLocation = new Vector3(
                PlayerPrefs.GetFloat(SaveKeyPrefix + "X"),
                PlayerPrefs.GetFloat(SaveKeyPrefix + "Y"),
                PlayerPrefs.GetFloat(SaveKeyPrefix + "Z"));

            if (savedLocation == Vector3.zero)
                savedScene = string.Empty;
        }

        void FindPlayer()
        {
            if (player == null)
                player = FindFirstObjectByType<SideScrollerController>();
        }
    }
}
