using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health health;
    public Slider slider;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (health == null)
            health = GetComponentInParent<Health>();

        if (health != null)
            health.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        if (health != null)
            HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (slider != null)
            slider.value = max > 0 ? (float)current / max : 0f;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }
}