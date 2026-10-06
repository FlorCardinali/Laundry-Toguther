using UnityEngine;

public class InteractuableTambor : MonoBehaviour, IInteractuable
{
    private IMediadorInteraccion mediador;

    void Awake() => mediador = GetComponentInParent<IMediadorInteraccion>();

    public void Interactuar(PlayerController jugador)
    {
        mediador?.ProcesarInteraccion(TipoAccion.Tambor, jugador.NetworkObjectId);
    }
}