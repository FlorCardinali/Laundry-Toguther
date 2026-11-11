using Unity.Netcode;
using UnityEngine;

public class MostradorInteraccion : NetworkBehaviour, IInteractuable
{
    [SerializeField] private float distanciaMaxima = 3.5f;

    public void Interactuar(PlayerController jugador)
    {
        if (!IsSpawned) return;
        if (jugador == null || !jugador.IsOwner) return;

        SolicitarAtencionServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SolicitarAtencionServerRpc(RpcParams rpcParams = default)
    {
        ulong idJugador = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
            idJugador, out NetworkClient cliente))
            return;

        if (cliente.PlayerObject == null) return;

        float distancia = Vector3.Distance(
            cliente.PlayerObject.transform.position,
            transform.position
        );

        if (distancia > distanciaMaxima) return;

        if (MostradorManager.Instancia == null) return;

        if (ColaManager.Instancia == null) return;

        NPCCliente npc = ColaManager.Instancia.ObtenerNpcEnMostrador();

        if (npc == null) return;
        if (npc.yaFueAtendido.Value) return;
        if (npc.estaSiendoAtendido.Value) return;

        npc.SolicitarAperturaPanelClientRpc(
            idJugador,
            npc.NetworkObjectId
        );
    }
}