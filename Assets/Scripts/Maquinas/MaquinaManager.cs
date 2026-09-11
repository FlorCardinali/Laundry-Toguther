using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class MaquinaManager : NetworkBehaviour
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

    [Header("Estado Interno")]
    public NetworkVariable<int> estado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> modoLavado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public TMP_Text textoPantalla;

    [Header("Memoria Interna (Ropa)")]
    public NetworkVariable<int> reqLavado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> reqSecado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> corrLavado = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> corrSecado = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        modoLavado.OnValueChanged += (viejo, nuevo) => ActualizarPantalla();
        estado.OnValueChanged += (viejo, nuevo) => ActualizarPantalla();
        ActualizarPantalla();
    }

    private void ActualizarPantalla()
    {
        if (textoPantalla == null) return;

        if (estado.Value == 0)
        {
            textoPantalla.text = "VACÍO";
            textoPantalla.color = Color.white;
        }
        else if (estado.Value == 2)
        {
            textoPantalla.text = textoAccion;
            textoPantalla.color = Color.cyan;
        }
        else if (estado.Value == 3)
        {
            textoPantalla.text = "¡LISTO!";
            textoPantalla.color = Color.green;
        }
        else
        {
            int m = modoLavado.Value;
            if (m >= 0 && m < nombresModos.Length)
            {
                textoPantalla.text = nombresModos[m];
                textoPantalla.color = coloresModos[m];
            }
        }
    }

    public void IniciarCiclo()
    {
        if (estado.Value == 1) StartCoroutine(RutinaTrabajo());
    }

    public void RecibirDatosDeRopa(ItemRopa ropa)
    {
        reqLavado.Value = ropa.lavadoRequerido.Value;
        reqSecado.Value = ropa.secadoRequerido.Value;
        corrLavado.Value = ropa.lavadoCorrectamente.Value;
        corrSecado.Value = ropa.secadoCorrectamente.Value;
    }

    private IEnumerator RutinaTrabajo()
    {
        estado.Value = 2;
        yield return new WaitForSeconds(tiempoDeCiclo);
        estado.Value = 3;
    }

    public ulong GenerarRopaProcesada()
    {
        if (esLavarropas)
        {
            return LavanderiaManager.Instancia.ProcesarLavado(
                reqLavado.Value, reqSecado.Value, corrLavado.Value, corrSecado.Value, modoLavado.Value, transform.position);
        }
        else
        {
            return LavanderiaManager.Instancia.ProcesarSecado(
                reqLavado.Value, reqSecado.Value, corrLavado.Value, corrSecado.Value, modoLavado.Value, transform.position);
        }
    }
}