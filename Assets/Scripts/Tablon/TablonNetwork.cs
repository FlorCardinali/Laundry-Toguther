using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TablonLogica))]
public class TablonNetwork : NetworkBehaviour
{
    private TablonLogica logica;

    [Header("Prefabs a spawnear (Red)")]
    public GameObject prefabLavarropas;

    [Header("Puntos de Aparición (Spawns)")]
    public Transform spawnLavarropas1;

    void Awake()
    {
        logica = GetComponent<TablonLogica>();
    }

    public override void OnNetworkSpawn()
    {
        logica.OnInteraccionComprar += InterceptarCompra;
    }

    public override void OnNetworkDespawn()
    {
        logica.OnInteraccionComprar -= InterceptarCompra;
    }

    private void InterceptarCompra(int idMejora)
    {
        if (!IsSpawned) return;
        ComprarMejoraServerRpc(idMejora);
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ComprarMejoraServerRpc(int idMejora)
    {
        if (logica.mejorasCompradas.Contains(idMejora)) return;

        switch (idMejora)
        {
            case 1:
                InstanciarObjetoRed(prefabLavarropas, spawnLavarropas1);
                break;
                // case 2: ...
        }
        ConfirmarCompraClientRpc(idMejora);
    }

    private void InstanciarObjetoRed(GameObject prefab, Transform puntoSpawn)
    {
        GameObject nuevoObjeto = Instantiate(prefab, puntoSpawn.position, puntoSpawn.rotation);
        nuevoObjeto.GetComponent<NetworkObject>().Spawn();
    }

    [ClientRpc]
    private void ConfirmarCompraClientRpc(int idMejora)
    {
        logica.ConfirmarCompraLocal(idMejora);
    }
}