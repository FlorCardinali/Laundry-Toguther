using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuConexion : MonoBehaviour
{
    public string nombreEscenaJuego = "Lavanderia";

    public void BotonHost()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaJuego, LoadSceneMode.Single);
    }

    public void BotonCliente()
    {
        NetworkManager.Singleton.StartClient();
    }
}