using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections; 

public class LavarropasManager : NetworkBehaviour
{
    // Estados: 0 = Vacío, 1 = Esperando Inicio, 2 = Lavando, 3 = Terminado
    public NetworkVariable<int> estado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> modoLavado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public TMP_Text textoPantalla;

    public override void OnNetworkSpawn()
    {
        modoLavado.OnValueChanged += (viejo, nuevo) => ActualizarPantalla();
        estado.OnValueChanged += (viejo, nuevo) => ActualizarPantalla();
        ActualizarPantalla();
    }

    private void ActualizarPantalla()
    {
        if (textoPantalla == null) return;

        if (estado.Value == 0) { textoPantalla.text = "VACÍO"; textoPantalla.color = Color.white; }
        else if (estado.Value == 2) { textoPantalla.text = "LAVANDO..."; textoPantalla.color = Color.cyan; }
        else if (estado.Value == 3) { textoPantalla.text = "¡LISTO!"; textoPantalla.color = Color.green; }
        else // Estado 1 (Configurando modos)
        {
            switch (modoLavado.Value)
            {
                case 0: textoPantalla.text = "SUAVE"; textoPantalla.color = Color.yellow; break;
                case 1: textoPantalla.text = "NORMAL"; textoPantalla.color = new Color(1f, 0.5f, 0f); break;
                case 2: textoPantalla.text = "FUERTE"; textoPantalla.color = Color.red; break;
            }
        }
    }

    // El servidor ejecuta esto para simular el lavado
    public void IniciarCicloDeLavado()
    {
        if (estado.Value == 1) StartCoroutine(RutinaLavar());
    }

    private IEnumerator RutinaLavar()
    {
        estado.Value = 2; 
        yield return new WaitForSeconds(5f);
        estado.Value = 3; 
    }
}