using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractuableMejora : MonoBehaviour, IInteractuable
{
    public int idMejora;
    public Image fondoBoton;
    public TMP_Text textoPrecio;
    public bool yaComprado = false;

    private IMediadorInteraccion mediador;

    void Awake() => mediador = GetComponentInParent<IMediadorInteraccion>();

    public void Interactuar(PlayerController jugador)
    {
        if (yaComprado) return; 
        
        // Pasamos el idMejora como parámetro extra
        mediador?.ProcesarInteraccion(TipoAccion.ComprarMejora, jugador.NetworkObjectId, idMejora);
    }

    public void MarcarComoComprado()
    {
        yaComprado = true; 
        if (fondoBoton != null) fondoBoton.color = new Color(0.3f, 0.3f, 0.3f); 
        if (textoPrecio != null) textoPrecio.text = "COMPRADO";
    }
}