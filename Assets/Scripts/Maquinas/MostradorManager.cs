
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class MostradorManager : NetworkBehaviour
{
    public static MostradorManager Instancia { get; private set; }

    [Header("Puntos Fijos de los Bolsos (0 al 3)")]
    public Transform[] puntosBolsos;

    [Header("Estado del Mostrador")]
    public NetworkVariable<bool> estaOcupado = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Pedido Actual")]
    public NetworkVariable<ulong> idNpcPedidoActual = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private List<GameObject> bolsosEnMesa = new List<GameObject>();

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
            Destroy(gameObject);
        else
            Instancia = this;
    }

    public bool IntentarDepositarPedido(
        GameObject prefabBolsa,
        int cantidadBolsos,
        ulong idNpcDueño)
    {
        if (!IsServer) return false;
        if (estaOcupado.Value) return false;
        if (prefabBolsa == null) return false;

        estaOcupado.Value = true;
        idNpcPedidoActual.Value = idNpcDueño;

        bolsosEnMesa.Clear();

        for (int i = 0; i < cantidadBolsos && i < puntosBolsos.Length; i++)
        {
            GameObject nuevoBolso = Instantiate(
                prefabBolsa,
                puntosBolsos[i].position,
                puntosBolsos[i].rotation
            );

            NetworkObject netObjBolso = nuevoBolso.GetComponent<NetworkObject>();
            netObjBolso.Spawn();

            ItemRopa scriptRopa = nuevoBolso.GetComponent<ItemRopa>();

            if (scriptRopa != null)
            {
                scriptRopa.idDuenoNpc = idNpcDueño;
                scriptRopa.OcultarItemRopaClientRpc();
                scriptRopa.MostrarClientRpc(false);
            }

            bolsosEnMesa.Add(nuevoBolso);
        }

        return true;
    }

    public NPCCliente ObtenerNpcPedidoActual()
    {
        if (!IsServer) return null;
        if (!estaOcupado.Value) return null;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            idNpcPedidoActual.Value,
            out NetworkObject npcObjeto))
        {
            return npcObjeto.GetComponent<NPCCliente>();
        }

        return null;
    }

    public void RegistrarBolsoRetirado(GameObject bolsoRetirado)
    {
        if (!IsServer) return;

        if (bolsosEnMesa.Contains(bolsoRetirado))
            bolsosEnMesa.Remove(bolsoRetirado);

        if (bolsosEnMesa.Count == 0)
        {
            ulong idNpc = idNpcPedidoActual.Value;

            estaOcupado.Value = false;
            idNpcPedidoActual.Value = 0;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                idNpc, out NetworkObject npcObj))
            {
                NPCCliente npc = npcObj.GetComponent<NPCCliente>();
                if (npc != null)
                {
                    npc.IniciarWanderingServerRpc();
                }
            }
        }
    }

    public void AsignarApodoABolsosActuales(string apodo)
    {
        if (!IsServer) return;

        foreach (GameObject bolso in bolsosEnMesa)
        {
            if (bolso == null) continue;

            ItemRopa ropa = bolso.GetComponent<ItemRopa>();
            if (ropa != null)
            {
                // Necesitamos una NetworkVariable o campo para el apodo en ItemRopa
                ropa.apodoDueno.Value = apodo;
            }
        }
    }
}
