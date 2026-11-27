using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class UIPlayerHealthBar : MonoBehaviour
{
    public PlayerAgent player;
    [SerializeField] private Slider slider;

    private bool IsPreDeath => player.Health <= 40;

    // Полоса после 0.6f уходит вглубь спрайта сердца или вообще исчезает,
    // поэтому я фиксирую её на 0.6f до момента смерти.
    public void SetHealth(float health)
    {
        if (health != 0f) slider.value = Mathf.Clamp(health/100, (IsPreDeath ? 0.532f : 0.6f), slider.maxValue);
        else slider.value = 0f;
    }

    public void ResetHealth()
    {
        slider.value = slider.maxValue;
    }
}