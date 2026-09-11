using Character;
using Game;
using UnityEngine;

namespace InteractionSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPusher : MonoBehaviour
    {
        const int ReleaseGraceFrames = 12;
        const float FrontDot = 0.35f;

        [SerializeField] SideScrollerController locomotion;

        CharacterController _controller;
        Pushable _hitThisFrame;
        Pushable _active;
        float _lastX;
        int _missedFrames;
        bool _ownsPushState;
        GameState _stateBeforePush = GameState.Gameplay;

        void Awake()
        {
            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();
            _controller = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            _lastX = transform.position.x;
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null || locomotion == null)
                return;

            if (hit.normal.y > 0.5f)
                return;

            Pushable pushable = hit.collider.GetComponentInParent<Pushable>();
            if (pushable == null || !pushable.isActiveAndEnabled)
                return;

            float facing = locomotion.FacingSign;
            if (hit.normal.x * facing >= -FrontDot)
                return;

            if (locomotion.HorizontalSpeed * facing <= 0f)
                return;

            if (!locomotion.IsGrounded)
                return;

            _hitThisFrame = pushable;
        }

        void LateUpdate()
        {
            float x = transform.position.x;
            float movedX = x - _lastX;

            if (_hitThisFrame != null)
            {
                _missedFrames = 0;
                BeginPush(_hitThisFrame);

                float intendedX = locomotion != null
                    ? locomotion.HorizontalSpeed * Time.deltaTime
                    : movedX;
                _hitThisFrame.Push(intendedX);

                float leftoverX = intendedX - movedX;
                if (_controller != null && Mathf.Abs(leftoverX) > 0.0001f)
                    _controller.Move(new Vector3(leftoverX, 0f, 0f));

                _hitThisFrame = null;
                _lastX = transform.position.x;
                return;
            }

            _lastX = x;
            if (_active == null)
                return;

            _missedFrames++;
            if (_missedFrames >= ReleaseGraceFrames)
                EndPush();
        }

        void BeginPush(Pushable pushable)
        {
            _active = pushable;
            if (GameStateManager.Instance == null)
                return;

            if (GameStateManager.Instance.CurrentState == GameState.GameplayPushing)
                return;

            _stateBeforePush = GameStateManager.Instance.CurrentState;
            _ownsPushState = true;
            GameStateManager.Instance.EnterGameplayPushing();
        }

        void EndPush()
        {
            if (_active != null)
                _active.ClearPush();
            _active = null;
            _missedFrames = 0;

            bool restore = _ownsPushState;
            _ownsPushState = false;
            if (!restore || GameStateManager.Instance == null)
                return;

            if (GameStateManager.Instance.CurrentState != GameState.GameplayPushing)
                return;

            GameStateManager.Instance.SetState(_stateBeforePush);
        }
    }
}
