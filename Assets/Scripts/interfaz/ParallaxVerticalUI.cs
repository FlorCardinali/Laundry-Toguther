using UnityEngine;
using UnityEngine.UI;

public class ParallaxVerticalUI : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Velocidad a la que baja el fondo. Usa valores negativos si quieres que suba.")]
    public float velocidad = 0.2f;

    private RawImage rawImage;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        Rect uvRect = rawImage.uvRect;
        uvRect.y += velocidad * Time.deltaTime;

        if (uvRect.y >= 1f || uvRect.y <= -1f)
        {
            uvRect.y = 0f;
        }

        rawImage.uvRect = uvRect;
    }
}