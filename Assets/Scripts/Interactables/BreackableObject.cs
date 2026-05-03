using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    private Health health;

    void Start()
    {
        health = GetComponent<Health>();
        health.OnDeath += Break;
    }

    void Break()
    {
        //Break Logic Here (Play Animation, Spawn Particles, etc.)
    }
}