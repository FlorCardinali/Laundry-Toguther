using Unity.Netcode;
using UnityEngine;

public class InteractuableInicio : MonoBehaviour, IInteractuable
{
    public void Interactuar(PlayerController jugador)
    {
        MaquinaManager maquina = GetComponentInParent<MaquinaManager>();
        if (maquina != null)
        {
            // Usamos el nombre nuevo del RPC
            LavanderiaManager.Instancia.InteractuarInicioRpc(maquina.NetworkObjectId);
        }
    }
}