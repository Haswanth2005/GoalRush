using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallDeform : MonoBehaviour
{
    [Header("Squash & Stretch")]
    [Range(1f, 3f)]
    public float squashAmount = 1.6f;
    [Range(0.05f, 0.4f)]
    public float duration = 0.12f;
    [Range(0f, 1f)]
    public float velocityThreshold = 1f;

    Transform _visual;
    float _timer = 0f;
    Vector3 _hitNormal;

    void Awake()
    {
        _visual = GetOrCreateVisualChild();
    }

    void OnCollisionEnter(Collision col)
    {
        float speed = col.relativeVelocity.magnitude;
        if (speed < velocityThreshold) return;

        _hitNormal = col.contacts[0].normal;
        _timer = duration;
    }

    void Update()
    {
        if (_timer <= 0f)
        {
            _visual.localScale = Vector3.Lerp(
                _visual.localScale, Vector3.one, Time.deltaTime * 20f);
            return;
        }

        _timer -= Time.deltaTime;

        float t = _timer / duration;
        float eased = t * t;
        float factor = Mathf.Lerp(1f, squashAmount, eased);
        float thin = 1f / factor;

        Vector3 localNormal = transform.InverseTransformDirection(_hitNormal);
        Vector3 scale = Vector3.one * factor;

        scale.x = Mathf.Lerp(scale.x, thin, Mathf.Abs(localNormal.x));
        scale.y = Mathf.Lerp(scale.y, thin, Mathf.Abs(localNormal.y));
        scale.z = Mathf.Lerp(scale.z, thin, Mathf.Abs(localNormal.z));

        _visual.localScale = scale;
    }

    Transform GetOrCreateVisualChild()
    {
        Transform existing = transform.Find("Visual");
        if (existing) return existing;

        var child = new GameObject("Visual");
        child.transform.parent = transform;
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        if (mf) { var cmf = child.AddComponent<MeshFilter>(); cmf.mesh = mf.mesh; Destroy(mf); }
        if (mr) { var cmr = child.AddComponent<MeshRenderer>(); cmr.materials = mr.materials; Destroy(mr); }
        return child.transform;
    }
}