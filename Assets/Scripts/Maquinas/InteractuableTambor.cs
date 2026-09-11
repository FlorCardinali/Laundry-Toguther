using Unity.Netcode;
using UnityEngine;

public class InteractuableTambor : MonoBehaviour, IInteractuable
{
    public void Interactuar(PlayerController jugador)
    {
        MaquinaManager maquina = GetComponentInParent<MaquinaManager>();
        if (maquina != null)
        {
            // Actualizado al nuevo nombre del RPC en el LavanderiaManager
            LavanderiaManager.Instancia.InteractuarTamborRpc(maquina.NetworkObjectId, jugador.NetworkObjectId);
        }
    }
}