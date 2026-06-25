using UnityEngine;

public class PushCart : BaseInteractable
{
    [Header("Cart")]
    public Transform pushPoint;

    [Header("Movement")]
    [SerializeField] private float pushSpeedMultiplier = 0.5f;

    private PlayerPushComponent currentPlayer;

    public Transform PushPoint => pushPoint;
    public float PushSpeedMultiplier => pushSpeedMultiplier;

    protected override void OnInteract()
    {
        if (currentPlayer == null)
            BeginPush();
        else
            EndPush();
    }

    private void BeginPush()
    {
        PlayerPushComponent player = FindFirstObjectByType<PlayerPushComponent>();

        if (player == null)
            return;

        currentPlayer = player;

        player.StartPushing(this);
    }

    private void EndPush()
    {
        if (currentPlayer == null)
            return;

        currentPlayer.StopPushing();
        currentPlayer = null;
    }
}