using UnityEngine;
using System.Collections.Generic;

public class BellSystem : MonoBehaviour
{
    [Header("Setup")]
    public Bell[] bells;
    public Door door;

    private List<int> playerSequence = new List<int>();

    void Awake()
    {
        for (int i = 0; i < bells.Length; i++)
        {
            bells[i].bellIndex = i;
        }
    }

    void Start()
    {
        foreach (var bell in bells)
        {
            bell.OnBellRung += OnBellRung;
        }
    }

    void OnBellRung(int id)
    {
        playerSequence.Add(id);

        Debug.Log("Player: " + string.Join(",", playerSequence));

        if (!IsValidSoFar())
        {
            Debug.Log("Wrong → Reset");
            ResetSequence();
            return;
        }

        if (playerSequence.Count == bells.Length)
        {
            Debug.Log("Correct sequence!");

            door.Activate();
            ResetSequence();
        }
    }

    bool IsValidSoFar()
    {
        for (int i = 0; i < playerSequence.Count; i++)
        {
            if (playerSequence[i] != i)
                return false;
        }
        return true;
    }

    void ResetSequence()
    {
        playerSequence.Clear();
    }
}