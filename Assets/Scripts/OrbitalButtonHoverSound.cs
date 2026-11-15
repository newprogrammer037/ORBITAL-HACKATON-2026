using UnityEngine;
using UnityEngine.EventSystems;

public sealed class OrbitalButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    private float nextAllowedTime;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Time.unscaledTime < nextAllowedTime) return;
        nextAllowedTime = Time.unscaledTime + .08f;
        OrbitalAudio.Play("hover_boton", .45f);
    }
}
