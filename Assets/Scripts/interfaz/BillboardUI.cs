using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Transform camaraTransform;

    void LateUpdate()
    {
        if (camaraTransform == null)
        {
            if (Camera.main != null)
            {
                camaraTransform = Camera.main.transform;
            }
            return;
        }

        // Hacemos que el canvas rote para mirar exactamente hacia donde apunta la cámara
        // Usar transform.position + cameraTransform.forward evita que el texto aparezca invertido o espejado
        transform.LookAt(transform.position + camaraTransform.forward);
    }
}