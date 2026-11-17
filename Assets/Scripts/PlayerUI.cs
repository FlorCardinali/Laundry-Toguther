using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    public GameObject panelNota;
    public GameObject panelOpciones;
    public TMP_Text textoLavado;
    public TMP_Text textoSecado;
    public TMP_Text textoCliente; 
    public TMP_Text textoEstadoRopa;
    public GameObject PanelFeedback;
    public TMP_Text textoDinero;
    public GameObject panelFinDia;
    public TMP_Text textoResumenDia;
    public TMP_Text textoHora;

    [Header("Referencias de Sala")]
    public TMP_Text textoCodigoSala; 

    [Header("Input Action para el Menú")]
    public InputActionReference accionMenu;

    [Header("Atención de clientes")]
    [SerializeField] private GameObject panelAtencion;
    [SerializeField] private TMP_InputField inputApodo;

    [Header("Lista de Jugadores (Menú Opciones)")]
    public GameObject textoHost;
    public GameObject textoCliente1;  
    public GameObject textoCliente2;  
    public GameObject textoCliente3;
    private ulong npcActualId;
    private float timerActualizarJugadores = 0f;
    private string[] nombresLavado = { "SUAVE", "NORMAL", "FUERTE" };
    private string[] nombresSecado = { "40°C", "50°C", "60°C" };
    private bool desconectando = false;

    private PlayerInventory inventarioLocal;

    private PlayerInput playerInputLocal;

    public void AsignarPlayerInput(PlayerInput input)
    {
        playerInputLocal = input;
    }

    private void OnEnable()
    {
        if (accionMenu != null)
        {
            accionMenu.action.Enable();
            accionMenu.action.performed += AlPresionarMenu;
        }
        
    }


    private void Start()
    {
        Invoke(nameof(SuscribirUI), 0.5f);
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarse;
        }
    }

    void SuscribirUI()
    {
        if (DineroManager.Instancia != null)
        {
            DineroManager.Instancia.dineroActual.OnValueChanged += ActualizarTextoDinero;
            ActualizarTextoDinero(0, DineroManager.Instancia.dineroActual.Value);
        }

        if (DiaManager.Instancia != null)
        {
            DiaManager.Instancia.horaActual.OnValueChanged += (_, _) => ActualizarHora();
            DiaManager.Instancia.minutoActual.OnValueChanged += (_, _) => ActualizarHora();
            ActualizarHora();
            DiaManager.Instancia.OnDiaCerrado += MostrarFinDia;
        }
    }


    private void ActualizarListaJugadores()
    {
        if (NetworkManager.Singleton == null) return;

        if (textoHost != null) textoHost.SetActive(false);
        if (textoCliente1 != null) textoCliente1.SetActive(false);
        if (textoCliente2 != null) textoCliente2.SetActive(false);
        if (textoCliente3 != null) textoCliente3.SetActive(false);

        int contadorClientes = 0;
        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.ClientId == NetworkManager.ServerClientId)
            {
                if (textoHost != null) textoHost.SetActive(true);
            }
            else
            {
                if (contadorClientes == 0 && textoCliente1 != null) textoCliente1.SetActive(true);
                else if (contadorClientes == 1 && textoCliente2 != null) textoCliente2.SetActive(true);
                else if (contadorClientes == 2 && textoCliente3 != null) textoCliente3.SetActive(true);

                contadorClientes++;
            }
        }
    }



    void AlDesconectarse(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer || clientId == NetworkManager.Singleton.LocalClientId)
        {
            desconectando = true; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkManager.Singleton.DisconnectReason != string.Empty)
            {
                Debug.Log("Desconectado: " + NetworkManager.Singleton.DisconnectReason);
            }

            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Menu_Principal");
        }
    }

    private void OnDisable()
    {
        if (accionMenu != null)
        {
            accionMenu.action.performed -= AlPresionarMenu;
        }
        if (DineroManager.Instancia != null)
            DineroManager.Instancia.dineroActual.OnValueChanged -= ActualizarTextoDinero;

        if (DiaManager.Instancia != null)
        {
            DiaManager.Instancia.horaActual.OnValueChanged -= (_, _) => ActualizarHora();
            DiaManager.Instancia.minutoActual.OnValueChanged -= (_, _) => ActualizarHora();
            DiaManager.Instancia.OnDiaCerrado -= MostrarFinDia;
        }
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarse;
        }
    }

    private void Update()
    {
      
        if (desconectando) return;

        bool hayPanelAbierto =
            (panelOpciones != null && panelOpciones.activeSelf) ||
            (panelAtencion != null && panelAtencion.activeSelf) ||
            (panelFinDia != null && panelFinDia.activeSelf);

        if (!hayPanelAbierto)
        {
            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (panelOpciones != null && panelOpciones.activeSelf)
        {
            timerActualizarJugadores += Time.deltaTime;
            if (timerActualizarJugadores >= 1f)
            {
                timerActualizarJugadores = 0f;
                ActualizarListaJugadores();
            }
        }
    }
    void ActualizarHora()
    {
        if (textoHora != null && DiaManager.Instancia != null)
            textoHora.text = DiaManager.Instancia.ObtenerHoraFormateada();
    }


    void MostrarFinDia()
    {
        if (panelFinDia == null) return;
        panelFinDia.SetActive(true);

        if (textoResumenDia != null && DineroManager.Instancia != null)
            textoResumenDia.text = $"Día {DiaManager.Instancia.diaActual.Value} terminado\nDinero: ${DineroManager.Instancia.dineroActual.Value}";

        // Pa que solo el host vea el botón de cambiar de día
        Transform botonContinuar = panelFinDia.transform.Find("ButtonSeguienteDia");
        if (botonContinuar != null)
            botonContinuar.gameObject.SetActive(NetworkManager.Singleton.IsHost);

        ActualizarEstadoCursor();
    }


    public void BotonContinuarDia()
    {
        if (NetworkManager.Singleton.IsHost)
            DiaManager.Instancia.IniciarNuevoDia();
        panelFinDia.SetActive(false);
        ActualizarEstadoCursor();
    }

    private void ActualizarTextoDinero(int anterior, int nuevo)
    {
        textoDinero.text = "$" + nuevo;
    }
    private void AlPresionarMenu(InputAction.CallbackContext context)
    {
        TogglePanelOpciones();
    }

    public void ConectarConJugador(PlayerInventory inventario)
    {
        inventarioLocal = inventario;

        gameObject.name = ">>> MI_CANVAS_LOCAL <<<";

        if (inventarioLocal != null)
        {
            inventarioLocal.OnMostrarNota += ActualizarTextosNota;
            inventarioLocal.OnOcultarNota += OcultarPanelNota;
        }

        OcultarPanelNota();
        if (panelOpciones != null) panelOpciones.SetActive(false);
        ActualizarEstadoCursor();
    }

    public void TogglePanelOpciones()
    {
        if (panelOpciones == null) return;

        bool estaAbierto = panelOpciones.activeSelf;
        panelOpciones.SetActive(!estaAbierto);
       
        ActualizarEstadoCursor();
    }


    private void ActualizarEstadoCursor()
    {
        bool hayPanelAbierto =
            (panelOpciones != null && panelOpciones.activeSelf) ||
            (panelAtencion != null && panelAtencion.activeSelf) ||
            (panelFinDia != null && panelFinDia.activeSelf);

        if (hayPanelAbierto)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    public void CopiarCodigoAlPortapapeles()
    {
        if (textoCodigoSala != null)
        {
            string textoCompleto = textoCodigoSala.text;
            string codigoLimpio = textoCompleto;
            //Estoy haciendoe sto porque no tengo ganas de separar lo del texto
            //del codgio en dos TMpro como dios manda xd asi que dije ya fue,
            //que lo separen los dso puntos
            if (textoCompleto.Contains(":"))
            {
                string[] partes = textoCompleto.Split(':');
                codigoLimpio = partes[partes.Length - 1].Trim(); 
            }
            //que util esta joyita de funcion, me huele a posibles vulnerabilidades xd
            GUIUtility.systemCopyBuffer = codigoLimpio;
            Debug.Log("¡Código copiado limpio: " + codigoLimpio + "!");
        }
        StartCoroutine(FeedBackVisualCopiado());
    }

    //algo de feedback
    private System.Collections.IEnumerator FeedBackVisualCopiado()
    {
        PanelFeedback.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        PanelFeedback.SetActive(false);
    }


    private void OnDestroy()
    {
        if (inventarioLocal != null)
        {
            inventarioLocal.OnMostrarNota -= ActualizarTextosNota;
            inventarioLocal.OnOcultarNota -= OcultarPanelNota;
        }
    }

    private void ActualizarTextosNota(int reqLav, int reqSec, bool corrLav, bool corrSec, string apodo, int estadoRopa)
    {
        if (panelNota == null) return;

        textoCliente.text = "Cliente: " + apodo;

        string nombreEstado = "Desconocido";
        if (estadoRopa == 1) nombreEstado = "Sucia";
        else if (estadoRopa == 2) nombreEstado = "Mojada";
        else if (estadoRopa == 3) nombreEstado = "Seca";
        else if (estadoRopa == 4) nombreEstado = "Arruinada";

        textoEstadoRopa.text = "Estado: " + nombreEstado;
        textoLavado.text = "Lavado: " + nombresLavado[reqLav];
        textoSecado.text = "Secado: " + nombresSecado[reqSec];


        if (estadoRopa == 1)
        {
            // Si la ropa todavía está sucia negro
            textoLavado.color = Color.black;
        }
        else
        {
            // Si ya se lavo, entocnes o bien o mal
            if (corrLav)
            {
                textoLavado.color = Color.green;
            }
            else
            {
                textoLavado.color = Color.red;
            }
        }

      
        if (estadoRopa == 1 || estadoRopa == 2)
        {
            // Si está sucia porque recien la agarre o recien la saque del lavarropas osea m,ojada
            textoSecado.color = Color.black;
        }
        else if (estadoRopa == 4 && !corrLav)
        {
           //Si entra aca es porque se arruino en la lavadora, como
           //lña ropa arruinada no entre en el secarropas, negro
            textoSecado.color = Color.black;
        }
        else
        {
            // Si llega hasta aca, es porque la ropa entro correcta al secarropas.
            // Entocnes evaluamos el color del resultado del maldito secado
            if (corrSec)
            {
                textoSecado.color = Color.green;
            }
            else
            {
                textoSecado.color = Color.red;
            }
        }

        panelNota.SetActive(true);
    }

    private void OcultarPanelNota()
    {
        if (panelNota != null) panelNota.SetActive(false);
    }

    public void AbrirPanelAtencion(ulong idNpc)
    {
        npcActualId = idNpc;

        if (panelAtencion == null || inputApodo == null)
        {
            return;
        }

        if (playerInputLocal != null)
            playerInputLocal.enabled = false;  

        panelAtencion.SetActive(true);
        ActualizarEstadoCursor();

        inputApodo.text = "";
        inputApodo.Select();
        inputApodo.ActivateInputField();
    }

    public void ConfirmarAtencion()
    {
        Debug.Log("Input asignado: " + (inputApodo != null));

        if (inputApodo == null)
        {
            return;
        }

     
        string apodo = inputApodo.text.Trim();

        if (string.IsNullOrWhiteSpace(apodo))
        {
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(npcActualId, out NetworkObject npcObjeto))
        {
            return;
        }

        NPCCliente npc = npcObjeto.GetComponent<NPCCliente>();

        if (npc == null)
        {
            return;
        }

        npc.AtenderClienteServerRpc(apodo);

        panelAtencion.SetActive(false);
        npcActualId = 0;

        if (playerInputLocal != null)
            playerInputLocal.enabled = true;

        ActualizarEstadoCursor();
    }

    public void CancelarAtencion()
    {
        panelAtencion.SetActive(false);
        npcActualId = 0;

        //"Reniego de la virtud de mi señora madre
        //y del mismísimo portal biológico por el
        //cual ingresé a esta existencia."
        if (playerInputLocal != null)
            playerInputLocal.enabled = true;

        ActualizarEstadoCursor();
    }

    public void BotonVolverAlMenu()
    {
        desconectando = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerInventory miInventario = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInventory>();
        if (miInventario != null && miInventario.itemEnMano.Value != 0)
        {
            miInventario.IntentarSoltar();
        }

        StartCoroutine(EsperarYDesconectar());
    }

    private System.Collections.IEnumerator EsperarYDesconectar()
    {
        yield return new WaitForSeconds(0.2f);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("Menu_Principal");
    }
}