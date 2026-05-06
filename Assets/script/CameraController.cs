using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Following")]
    [Tooltip("How long it takes for the camera to catch up to the ball. Higher = smoother/laggy.")]
    public float smoothTime = 0.2f;
    private Vector3 _velocity = Vector3.zero;
    
    [Header("Dynamic Zoom")]
    public bool enableDynamicZoom = true;
    [Tooltip("The maximum FOV when the ball is far away from the player.")]
    public float maxZoomOutFov = 75f;
    [Tooltip("How fast the zoom transitions.")]
    public float zoomSpeed = 3f;
    [Tooltip("How much the distance affects the zoom. Higher = zooms out faster.")]
    public float ballDistanceImpact = 0.5f;

    private Camera _cam;
    private Team _team;
    
    // World space offset
    private Vector3 _worldOffset;
    
    private float _baseFov;
    private bool _isInitialized = false;

    private void Start()
    {
        _cam = GetComponent<Camera>();
        _baseFov = _cam.fieldOfView;

        // Try to find the team immediately to calculate the offset relative to the starting player
        Team[] allTeams = Object.FindObjectsByType<Team>(FindObjectsSortMode.None);
        foreach (Team t in allTeams)
        {
            if (t._currentPlayer.Count > 0)
            {
                _team = t;
                break;
            }
        }

        if (_team != null && _team._currentPlayer.Count > 0)
        {
            _worldOffset = transform.position - _team._currentPlayer[0].transform.position;
            _isInitialized = true;
        }
    }

    private void LateUpdate()
    {
        if (_team == null)
        {
            _team = Object.FindAnyObjectByType<Team>();
            if (_team == null) return;
        }

        if (_team._currentPlayer.Count == 0) return;

        Player targetPlayer = _team._currentPlayer[0];
        if (targetPlayer == null) return;

        // Settings for a high-quality feel
        float backDistance = 7f;
        float height = 4f;
        float rotationSpeed = 5f; // Speed of camera rotation smoothing

        // 1. Position smoothing
        // Position target: Stay at a fixed world-space offset relative to the player 
        // to prevent nausea from constant camera spinning, but stay far enough back.
        Vector3 behindPos = targetPlayer.transform.position + new Vector3(0, height, -backDistance);
        
        transform.position = Vector3.SmoothDamp(transform.position, behindPos, ref _velocity, smoothTime);

        // 2. Rotation smoothing
        // Look at the midpoint between player and ball
        Vector3 focusPoint;
        if (Ball.Instance != null)
        {
            focusPoint = Vector3.Lerp(targetPlayer.transform.position, Ball.Instance.transform.position, 0.4f);
        }
        else
        {
            focusPoint = targetPlayer.transform.position;
        }

        // Add a vertical offset to the look target so we aren't looking at their feet
        Vector3 lookTarget = focusPoint + Vector3.up * 1.2f;
        
        // Calculate the rotation we WANT
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        
        // Smoothly rotate toward that target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        // 3. Dynamic Zoom (Smoothly adjust FOV)
        if (enableDynamicZoom && Ball.Instance != null)
        {
            float distToBall = Vector3.Distance(targetPlayer.transform.position, Ball.Instance.transform.position);
            float targetFov = _baseFov + (distToBall * ballDistanceImpact);
            targetFov = Mathf.Clamp(targetFov, _baseFov, maxZoomOutFov);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * zoomSpeed);
        }
    }
}
