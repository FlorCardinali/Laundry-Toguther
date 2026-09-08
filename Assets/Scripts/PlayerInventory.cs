using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Referencias Físicas")]
    public Camera camaraJugador;
    public Transform puntoDeCaida;

    [Header("Inventario (Visuales y Físicos)")]
    public GameObject[] itemsVisuales;
    public GameObject[] prefabsFisicos;

    public NetworkVariable<ulong> objetoOcultoEnMano = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> itemEnMano = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<int, int, bool, bool> OnMostrarNota;
    public event Action OnOcultarNota;

    public override void OnNetworkSpawn()
    {
        ActualizarVisuales(itemEnMano.Value);
        itemEnMano.OnValueChanged += (viejo, nuevo) => {
            ActualizarVisuales(nuevo);
            if (nuevo == 0) OnOcultarNota?.Invoke();
        };
    }

    public override void OnNetworkDespawn()
    {
        itemEnMano.OnValueChanged -= (viejo, nuevo) => ActualizarVisuales(nuevo);
    }

    // Funciones públicas para disparar las notas desde otros scripts
    public void DispararMostrarNota(int reqLav, int reqSec, bool corrLav, bool corrSec) { OnMostrarNota?.Invoke(reqLav, reqSec, corrLav, corrSec); }
    public void DispararOcultarNota() { OnOcultarNota?.Invoke(); }

    public void IntentarSoltar()
    {
        if (itemEnMano.Value != 0)
        {
            Vector3 posicionSegura = CalcularPosicionDeCaida();
            SoltarItemServerRpc(posicionSegura);
            OnOcultarNota?.Invoke();
        }
    }

    private Vector3 CalcularPosicionDeCaida()
    {
        Vector3 origen = camaraJugador.transform.position;
        Vector3 destino = puntoDeCaida.position;
        Vector3 direccion = destino - origen;
        float distancia = Vector3.Distance(origen, destino);
        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, distancia))
            return hit.point - (direccion.normalized * 0.3f);
        return destino;
    }

    [ServerRpc]
    public void AgarrarItemServerRpc(int idItem, ulong idObjetoRed)
    {
        if (itemEnMano.Value != 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idObjetoRed, out NetworkObject objetoEnElMundo))
        {
            ItemRopa scriptRopa = objetoEnElMundo.GetComponent<ItemRopa>();
            scriptRopa.OcultarClientRpc();

            itemEnMano.Value = idItem;
            objetoOcultoEnMano.Value = idObjetoRed;

            MostrarNotaClientRpc(scriptRopa.lavadoRequerido.Value, scriptRopa.secadoRequerido.Value, scriptRopa.lavadoCorrectamente.Value, scriptRopa.secadoCorrectamente.Value);
        }
    }

    [ServerRpc]
    private void SoltarItemServerRpc(Vector3 posicionSegura)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetoOcultoEnMano.Value, out NetworkObject objetoOculto))
        {
            NetworkTransform netTransform = objetoOculto.GetComponent<NetworkTransform>();
            if (netTransform != null) netTransform.Teleport(posicionSegura, Quaternion.identity, objetoOculto.transform.localScale);
            else objetoOculto.transform.position = posicionSegura;

            ItemRopa scriptRopa = objetoOculto.GetComponent<ItemRopa>();
            scriptRopa.MostrarClientRpc();
        }
        itemEnMano.Value = 0;
        objetoOcultoEnMano.Value = 0;
    }

    [ClientRpc]
    public void MostrarNotaClientRpc(int reqLav, int reqSec, bool corrLav, bool corrSec)
    {
        OnMostrarNota?.Invoke(reqLav, reqSec, corrLav, corrSec);
    }

    private void ActualizarVisuales(int idItem)
    {
        for (int i = 0; i < itemsVisuales.Length; i++)
        {
            if (itemsVisuales[i] != null) itemsVisuales[i].SetActive(i == (idItem - 1));
        }
    }
}