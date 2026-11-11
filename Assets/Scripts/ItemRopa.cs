using Unity.Netcode;
using UnityEngine;

public class ItemRopa : NetworkBehaviour, IInteractuable
{
    [Tooltip("1 = Sucia, 2 = Mojada, 3 = Seca, 4 = Arruinada")]
    public int EstadoRopa;
    private Coroutine animacionCoroutine;
    [Header("Componentes a Ocultar")]
    public MeshRenderer mallaVisual;
    public Collider colisionador;
    public Rigidbody fisicas;

    [Header("Vínculo con el Cliente")]
    public ulong idDuenoNpc;

    [Header("Requisitos del Cliente")]
    public NetworkVariable<int> lavadoRequerido = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> secadoRequerido = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> lavadoCorrectamente = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> secadoCorrectamente = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<Unity.Collections.FixedString64Bytes> apodoDueno =
    new NetworkVariable<Unity.Collections.FixedString64Bytes>("",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool yaSeGeneroPedido = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer && EstadoRopa == 1 && !yaSeGeneroPedido)
        {
            lavadoRequerido.Value = Random.Range(0, 3);
            secadoRequerido.Value = Random.Range(0, 3);
            yaSeGeneroPedido = true;
        }
    }

    public void Interactuar(PlayerController jugador)
    {
        ulong idRed = GetComponent<NetworkObject>().NetworkObjectId;
        jugador.GetComponent<PlayerInventory>().AgarrarItemServerRpc(EstadoRopa, idRed);
    }

    [ClientRpc]
    public void OcultarItemRopaClientRpc()
    {
        if (mallaVisual != null) mallaVisual.enabled = false;
        if (colisionador != null) colisionador.enabled = false;
        if (fisicas != null) fisicas.isKinematic = true;
    }

    [ClientRpc]
    public void MostrarClientRpc(bool animarEnSuelo = true)
    {
        if (mallaVisual != null) mallaVisual.enabled = true;
        if (colisionador != null)
        {
            colisionador.enabled = true;
           
        }
        if (fisicas != null) fisicas.isKinematic = true;

        if (animarEnSuelo)
        {
            if (animacionCoroutine != null) StopCoroutine(animacionCoroutine);
            animacionCoroutine = StartCoroutine(AnimarEfectoCaidaSuelo());
        }
    }


    private System.Collections.IEnumerator AnimarEfectoCaidaSuelo()
    {
        Quaternion rotacionBase = transform.rotation;

        float inclinacionX = Random.Range(-10f, 10f);
        float inclinacionZ = Random.Range(-10f, 10f);
        Quaternion rotacionFinal = rotacionBase * Quaternion.Euler(inclinacionX, 0, inclinacionZ);

        float tiempo = 0f;
        float duracion = 0.25f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            float rebote = Mathf.Sin(t * Mathf.PI) * 0.1f;
            transform.position += new Vector3(0, rebote * Time.deltaTime * 15f, 0);
            transform.rotation = Quaternion.Slerp(rotacionBase, rotacionFinal, t);

            yield return null;
        }

        transform.rotation = rotacionFinal;
    }
}