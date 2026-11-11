using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

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

    [Header("Referencias de Sala")]
    public TMP_Text textoCodigoSala; 

    [Header("Input Action para el Menú")]
    public InputActionReference accionMenu;

    [Header("Atención de clientes")]
    [SerializeField] private GameObject panelAtencion;
    [SerializeField] private TMP_InputField inputApodo;

 
    private ulong npcActualId;

    private string[] nombresLavado = { "SUAVE", "NORMAL", "FUERTE" };
    private string[] nombresSecado = { "40°C", "50°C", "65°C" };

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

    private void OnDisable()
    {
        if (accionMenu != null)
        {
            accionMenu.action.performed -= AlPresionarMenu;
        }
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
            (panelAtencion != null && panelAtencion.activeSelf);

        if (hayPanelAbierto)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
        if (panelNota == null)
        {
            return;
        }

        string txtCli = "Cliente: " + apodo;
        string txtEst = "Estado: " + (estadoRopa == 1 ? "Sucia" : estadoRopa == 2 ? "Mojada" : estadoRopa == 3 ? "Seca" : "Arruinada");

        string txtLav = "Lavado: " + nombresLavado[reqLav];
        if (corrLav) txtLav = "<s>" + txtLav + "</s>";

        string txtSec = "Secado: " + nombresSecado[reqSec];
        if (corrSec) txtSec = "<s>" + txtSec + "</s>";

        textoCliente.text = txtCli;
        textoEstadoRopa.text = txtEst;
        textoLavado.text = txtLav;
        textoSecado.text = txtSec;

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
            Debug.LogError("Faltan referencias del panel de atención.");
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
            Debug.LogWarning("El InputField está vacío.");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(npcActualId, out NetworkObject npcObjeto))
        {
            Debug.LogError("No se encontró el NPC de este pedido.");
            return;
        }

        NPCCliente npc = npcObjeto.GetComponent<NPCCliente>();

        if (npc == null)
        {
            Debug.LogError("El objeto encontrado no tiene NPCCliente.");
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


}