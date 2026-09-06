using Unity.Netcode;
using UnityEngine;

public class ItemRopa : NetworkBehaviour, IInteractuable
{
    public int idItem;

    [Header("Componentes a Ocultar")]
    public MeshRenderer mallaVisual;
    public Collider colisionador;
    public Rigidbody fisicas;

    [Header("Requisitos del Cliente")]
    public NetworkVariable<int> lavadoRequerido = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> secadoRequerido = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> lavadoCorrectamente = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> secadoCorrectamente = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool yaSeGeneroPedido = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer && idItem == 1 && !yaSeGeneroPedido)
        {
            lavadoRequerido.Value = Random.Range(0, 3);
            secadoRequerido.Value = Random.Range(0, 3);
            yaSeGeneroPedido = true;
        }
    }

    public void Interactuar(PlayerController jugador)
    {
        ulong idRed = GetComponent<NetworkObject>().NetworkObjectId;
        jugador.AgarrarItemServerRpc(idItem, idRed);
    }

    [ClientRpc]
    public void OcultarClientRpc()
    {
        if (mallaVisual != null) mallaVisual.enabled = false;
        if (colisionador != null) colisionador.enabled = false;
        if (fisicas != null) fisicas.isKinematic = true;
    }

    [ClientRpc]
    public void MostrarClientRpc()
    {
        if (mallaVisual != null) mallaVisual.enabled = true;
        if (colisionador != null) colisionador.enabled = true;
        if (fisicas != null) fisicas.isKinematic = false;
    }
}