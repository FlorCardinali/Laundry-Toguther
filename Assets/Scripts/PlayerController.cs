using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Referencias")]
    public Camera camaraJugador;

    [Header("Interfaz Local")]
    public GameObject canvasUIPrefab;

    private PlayerInventory inventario;
    private MaquinaManager maquinaMiradaActual = null;

    void Awake()
    {
        inventario = GetComponent<PlayerInventory>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<PlayerInput>().enabled = false;
        }
        else
        {
            if (canvasUIPrefab != null)
            {
                GameObject miCanvas = Instantiate(canvasUIPrefab, transform);
                miCanvas.name = ">>> MI_CANVAS_LOCAL <<<";

                PlayerUI uiScript = miCanvas.GetComponent<PlayerUI>();
                if (uiScript != null) uiScript.ConectarConJugador(inventario);
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        DetectarHover();
    }

    public void OnInteractuar(InputValue valor)
    {
        if (!IsOwner) return;
        if (valor.isPressed) IntentarInteractuar();
    }

    public void OnSoltar(InputValue valor)
    {
        if (!IsOwner) return;
        if (valor.isPressed && inventario != null) inventario.IntentarSoltar();
    }

    void IntentarInteractuar()
    {
        Ray ray = camaraJugador.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            IInteractuable objetoInteractuable = hit.collider.GetComponent<IInteractuable>();
            if (objetoInteractuable != null) objetoInteractuable.Interactuar(this);
        }
    }

    void DetectarHover()
    {
        if (inventario == null || inventario.itemEnMano.Value != 0) return;

        Ray ray = camaraJugador.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            MaquinaManager maquina = hit.collider.GetComponentInParent<MaquinaManager>();
            if (maquina != null && maquina.estado.Value > 0)
            {
                inventario.DispararMostrarNota(maquina.reqLavado.Value, maquina.reqSecado.Value, maquina.corrLavado.Value, maquina.corrSecado.Value);
                maquinaMiradaActual = maquina;
                return;
            }
        }

        if (maquinaMiradaActual != null)
        {
            inventario.DispararOcultarNota();
            maquinaMiradaActual = null;
        }
    }
}