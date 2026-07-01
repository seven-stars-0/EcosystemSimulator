/// <summary>
/// Canale statico per comunicare blocchi di input tra UI e sistemi di gioco.
/// Evita dipendenze dirette tra SettingsScreen e WorldCamera.
/// </summary>
public static class InputGuard
{
    /// <summary>
    /// Quando true, WorldCamera blocca TUTTI gli input (mouse + tastiera).
    /// Impostato da SettingsScreen.OnShow / OnHide.
    /// </summary>
    public static bool CameraInputBlocked { get; set; }
}