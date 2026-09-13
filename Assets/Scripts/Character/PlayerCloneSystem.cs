using System.Collections.Generic;
using Events;
using Game;
using InteractionSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Character
{
    public class PlayerCloneSystem : MonoBehaviour
    {
        [SerializeField] int maxClones = 0;
        [SerializeField] float spawnOffset = 1.2f;
        [SerializeField] SideScrollerCameraFollow cameraFollow;

        readonly List<PlayerCloneBody> _clones = new List<PlayerCloneBody>();

        Transform _original;
        SideScrollerController _originalMotor;
        Transform _possessed;
        SideScrollerController _possessedMotor;
        int _possessedIndex;
        Camera _worldCamera;
        bool _cameraWasChildOfOriginal;
        Vector3 _cameraLocalPos;
        Quaternion _cameraLocalRot;

        public int MaxClones => maxClones;
        public int CloneCount => _clones.Count;
        public bool IsPossessingClone => _possessedIndex > 0;
        public string PossessedName =>
            _possessedIndex <= 0 ? "Player" : "Clone " + _possessedIndex;

        public const string OverlayPrefsKey = "Serum.CloneOverlay.Enabled";

        void Awake()
        {
            _original = transform;
            _originalMotor = GetComponent<SideScrollerController>();
            _possessed = _original;
            _possessedMotor = _originalMotor;
            _possessedIndex = 0;

            if (cameraFollow == null)
                cameraFollow = FindFirstObjectByType<SideScrollerCameraFollow>();

            _worldCamera = Camera.main;
            _cameraWasChildOfOriginal = _worldCamera != null && _worldCamera.transform.IsChildOf(transform);
            if (_worldCamera != null)
            {
                _cameraLocalPos = _worldCamera.transform.localPosition;
                _cameraLocalRot = _worldCamera.transform.localRotation;
            }
        }

        void Update()
        {
            if (!AllowsGameplayInput())
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.qKey.wasPressedThisFrame)
                SpawnClone();

            if (keyboard.tabKey.wasPressedThisFrame)
                CyclePossessed();

            if (keyboard.fKey.wasPressedThisFrame)
                KillPossessedClone();
        }

        public void SetMaxClones(int value)
        {
            maxClones = Mathf.Max(0, value);
        }

        public void AddMaxClones(int amount)
        {
            SetMaxClones(maxClones + amount);
        }

        public void SpawnClone()
        {
            if (!AllowsGameplayInput())
                return;

            PruneDead();
            if (_clones.Count >= maxClones)
                return;

            Transform source = _possessed != null ? _possessed : _original;
            SideScrollerController sourceMotor = source.GetComponent<SideScrollerController>();
            float facing = sourceMotor != null ? sourceMotor.FacingSign : 1f;
            Vector3 spawnPos = source.position + new Vector3(facing * spawnOffset, 0f, 0f);

            GameObject holder = new GameObject("PlayerCloneSpawnHolder");
            holder.SetActive(false);
            GameObject cloneGo = Instantiate(gameObject, holder.transform, true);
            cloneGo.name = "PlayerClone";
            StripClone(cloneGo);

            PlayerCloneBody body = cloneGo.GetComponent<PlayerCloneBody>();
            if (body == null)
                body = cloneGo.AddComponent<PlayerCloneBody>();

            cloneGo.transform.SetParent(null, true);
            Destroy(holder);

            cloneGo.transform.SetPositionAndRotation(spawnPos, source.rotation);
            cloneGo.SetActive(true);

            SideScrollerController motor = cloneGo.GetComponent<SideScrollerController>();
            if (motor != null)
                motor.SetLocomotionEnabled(false);

            _clones.Add(body);
        }

        public void CyclePossessed()
        {
            if (!AllowsGameplayInput())
                return;

            PruneDead();
            int bodyCount = 1 + _clones.Count;
            if (bodyCount <= 1)
                return;

            Possess((_possessedIndex + 1) % bodyCount);
        }

        public void KillPossessedClone()
        {
            if (!AllowsGameplayInput() || _possessedIndex <= 0)
                return;

            PruneDead();
            int cloneIndex = _possessedIndex - 1;
            if (cloneIndex < 0 || cloneIndex >= _clones.Count)
                return;

            PlayerCloneBody body = _clones[cloneIndex];
            _clones.RemoveAt(cloneIndex);
            if (body != null)
                body.Kill();

            Possess(0);
        }

        void Possess(int bodyIndex)
        {
            int bodyCount = 1 + _clones.Count;
            if (bodyCount <= 0)
                return;

            bodyIndex = Mathf.Clamp(bodyIndex, 0, bodyCount - 1);

            if (_possessedMotor != null)
                _possessedMotor.SetLocomotionEnabled(false);

            if (bodyIndex == 0)
            {
                _possessed = _original;
                _possessedMotor = _originalMotor;
            }
            else
            {
                PlayerCloneBody body = _clones[bodyIndex - 1];
                _possessed = body != null ? body.transform : _original;
                _possessedMotor = _possessed != null
                    ? _possessed.GetComponent<SideScrollerController>()
                    : _originalMotor;
                if (_possessed == _original)
                    bodyIndex = 0;
            }

            _possessedIndex = bodyIndex;
            if (_possessedMotor != null)
                _possessedMotor.SetLocomotionEnabled(true);

            RetargetCamera(_possessed);
        }

        void StripClone(GameObject cloneGo)
        {
            PlayerCloneSystem cloneSystem = cloneGo.GetComponent<PlayerCloneSystem>();
            if (cloneSystem != null)
                DestroyImmediate(cloneSystem);

            PlayerInteractor interactor = cloneGo.GetComponent<PlayerInteractor>();
            if (interactor != null)
                DestroyImmediate(interactor);

            SerumActionBridge bridge = cloneGo.GetComponent<SerumActionBridge>();
            if (bridge != null)
                DestroyImmediate(bridge);

            StripClonedCameras(cloneGo);
        }

        void StripClonedCameras(GameObject cloneGo)
        {
            Camera[] cams = cloneGo.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cams.Length; i++)
            {
                Camera cam = cams[i];
                if (cam == null)
                    continue;

                GameObject camGo = cam.gameObject;
                if (camGo == cloneGo)
                {
                    DestroyImmediate(cam);
                    AudioListener listener = cloneGo.GetComponent<AudioListener>();
                    if (listener != null)
                        DestroyImmediate(listener);
                    continue;
                }

                DestroyImmediate(camGo);
            }
        }

        void RetargetCamera(Transform body)
        {
            if (body == null)
                return;

            if (cameraFollow == null)
                cameraFollow = FindFirstObjectByType<SideScrollerCameraFollow>();

            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(body);
                return;
            }

            if (!_cameraWasChildOfOriginal || _worldCamera == null)
                return;

            _worldCamera.transform.SetParent(body, false);
            _worldCamera.transform.localPosition = _cameraLocalPos;
            _worldCamera.transform.localRotation = _cameraLocalRot;
        }

        void PruneDead()
        {
            bool lostPossessed = false;
            for (int i = _clones.Count - 1; i >= 0; i--)
            {
                if (_clones[i] != null && !_clones[i].IsDead)
                    continue;

                if (_possessedIndex == i + 1)
                    lostPossessed = true;

                _clones.RemoveAt(i);
                if (_possessedIndex > i + 1)
                    _possessedIndex--;
            }

            if (lostPossessed || _possessed == null)
                Possess(0);
        }

        static bool AllowsGameplayInput()
        {
            return GameStateManager.Instance == null || GameStateManager.Instance.AllowsMove;
        }

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect = new Rect(12f, 220f, 240f, 280f);
        bool _overlayRectInit;
        GUIStyle _overlayKeyStyle;

        void OnGUI()
        {
            if (!UnityEditor.EditorPrefs.GetBool(OverlayPrefsKey, false))
                return;

            if (!_overlayRectInit)
            {
                _overlayRect = new Rect(12f, 220f, 240f, 280f);
                _overlayRectInit = true;
            }

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, "Clone Overlay");
        }

        void DrawOverlay(int windowId)
        {
            DrawRow($"Max  {maxClones}", maxClones > 0, 220f);
            DrawRow($"Alive  {CloneCount} / {maxClones}", CloneCount > 0, 220f);
            DrawRow($"Possessed  {PossessedName}", IsPossessingClone, 220f);
            DrawRow("Q  spawn   Tab  switch   F  kill", false, 220f);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Max +1"))
                AddMaxClones(1);
            if (GUILayout.Button("Max -1"))
                AddMaxClones(-1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn"))
                SpawnClone();
            if (GUILayout.Button("Switch"))
                CyclePossessed();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Kill possessed clone"))
                KillPossessedClone();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        GUIStyle OverlayKeyStyle
        {
            get
            {
                if (_overlayKeyStyle == null)
                {
                    _overlayKeyStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 12
                    };
                    _overlayKeyStyle.normal.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.hover.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.active.background = Texture2D.whiteTexture;
                }

                return _overlayKeyStyle;
            }
        }

        void DrawRow(string label, bool active, float width)
        {
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? OverlayActiveBg : OverlayIdleBg;
            OverlayKeyStyle.normal.textColor = active ? Color.black : OverlayIdleText;
            OverlayKeyStyle.hover.textColor = OverlayKeyStyle.normal.textColor;
            OverlayKeyStyle.active.textColor = OverlayKeyStyle.normal.textColor;
            GUILayout.Box(label, OverlayKeyStyle, GUILayout.Width(width), GUILayout.Height(22f));
            GUI.backgroundColor = previousBg;
        }
#endif
    }
}
