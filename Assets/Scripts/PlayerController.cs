using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode.Components;

public class PlayerController : NetworkBehaviour
{
    [Header("Configuración Básica")]
    public CharacterController controller;
    public Camera camaraJugador;
    public float velocidad = 5f;
    public Transform puntoDeCaida;

    [Header("Físicas y Gravedad")]
    public float gravedad = -9.81f;
    private float velocidadY;

    [Header("Cámara y Rotación")]
    public float sensibilidadRaton = 20f;
    public float limiteMirarArriba = -60f;
    public float limiteMirarAbajo = 60f;

    [Header("Interfaz de Cliente (Nota)")]
    public GameObject panelNota;
    public TMP_Text textoLavado;
    public TMP_Text textoSecado;
    [Header("Interfaz Local")]
    public GameObject canvasJugador;

    private string[] nombresLavado = { "SUAVE", "NORMAL", "FUERTE" };
    private string[] nombresSecado = { "40°C", "50°C", "65°C" };

    [Header("Inventario (Visuales y Físicos)")]
    [Tooltip("Orden: 0 = Sucia (ID 1), 1 = Mojada (ID 2), 2 = Seca (ID 3)")]
    public GameObject[] itemsVisuales;
    public GameObject[] prefabsFisicos;

    public NetworkVariable<ulong> objetoOcultoEnMano = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> rotacionXRed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> itemEnMano = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float rotacionX = 0f;
    private Vector2 inputMirar;
    private Vector2 inputMovimiento;
    private MaquinaManager maquinaMiradaActual = null;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            camaraJugador.enabled = false;
            camaraJugador.GetComponent<AudioListener>().enabled = false;
            GetComponent<PlayerInput>().enabled = false;
            if (canvasJugador != null) canvasJugador.SetActive(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ActualizarVisuales(itemEnMano.Value);
        itemEnMano.OnValueChanged += (viejo, nuevo) => {
            ActualizarVisuales(nuevo);
            if (nuevo == 0) OcultarNotaUI();
        };
    }

    public override void OnNetworkDespawn()
    {
        itemEnMano.OnValueChanged -= (viejo, nuevo) => ActualizarVisuales(nuevo);
    }

    void Update()
    {
        if (!IsOwner)
        {
            camaraJugador.transform.localRotation = Quaternion.Euler(rotacionXRed.Value, 0f, 0f);
            return;
        }

        MoverJugador();
        RotarCamara();
        DetectarHover();
    }

    public void OnMover(InputValue valor)
    {
        if (!IsOwner) return;
        inputMovimiento = valor.Get<Vector2>();
    }

    public void OnMirar(InputValue valor)
    {
        if (!IsOwner) return;
        inputMirar = valor.Get<Vector2>();
    }

    public void OnInteractuar(InputValue valor)
    {
        if (!IsOwner) return;
        if (valor.isPressed) IntentarInteractuar();
    }

    public void OnSoltar(InputValue valor)
    {
        if (!IsOwner) return;
        if (valor.isPressed && itemEnMano.Value != 0)
        {
            Vector3 posicionSegura = CalcularPosicionDeCaida();
            SoltarItemServerRpc(posicionSegura);
            OcultarNotaUI();
        }
    }

    void MoverJugador()
    {
        if (controller.isGrounded && velocidadY < 0)
        {
            velocidadY = -2f;
        }

        Vector3 movimiento = transform.right * inputMovimiento.x + transform.forward * inputMovimiento.y;
        controller.Move(movimiento * velocidad * Time.deltaTime);

        velocidadY += gravedad * Time.deltaTime;
        Vector3 caída = new Vector3(0, velocidadY, 0);
        controller.Move(caída * Time.deltaTime);
    }

    void RotarCamara()
    {
        float mouseX = inputMirar.x * sensibilidadRaton;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = inputMirar.y * sensibilidadRaton;
        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, limiteMirarArriba, limiteMirarAbajo);

        camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
        rotacionXRed.Value = rotacionX;
    }

    void IntentarInteractuar()
    {
        Ray ray = camaraJugador.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            IInteractuable objetoInteractuable = hit.collider.GetComponent<IInteractuable>();
            if (objetoInteractuable != null)
            {
                objetoInteractuable.Interactuar(this);
            }
        }
    }

    void DetectarHover()
    {
        if (itemEnMano.Value != 0) return;

        Ray ray = camaraJugador.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            MaquinaManager maquina = hit.collider.GetComponentInParent<MaquinaManager>();

            if (maquina != null && maquina.estado.Value > 0)
            {
                MostrarNotaLocal(maquina.reqLavado.Value, maquina.reqSecado.Value, maquina.corrLavado.Value, maquina.corrSecado.Value);
                maquinaMiradaActual = maquina;
                return;
            }
        }

        if (maquinaMiradaActual != null)
        {
            OcultarNotaUI();
            maquinaMiradaActual = null;
        }
    }

    private Vector3 CalcularPosicionDeCaida()
    {
        Vector3 origen = camaraJugador.transform.position;
        Vector3 destino = puntoDeCaida.position;
        Vector3 direccion = destino - origen;
        float distancia = Vector3.Distance(origen, destino);
        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, distancia))
        {
            return hit.point - (direccion.normalized * 0.3f);
        }
        return destino;
    }

    [ServerRpc]
    public void AgarrarItemServerRpc(int idItem, ulong idObjetoRed)
    {
        if (itemEnMano.Value != 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idObjetoRed, out NetworkObject objetoEnElMundo))
        {
            ItemRopa scriptRopa = objetoEnElMundo.GetComponent<ItemRopa>();
            scriptRopa.OcultarClientRpc();

            itemEnMano.Value = idItem;
            objetoOcultoEnMano.Value = idObjetoRed;

            MostrarNotaClientRpc(scriptRopa.lavadoRequerido.Value, scriptRopa.secadoRequerido.Value, scriptRopa.lavadoCorrectamente.Value, scriptRopa.secadoCorrectamente.Value);
        }
    }

    [ServerRpc]
    public void SoltarItemServerRpc(Vector3 posicionSegura)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetoOcultoEnMano.Value, out NetworkObject objetoOculto))
        {
            NetworkTransform netTransform = objetoOculto.GetComponent<NetworkTransform>();

            if (netTransform != null)
            {
                netTransform.Teleport(posicionSegura, Quaternion.identity, objetoOculto.transform.localScale);
            }
            else
            {
                objetoOculto.transform.position = posicionSegura;
            }

            ItemRopa scriptRopa = objetoOculto.GetComponent<ItemRopa>();
            scriptRopa.MostrarClientRpc();
        }
        itemEnMano.Value = 0;
        objetoOcultoEnMano.Value = 0;
    }

    [ServerRpc]
    public void InteractuarTamborServerRpc(ulong idMaquina)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idMaquina, out NetworkObject objeto))
        {
            MaquinaManager maquina = objeto.GetComponent<MaquinaManager>();

            if (itemEnMano.Value == maquina.idItemRequerido && maquina.estado.Value == 0)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objetoOcultoEnMano.Value, out NetworkObject objOculto))
                {
                    maquina.RecibirDatosDeRopa(objOculto.GetComponent<ItemRopa>());
                    objOculto.Despawn();
                }

                itemEnMano.Value = 0;
                objetoOcultoEnMano.Value = 0;
                maquina.estado.Value = 1;
            }
            else if (itemEnMano.Value == 0 && maquina.estado.Value == 3)
            {
                ulong nuevoId = maquina.GenerarRopaProcesada();
                itemEnMano.Value = maquina.idItemDevuelto;
                objetoOcultoEnMano.Value = nuevoId;
                maquina.estado.Value = 0;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nuevoId, out NetworkObject nuevaRopa))
                {
                    ItemRopa ropaNueva = nuevaRopa.GetComponent<ItemRopa>();
                    MostrarNotaClientRpc(ropaNueva.lavadoRequerido.Value, ropaNueva.secadoRequerido.Value, ropaNueva.lavadoCorrectamente.Value, ropaNueva.secadoCorrectamente.Value);
                }
            }
        }
    }

    [ServerRpc]
    public void InteractuarPanelServerRpc(ulong idMaquina)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idMaquina, out NetworkObject objeto))
        {
            MaquinaManager maquina = objeto.GetComponent<MaquinaManager>();
            if (maquina.estado.Value == 1)
            {
                maquina.modoLavado.Value = (maquina.modoLavado.Value + 1) % maquina.nombresModos.Length;
            }
        }
    }

    [ServerRpc]
    public void InteractuarInicioServerRpc(ulong idMaquina)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idMaquina, out NetworkObject objeto))
        {
            MaquinaManager maquina = objeto.GetComponent<MaquinaManager>();
            if (maquina.estado.Value == 1)
            {
                maquina.IniciarCiclo();
            }
        }
    }

    [ServerRpc]
    public void ComprarMejoraServerRpc(ulong idTablon, int idMejora)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idTablon, out NetworkObject objeto))
        {
            TablonManager tablon = objeto.GetComponent<TablonManager>();
            tablon.InstanciarMejora(idMejora);
            Debug.Log("¡Compraste la mejora número: " + idMejora + "!");
        }
    }

    public void MostrarNotaLocal(int reqLav, int reqSec, bool corrLav, bool corrSec)
    {
        if (panelNota == null) return;

        string txtLav = "Lavado: " + nombresLavado[reqLav];
        if (corrLav) txtLav = "<s>" + txtLav + "</s>";

        string txtSec = "Secado: " + nombresSecado[reqSec];
        if (corrSec) txtSec = "<s>" + txtSec + "</s>";

        textoLavado.text = txtLav;
        textoSecado.text = txtSec;
        panelNota.SetActive(true);
    }

    [ClientRpc]
    public void MostrarNotaClientRpc(int reqLav, int reqSec, bool corrLav, bool corrSec)
    {
        MostrarNotaLocal(reqLav, reqSec, corrLav, corrSec);
    }

    private void OcultarNotaUI()
    {
        if (panelNota != null) panelNota.SetActive(false);
    }

    private void ActualizarVisuales(int idItem)
    {
        for (int i = 0; i < itemsVisuales.Length; i++)
        {
            if (itemsVisuales[i] != null)
            {
                itemsVisuales[i].SetActive(i == (idItem - 1));
            }
        }
    }
}