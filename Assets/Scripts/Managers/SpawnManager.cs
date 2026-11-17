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

    public Vector3 ObtenerPosicionParaCliente(ulong clientId)
    {
        // Si asignaste puntos en el inspector, los usa de forma ordenada por ID
        if (puntosDeSpawn != null && puntosDeSpawn.Length > 0)
        {
            int indice = (int)(clientId % (ulong)puntosDeSpawn.Length);
            if (puntosDeSpawn[indice] != null)
            {
                return puntosDeSpawn[indice].position;
            }
        }

        // CORRECCIÓN: Si olvidaste asignar puntos, devuelve una posición por defecto segura (ej: X desplazado por ID, pero ordenada)
        Debug.LogWarning("SpawnManager: No hay puntos de spawn asignados. Usando posición por defecto.");
        return new Vector3(clientId * 2f, 1f, 0f);
    }

    // Para el fin del día o respawn
    public void ReubicarTodosLosJugadores()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                CharacterController cc = client.PlayerObject.GetComponent<CharacterController>();

                // 1. Apagamos el Character Controller para que Unity nos deje moverlo sin pelear con las físicas
                if (cc != null) cc.enabled = false;

                // 2. Obtenemos la posición limpia
                Vector3 nuevaPos = ObtenerPosicionParaCliente(client.ClientId);

                // 3. Movemos el transform
                client.PlayerObject.transform.position = nuevaPos;

                // 4. Volvemos a encender el Character Controller
                if (cc != null) cc.enabled = true;
            }
        }
    }
}