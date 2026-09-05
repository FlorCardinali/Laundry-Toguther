using Unity.Netcode;
using UnityEngine;

public class ItemRopa : NetworkBehaviour, IInteractuable
{
    [Header("Configuración")]
    public int idItem;

    public void Interactuar(PlayerController jugador)
    {
        ulong idDeRed = GetComponent<NetworkObject>().NetworkObjectId;
        jugador.AgarrarItemServerRpc(idItem, idDeRed);
    }
}