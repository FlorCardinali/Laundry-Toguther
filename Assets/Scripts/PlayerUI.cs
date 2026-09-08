using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    public GameObject panelNota;
    public TMP_Text textoLavado;
    public TMP_Text textoSecado;

    private string[] nombresLavado = { "SUAVE", "NORMAL", "FUERTE" };
    private string[] nombresSecado = { "40°C", "50°C", "65°C" };

    private PlayerInventory inventarioLocal;

    public void ConectarConJugador(PlayerInventory inventario)
    {
        inventarioLocal = inventario;

        gameObject.name = ">>> MI_CANVAS_LOCAL <<<";

        if (inventarioLocal != null)
        {
            inventarioLocal.OnMostrarNota += ActualizarTextosNota;
            inventarioLocal.OnOcultarNota += OcultarPanelNota;
            Debug.Log("5. PlayerUI: ¡Suscrito a la radio del inventario con éxito!");
        }

        OcultarPanelNota();
    }

    private void OnDestroy()
    {
        if (inventarioLocal != null)
        {
            inventarioLocal.OnMostrarNota -= ActualizarTextosNota;
            inventarioLocal.OnOcultarNota -= OcultarPanelNota;
        }
    }

    private void ActualizarTextosNota(int reqLav, int reqSec, bool corrLav, bool corrSec)
    {
        Debug.Log($"6. PlayerUI: Recibí la señal. Requisitos -> Lavado: {reqLav}, Secado: {reqSec}");

        if (panelNota == null)
        {
            Debug.LogError("PlayerUI: ¡El Panel de la nota está vacío en el Inspector!");
            return;
        }

        string txtLav = "Lavado: " + nombresLavado[reqLav];
        if (corrLav) txtLav = "<s>" + txtLav + "</s>";

        string txtSec = "Secado: " + nombresSecado[reqSec];
        if (corrSec) txtSec = "<s>" + txtSec + "</s>";

        textoLavado.text = txtLav;
        textoSecado.text = txtSec;

        panelNota.SetActive(true);
        Debug.Log("7. PlayerUI: Panel encendido. ¡Si no lo ves, se dibujó fuera de la pantalla!");
    }

    private void OcultarPanelNota()
    {
        if (panelNota != null) panelNota.SetActive(false);
    }
}