using UnityEngine;
using TMPro;

public class MostrarCodigoSala : MonoBehaviour
{
    [Header("Referencia al Texto")]
    public TMP_Text textoUI; 

    void Start()
    {
        if (textoUI != null)
        {
            if (!string.IsNullOrEmpty(MenuConexion.codigoSalaGlobal))
            {
                textoUI.gameObject.SetActive(true);
                textoUI.text = "Código de Sala: " + MenuConexion.codigoSalaGlobal;
            }
            else
            {
                textoUI.gameObject.SetActive(true);
                textoUI.text = "Código de Sala: MODO_PRUEBA";
            }
        }
        else
        {
            Debug.LogError("MostrarCodigoSala: ¡Falta asignar el componente TMP_Text en el Inspector!");
        }
    }
}