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

    [Header("Configuración de Soltar")]
    public float distanciaMaximaSoltar = 3f;
    public float alturaOffset = 0.25f;
    public LayerMask capasSoltables;

    public NetworkVariable<ulong> objetoOcultoEnMano = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> itemEnMano = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<int, int, bool, bool, string, int> OnMostrarNota;
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

        if (IsServer && itemEnMano.Value != 0 && objetoOcultoEnMano.Value != 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetoOcultoEnMano.Value, out NetworkObject objetoOculto))
            {
                Vector3 posicionCaida = transform.position + transform.forward * 0.5f + Vector3.up * 0.25f;

                NetworkTransform netTransform = objetoOculto.GetComponent<NetworkTransform>();
                if (netTransform != null)
                {
                    netTransform.Teleport(posicionCaida, Quaternion.identity, objetoOculto.transform.localScale);
                }
                else
                {
                    objetoOculto.transform.position = posicionCaida;
                }

                ItemRopa scriptRopa = objetoOculto.GetComponent<ItemRopa>();
                if (scriptRopa != null)
                {
                    scriptRopa.MostrarClientRpc();
                }
            }
        }
    }

    // Funciones públicas para disparar las notas desde otros scripts
    public void DispararMostrarNota(int reqLav, int reqSec, bool corrLav, bool corrSec, string apodo, int estadoRopa) { OnMostrarNota?.Invoke(reqLav, reqSec, corrLav, corrSec, apodo, estadoRopa); }
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
        Ray ray = camaraJugador.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaximaSoltar, capasSoltables))
        {
            if (!HayObstaculoArriba(hit.point))
            {
                return hit.point + Vector3.up * alturaOffset;
            }
        }

        Vector3 puntoFrente = transform.position + transform.forward * 1.8f;
        Vector3 origen = puntoFrente + Vector3.up * 2f;

        if (Physics.Raycast(origen, Vector3.down, out RaycastHit hitSuelo, 5f, capasSoltables))
        {
            if (!HayObstaculoArriba(hitSuelo.point))
            {
                return hitSuelo.point + Vector3.up * alturaOffset;
            }
        }

        return transform.position + transform.forward * 1.2f + Vector3.up * alturaOffset;
    }

    private bool HayObstaculoArriba(Vector3 puntoSuelo)
    {
        Collider[] cols = Physics.OverlapSphere(puntoSuelo + Vector3.up * 0.6f, 0.4f);

        foreach (var col in cols)
        {
            if (col.gameObject.layer != LayerMask.NameToLayer("Suelo") &&
                col.GetComponent<ItemRopa>() == null)
            {
                return true;
            }
        }
        return false;
    }

    [ServerRpc]
    public void AgarrarItemServerRpc(int idItem, ulong idObjetoRed)
    {
        if (itemEnMano.Value != 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idObjetoRed, out NetworkObject objetoEnElMundo))
        {
            ItemRopa scriptRopa = objetoEnElMundo.GetComponent<ItemRopa>();
            if (scriptRopa == null) return;

            itemEnMano.Value = scriptRopa.EstadoRopa;
            objetoOcultoEnMano.Value = idObjetoRed;

            scriptRopa.OcultarItemRopaClientRpc();

            if (MostradorManager.Instancia != null)
            {
                try { MostradorManager.Instancia.RegistrarBolsoRetirado(objetoEnElMundo.gameObject); }
                catch (System.Exception e) { Debug.LogError("Error en RegistrarBolsoRetirado: " + e); }
            }

            MostrarNotaClientRpc(
                scriptRopa.lavadoRequerido.Value,
                scriptRopa.secadoRequerido.Value,
                scriptRopa.lavadoCorrectamente.Value,
                scriptRopa.secadoCorrectamente.Value,
                scriptRopa.apodoDueno.Value.ToString(),
                scriptRopa.EstadoRopa);
        }
    }

    [ServerRpc]
    private void SoltarItemServerRpc(Vector3 posicionSegura)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetoOcultoEnMano.Value, out NetworkObject objetoOculto))
        {
            NetworkTransform netTransform = objetoOculto.GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.Teleport(posicionSegura, Quaternion.identity, objetoOculto.transform.localScale);
            }
            else
            {
                objetoOculto.transform.position = posicionSegura;
            }

            ItemRopa scriptRopa = objetoOculto.GetComponent<ItemRopa>();
            scriptRopa.MostrarClientRpc();
        }
        itemEnMano.Value = 0;
        objetoOcultoEnMano.Value = 0;
    }

    //para pedir al server msotrar nota
    [ClientRpc]
    public void MostrarNotaClientRpc(int reqLav, int reqSec, bool corrLav, bool corrSec, string apodo, int estadoRopa)
    {
        if (!IsOwner) return;
        OnMostrarNota?.Invoke(reqLav, reqSec, corrLav, corrSec, apodo, estadoRopa);
    }

    //esta cosa cambia los modelos falsos que sostiene cada jugador apra simular que sostiene cosas
    private void ActualizarVisuales(int idItem)
    {
        for (int i = 0; i < itemsVisuales.Length; i++)
        {
            if (itemsVisuales[i] != null)
            {
                bool activo = (idItem > 0) && (i == (idItem - 1)) && (i < itemsVisuales.Length);
                itemsVisuales[i].SetActive(activo);
            }
        }
    }
}