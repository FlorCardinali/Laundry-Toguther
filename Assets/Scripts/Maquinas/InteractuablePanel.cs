using UnityEngine;

public class InteractuablePanel : MonoBehaviour, IInteractuable
{
    private IMediadorInteraccion mediador;

    void Awake() => mediador = GetComponentInParent<IMediadorInteraccion>();

    public void Interactuar(PlayerController jugador)
    {
        mediador?.ProcesarInteraccion(TipoAccion.Panel, jugador.NetworkObjectId);
    }
}