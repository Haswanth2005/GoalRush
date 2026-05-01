using UnityEngine;

[RequireComponent(typeof(Player))]
public class DirectionIndicator : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private Player _player;
    private Team _team;
    private Kicking _kicking;

    [SerializeField] private float arrowLength = 3f;
    [SerializeField] private float arrowWidth = 0.5f;

    [Header("Charge Visual")]
    [SerializeField] private float chargeWidthMultiplier = 2.5f;
    [SerializeField] private float chargeLengthMultiplier = 1.6f;

    // Charge color gradient: white → yellow → red
    private static readonly Color _colorIdle = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color _colorMid  = new Color(1f, 0.9f, 0.2f, 0.85f);
    private static readonly Color _colorFull = new Color(1f, 0.15f, 0.1f, 1f);

    private void Awake()
    {
        _player = GetComponent<Player>();
        _team = GetComponentInParent<Team>();
        
        // Create LineRenderer
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = arrowWidth;
        _lineRenderer.endWidth = 0f; // Taper to 0 to look like a triangle/arrow
        
        // Basic material and color
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = _colorIdle;
        _lineRenderer.endColor = new Color(1f, 1f, 1f, 0f);
        
        _lineRenderer.numCapVertices = 0;
        _lineRenderer.alignment = LineAlignment.TransformZ; // Flat on the ground
    }

    private void Start()
    {
        // Kicking lives on the Team object, grab reference once
        if (_team != null)
            _kicking = _team.GetComponent<Kicking>();
    }

    private void Update()
    {
        // Only show indicator for the currently controlled player
        bool isControlled = _team != null && _team._currentPlayer.Count > 0 && _team._currentPlayer[0] == _player;
        _lineRenderer.enabled = isControlled;

        if (isControlled)
        {
            float charge = (_kicking != null && _kicking.IsCharging) ? _kicking.ChargePercent : 0f;

            // ---- Color: white → yellow → red ----
            Color startColor;
            if (charge < 0.5f)
                startColor = Color.Lerp(_colorIdle, _colorMid, charge * 2f);
            else
                startColor = Color.Lerp(_colorMid, _colorFull, (charge - 0.5f) * 2f);

            Color endColor = startColor;
            endColor.a = 0f;

            _lineRenderer.startColor = startColor;
            _lineRenderer.endColor = endColor;

            // ---- Width & Length scale with charge ----
            float widthScale = Mathf.Lerp(1f, chargeWidthMultiplier, charge);
            float lengthScale = Mathf.Lerp(1f, chargeLengthMultiplier, charge);

            _lineRenderer.startWidth = arrowWidth * widthScale;
            _lineRenderer.endWidth = 0f;

            // ---- Positions ----
            Vector3 startPos = transform.position;
            startPos.y = 0.1f; // Just above ground

            Vector3 direction = GetMouseDirection(transform.position);
            Vector3 endPos = startPos + direction * arrowLength * lengthScale;

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);
        }
    }

    public static Vector3 GetMouseDirection(Vector3 origin)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, origin.y, 0));
        
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 direction = hitPoint - origin;
            direction.y = 0;
            if (direction.magnitude > 0.01f)
            {
                return direction.normalized;
            }
        }
        
        return Camera.main.transform.forward;
    }
}
