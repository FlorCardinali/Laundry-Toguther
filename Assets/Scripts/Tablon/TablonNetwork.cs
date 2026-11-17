using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TablonLogica))]
public class TablonNetwork : NetworkBehaviour
{
    private TablonLogica logica;

    [Header("Prefabs a spawnear (Red)")]
    public GameObject prefabLavarropas;
    public GameObject prefabSecarropas;
    public GameObject prefabReiniciadora;


    [Header("Puntos de Aparición (Spawns)")]
    public Transform spawnLavarropas1;
    public Transform spawnSecarropas1;
    public Transform spawnReiniciadora1;

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

        int precio = logica.ObtenerPrecio(idMejora);

        if (DineroManager.Instancia == null) return;
        if (!DineroManager.Instancia.TieneSuficiente(precio))
        {
            Debug.Log("Dinero insuficiente para mejora " + idMejora);
            return;
        }

        DineroManager.Instancia.RestarDinero(precio);

        switch (idMejora)
        {
            case 1:
                InstanciarObjetoRed(prefabLavarropas, spawnLavarropas1);
                break;
            case 2:
                InstanciarObjetoRed(prefabSecarropas, spawnSecarropas1);
                break;
            case 3:
                InstanciarObjetoRed(prefabReiniciadora, spawnReiniciadora1);
                break;
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