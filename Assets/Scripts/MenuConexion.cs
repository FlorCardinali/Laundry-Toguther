using System.Threading.Tasks;
using System.Collections;
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
    public GameObject panelConexion;
    public GameObject panelCreditos;
    public GameObject panelOpciones;
    public TMP_Dropdown dropdownPantalla;
    public UnityEngine.UI.Slider sliderSensibilidad;
    [Header("Panel de Error")]
    public GameObject panelErrorCodigo;

    private async void Start()
    {
        
        int modoGuardado = PlayerPrefs.GetInt("ModoPantalla", 2); 

        if (dropdownPantalla != null)
        {
            dropdownPantalla.value = modoGuardado;
            dropdownPantalla.onValueChanged.AddListener(CambiarModoPantalla);
        }

        CambiarModoPantalla(modoGuardado);

        if (sliderSensibilidad != null)
        {
            float sensibilidadGuardada = PlayerPrefs.GetFloat("SensibilidadMouse", 2f);
            sliderSensibilidad.value = sensibilidadGuardada;
            sliderSensibilidad.onValueChanged.AddListener(CambiarSensibilidad);
        }
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Jugador autenticado con ID: " + AuthenticationService.Instance.PlayerId);
        }
    }

    public void CambiarModoPantalla(int modo)
    {
        switch (modo)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
        }

        PlayerPrefs.SetInt("ModoPantalla", modo);
        PlayerPrefs.Save();
    }

    public void CambiarSensibilidad(float valor)
    {
        PlayerPrefs.SetFloat("SensibilidadMouse", valor);
        PlayerPrefs.Save();
    }


    public void BotonSalir()
    {
        Application.Quit();
    }

    public void BotonAbrirPanelConexion()
    {
        if (panelConexion != null)
        {
            panelConexion.SetActive(true);
        }
    }

    public void BotonAbrirPanelCreditos()
    {
        if (panelCreditos != null)
        {
            panelCreditos.SetActive(true);
        }
    }

    public void BotonAbrirPanelOpciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(true);
        }
    }

    public void BotonCerrarPaneles()
    {
        panelConexion.SetActive(false);
        panelCreditos.SetActive(false);
        panelOpciones.SetActive(false);
    }

    public async void BotonHostRelay()
    {
        try
        {
            Allocation asignacion = await RelayService.Instance.CreateAllocationAsync(maximoJugadores - 1);

            string codigoSala = await RelayService.Instance.GetJoinCodeAsync(asignacion.AllocationId);
            codigoSalaGlobal = codigoSala;

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
            return;
        }

        try
        {
            JoinAllocation asignacionUnion = await RelayService.Instance.JoinAllocationAsync(inputCodigoSala.text);

            codigoSalaGlobal = inputCodigoSala.text.Trim();

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                asignacionUnion.RelayServer.IpV4,
                (ushort)asignacionUnion.RelayServer.Port,
                asignacionUnion.AllocationIdBytes,
                asignacionUnion.Key,
                asignacionUnion.ConnectionData,
                asignacionUnion.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning("Error al unirse a la sala de Relay: (pusiste mal el codigo xd) " + e.Message);
            MostrarErrorTemporal();
        }
    }

    private void MostrarErrorTemporal()
    {
        if (panelErrorCodigo != null)
        {
            StopAllCoroutines();
            StartCoroutine(RutinaMostrarError());
        }
    }

    private IEnumerator RutinaMostrarError()
    {
        panelErrorCodigo.SetActive(true);
        yield return new WaitForSeconds(2f);
        panelErrorCodigo.SetActive(false);
    }
}