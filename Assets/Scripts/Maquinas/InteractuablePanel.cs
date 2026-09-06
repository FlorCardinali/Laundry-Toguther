using Unity.Netcode;
using UnityEngine;

public class InteractuablePanel : MonoBehaviour, IInteractuable
{
    public void Interactuar(PlayerController jugador)
    {
        // Buscamos el ID del padre (el lavarropas)
        ulong idLavarropas = GetComponentInParent<NetworkObject>().NetworkObjectId;
        jugador.InteractuarPanelServerRpc(idLavarropas);
    }
}