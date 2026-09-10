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
        [SerializeField] Transform detachTarget;
        [SerializeField] Transform[] detachTargets;
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

        public void Unparent()
        {
            DetachToWorld();
        }

        public void DetachToWorld()
        {
            Detach(GetDetachTarget(), true);
        }

        public void UnparentKeepLocal()
        {
            Detach(GetDetachTarget(), false);
        }

        public void UnparentConfigured()
        {
            Detach(detachTarget, true);
            if (detachTargets == null)
                return;

            for (int i = 0; i < detachTargets.Length; i++)
                Detach(detachTargets[i], true);
        }

        public void UnparentChildren()
        {
            Transform root = _transform != null ? _transform : transform;
            for (int i = root.childCount - 1; i >= 0; i--)
                Detach(root.GetChild(i), true);
        }

        public void UnparentByName(string childName)
        {
            if (string.IsNullOrEmpty(childName))
            {
                Debug.LogWarning("SerumActionBridge.UnparentByName called with empty name.", this);
                return;
            }

            Transform root = _transform != null ? _transform : transform;
            Transform child = FindChildRecursive(root, childName);
            if (child == null)
            {
                Debug.LogWarning($"SerumActionBridge could not find child '{childName}' to unparent.", this);
                return;
            }

            Detach(child, true);
        }

        public void UnparentTransform(Transform target)
        {
            Detach(target, true);
        }

        Transform GetDetachTarget()
        {
            if (detachTarget != null)
                return detachTarget;

            return _transform != null ? _transform : transform;
        }

        void Detach(Transform target, bool keepWorldTransform)
        {
            if (target == null)
            {
                Debug.LogWarning("SerumActionBridge.Unparent target is null.", this);
                return;
            }

            if (target.parent == null)
            {
                Debug.LogWarning($"SerumActionBridge.Unparent skipped '{target.name}' because it has no parent. Assign Detach Target (the object under the player), or use UnparentByName / UnparentChildren.", this);
                return;
            }

            StopSmoothMotion();
            target.SetParent(null, keepWorldTransform);
        }

        static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
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
            RotateByY(yawDegrees);
        }

        public void RotateByX(float degrees)
        {
            RotateByWorldAxis(Vector3.right, degrees);
        }

        public void RotateByY(float degrees)
        {
            RotateByWorldAxis(Vector3.up, degrees);
        }

        public void RotateByZ(float degrees)
        {
            RotateByWorldAxis(Vector3.forward, degrees);
        }

        public void RotateTo(float yawDegrees, float speed)
        {
            SmoothRotateToY(yawDegrees, speed);
        }

        public void RotateToDefault(float yawDegrees)
        {
            SmoothRotateToY(yawDegrees, defaultRotateSpeed);
        }

        public void SmoothRotateTo(float yawDegrees, float speed)
        {
            SmoothRotateToY(yawDegrees, speed);
        }

        public void SmoothRotateToDefault(float yawDegrees)
        {
            SmoothRotateToY(yawDegrees, defaultRotateSpeed);
        }

        public void SmoothRotateToX(float degrees)
        {
            SmoothRotateToX(degrees, defaultRotateSpeed);
        }

        public void SmoothRotateToX(float degrees, float speed)
        {
            SmoothRotateToWorldAxis(0, degrees, speed);
        }

        public void SmoothRotateToY(float degrees)
        {
            SmoothRotateToY(degrees, defaultRotateSpeed);
        }

        public void SmoothRotateToY(float degrees, float speed)
        {
            SmoothRotateToWorldAxis(1, degrees, speed);
        }

        public void SmoothRotateToZ(float degrees)
        {
            SmoothRotateToZ(degrees, defaultRotateSpeed);
        }

        public void SmoothRotateToZ(float degrees, float speed)
        {
            SmoothRotateToWorldAxis(2, degrees, speed);
        }

        public void SmoothRotateBy(float yawDegrees, float speed)
        {
            SmoothRotateByY(yawDegrees, speed);
        }

        public void SmoothRotateByDefault(float yawDegrees)
        {
            SmoothRotateByY(yawDegrees, defaultRotateSpeed);
        }

        public void SmoothRotateByX(float degrees)
        {
            SmoothRotateByX(degrees, defaultRotateSpeed);
        }

        public void SmoothRotateByX(float degrees, float speed)
        {
            SmoothRotateByWorldAxis(Vector3.right, degrees, speed);
        }

        public void SmoothRotateByY(float degrees)
        {
            SmoothRotateByY(degrees, defaultRotateSpeed);
        }

        public void SmoothRotateByY(float degrees, float speed)
        {
            SmoothRotateByWorldAxis(Vector3.up, degrees, speed);
        }

        public void SmoothRotateByZ(float degrees)
        {
            SmoothRotateByZ(degrees, defaultRotateSpeed);
        }

        public void SmoothRotateByZ(float degrees, float speed)
        {
            SmoothRotateByWorldAxis(Vector3.forward, degrees, speed);
        }

        public void FaceLeft()
        {
            StopSmoothMotion();
            Vector3 euler = _transform.eulerAngles;
            euler.y = faceLeftYaw;
            _transform.rotation = Quaternion.Euler(euler);
        }

        public void FaceRight()
        {
            StopSmoothMotion();
            Vector3 euler = _transform.eulerAngles;
            euler.y = faceRightYaw;
            _transform.rotation = Quaternion.Euler(euler);
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

        public void EnterGameplayStealth()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("SerumActionBridge.EnterGameplayStealth requires GameStateManager.", this);
                return;
            }

            GameStateManager.Instance.EnterGameplayStealth();
        }

        public void ExitGameplayStealth()
        {
            UnlockPlayerControl();
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

        void RotateByWorldAxis(Vector3 worldAxis, float degrees)
        {
            StopSmoothMotion();
            _transform.Rotate(worldAxis, degrees, Space.World);
        }

        void SmoothRotateByWorldAxis(Vector3 worldAxis, float degrees, float speed)
        {
            if (Mathf.Abs(degrees) <= 0.01f)
                return;

            StopSmoothMotion();
            float rotateSpeed = Mathf.Max(0.01f, speed);
            _rotateRoutine = StartCoroutine(
                RotateByWorldAxisRoutine(worldAxis.normalized, degrees, DurationFromDistance(Mathf.Abs(degrees), rotateSpeed)));
        }

        void SmoothRotateToWorldAxis(int axisIndex, float worldDegrees, float speed)
        {
            StopSmoothMotion();
            Vector3 euler = _transform.eulerAngles;
            float current = euler[axisIndex];
            float angle = Mathf.Abs(Mathf.DeltaAngle(current, worldDegrees));
            if (angle <= 0.01f)
            {
                euler[axisIndex] = worldDegrees;
                _transform.rotation = Quaternion.Euler(euler);
                return;
            }

            float rotateSpeed = Mathf.Max(0.01f, speed);
            _rotateRoutine = StartCoroutine(
                RotateToWorldAxisRoutine(axisIndex, worldDegrees, DurationFromDistance(angle, rotateSpeed)));
        }

        IEnumerator RotateByWorldAxisRoutine(Vector3 worldAxis, float degrees, float duration)
        {
            Quaternion start = _transform.rotation;
            Quaternion target = Quaternion.AngleAxis(degrees, worldAxis) * start;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(Mathf.Clamp01(elapsed / duration));
                _transform.rotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }

            _transform.rotation = target;
            _rotateRoutine = null;
        }

        IEnumerator RotateToWorldAxisRoutine(int axisIndex, float targetDegrees, float duration)
        {
            Vector3 startEuler = _transform.eulerAngles;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(Mathf.Clamp01(elapsed / duration));
                Vector3 euler = startEuler;
                euler[axisIndex] = Mathf.LerpAngle(startEuler[axisIndex], targetDegrees, t);
                _transform.rotation = Quaternion.Euler(euler);
                yield return null;
            }

            Vector3 endEuler = startEuler;
            endEuler[axisIndex] = targetDegrees;
            _transform.rotation = Quaternion.Euler(endEuler);
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
