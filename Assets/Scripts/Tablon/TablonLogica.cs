using UnityEngine;
using System;
using System.Collections.Generic;

public class TablonLogica : MonoBehaviour, IMediadorInteraccion
{
    [Header("Botones de la Interfaz")]
    public InteractuableMejora[] botonesDeMejora;

    [Header("Memoria Local")]
    public List<int> mejorasCompradas = new List<int>();
    public event Action<int> OnInteraccionComprar;


    public void ProcesarInteraccion(TipoAccion accion, ulong idJugador, int parametroExtra = 0)
    {
        if (accion == TipoAccion.ComprarMejora)
        {
            OnInteraccionComprar?.Invoke(parametroExtra);
        }
    }

    public void ConfirmarCompraLocal(int idMejora)
    {
        if (!mejorasCompradas.Contains(idMejora))
        {
            mejorasCompradas.Add(idMejora);
        }

        foreach (InteractuableMejora boton in botonesDeMejora)
        {
            if (boton.idMejora == idMejora)
            {
                boton.MarcarComoComprado();
                break;
            }
        }
    }
}