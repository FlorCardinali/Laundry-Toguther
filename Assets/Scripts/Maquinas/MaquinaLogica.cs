using UnityEngine;
using TMPro;
using System;

public class MaquinaLogica : MonoBehaviour, IMediadorInteraccion
{
    [Header("Configuración de la Máquina")]
    public string textoAccion = "LAVANDO...";
    public float tiempoDeCiclo = 5f;
    public int idItemRequerido = 1;
    public int idItemDevuelto = 2;
    public bool esLavarropas = true;

    [Header("Configuración de Modos")]
    public string[] nombresModos = new string[] { "SUAVE", "NORMAL", "FUERTE" };
    public Color[] coloresModos = new Color[] { Color.yellow, new Color(1f, 0.5f, 0f), Color.red };

    [Header("Estado Local (Sin Red)")]
    public int estadoLocal = 0;
    public int modoLavadoLocal = 0;

    public TMP_Text textoPantalla;

    public event Action<TipoAccion, ulong> OnInteraccionSolicitada;

    public void ProcesarInteraccion(TipoAccion accion, ulong idJugador, int parametroExtra = 0)
    {
        OnInteraccionSolicitada?.Invoke(accion, idJugador);
    }

    // El Envoltorio de red o el manager local llamarán a estos métodos para cambiar la visual
    public void ForzarActualizacionVisual(int nuevoEstado, int nuevoModo)
    {
        estadoLocal = nuevoEstado;
        modoLavadoLocal = nuevoModo;
        ActualizarPantalla();
    }

    private void ActualizarPantalla()
    {
        if (textoPantalla == null) return;

        if (estadoLocal == 0)
        {
            textoPantalla.text = "VACÍO";
            textoPantalla.color = Color.white;
        }
        else if (estadoLocal == 2)
        {
            textoPantalla.text = textoAccion;
            textoPantalla.color = Color.cyan;
        }
        else if (estadoLocal == 3)
        {
            textoPantalla.text = "¡LISTO!";
            textoPantalla.color = Color.green;
        }
        else
        {
            int m = modoLavadoLocal;
            if (m >= 0 && m < nombresModos.Length)
            {
                textoPantalla.text = nombresModos[m];
                textoPantalla.color = coloresModos[m];
            }
        }
    }
}