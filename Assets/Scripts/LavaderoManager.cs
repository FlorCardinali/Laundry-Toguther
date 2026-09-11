using Unity.Netcode;
using UnityEngine;

public class LavanderiaManager : NetworkBehaviour
{
    public static LavanderiaManager Instancia { get; private set; }

    [Header("Prefabs Centralizados (Resultados)")]
    public GameObject prefabRopaMojada;
    public GameObject prefabRopaSeca;

    private void Awake()
    {
        if (Instancia != null && Instancia != this) Destroy(gameObject);
        else Instancia = this;
    }

    // ==========================================
    // LÓGICA DE VALIDACIÓN Y CREACIÓN
    // ==========================================
    public ulong ProcesarLavado(int reqLav, int reqSec, bool corrLav, bool corrSec, int modoUsado, Vector3 posicionDeMaquina)
    {
        if (!IsServer) return 0;

        if (reqLav == modoUsado) corrLav = true;

        GameObject nuevaRopa = Instantiate(prefabRopaMojada, posicionDeMaquina, Quaternion.identity);
        nuevaRopa.GetComponent<NetworkObject>().Spawn();

        ItemRopa scriptRopa = nuevaRopa.GetComponent<ItemRopa>();
        scriptRopa.lavadoRequerido.Value = reqLav;
        scriptRopa.secadoRequerido.Value = reqSec;
        scriptRopa.lavadoCorrectamente.Value = corrLav;
        scriptRopa.secadoCorrectamente.Value = corrSec;

        scriptRopa.OcultarClientRpc();
        return nuevaRopa.GetComponent<NetworkObject>().NetworkObjectId;
    }

    public ulong ProcesarSecado(int reqLav, int reqSec, bool corrLav, bool corrSec, int modoUsado, Vector3 posicionDeMaquina)
    {
        if (!IsServer) return 0;

        if (reqSec == modoUsado) corrSec = true;

        GameObject nuevaRopa = Instantiate(prefabRopaSeca, posicionDeMaquina, Quaternion.identity);
        nuevaRopa.GetComponent<NetworkObject>().Spawn();

        ItemRopa scriptRopa = nuevaRopa.GetComponent<ItemRopa>();
        scriptRopa.lavadoRequerido.Value = reqLav;
        scriptRopa.secadoRequerido.Value = reqSec;
        scriptRopa.lavadoCorrectamente.Value = corrLav;
        scriptRopa.secadoCorrectamente.Value = corrSec;

        scriptRopa.OcultarClientRpc();
        return nuevaRopa.GetComponent<NetworkObject>().NetworkObjectId;
    }

    // ==========================================
    // RPCS CENTRALIZADOS (Sintaxis moderna de Netcode)
    // ==========================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void InteractuarTamborRpc(ulong idMaquina, ulong idJugador)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idMaquina, out NetworkObject objMaquina) &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idJugador, out NetworkObject objJugador))
        {
            MaquinaManager maquina = objMaquina.GetComponent<MaquinaManager>();
            PlayerInventory inventario = objJugador.GetComponent<PlayerInventory>();

            if (inventario.itemEnMano.Value == maquina.idItemRequerido && maquina.estado.Value == 0)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(inventario.objetoOcultoEnMano.Value, out NetworkObject objOculto))
                {
                    maquina.RecibirDatosDeRopa(objOculto.GetComponent<ItemRopa>());
                    objOculto.Despawn();
                }
                inventario.itemEnMano.Value = 0;
                inventario.objetoOcultoEnMano.Value = 0;
                maquina.estado.Value = 1;
            }
            else if (inventario.itemEnMano.Value == 0 && maquina.estado.Value == 3)
            {
                ulong nuevoId = maquina.GenerarRopaProcesada();
                inventario.itemEnMano.Value = maquina.idItemDevuelto;
                inventario.objetoOcultoEnMano.Value = nuevoId;
                maquina.estado.Value = 0;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nuevoId, out NetworkObject nuevaRopa))
                {
                    ItemRopa ropaNueva = nuevaRopa.GetComponent<ItemRopa>();
                    inventario.MostrarNotaClientRpc(ropaNueva.lavadoRequerido.Value, ropaNueva.secadoRequerido.Value, ropaNueva.lavadoCorrectamente.Value, ropaNueva.secadoCorrectamente.Value);
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void InteractuarPanelRpc(ulong idMaquina)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idMaquina, out NetworkObject objeto))
        {
            MaquinaManager maquina = objeto.GetComponent<MaquinaManager>();
            if (maquina.estado.Value == 1) maquina.modoLavado.Value = (maquina.modoLavado.Value + 1) % maquina.nombresModos.Length;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void InteractuarInicioRpc(ulong idMaquina)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idMaquina, out NetworkObject objeto))
        {
            MaquinaManager maquina = objeto.GetComponent<MaquinaManager>();
            if (maquina.estado.Value == 1) maquina.IniciarCiclo();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ComprarMejoraRpc(ulong idTablon, int idMejora)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idTablon, out NetworkObject objeto))
        {
            TablonManager tablon = objeto.GetComponent<TablonManager>();
            tablon.InstanciarMejora(idMejora);
            Debug.Log("¡Compraste la mejora número: " + idMejora + "!");
        }
    }

    
}