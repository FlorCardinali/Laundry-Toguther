using Unity.Netcode;
using UnityEngine;

public class InteractuableTambor : MonoBehaviour, IInteractuable
{
    public void Interactuar(PlayerController jugador)
    {
        // Buscamos el ID del padre (el lavarropas)
        ulong idLavarropas = GetComponentInParent<NetworkObject>().NetworkObjectId;
        jugador.InteractuarTamborServerRpc(idLavarropas);
    }
}