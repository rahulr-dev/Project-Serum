using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using Game;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
    public class SerumActionBridge : MonoBehaviour
    {
        [Serializable]
        class NamedUnityEvent
        {
            public string id;
            public UnityEvent onRaised = new UnityEvent();
        }

        [Header("Transform")]
        [SerializeField] float defaultMoveSpeed = 4f;
        [SerializeField] float defaultRotateSpeed = 360f;
        [SerializeField] float faceLeftYaw = -90f;
        [SerializeField] float faceRightYaw = 90f;

        [Header("Optional Player")]
        [SerializeField] CharacterAnimation characterAnimation;
        [SerializeField] SideScrollerController locomotion;
        [SerializeField] bool forwardControllerEvents = true;
        [SerializeField] float defaultRunSpeed = 4f;
        [SerializeField] bool clearAnimOnRunComplete = true;
        [SerializeField] bool reenableLocomotionOnRunComplete = true;

        [Header("Named Events")]
        [SerializeField] List<NamedUnityEvent> namedEvents = new List<NamedUnityEvent>
        {
            new NamedUnityEvent { id = "Jumped" },
            new NamedUnityEvent { id = "Landed" },
            new NamedUnityEvent { id = "DodgeStart" },
            new NamedUnityEvent { id = "ScriptedRunStarted" },
            new NamedUnityEvent { id = "ScriptedRunEnded" },
        };

        Transform _transform;
        Coroutine _moveRoutine;
        Coroutine _rotateRoutine;

        void Awake()
        {
            _transform = transform;

            if (characterAnimation == null)
                characterAnimation = GetComponent<CharacterAnimation>();

            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();
        }

        void OnEnable()
        {
            if (!forwardControllerEvents || locomotion == null)
                return;

            locomotion.OnJumped += HandleJumped;
            locomotion.OnLanded += HandleLanded;
            locomotion.OnMovingChanged += HandleMovingChanged;
            locomotion.OnScriptedRunStarted += HandleScriptedRunStarted;
            locomotion.OnScriptedRunCompleted += HandleScriptedRunCompleted;
        }

        void OnDisable()
        {
            if (locomotion == null)
                return;

            locomotion.OnJumped -= HandleJumped;
            locomotion.OnLanded -= HandleLanded;
            locomotion.OnMovingChanged -= HandleMovingChanged;
            locomotion.OnScriptedRunStarted -= HandleScriptedRunStarted;
            locomotion.OnScriptedRunCompleted -= HandleScriptedRunCompleted;
        }

        void HandleJumped()
        {
            Raise("Jumped");
        }

        void HandleLanded()
        {
            Raise("Landed");
        }

        void HandleMovingChanged(bool moving)
        {
            Raise(moving ? "MovingStarted" : "MovingStopped");
        }

        void HandleScriptedRunStarted()
        {
            Raise("ScriptedRunStarted");
        }

        void HandleScriptedRunCompleted()
        {
            if (clearAnimOnRunComplete)
                ClearAnimOverride();

            if (reenableLocomotionOnRunComplete)
                EnableLocomotion();

            Raise("ScriptedRunEnded");
        }

        public void SetPosition(Vector3 worldPos)
        {
            StopSmoothMotion();
            _transform.position = worldPos;
        }

        public void SetLocalPosition(Vector3 localPos)
        {
            StopSmoothMotion();
            _transform.localPosition = localPos;
        }

        public void MoveBy(Vector3 delta)
        {
            StopSmoothMotion();
            _transform.position += delta;
        }

        public void MoveTo(Vector3 worldPos, float speed)
        {
            StopSmoothMotion();
            float moveSpeed = Mathf.Max(0.01f, speed);
            float distance = Vector3.Distance(_transform.position, worldPos);
            if (distance <= 0.0001f)
                return;

            if (moveSpeed <= 0f)
            {
                _transform.position = worldPos;
                return;
            }

            _moveRoutine = StartCoroutine(MoveToRoutine(worldPos, DurationFromDistance(distance, moveSpeed)));
        }

        public void MoveToDefault(Vector3 worldPos)
        {
            MoveTo(worldPos, defaultMoveSpeed);
        }

        public void MoveLeft(float distance)
        {
            MoveBy(Vector3.left * distance);
        }

        public void MoveRight(float distance)
        {
            MoveBy(Vector3.right * distance);
        }

        public void MoveUp(float distance)
        {
            MoveBy(Vector3.up * distance);
        }

        public void SmoothMoveBy(float offsetX, float offsetY, float offsetZ, float speed)
        {
            SmoothMoveBy(new Vector3(offsetX, offsetY, offsetZ), speed);
        }

        public void SmoothMoveBy(float offsetX, float offsetY, float offsetZ)
        {
            SmoothMoveBy(new Vector3(offsetX, offsetY, offsetZ), defaultMoveSpeed);
        }

        public void SmoothMoveBy(Vector3 delta, float speed)
        {
            if (delta.sqrMagnitude <= 0.0001f)
                return;

            StopSmoothMotion();
            _moveRoutine = StartCoroutine(SmoothMoveByRoutine(delta, speed));
        }

        public void SmoothMoveLeft(float distance)
        {
            SmoothMoveBy(Vector3.left * distance, defaultMoveSpeed);
        }

        public void SmoothMoveLeft(float distance, float speed)
        {
            SmoothMoveBy(Vector3.left * distance, speed);
        }

        public void SmoothMoveRight(float distance)
        {
            SmoothMoveBy(Vector3.right * distance, defaultMoveSpeed);
        }

        public void SmoothMoveRight(float distance, float speed)
        {
            SmoothMoveBy(Vector3.right * distance, speed);
        }

        public void SmoothMoveForward(float distance)
        {
            SmoothMoveBy(Vector3.forward * distance, defaultMoveSpeed);
        }

        public void SmoothMoveForward(float distance, float speed)
        {
            SmoothMoveBy(Vector3.forward * distance, speed);
        }

        public void SmoothMoveBackward(float distance)
        {
            SmoothMoveBy(Vector3.back * distance, defaultMoveSpeed);
        }

        public void SmoothMoveBackward(float distance, float speed)
        {
            SmoothMoveBy(Vector3.back * distance, speed);
        }

        public void SmoothMoveUp(float distance)
        {
            SmoothMoveBy(Vector3.up * distance, defaultMoveSpeed);
        }

        public void SmoothMoveUp(float distance, float speed)
        {
            SmoothMoveBy(Vector3.up * distance, speed);
        }

        public void SmoothMoveTo(Vector3 worldPos, float speed)
        {
            SmoothMoveBy(worldPos - _transform.position, speed);
        }

        public void SmoothMoveToDefault(Vector3 worldPos)
        {
            SmoothMoveTo(worldPos, defaultMoveSpeed);
        }

        public void SetRotation(Vector3 euler)
        {
            StopSmoothMotion();
            _transform.rotation = Quaternion.Euler(euler);
        }

        public void SetLocalRotation(Vector3 euler)
        {
            StopSmoothMotion();
            _transform.localRotation = Quaternion.Euler(euler);
        }

        public void RotateBy(float yawDegrees)
        {
            StopSmoothMotion();
            _transform.Rotate(0f, yawDegrees, 0f, Space.World);
        }

        public void RotateTo(float yawDegrees, float speed)
        {
            SmoothRotateTo(yawDegrees, speed);
        }

        public void RotateToDefault(float yawDegrees)
        {
            SmoothRotateTo(yawDegrees, defaultRotateSpeed);
        }

        public void SmoothRotateTo(float yawDegrees, float speed)
        {
            StopSmoothMotion();
            float rotateSpeed = Mathf.Max(0.01f, speed);
            float angle = Mathf.Abs(Mathf.DeltaAngle(_transform.eulerAngles.y, yawDegrees));
            if (angle <= 0.01f)
            {
                _transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
                return;
            }

            if (rotateSpeed <= 0f)
            {
                _transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
                return;
            }

            _rotateRoutine = StartCoroutine(RotateToRoutine(yawDegrees, DurationFromDistance(angle, rotateSpeed)));
        }

        public void SmoothRotateToDefault(float yawDegrees)
        {
            SmoothRotateTo(yawDegrees, defaultRotateSpeed);
        }

        public void SmoothRotateBy(float yawDegrees, float speed)
        {
            SmoothRotateTo(_transform.eulerAngles.y + yawDegrees, speed);
        }

        public void SmoothRotateByDefault(float yawDegrees)
        {
            SmoothRotateBy(yawDegrees, defaultRotateSpeed);
        }

        public void FaceLeft()
        {
            SetRotation(new Vector3(0f, faceLeftYaw, 0f));
        }

        public void FaceRight()
        {
            SetRotation(new Vector3(0f, faceRightYaw, 0f));
        }

        public void SmoothFaceLeft()
        {
            SmoothRotateTo(faceLeftYaw, defaultRotateSpeed);
        }

        public void SmoothFaceLeft(float speed)
        {
            SmoothRotateTo(faceLeftYaw, speed);
        }

        public void SmoothFaceRight()
        {
            SmoothRotateTo(faceRightYaw, defaultRotateSpeed);
        }

        public void SmoothFaceRight(float speed)
        {
            SmoothRotateTo(faceRightYaw, speed);
        }

        public void StopSmoothMotion()
        {
            StopMoveRoutine();
            StopRotateRoutine();

            if (locomotion != null && locomotion.IsScriptedRunning)
                locomotion.StopScriptedRun();
        }

        public void SetActive(bool enabled)
        {
            gameObject.SetActive(enabled);
        }

        public void Activate()
        {
            SetActive(true);
        }

        public void Deactivate()
        {
            SetActive(false);
        }

        public void SetComponentEnabled(string typeName, bool enabled)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.LogWarning("SerumActionBridge.SetComponentEnabled called with empty type name.", this);
                return;
            }

            Component component = FindComponentByTypeName(typeName);
            if (component is Behaviour behaviour)
            {
                behaviour.enabled = enabled;
                return;
            }

            if (component is Collider collider)
            {
                collider.enabled = enabled;
                return;
            }

            Debug.LogWarning($"SerumActionBridge could not find enableable component '{typeName}'.", this);
        }

        public void Raise(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return;

            for (int i = 0; i < namedEvents.Count; i++)
            {
                NamedUnityEvent entry = namedEvents[i];
                if (entry == null || entry.id != eventId)
                    continue;

                entry.onRaised?.Invoke();
                return;
            }

            Debug.LogWarning($"SerumActionBridge could not find named event '{eventId}'.", this);
        }

        public void PlayIdle()
        {
            if (characterAnimation == null)
            {
                Debug.LogWarning("SerumActionBridge.PlayIdle requires CharacterAnimation.", this);
                return;
            }

            characterAnimation.SetSpeed(0f);
        }

        public void PlayRun()
        {
            if (characterAnimation == null)
            {
                Debug.LogWarning("SerumActionBridge.PlayRun requires CharacterAnimation.", this);
                return;
            }

            characterAnimation.SetSpeed(1f);
        }

        public void SetAnimSpeed(float value)
        {
            if (characterAnimation == null)
            {
                Debug.LogWarning("SerumActionBridge.SetAnimSpeed requires CharacterAnimation.", this);
                return;
            }

            characterAnimation.SetSpeed(value);
        }

        public void ClearAnimOverride()
        {
            if (characterAnimation == null)
            {
                Debug.LogWarning("SerumActionBridge.ClearAnimOverride requires CharacterAnimation.", this);
                return;
            }

            characterAnimation.ClearSpeedOverride();
        }

        public void EnableLocomotion()
        {
            SetLocomotionEnabled(true);
        }

        public void DisableLocomotion()
        {
            SetLocomotionEnabled(false);
        }

        public void SetLocomotionEnabled(bool enabled)
        {
            if (locomotion == null)
            {
                Debug.LogWarning("SerumActionBridge.SetLocomotionEnabled requires SideScrollerController.", this);
                return;
            }

            locomotion.SetLocomotionEnabled(enabled);
        }

        public void ForceJump()
        {
            if (locomotion == null)
            {
                Debug.LogWarning("SerumActionBridge.ForceJump requires SideScrollerController.", this);
                return;
            }

            locomotion.ForceJump();
        }

        public void FacePlayerLeft()
        {
            if (locomotion == null)
            {
                Debug.LogWarning("SerumActionBridge.FacePlayerLeft requires SideScrollerController.", this);
                return;
            }

            locomotion.FaceLeft();
        }

        public void FacePlayerRight()
        {
            if (locomotion == null)
            {
                Debug.LogWarning("SerumActionBridge.FacePlayerRight requires SideScrollerController.", this);
                return;
            }

            locomotion.FaceRight();
        }

        public void RunRight(float distance)
        {
            RunTo(distance, 0f);
        }

        public void RunRight(float distance, float speed)
        {
            RunTo(distance, 0f, speed);
        }

        public void RunLeft(float distance)
        {
            RunTo(-distance, 0f);
        }

        public void RunLeft(float distance, float speed)
        {
            RunTo(-distance, 0f, speed);
        }

        public void RunForward(float distance)
        {
            RunTo(0f, distance);
        }

        public void RunForward(float distance, float speed)
        {
            RunTo(0f, distance, speed);
        }

        public void RunBackward(float distance)
        {
            RunTo(0f, -distance);
        }

        public void RunBackward(float distance, float speed)
        {
            RunTo(0f, -distance, speed);
        }

        public void RunTo(float offsetX, float offsetZ)
        {
            RunTo(offsetX, offsetZ, defaultRunSpeed);
        }

        public void RunTo(float offsetX, float offsetZ, float speed)
        {
            if (locomotion == null)
            {
                Debug.LogWarning("SerumActionBridge.RunTo requires SideScrollerController.", this);
                return;
            }

            Vector3 offset = new Vector3(offsetX, 0f, offsetZ);
            if (offset.sqrMagnitude <= 0.0001f)
                return;

            float runSpeed = Mathf.Max(0.01f, speed);
            SetLocomotionEnabled(false);
            SetAnimSpeed(GetRunAnimSpeed(runSpeed));
            locomotion.StartScriptedRunAtSpeed(offset, runSpeed);
        }

        public void RunToDefault(float offsetX, float offsetZ)
        {
            RunTo(offsetX, offsetZ, defaultRunSpeed);
        }

        public void RunToOverDuration(float offsetX, float offsetZ, float duration)
        {
            if (locomotion == null)
            {
                Debug.LogWarning("SerumActionBridge.RunToOverDuration requires SideScrollerController.", this);
                return;
            }

            Vector3 offset = new Vector3(offsetX, 0f, offsetZ);
            if (offset.sqrMagnitude <= 0.0001f)
                return;

            float runSpeed = offset.magnitude / Mathf.Max(0.01f, duration);
            RunTo(offsetX, offsetZ, runSpeed);
        }

        public void StopScriptedRun()
        {
            if (locomotion == null)
            {
                Debug.LogWarning("SerumActionBridge.StopScriptedRun requires SideScrollerController.", this);
                return;
            }

            locomotion.StopScriptedRun();
        }

        public void LockPlayerControl()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("SerumActionBridge.LockPlayerControl requires GameStateManager.", this);
                return;
            }

            GameStateManager.Instance.EnterCutscene();
        }

        public void UnlockPlayerControl()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("SerumActionBridge.UnlockPlayerControl requires GameStateManager.", this);
                return;
            }

            GameStateManager.Instance.EnterGameplay();
        }

        public void EnterQTEState()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("SerumActionBridge.EnterQTEState requires GameStateManager.", this);
                return;
            }

            GameStateManager.Instance.EnterQTE();
        }

        public void ExitQTEState()
        {
            UnlockPlayerControl();
        }

        IEnumerator SmoothMoveByRoutine(Vector3 delta, float speed)
        {
            Vector3 start = _transform.position;
            Vector3 target = start + delta;
            float duration = DurationFromDistance(delta.magnitude, speed);
            yield return SmoothMoveRoutine(start, target, duration);
        }

        IEnumerator MoveToRoutine(Vector3 target, float duration)
        {
            yield return SmoothMoveRoutine(_transform.position, target, duration);
        }

        IEnumerator SmoothMoveRoutine(Vector3 start, Vector3 target, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(Mathf.Clamp01(elapsed / duration));
                _transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            _transform.position = target;
            _moveRoutine = null;
        }

        IEnumerator RotateToRoutine(float targetYaw, float duration)
        {
            float startYaw = _transform.eulerAngles.y;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(Mathf.Clamp01(elapsed / duration));
                float yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
                _transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                yield return null;
            }

            _transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            _rotateRoutine = null;
        }

        static float DurationFromDistance(float distance, float speed)
        {
            return distance / Mathf.Max(0.01f, speed);
        }

        static float SmoothStep01(float t)
        {
            return t * t * (3f - 2f * t);
        }

        float GetRunAnimSpeed(float runSpeed)
        {
            if (locomotion == null)
                return 1f;

            return locomotion.MoveSpeed > 0f
                ? Mathf.Clamp01(runSpeed / locomotion.MoveSpeed)
                : 1f;
        }

        void StopMoveRoutine()
        {
            if (_moveRoutine == null)
                return;

            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        void StopRotateRoutine()
        {
            if (_rotateRoutine == null)
                return;

            StopCoroutine(_rotateRoutine);
            _rotateRoutine = null;
        }

        Component FindComponentByTypeName(string typeName)
        {
            Component[] components = GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && component.GetType().Name == typeName)
                    return component;
            }

            return null;
        }
    }
}
