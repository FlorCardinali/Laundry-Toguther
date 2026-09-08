using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Configuración Básica")]
    public Camera camaraJugador;
    public float velocidad = 5f;

    private CharacterController controller;

    [Header("Físicas y Gravedad")]
    public float gravedad = -9.81f;
    private float velocidadY;

    [Header("Cámara y Rotación")]
    public float sensibilidadRaton = 20f;
    public float limiteMirarArriba = -60f;
    public float limiteMirarAbajo = 60f;
    public NetworkVariable<float> rotacionXRed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float rotacionX = 0f;
    private Vector2 inputMirar;
    private Vector2 inputMovimiento;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            camaraJugador.enabled = false;
            camaraJugador.GetComponent<AudioListener>().enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
    }

    public void OnMover(InputValue valor) { if (IsOwner) inputMovimiento = valor.Get<Vector2>(); }
    public void OnMirar(InputValue valor) { if (IsOwner) inputMirar = valor.Get<Vector2>(); }

    void MoverJugador()
    {
        if (controller.isGrounded && velocidadY < 0) velocidadY = -2f;

        Vector3 movimiento = transform.right * inputMovimiento.x + transform.forward * inputMovimiento.y;
        controller.Move(movimiento * velocidad * Time.deltaTime);

        velocidadY += gravedad * Time.deltaTime;
        controller.Move(new Vector3(0, velocidadY, 0) * Time.deltaTime);
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
}