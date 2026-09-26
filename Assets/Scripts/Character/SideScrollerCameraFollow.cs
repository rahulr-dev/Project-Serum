using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

namespace Character
{
    public class SideScrollerCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform lookAt;
        [SerializeField] CinemachineSplineDolly dolly;
        [SerializeField] Vector3 offset;
        [FormerlySerializedAs("smoothTime")]
        [FormerlySerializedAs("followSmoothTime")]
        [SerializeField, Min(0f)] float followSmoothTime = 0.12f;
        [SerializeField, Min(0f)] float lookAtSmoothTime = 0.5f;

        float _currentPosition;
        float _positionVelocity;
        CinemachineCamera _camera;
        CinemachineRotationComposer _rotationComposer;

        void OnEnable()
        {
            if (dolly == null)
                dolly = GetComponent<CinemachineSplineDolly>();

            _currentPosition = dolly != null ? dolly.CameraPosition : 0f;
            _positionVelocity = 0f;
            CacheCamera();
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            if (dolly == null)
                dolly = GetComponent<CinemachineSplineDolly>();
            if (dolly == null || dolly.Spline == null)
                return;

            UpdateLookAtTarget();

            Vector3 query = target.position + offset;

            float3 local = dolly.Spline.transform.InverseTransformPoint(query);
            SplineUtility.GetNearestPoint(dolly.Spline.Spline, local, out _, out float normalizedT);
            normalizedT = Mathf.Clamp01(normalizedT);

            float desired = ToCameraPosition(normalizedT);
            _currentPosition = Mathf.SmoothDamp(
                _currentPosition, desired, ref _positionVelocity, followSmoothTime);
            dolly.CameraPosition = _currentPosition;
        }

        public void SetTarget(Transform follow)
        {
            target = follow;
        }

        public void SetLookAt(Transform targetTransform)
        {
            lookAt = targetTransform;
            UpdateLookAtTarget();
        }

        float ToCameraPosition(float normalizedT)
        {
            Spline spline = dolly.Spline.Spline;
            switch (dolly.PositionUnits)
            {
                case PathIndexUnit.Distance:
                    return normalizedT * spline.GetLength();
                case PathIndexUnit.Knot:
                    return normalizedT * Mathf.Max(0, spline.Count - 1);
                default:
                    return normalizedT;
            }
        }

        void CacheCamera()
        {
            _camera = dolly != null ? dolly.GetComponent<CinemachineCamera>() : null;
            _rotationComposer = dolly != null ? dolly.GetComponent<CinemachineRotationComposer>() : null;
        }

        void UpdateLookAtTarget()
        {
            if (_camera == null)
                CacheCamera();

            if (_camera != null)
                _camera.Target.LookAtTarget = lookAt != null ? lookAt : target;

            if (_rotationComposer != null)
                _rotationComposer.Damping = Vector2.one * lookAtSmoothTime;
        }
    }
}
