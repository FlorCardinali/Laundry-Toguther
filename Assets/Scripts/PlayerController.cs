using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private float rotacionX = 0f;
    private Vector2 inputMirar;

    [Header("Inventario Visual")]
    public GameObject ropaSuciaVisual;
    public GameObject ropaLimpiaVisual;

    [Header("Prefabs Físicos")]
    public GameObject ropaSuciaPrefabFisico;
    public GameObject ropaLimpiaPrefabFisico;

    private Vector2 inputMovimiento;

    public NetworkVariable<int> itemEnMano = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            camaraJugador.gameObject.SetActive(false);
            camaraJugador.GetComponent<AudioListener>().enabled = false;
            GetComponent<PlayerInput>().enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ActualizarVisuales(itemEnMano.Value);
        itemEnMano.OnValueChanged += (viejo, nuevo) => ActualizarVisuales(nuevo);
    }

    public override void OnNetworkDespawn()
    {
        itemEnMano.OnValueChanged -= (viejo, nuevo) => ActualizarVisuales(nuevo);
    }

    void Update()
    {
        if (!IsOwner) return;

        MoverJugador();
        RotarCamara();
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
        if (valor.isPressed && itemEnMano.Value != 0) SoltarItemServerRpc();
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
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);

        camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
    }

    //Hae click -> el raycast detecta el objeto -> El objeto te dice "soy la ropa sucia (ID en 1) y mi codigo de red es el 654216 -> Tu jugador le manda eso al Servidor porque es rpc -> el servidor busca el objeto por su id de networkobject, lo destruye del mapa y pone tu mano en estado 1 para que actives internamente el objeto falso que solo sirve para que los demas te vean.

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

    //AGARRAR COSAS
    [ServerRpc]
    public void AgarrarItemServerRpc(int idItem, ulong idObjetoRed)
    {
        if (itemEnMano.Value != 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idObjetoRed, out NetworkObject objetoEnElMundo))
        {
            objetoEnElMundo.Despawn();
            itemEnMano.Value = idItem;
        }
    }
    [ServerRpc]
    public void SoltarItemServerRpc()
    {
        GameObject prefabASpawnear = itemEnMano.Value == 1 ? ropaSuciaPrefabFisico : ropaLimpiaPrefabFisico;
        GameObject nuevoObjeto = Instantiate(prefabASpawnear, puntoDeCaida.position, Quaternion.identity);
        nuevoObjeto.GetComponent<NetworkObject>().Spawn();

        itemEnMano.Value = 0;
    }
    //FIN DE SISTEMA DE AGARRAR COSAS



    //ESTO ES DEL LAVARROPAS
    [ServerRpc]
    public void InteractuarTamborServerRpc(ulong idLavarropas)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idLavarropas, out NetworkObject objeto))
        {
            LavarropasManager lavarropas = objeto.GetComponent<LavarropasManager>();
            //si lavarropas vacio y tengo cosa en mano que esta sucia
            if (itemEnMano.Value == 1 && lavarropas.estado.Value == 0)
            {
                itemEnMano.Value = 0; 
                lavarropas.estado.Value = 1; 
            }
            else if (itemEnMano.Value == 0 && lavarropas.estado.Value == 3)
            {
                itemEnMano.Value = 2; 
                lavarropas.estado.Value = 0; 
            }
        }
    }
    [ServerRpc]
    public void InteractuarPanelServerRpc(ulong idLavarropas)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idLavarropas, out NetworkObject objeto))
        {
            LavarropasManager lavarropas = objeto.GetComponent<LavarropasManager>();
            //si ya tengo ropa sucia adentro del tambor me dej cambio el modo
            if (lavarropas.estado.Value == 1)
            {
                lavarropas.modoLavado.Value = (lavarropas.modoLavado.Value + 1) % 3;
            }
        }
    }
    [ServerRpc]
    public void InteractuarInicioServerRpc(ulong idLavarropas)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idLavarropas, out NetworkObject objeto))
        {
            LavarropasManager lavarropas = objeto.GetComponent<LavarropasManager>();
            //arrancamos si tengo ropa ensima con el modo que este puesto
            if (lavarropas.estado.Value == 1)
            {
                lavarropas.IniciarCicloDeLavado();
            }
        }
    }
    
    //FIN DE PORQUERIAS DEL LAVARROPAS
    
    
    private void ActualizarVisuales(int idItem)
    {
        ropaSuciaVisual.SetActive(idItem == 1);
        ropaLimpiaVisual.SetActive(idItem == 2);
    }
}