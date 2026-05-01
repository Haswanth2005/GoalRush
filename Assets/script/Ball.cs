using UnityEngine;

public class Ball : MonoBehaviour
{
    public static Ball Instance;
    public Rigidbody rb;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
    }
}