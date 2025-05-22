using UnityEngine;

public class Shadow : MonoBehaviour
{
    [SerializeField] private Transform targetObject;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string colorPropertyName = "_Color";
    [SerializeField] private float maxWidthMultiplier = 2f;
    [SerializeField] private float rotationSmoothing = 5f;

    private Material _material;
    private Color _originalColor;
    private Vector3 _originalScale;
    private float _currentWidthMultiplier = 1f;

    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            _material = targetRenderer.material;
            _originalColor = _material.GetColor(colorPropertyName);
        }

        if (targetObject == null)
            targetObject = Camera.main?.transform;

        _originalScale = transform.localScale;
    }

    void Update()
    {
        if (targetObject == null || _material == null) return;

        UpdateTransparency();
        UpdateWidth();
    }

    private void UpdateTransparency()
    {
        float distance = Vector3.Distance(transform.position, targetObject.position);
        float alpha = Mathf.Clamp01(1 - Mathf.InverseLerp(minDistance, maxDistance, distance));

        Color newColor = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
        _material.SetColor(colorPropertyName, newColor);
    }

    private void UpdateWidth()
    {
        float angle = Mathf.Abs(Vector3.Dot(targetObject.right, Vector3.up));
        float targetMultiplier = 1f + (maxWidthMultiplier - 1f) * angle;
        _currentWidthMultiplier = Mathf.Lerp(_currentWidthMultiplier, targetMultiplier, rotationSmoothing * Time.deltaTime);

        Vector3 newScale = _originalScale;
        newScale.x *= _currentWidthMultiplier;
        transform.localScale = newScale;
    }

    void OnValidate()
    {
        if (minDistance > maxDistance)
            minDistance = maxDistance;
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}