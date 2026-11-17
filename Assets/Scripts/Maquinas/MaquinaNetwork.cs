using Unity.Netcode;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MaquinaLogica))]
public class MaquinaNetwork : NetworkBehaviour
{
    private MaquinaLogica logica;

    [Header("Variables de Red Sincronizadas")]
    public NetworkVariable<int> estadoNet = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> modoLavadoNet = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> reqLavado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> reqSecado = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> corrLavado = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> corrSecado = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private ulong idDuenoGuardado;
    public string apodoGuardado;
    public int EstadoRopaActual
    {
        get
        {
            if (estadoNet.Value == 0) return 0;

            if (logica.esLavarropas)
            {
               
                if (estadoNet.Value < 3) return 1; 

                return corrLavado.Value ? 2 : 4; 
            }
            else
            {
                if (estadoNet.Value < 3) return 2; 

                bool quedaBien = corrLavado.Value && corrSecado.Value;
                return quedaBien ? 3 : 4; 
            }
        }
    }


    void Awake()
    {
        logica = GetComponent<MaquinaLogica>();
    }

    public override void OnNetworkSpawn()
    {
        logica.OnInteraccionSolicitada += InterceptarInteraccion;

        estadoNet.OnValueChanged += (viejo, nuevo) => logica.ForzarActualizacionVisual(nuevo, modoLavadoNet.Value);
        modoLavadoNet.OnValueChanged += (viejo, nuevo) => logica.ForzarActualizacionVisual(estadoNet.Value, nuevo);

        logica.ForzarActualizacionVisual(estadoNet.Value, modoLavadoNet.Value);
    }

    public override void OnNetworkDespawn()
    {
        logica.OnInteraccionSolicitada -= InterceptarInteraccion;
    }

    private void InterceptarInteraccion(TipoAccion accion, ulong idJugador)
    {
        if (!IsSpawned) return; 

        switch (accion)
        {
            case TipoAccion.Tambor: InteractuarTamborServerRpc(idJugador); break;
            case TipoAccion.Panel: InteractuarPanelServerRpc(); break;
            case TipoAccion.Inicio: InteractuarInicioServerRpc(); break;
        }
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractuarPanelServerRpc()
    {
        if (estadoNet.Value == 1) modoLavadoNet.Value = (modoLavadoNet.Value + 1) % logica.nombresModos.Length;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractuarInicioServerRpc()
    {
        if (estadoNet.Value == 1) StartCoroutine(RutinaTrabajoNet());
    }

    private IEnumerator RutinaTrabajoNet()
    {
        estadoNet.Value = 2;
        yield return new WaitForSeconds(logica.tiempoDeCiclo);

        if (logica.esLavarropas)
        {
            bool correcto = (reqLavado.Value == modoLavadoNet.Value);
            corrLavado.Value = correcto;
        }
        else
        {
            bool correcto = (reqSecado.Value == modoLavadoNet.Value);
            corrSecado.Value = correcto;
        }

        estadoNet.Value = 3;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractuarTamborServerRpc(ulong idJugador)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idJugador, out NetworkObject objJugador))
        {
            PlayerInventory inventario = objJugador.GetComponent<PlayerInventory>();

            // === METER ROPA ===
            if (inventario.itemEnMano.Value == logica.idItemRequerido && estadoNet.Value == 0)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(inventario.objetoOcultoEnMano.Value, out NetworkObject objOculto))
                {
                    ItemRopa ropa = objOculto.GetComponent<ItemRopa>();

                    idDuenoGuardado = ropa.idDuenoNpc;
                    apodoGuardado = ropa.apodoDueno.Value.ToString();

                    reqLavado.Value = ropa.lavadoRequerido.Value;
                    reqSecado.Value = ropa.secadoRequerido.Value;
                    corrLavado.Value = ropa.lavadoCorrectamente.Value;
                    corrSecado.Value = ropa.secadoCorrectamente.Value;

                    objOculto.Despawn();
                }

                inventario.itemEnMano.Value = 0;
                inventario.objetoOcultoEnMano.Value = 0;
                estadoNet.Value = 1;
            }
            // === SACAR ROPA ===
            else if (inventario.itemEnMano.Value == 0 && estadoNet.Value == 3)
            {
                ulong nuevoId = logica.esLavarropas
                    ? LavanderiaManager.Instancia.ProcesarLavado(
                        reqLavado.Value, reqSecado.Value, corrLavado.Value, corrSecado.Value,
                        modoLavadoNet.Value, transform.position,
                        idDuenoGuardado, apodoGuardado)          
                    : LavanderiaManager.Instancia.ProcesarSecado(
                        reqLavado.Value, reqSecado.Value, corrLavado.Value, corrSecado.Value,
                        modoLavadoNet.Value, transform.position,
                        idDuenoGuardado, apodoGuardado);         

                inventario.objetoOcultoEnMano.Value = nuevoId;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nuevoId, out NetworkObject nuevaRopaObj))
                {
                    ItemRopa ropaNueva = nuevaRopaObj.GetComponent<ItemRopa>();
                    if (ropaNueva != null)
                    {
                        inventario.itemEnMano.Value = ropaNueva.EstadoRopa;
                        inventario.MostrarNotaClientRpc(
                            reqLavado.Value,
                            reqSecado.Value,
                            corrLavado.Value,
                            corrSecado.Value,
                            apodoGuardado,
                            ropaNueva.EstadoRopa
                        );
                    }
                }

                estadoNet.Value = 0;
            }
        }
    }
}