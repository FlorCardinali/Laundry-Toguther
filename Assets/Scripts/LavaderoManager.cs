using Unity.Netcode;
using UnityEngine;

public class LavanderiaManager : NetworkBehaviour
{
    public static LavanderiaManager Instancia { get; private set; }

    [Header("Prefabs Centralizados (Resultados)")]
    public GameObject prefabRopaMojada;
    public GameObject prefabRopaSeca;
    public GameObject prefabRopaArruinada;

    private void Awake()
    {
        if (Instancia != null && Instancia != this) Destroy(gameObject);
        else Instancia = this;
    }


    public ulong ProcesarLavado(int reqLav, int reqSec, bool corrLav, bool corrSec, int modoUsado, Vector3 posicionDeMaquina, ulong idDueno, string apodo)
    {
        if (!IsServer) return 0;

        bool lavadoCorrecto = (reqLav == modoUsado);
        corrLav = lavadoCorrecto;

        GameObject prefabAUsar = lavadoCorrecto ? prefabRopaMojada : prefabRopaArruinada;

        GameObject nuevaRopa = Instantiate(prefabAUsar, posicionDeMaquina, Quaternion.identity);
        nuevaRopa.GetComponent<NetworkObject>().Spawn();

        ItemRopa scriptRopa = nuevaRopa.GetComponent<ItemRopa>();
        scriptRopa.lavadoRequerido.Value = reqLav;
        scriptRopa.secadoRequerido.Value = reqSec;
        scriptRopa.lavadoCorrectamente.Value = corrLav;
        scriptRopa.secadoCorrectamente.Value = corrSec;
        scriptRopa.idDuenoNpc = idDueno;
        scriptRopa.apodoDueno.Value = apodo;
        scriptRopa.EstadoRopa = lavadoCorrecto ? 2 : 4; 

        // Si es arruinada, forzamos los flags en false
        if (!lavadoCorrecto)
        {
            scriptRopa.lavadoCorrectamente.Value = false;
            scriptRopa.secadoCorrectamente.Value = false;
        }

        scriptRopa.OcultarItemRopaClientRpc();
        return nuevaRopa.GetComponent<NetworkObject>().NetworkObjectId;
    }

    public ulong ProcesarSecado(int reqLav, int reqSec, bool corrLav, bool corrSec, int modoUsado, Vector3 posicionDeMaquina, ulong idDueno, string apodo)
    {
        if (!IsServer) return 0;

        bool secadoCorrecto = (reqSec == modoUsado);
        corrSec = secadoCorrecto;

        // Si el lavado ya venía mal, o el secado sale mal → arruinada
        bool quedaBien = corrLav && secadoCorrecto;

        GameObject prefabAUsar = quedaBien ? prefabRopaSeca : prefabRopaArruinada;

        GameObject nuevaRopa = Instantiate(prefabAUsar, posicionDeMaquina, Quaternion.identity);
        nuevaRopa.GetComponent<NetworkObject>().Spawn();

        ItemRopa scriptRopa = nuevaRopa.GetComponent<ItemRopa>();
        scriptRopa.lavadoRequerido.Value = reqLav;
        scriptRopa.secadoRequerido.Value = reqSec;
        scriptRopa.lavadoCorrectamente.Value = corrLav;
        scriptRopa.secadoCorrectamente.Value = corrSec;
        scriptRopa.idDuenoNpc = idDueno;
        scriptRopa.apodoDueno.Value = apodo;
        scriptRopa.EstadoRopa = quedaBien ? 3 : 4; // 3 para seca, 4 para arruinada

        if (!quedaBien)
        {
            scriptRopa.lavadoCorrectamente.Value = false;
            scriptRopa.secadoCorrectamente.Value = false;
        }

        scriptRopa.OcultarItemRopaClientRpc();
        return nuevaRopa.GetComponent<NetworkObject>().NetworkObjectId;
    }


}