using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerSpawn : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            StartCoroutine(SpawnearAlSiguienteFrame());
        }
    }

    private IEnumerator SpawnearAlSiguienteFrame()
    {
        yield return null;

        if (SpawnManager.Instance != null)
        {
            Vector3 nuevaPosicion = SpawnManager.Instance.ObtenerPosicionParaCliente(OwnerClientId);

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            transform.position = nuevaPosicion;

            // Forzamos a que el CharacterController se entere del nuevo lugar
            if (cc != null) cc.enabled = true;

            // Opcional: si usás NetworkTransform, forzamos el teletransporte
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.Teleport(nuevaPosicion, transform.rotation, transform.localScale);
            }
        }
    }
}