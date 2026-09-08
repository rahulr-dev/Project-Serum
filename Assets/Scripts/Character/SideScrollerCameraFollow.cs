using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Character
{
    public class SideScrollerCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] CinemachineSplineDolly dolly;
        [SerializeField] float offsetX;
        [SerializeField] float followSmoothTime = 0.12f;
        [SerializeField] float lookAhead;

        float _currentPosition;
        float _positionVelocity;
        SideScrollerController _mover;

        void OnEnable()
        {
            if (dolly == null)
                dolly = GetComponent<CinemachineSplineDolly>();

            _currentPosition = dolly != null ? dolly.CameraPosition : 0f;
            _positionVelocity = 0f;
            CacheMover();
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            if (dolly == null)
                dolly = GetComponent<CinemachineSplineDolly>();
            if (dolly == null || dolly.Spline == null)
                return;

            if (_mover == null)
                CacheMover();

            float lookAheadX = 0f;
            if (lookAhead != 0f && _mover != null)
            {
                float speed = _mover.HorizontalSpeed;
                if (speed != 0f)
                    lookAheadX = Mathf.Sign(speed) * lookAhead;
            }

            Vector3 query = target.position;
            query.x += offsetX + lookAheadX;
            query.y = dolly.Spline.transform.position.y;

            float3 local = dolly.Spline.transform.InverseTransformPoint(query);
            SplineUtility.GetNearestPoint(dolly.Spline.Spline, local, out _, out float normalizedT);
            normalizedT = Mathf.Clamp01(normalizedT);

            float desired = ToCameraPosition(normalizedT);
            _currentPosition = Mathf.SmoothDamp(
                _currentPosition, desired, ref _positionVelocity, followSmoothTime);
            dolly.CameraPosition = _currentPosition;
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

        void CacheMover()
        {
            _mover = target != null ? target.GetComponent<SideScrollerController>() : null;
        }
    }
}
