using UnityEngine;

public class PlayerPushComponent : MonoBehaviour
{
    private PushCart currentCart;

    private PlayerController movement;

    private float originalSpeed;

    public bool IsPushing => currentCart != null;

    private void Awake()
    {
        movement = GetComponent<PlayerController>();
    }

    public void StartPushing(PushCart cart)
    {
        currentCart = cart;

        originalSpeed = movement.moveSpeed;

        movement.moveSpeed *= cart.PushSpeedMultiplier;
        movement.canJump = false;

        SnapCartToPlayer();
    }

    public void StopPushing()
    {
        if (currentCart == null)
            return;

        movement.moveSpeed = originalSpeed;
        movement.canJump = true;

        currentCart = null;
    }

    private void LateUpdate()
    {
        if (currentCart == null)
            return;

        SnapCartToPlayer();
    }

    private void SnapCartToPlayer()
    {
        Transform pushPoint = currentCart.PushPoint;

        if (pushPoint == null)
            return;

        Vector3 offset = currentCart.transform.position - pushPoint.position;

        currentCart.transform.position =
            transform.position + offset;
    }
}