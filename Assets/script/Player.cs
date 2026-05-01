using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform ballPosition;
    [HideInInspector] public bool hasPossession = false;

    private PlayerMovement _movement;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        // All players start as AI (disabled movement)
        // Team.cs will enable the first one
        if (_movement != null) _movement.enabled = false;
    }

    public void UserBrain()
    {
        if (_movement != null) _movement.enabled = true;
    }

    public void AiBrain()
    {
        if (_movement != null) _movement.enabled = false;
    }
}