public enum TipoAccion
{
    Tambor,
    Panel,
    Inicio,
    ComprarMejora
}

public interface IMediadorInteraccion
{
    // Único canal de comunicación hacia la red
    void ProcesarInteraccion(TipoAccion accion, ulong idJugador, int parametroExtra = 0);
}