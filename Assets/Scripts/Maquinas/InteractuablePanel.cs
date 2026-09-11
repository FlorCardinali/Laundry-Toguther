using Unity.Netcode;
using UnityEngine;

public class InteractuablePanel : MonoBehaviour, IInteractuable
{
    public void Interactuar(PlayerController jugador)
    {
        MaquinaManager maquina = GetComponentInParent<MaquinaManager>();
        if (maquina != null)
        {
            // Usamos el nombre nuevo del RPC
            LavanderiaManager.Instancia.InteractuarPanelRpc(maquina.NetworkObjectId);
        }
    }
}