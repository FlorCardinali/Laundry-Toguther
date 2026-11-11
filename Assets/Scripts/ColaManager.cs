using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ColaManager : NetworkBehaviour
{
    public static ColaManager Instancia { get; private set; }

    [Header("Configuración de Spawn y Fila")]
    public GameObject prefabNPC;
    public Transform puntoSpawnNPCCalle;
    public Transform[] puntosDeFila;

    [Header("Zona de Espera (Wandering)")]
    public Transform zonaEsperaWandering;
    public float radioZonaEspera = 4f;   
    public float tiempoEntrePuntosWandering = 4f;

    [Header("Tiempos de Spawn")]
    public float tiempoEntreSpawns = 35f;
    private float temporizadorSpawn = 0f;

    // Lista que almacena los NPCs que están haciendo fila actualmente
    private List<NPCCliente> clientesEnFila = new List<NPCCliente>();

    //singleton
    private void Awake()
    {
        if (Instancia != null && Instancia != this) Destroy(gameObject);
        else Instancia = this;
    }

    private void Update()
    {
        if (!IsServer) return;

        temporizadorSpawn += Time.deltaTime;
        if (temporizadorSpawn >= tiempoEntreSpawns)
        {
            temporizadorSpawn = 0f;
            IntentarSpawnearCliente();
        }
    }

    private void IntentarSpawnearCliente()
    {
        // Si la fila está llena esperaa
        if (clientesEnFila.Count >= puntosDeFila.Length) return;

        if (prefabNPC != null && puntoSpawnNPCCalle != null)
        {
            GameObject nuevoNpcObj = Instantiate(prefabNPC, puntoSpawnNPCCalle.position, puntoSpawnNPCCalle.rotation);
            NetworkObject netObj = nuevoNpcObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            NPCCliente npcScript = nuevoNpcObj.GetComponent<NPCCliente>();
            if (npcScript != null)
            {
                clientesEnFila.Add(npcScript);
                ActualizarPosicionesFila();
            }
        }
    }

    public void ClienteAtendido(NPCCliente clienteAtendido)
    {
        if (clientesEnFila.Contains(clienteAtendido))
        {
            clientesEnFila.Remove(clienteAtendido);
            ActualizarPosicionesFila();
        }
    }

    private void ActualizarPosicionesFila()
    {
        for (int i = 0; i < clientesEnFila.Count; i++)
        {
            if (i < puntosDeFila.Length)
            {
                clientesEnFila[i].MoverHacia(puntosDeFila[i].position);

               
            }
        }
    }


    public NPCCliente ObtenerNpcEnMostrador()
    {
        if (clientesEnFila.Count > 0)
            return clientesEnFila[0];

        return null;
    }
}