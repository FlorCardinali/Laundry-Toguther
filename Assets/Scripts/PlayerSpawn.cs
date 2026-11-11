using UnityEngine;
using Unity.Netcode;

public class PlayerSpawn : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Solo movemos a nuestro propio personaje local
        if (IsOwner)
        {
            if (SpawnManager.Instance != null)
            {
                // Obtenemos la posición exacta según el ID de red de este cliente
                Vector3 nuevaPosicion = SpawnManager.Instance.ObtenerPosicionParaCliente(OwnerClientId);

                CharacterController cc = GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                transform.position = nuevaPosicion;

                if (cc != null) cc.enabled = true;
            }
        }
    }
}