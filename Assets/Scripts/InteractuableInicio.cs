using Unity.Netcode;
using UnityEngine;

public class InteractuableInicio : MonoBehaviour, IInteractuable
{
    public void Interactuar(PlayerController jugador)
    {
        ulong idLavarropas = GetComponentInParent<NetworkObject>().NetworkObjectId;
        jugador.InteractuarInicioServerRpc(idLavarropas);
    }
}