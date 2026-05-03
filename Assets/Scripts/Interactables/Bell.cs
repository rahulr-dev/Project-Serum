using UnityEngine;
using System;

public class Bell : BaseInteractable
{
    [HideInInspector] public int bellIndex;

    public AudioSource audioSource;
    public float basePitch = 0.8f;
    public float pitchStep = 0.2f;

    public Action<int> OnBellRung;

    protected override void OnInteract()
    {
        Ring();
    }

    public void Ring()
    {
        if (audioSource != null)
        {
            float pitch = basePitch + (bellIndex * pitchStep);
            audioSource.pitch = pitch;
            audioSource.Play();
        }

        Debug.Log("Bell rung: " + bellIndex);

        OnBellRung?.Invoke(bellIndex);
    }
}