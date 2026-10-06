using UnityEngine;

public class InteractuableInicio : MonoBehaviour, IInteractuable
{
    private IMediadorInteraccion mediador;

    void Awake() => mediador = GetComponentInParent<IMediadorInteraccion>();

    public void Interactuar(PlayerController jugador)
    {
        mediador?.ProcesarInteraccion(TipoAccion.Inicio, jugador.NetworkObjectId);
    }
}