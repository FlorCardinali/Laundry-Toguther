using Unity.Netcode;
using UnityEngine;

public class GeneradorDeRopa : NetworkBehaviour
{
    [Header("Configuración de Spawn")]
    public GameObject prefabRopaFisica;
    public Transform puntoDeSpawn;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GameObject nuevaRopa = Instantiate(prefabRopaFisica, puntoDeSpawn.position, puntoDeSpawn.rotation);
            nuevaRopa.GetComponent<NetworkObject>().Spawn();
        }
    }
}