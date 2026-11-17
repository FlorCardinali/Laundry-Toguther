using UnityEngine;
using Unity.Netcode;

public class InterruptorController: NetworkBehaviour
{
    [Header("Configuración de Zona")]
    [SerializeField] private string tagDeLuces = "LuzZonaA"; 

    private Light[] lucesControladas;

    public NetworkVariable<bool> lucesEncendidas = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        GameObject[] objetosConLuz = GameObject.FindGameObjectsWithTag(tagDeLuces);
        lucesControladas = new Light[objetosConLuz.Length];

        for (int i = 0; i < objetosConLuz.Length; i++)
        {
            lucesControladas[i] = objetosConLuz[i].GetComponent<Light>();
        }

        lucesEncendidas.OnValueChanged += OnEstadoLucesChanged;
        ActualizarLucesVisuales(lucesEncendidas.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        lucesEncendidas.OnValueChanged -= OnEstadoLucesChanged;
    }

    private void OnEstadoLucesChanged(bool valorAnterior, bool valorNuevo)
    {
        ActualizarLucesVisuales(valorNuevo);
    }

    private void ActualizarLucesVisuales(bool encender)
    {
        if (lucesControladas == null) return;

        foreach (Light luz in lucesControladas)
        {
            if (luz != null)
            {
                luz.enabled = encender;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AlternarLucesServerRpc(RpcParams rpcParams = default)
    {
        lucesEncendidas.Value = !lucesEncendidas.Value;
    }
}