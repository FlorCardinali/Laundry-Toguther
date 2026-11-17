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

    [Header("Progresión de Clientes por Días")]
    [Tooltip("Tiempo base entre spawns en el Día 1 (en segundos).")]
    public float tiempoBaseSpawn = 35f;
    [Tooltip("Cuántos segundos se reduce el tiempo entre spawns por cada día transcurrido.")]
    public float reduccionTiempoPorDia = 2f;
    [Tooltip("Límite mínimo de segundos entre spawns para que el juego sea jugable.")]
    public float tiempoMinimoSpawn = 8f;

    private float temporizadorSpawn = 0f;
    private int clientesSpawneadosHoy = 0;
    private int diaRegistrado = -1;

    // Lista que almacena los NPCs que están haciendo fila actualmente
    private List<NPCCliente> clientesEnFila = new List<NPCCliente>();

    private void Awake()
    {
        if (Instancia != null && Instancia != this) Destroy(gameObject);
        else Instancia = this;
    }

    private void Update()
    {
        if (!IsServer) return;

        // Detectar si cambió el día para reiniciar el contador diario
        int diaActual = DiaManager.Instancia != null ? DiaManager.Instancia.diaActual.Value : 1;
        if (diaActual != diaRegistrado)
        {
            diaRegistrado = diaActual;
            clientesSpawneadosHoy = 0;
            Debug.Log($"[ColaManager] Nuevo día detectado ({diaActual}). Contador de spawns reiniciado.");
        }

        if (DiaManager.Instancia != null && DiaManager.Instancia.diaCerrado.Value) return;

        // Verificar si ya alcanzamos el límite de clientes permitidos para el día actual
        int maxClientesHoy = CalcularClientesMaximosDelDia(diaActual);
        if (clientesSpawneadosHoy >= maxClientesHoy) return;

        // Si la fila está llena, espera a que se libere un espacio
        if (clientesEnFila.Count >= puntosDeFila.Length) return;

        // Controlar el intervalo dinámico según el día actual
        float intervaloActual = Mathf.Max(tiempoMinimoSpawn, tiempoBaseSpawn - ((diaActual - 1) * reduccionTiempoPorDia));

        temporizadorSpawn += Time.deltaTime;
        if (temporizadorSpawn >= intervaloActual)
        {
            temporizadorSpawn = 0f;
            IntentarSpawnearCliente();
        }
    }

    private int CalcularClientesMaximosDelDia(int dia)
    {
        if (dia <= 5)
        {
            return 5 + (dia - 1);
        }
        else
        {
            int clientesExtra = Mathf.RoundToInt((dia - 5) * 1.5f);
            return Mathf.Min(30, 9 + clientesExtra);
        }
    }

    private void IntentarSpawnearCliente()
    {
        if (prefabNPC != null && puntoSpawnNPCCalle != null)
        {
            GameObject nuevoNpcObj = Instantiate(prefabNPC, puntoSpawnNPCCalle.position, puntoSpawnNPCCalle.rotation);
            NetworkObject netObj = nuevoNpcObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            NPCCliente npcScript = nuevoNpcObj.GetComponent<NPCCliente>();
            if (npcScript != null)
            {
                clientesEnFila.Add(npcScript);
                clientesSpawneadosHoy++;
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