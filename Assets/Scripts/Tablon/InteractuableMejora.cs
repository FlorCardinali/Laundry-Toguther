using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

public class InteractuableMejora : MonoBehaviour, IInteractuable
{
    public int idMejora;

    [Header("Referencias Visuales")]
    public Image fondoBoton; 
    public TMP_Text textoPrecio;

    public bool yaComprado = false;

    public void Interactuar(PlayerController jugador)
    {
        if (yaComprado) return;

        TablonManager manager = GetComponentInParent<TablonManager>();
        if (manager != null)
        {
            manager.ProcesarCompra(jugador, idMejora);
        }
    }
    public void MarcarComoComprado()
    {
        yaComprado = true;

        if (fondoBoton != null)
            fondoBoton.color = new Color(0.3f, 0.3f, 0.3f); 

        if (textoPrecio != null)
            textoPrecio.text = "COMPRADO";
    }
}