using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    [Header("Arrastrá acá los puntos de spawn")]
    public Transform[] puntosDeSpawn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Le asignamos un punto fijo y único según el ID del jugador (Cliente 0 = Host, Cliente 1 = Segundo jugador, etc.)
    public Vector3 ObtenerPosicionParaCliente(ulong clientId)
    {
        if (puntosDeSpawn == null || puntosDeSpawn.Length == 0)
        {
            return new Vector3(clientId * 2f, 0f, 0f); // Separación por defecto si no hay puntos
        }

        // Usamos el ID del cliente para calcular qué punto le toca de forma fija
        int indice = (int)(clientId % (ulong)puntosDeSpawn.Length);

        // Retornamos la posición absoluta del mundo usando .position directamente del Transform hijo
        return puntosDeSpawn[indice].position;
    }
}