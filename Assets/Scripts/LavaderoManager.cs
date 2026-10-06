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

    
}