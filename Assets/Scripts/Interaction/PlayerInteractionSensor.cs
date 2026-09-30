using UnityEngine;

namespace InteractionSystem
{
    // A separate body leaves the player's movement physics unchanged.
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class PlayerInteractionSensor : MonoBehaviour
    {
        PlayerInteractor _owner;

        internal void Initialize(PlayerInteractor owner, Vector3 offset, float radius)
        {
            _owner = owner;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            transform.localPosition = offset;
            SphereCollider sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(0.01f, radius);
            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_owner != null && _owner.isActiveAndEnabled)
                _owner.TrackCollider(other);
        }

        void OnTriggerExit(Collider other)
        {
            if (_owner != null && _owner.isActiveAndEnabled)
                _owner.UntrackCollider(other);
        }
    }
}
