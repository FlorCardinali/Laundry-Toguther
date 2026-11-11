using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class NPCCliente : NetworkBehaviour
{
    [Header("Referencias de Movimiento")]
    private NavMeshAgent agente;

    [Header("UI Flotante")]
    public TMP_Text textoApodoFlotante;

    [Header("Prefabs de Bolsos")]
    public GameObject prefabBolsaRopa;

    [Header("Personalización Visual")]
    public MeshRenderer rendererCuerpo;

    [Header("Gestión de Bolsos en Mostrador")]
    private bool bolsosSpawneados = false;
    public ulong idNpcRed;

    [Header("Wandering")]
    private bool puedeHacerWandering = false;
    private float temporizadorWandering = 0f;

    [Header("Variables de Red")]
    public NetworkVariable<Color> colorCliente = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> cantidadBolsos = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> estaSiendoAtendido = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> yaFueAtendido = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // pa sinbcronizar el aporo
    public NetworkVariable<Unity.Collections.FixedString64Bytes> apodoCliente =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //para entregar el pedidito
    public NetworkVariable<int> bolsasEntregadas = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> bolsasCorrectas = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            if (agente != null) agente.enabled = false;
        }
        if (IsServer)
        {
            idNpcRed = GetComponent<NetworkObject>().NetworkObjectId;
            // aca me falta justar que cambia la cantida debolsas segun la cantidad de dias
            cantidadBolsos.Value = Random.Range(1, 5);
        }

        apodoCliente.OnValueChanged += (viejoApodo, nuevoApodo) => {
            ActualizarTextoApodo(nuevoApodo.ToString());
        };

        ActualizarTextoApodo(apodoCliente.Value.ToString());

        colorCliente.OnValueChanged += (viejoColor, nuevoColor) => {
            AplicarColor(nuevoColor);
        };

        AplicarColor(colorCliente.Value);

        if (IsServer)
        {
            colorCliente.Value = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));
        }
    }


    private void Update()
    {
        if (!IsServer) return;

        //el wandering
        if (puedeHacerWandering && agente != null && !agente.pathPending)
        {
            if (agente.remainingDistance <= agente.stoppingDistance + 0.15f)
            {
                temporizadorWandering += Time.deltaTime;

                if (temporizadorWandering >= ColaManager.Instancia.tiempoEntrePuntosWandering)
                {
                    temporizadorWandering = 0f;
                    IrAPuntoAleatorioEnZona();
                }
            }
        }
    }


    private void SpawnearBolsosEnMostrador()
    {
        if (bolsosSpawneados) return; 
        if (prefabBolsaRopa == null) return;

        if (MostradorManager.Instancia != null)
        {
            bool depositoExitoso = MostradorManager.Instancia.IntentarDepositarPedido(
                prefabBolsaRopa,
                cantidadBolsos.Value,
                idNpcRed
            );

            if (depositoExitoso)
            {
                bolsosSpawneados = true;

                MostradorManager.Instancia.AsignarApodoABolsosActuales(apodoCliente.Value.ToString());
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void IniciarWanderingServerRpc()
    {
        if (!IsServer) return;

        puedeHacerWandering = true;
        temporizadorWandering = 0f;

        if (ColaManager.Instancia != null)
        {
            ColaManager.Instancia.ClienteAtendido(this);
        }

        IrAPuntoAleatorioEnZona();
    }
    private void IrAPuntoAleatorioEnZona()
    {
        if (ColaManager.Instancia == null || ColaManager.Instancia.zonaEsperaWandering == null)
            return;

        Vector3 centro = ColaManager.Instancia.zonaEsperaWandering.position;
        float radio = ColaManager.Instancia.radioZonaEspera;

        Vector2 offset = Random.insideUnitCircle * radio;
        Vector3 destino = centro + new Vector3(offset.x, 0f, offset.y);

        MoverHacia(destino);
    }


    private void AplicarColor(Color color)
    {
        if (rendererCuerpo != null)
        {
            rendererCuerpo.material.color = color;
        }
    }


    public void MoverHacia(Vector3 destino)
    {
        if (IsServer && agente != null && agente.enabled)
        {
            agente.SetDestination(destino);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AtenderClienteServerRpc(string nuevoApodo, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong idJugador = rpcParams.Receive.SenderClientId;

        if (yaFueAtendido.Value || estaSiendoAtendido.Value)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nuevoApodo))
        {
            return;
        }

        if (MostradorManager.Instancia == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
            idJugador, out NetworkClient cliente))
            return;

        if (cliente.PlayerObject == null)
            return;

        float distancia = Vector3.Distance(
            cliente.PlayerObject.transform.position,
            transform.position
        );

        if (distancia > 5f)
            return;

        estaSiendoAtendido.Value = true;

        apodoCliente.Value = nuevoApodo.Trim();

        if (MostradorManager.Instancia != null)
        {
            MostradorManager.Instancia.AsignarApodoABolsosActuales(nuevoApodo.Trim());
        }


        yaFueAtendido.Value = true;
        estaSiendoAtendido.Value = false;

        SpawnearBolsosEnMostrador();
    }

    [ClientRpc]
    public void SolicitarAperturaPanelClientRpc(ulong idJugador, ulong idNpc)
    {
        if (NetworkManager.Singleton.LocalClientId != idJugador)
            return;

        if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return;

        PlayerUI ui = NetworkManager.Singleton.LocalClient.PlayerObject
            .GetComponentInChildren<PlayerUI>();

        if (ui == null)
        {
            Debug.LogError("No se encontró PlayerUI en el jugador local.");
            return;
        }

        ui.AbrirPanelAtencion(idNpc);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EntregarRopaServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong idJugador = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(idJugador, out NetworkClient cliente)) return;
        PlayerInventory inv = cliente.PlayerObject.GetComponent<PlayerInventory>();
        if (inv == null || inv.objetoOcultoEnMano.Value == 0) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(inv.objetoOcultoEnMano.Value, out NetworkObject ropaObj)) return;
        ItemRopa ropa = ropaObj.GetComponent<ItemRopa>();

        if (ropa.idDuenoNpc != this.idNpcRed) return;

        bool estaBien = ropa.EstadoRopa == 3 && ropa.lavadoCorrectamente.Value && ropa.secadoCorrectamente.Value;

        if (estaBien)
            bolsasCorrectas.Value++;

        ropaObj.Despawn();
        inv.itemEnMano.Value = 0;
        inv.objetoOcultoEnMano.Value = 0;

        bolsasEntregadas.Value++;

        // Si ya entregó todas las bolsas que le correspondían
        if (bolsasEntregadas.Value >= cantidadBolsos.Value)
        {
            FinalizarPedido(idJugador);
        }
    }

    private void FinalizarPedido(ulong idJugador)
    {
        if (bolsasCorrectas.Value > 0)
        {
            Debug.Log($"NPC {idNpcRed}: Pagando al jugador {idJugador} por {bolsasCorrectas.Value} bolsa(s) correcta(s) de {cantidadBolsos.Value}.");
        }

        if (bolsasCorrectas.Value < cantidadBolsos.Value)
        {
            if (agente != null) agente.speed *= 2f;
        }

        NetworkObject.Despawn();
    }


    private void ActualizarTextoApodo(string apodo)
    {
        if (textoApodoFlotante != null)
        {
            if (string.IsNullOrEmpty(apodo))
            {
                textoApodoFlotante.text = "Cliente Desconocido";
            }
            else
            {
                textoApodoFlotante.text = apodo;
            }
        }
    }
}