using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class TablonManager : NetworkBehaviour
{
    [Header("Prefabs a spawnear")]
    public GameObject prefabLavarropas;

    [Header("Puntos de Aparición (Spawns)")]
    public Transform spawnLavarropas1;

    [Header("Botones de la Interfaz")]
    public InteractuableMejora[] botonesDeMejora;
    private List<int> mejorasCompradas = new List<int>();

    public void ProcesarCompra(PlayerController jugador, int idMejora)
    {
        ulong idTablonRed = GetComponent<NetworkObject>().NetworkObjectId;
        jugador.ComprarMejoraServerRpc(idTablonRed, idMejora);
    }

    // El servidor ejecuta esto
    public void InstanciarMejora(int idMejora)
    {
        if (mejorasCompradas.Contains(idMejora)) return; 

        mejorasCompradas.Add(idMejora);

        switch (idMejora)
        {
            case 1: 
                InstanciarObjeto(prefabLavarropas, spawnLavarropas1);
                break;
                // case 2: etc...
        }

        ActualizarUIClientRpc(idMejora);
    }

    private void InstanciarObjeto(GameObject prefab, Transform puntoSpawn)
    {
        GameObject nuevoObjeto = Instantiate(prefab, puntoSpawn.position, puntoSpawn.rotation);
        nuevoObjeto.GetComponent<NetworkObject>().Spawn();
    }

    [ClientRpc]
    private void ActualizarUIClientRpc(int idMejora)
    {
        foreach (InteractuableMejora boton in botonesDeMejora)
        {
            if (boton.idMejora == idMejora)
            {
                boton.MarcarComoComprado();
                break; 
            }
        }
    }
}