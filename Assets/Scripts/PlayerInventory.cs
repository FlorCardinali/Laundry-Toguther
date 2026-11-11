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
    }

    // Funciones públicas para disparar las notas desde otros scripts
    public void DispararMostrarNota(int reqLav, int reqSec, bool corrLav, bool corrSec, string apodo, int estadoRopa) { OnMostrarNota?.Invoke(reqLav, reqSec, corrLav, corrSec, apodo, estadoRopa); }
    public void DispararOcultarNota() { OnOcultarNota?.Invoke(); }

    public void IntentarSoltar()
    {
        Debug.Log($"[Soltar] itemEnMano={itemEnMano.Value}, objetoOculto={objetoOcultoEnMano.Value}, IsOwner={IsOwner}");
        if (itemEnMano.Value != 0)
        {
            Vector3 posicionSegura = CalcularPosicionDeCaida();
            SoltarItemServerRpc(posicionSegura);
            OnOcultarNota?.Invoke();
        }
        else Debug.LogWarning("[Soltar] itemEnMano es 0, no hago nada");
    }

    private Vector3 CalcularPosicionDeCaida()
    {
        Vector3 origenPlayer = transform.position + Vector3.up * 0.5f;
        Vector3 direccionMirada = camaraJugador.transform.forward;
        Vector3 direccionPlana = new Vector3(direccionMirada.x, 0, direccionMirada.z).normalized;

        if (direccionPlana == Vector3.zero) direccionPlana = transform.forward;

        float distanciaCaida = 1.2f;
        Vector3 destinoIdeal = origenPlayer + (direccionPlana * distanciaCaida);

        if (Physics.Raycast(origenPlayer, direccionPlana, out RaycastHit hitPared, distanciaCaida))
        {
            destinoIdeal = hitPared.point - (direccionPlana * 0.3f);
        }

        Vector3 origenSuelo = destinoIdeal + Vector3.up * 2f;
        if (Physics.Raycast(origenSuelo, Vector3.down, out RaycastHit hitSuelo, 4f))
        {

            return hitSuelo.point + new Vector3(0, 0.2f, 0);
        }

        return destinoIdeal + new Vector3(0, 0.2f, 0);
    }

    [ServerRpc]
    public void AgarrarItemServerRpc(int idItem, ulong idObjetoRed)
    {
        if (itemEnMano.Value != 0) return;
        Debug.Log($"[Agarrar] entró. idObjetoRed={idObjetoRed}, itemEnMano antes={itemEnMano.Value}");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idObjetoRed, out NetworkObject objetoEnElMundo))
        {
            ItemRopa scriptRopa = objetoEnElMundo.GetComponent<ItemRopa>();
            if (scriptRopa == null) return;

            // 1. Setear NV primero
            itemEnMano.Value = scriptRopa.EstadoRopa;
            objetoOcultoEnMano.Value = idObjetoRed;

            // 2. Ocultar después
            scriptRopa.OcultarItemRopaClientRpc();

            // 3. Registrar retiro con try/catch
            if (MostradorManager.Instancia != null)
            {
                try { MostradorManager.Instancia.RegistrarBolsoRetirado(objetoEnElMundo.gameObject); }
                catch (System.Exception e) { Debug.LogError("Error en RegistrarBolsoRetirado: " + e); }
            }

            // 4. Nota al final
            MostrarNotaClientRpc(
                scriptRopa.lavadoRequerido.Value,
                scriptRopa.secadoRequerido.Value,
                scriptRopa.lavadoCorrectamente.Value,
                scriptRopa.secadoCorrectamente.Value,
                scriptRopa.apodoDueno.Value.ToString(),
                scriptRopa.EstadoRopa);
        }
        Debug.Log($"[Agarrar] itemEnMano ahora={itemEnMano.Value}, objetoOculto={objetoOcultoEnMano.Value}");
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

    [ClientRpc]
    public void MostrarNotaClientRpc(int reqLav, int reqSec, bool corrLav, bool corrSec, string apodo, int estadoRopa)
    {
        if (!IsOwner) return;
        OnMostrarNota?.Invoke(reqLav, reqSec, corrLav, corrSec, apodo, estadoRopa);
    }

    private void ActualizarVisuales(int idItem)
    {
        for (int i = 0; i < itemsVisuales.Length; i++)
        {
            if (itemsVisuales[i] != null)
            {
                Debug.Log($"[Visuales] idItem={idItem}, itemsVisuales.Length={itemsVisuales.Length}");
                bool activo = (idItem > 0) && (i == (idItem - 1)) && (i < itemsVisuales.Length);
                itemsVisuales[i].SetActive(activo);
            }
        }
    }
}