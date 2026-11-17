using Unity.Netcode;
using UnityEngine;
using System;

public class DiaManager : NetworkBehaviour
{
    public static DiaManager Instancia { get; private set; }

    [Header("Configuración")]
    public float minutosDeJuegoPorMinutoReal = 45f;
    public int horaInicio = 8;
    public int horaCierre = 22;

    [Header("Control del Sol (Iluminación)")]
    public Light luzSol; 
    public float rotacionXDia = 50f;   
    public float rotacionXNoche = -90f; 

    public NetworkVariable<int> horaActual = new(8);
    public NetworkVariable<int> minutoActual = new(0);
    public NetworkVariable<int> diaActual = new(1);
    public NetworkVariable<bool> diaCerrado = new(false);

    public event Action OnDiaCerrado;

    float temporizador;
    float segundosPorMinutoJuego;
    void Awake()
    {
        if (Instancia != null && Instancia != this) Destroy(gameObject);
        else Instancia = this;

        segundosPorMinutoJuego = 60f / minutosDeJuegoPorMinutoReal;
    }

    void Update()
    {
        ActualizarRotacionSol();

        if (!IsServer || diaCerrado.Value) return;

        temporizador += Time.deltaTime;
        if (temporizador >= segundosPorMinutoJuego)
        {
            temporizador = 0;
            minutoActual.Value++;

            if (minutoActual.Value >= 60)
            {
                minutoActual.Value = 0;
                horaActual.Value++;

                if (horaActual.Value >= horaCierre)
                    CerrarDiaServer();
            }
        }
    }


    private void ActualizarRotacionSol()
    {
        if (luzSol == null) return;

        float horaTotalActual = horaActual.Value + (minutoActual.Value / 60f);
        float progreso = Mathf.InverseLerp(horaInicio, horaCierre, horaTotalActual);

        float anguloX = Mathf.Lerp(0f, 180f, progreso);
        luzSol.transform.rotation = Quaternion.Euler(anguloX, 30f, 0f);

        if (horaActual.Value >= horaCierre || horaActual.Value < horaInicio)
        {
            luzSol.enabled = false; 
        }
        else
        {
            luzSol.enabled = true;  
        }
    }


    public string ObtenerHoraFormateada()
    {
        return $"{horaActual.Value:00}:{minutoActual.Value:00}";
    }

    void CerrarDiaServer()
    {
        diaCerrado.Value = true;
        // Parar spawns
        if (ColaManager.Instancia != null)
            ColaManager.Instancia.enabled = false;

        CerrarDiaClientRpc();
    }

    [ClientRpc]
    void CerrarDiaClientRpc()
    {
        OnDiaCerrado?.Invoke();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PedirCerrarDiaServerRpc(RpcParams rpcParams = default)
    {
        // Solo host
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
        if (diaCerrado.Value) return;
        CerrarDiaServer();
    }

    public void IniciarNuevoDia()
    {
        if (!IsServer) return;  
        diaActual.Value++;
        horaActual.Value = horaInicio;
        diaCerrado.Value = false;
        temporizador = 0;

        if (ColaManager.Instancia != null)
            ColaManager.Instancia.enabled = true;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.ReubicarTodosLosJugadores();
        }
    }
}