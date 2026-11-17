using Unity.Netcode;
using UnityEngine;

public class DineroManager : NetworkBehaviour
{
    public static DineroManager Instancia { get; private set; }

    [Header("Configuración Inicial")]
    [SerializeField] private int dineroInicial = 0;

    public NetworkVariable<int> dineroActual = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
            Destroy(gameObject);
        else
            Instancia = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            dineroActual.Value = dineroInicial;
        }
    }
    public void SumarDinero(int cantidad)
    {
        if (!IsServer) return;
        dineroActual.Value += cantidad;
    }

    public void RestarDinero(int cantidad)
    {
        if (!IsServer) return;
        if (dineroActual.Value >= cantidad)
            dineroActual.Value -= cantidad;
    }

    public bool TieneSuficiente(int cantidad)
    {
        return dineroActual.Value >= cantidad;
    }
}