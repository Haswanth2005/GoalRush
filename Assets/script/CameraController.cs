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
        // 1. Initialization Check
        if (!_isInitialized || _team == null || _team._currentPlayer.Count == 0) 
        {
            if (_team == null)
            {
                Team[] allTeams = Object.FindObjectsByType<Team>(FindObjectsSortMode.None);
                foreach (Team t in allTeams)
                {
                    if (t._currentPlayer.Count > 0)
                    {
                        _team = t;
                        break;
                    }
                }
            }

            if (_team != null && _team._currentPlayer.Count > 0)
            {
                // Calculate the initial offset so the camera maintains its current height/angle
                _worldOffset = transform.position - _team._currentPlayer[0].transform.position;
                _isInitialized = true;
            }
            return;
        }

        // 2. Hybrid Follow: Track the midpoint between the Player and the Ball
        Player targetPlayer = _team._currentPlayer[0];
        Vector3 focusPoint;
        
        if (Ball.Instance != null)
        {
            focusPoint = Vector3.Lerp(targetPlayer.transform.position, Ball.Instance.transform.position, 0.5f);
        }
        else
        {
            focusPoint = targetPlayer.transform.position;
        }

        Vector3 targetPosition = focusPoint + _worldOffset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothTime);

        // 3. Dynamic Zoom (Zooms out if active player is far from ball)
        if (enableDynamicZoom && _team != null && _team._currentPlayer.Count > 0)
        {
            float distToBall = Vector3.Distance(targetPlayer.transform.position, Ball.Instance.transform.position);
            
            float targetFov = _baseFov + (distToBall * ballDistanceImpact);
            targetFov = Mathf.Clamp(targetFov, _baseFov, maxZoomOutFov);

            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * zoomSpeed);
        }
    }
}
