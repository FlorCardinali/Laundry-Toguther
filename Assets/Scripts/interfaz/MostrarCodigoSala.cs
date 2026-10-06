using UnityEngine;
using TMPro;

public class MostrarCodigoSala : MonoBehaviour
{
    private TMP_Text textoUI;

    void Start()
    {
        textoUI = GetComponent<TMP_Text>();

        if (!string.IsNullOrEmpty(MenuConexion.codigoSalaGlobal))
        {
            textoUI.text = "Código de Sala: " + MenuConexion.codigoSalaGlobal;
        }
        else
        {
            textoUI.gameObject.SetActive(false); 
        }
    }
}