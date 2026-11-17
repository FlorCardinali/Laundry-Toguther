using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerSpawn : NetworkBehaviour
{
    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            StartCoroutine(ActivarControlador());
        }
    }

    private IEnumerator ActivarControlador()
    {
        yield return null;

        if (controller != null) controller.enabled = true;
    }
}