using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuConexion : MonoBehaviour
{
    public static string codigoSalaGlobal = "";
    public string nombreEscenaJuego = "Lavanderia";
    public int maximoJugadores = 4;

    [Header("UI Referencias")]
    public TMP_InputField inputCodigoSala;
    public TMP_Text textoCodigoGenerado;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Jugador autenticado con ID: " + AuthenticationService.Instance.PlayerId);
        }
    }

    public async void BotonHostRelay()
    {
        try
        {
            Allocation asignacion = await RelayService.Instance.CreateAllocationAsync(maximoJugadores - 1);

            string codigoSala = await RelayService.Instance.GetJoinCodeAsync(asignacion.AllocationId);
            codigoSalaGlobal = codigoSala;

            if (textoCodigoGenerado != null)
                textoCodigoGenerado.text = "Código de Sala: " + codigoSala;

            Debug.Log("Código generado: " + codigoSala);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
            asignacion.RelayServer.IpV4,
            (ushort)asignacion.RelayServer.Port,
            asignacion.AllocationIdBytes,
            asignacion.Key,
            asignacion.ConnectionData
            );

          
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaJuego, LoadSceneMode.Single);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Error al crear la sala de Relay: " + e.Message);
        }
    }

    public async void BotonClienteRelay()
    {
        if (string.IsNullOrEmpty(inputCodigoSala.text))
        {
            Debug.LogWarning("¡Tenés que ingresar un código de sala!");
            return;
        }

        try
        {

            JoinAllocation asignacionUnion = await RelayService.Instance.JoinAllocationAsync(inputCodigoSala.text);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                asignacionUnion.RelayServer.IpV4,
                (ushort)asignacionUnion.RelayServer.Port,
                asignacionUnion.AllocationIdBytes,
                asignacionUnion.Key,
                asignacionUnion.ConnectionData,
                asignacionUnion.HostConnectionData
            );

            // Iniciar Cliente
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Error al unirse a la sala de Relay: " + e.Message);
        }
    }
}