using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class DatosMejora
{
    public int idMejora;
    public string nombreMejora;
    public int precio;
}

public class TablonLogica : MonoBehaviour, IMediadorInteraccion
{
    [Header("Botones de la Interfaz")]
    public InteractuableMejora[] botonesDeMejora;

    [Header("Configuración de Mejoras")]
    public List<DatosMejora> listaMejoras = new List<DatosMejora>();

    [Header("Memoria Local")]
    public List<int> mejorasCompradas = new List<int>();

    public event Action<int> OnInteraccionComprar;

    private void Start()
    {
        ActualizarTextosBotones();
    }

    public void ProcesarInteraccion(TipoAccion accion, ulong idJugador, int parametroExtra = 0)
    {
        if (accion == TipoAccion.ComprarMejora)
        {
            OnInteraccionComprar?.Invoke(parametroExtra);
        }
    }

    public int ObtenerPrecio(int idMejora)
    {
        DatosMejora datos = listaMejoras.Find(m => m.idMejora == idMejora);
        return datos != null ? datos.precio : 999;
    }

    public string ObtenerNombre(int idMejora)
    {
        DatosMejora datos = listaMejoras.Find(m => m.idMejora == idMejora);
        return datos != null ? datos.nombreMejora : "Desconocida";
    }

    public void ConfirmarCompraLocal(int idMejora)
    {
        if (!mejorasCompradas.Contains(idMejora))
            mejorasCompradas.Add(idMejora);

        foreach (InteractuableMejora boton in botonesDeMejora)
        {
            if (boton.idMejora == idMejora)
            {
                boton.MarcarComoComprado();
                break;
            }
        }
    }

    private void ActualizarTextosBotones()
    {
        foreach (InteractuableMejora boton in botonesDeMejora)
        {
            if (boton == null) continue;

            DatosMejora datos = listaMejoras.Find(m => m.idMejora == boton.idMejora);

            if (datos != null && boton.textoPrecio != null)
            {
                boton.textoPrecio.text = "$" + datos.precio;
            }
        }
    }
}