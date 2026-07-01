using UnityEngine;
using TMPro;

// ============================================================================
//  SpawnCountPanel  -  Input fields per lo spawn casuale (interi positivi).
// ----------------------------------------------------------------------------
//  Due TMP_InputField definiscono quante prede e quanti predatori verranno
//  generati a caso all'avvio (oltre a quelli piazzati a mano). I valori sono
//  salvati nella mappa corrente (MapData.randomPrey/PredatorCount).
//
//  Collegamento: crea due TMP InputField nel canvas (editor o settings) e
//  assegnali; metti questo componente su un GameObject del pannello. Si
//  popola da solo dalla mappa quando viene attivato.
// ============================================================================

public class SpawnCountPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField preyCountField;
    [SerializeField] private TMP_InputField predatorCountField;

    private void OnEnable()
    {
        var map = MapSession.Instance != null ? MapSession.Instance.CurrentMap : null;

        Hook(preyCountField, map != null ? map.randomPreyCount : 0, OnPreyChanged);
        Hook(predatorCountField, map != null ? map.randomPredatorCount : 0, OnPredatorChanged);
    }

    private void Hook(TMP_InputField field, int value, UnityEngine.Events.UnityAction<string> cb)
    {
        if (field == null) return;
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.onValueChanged.RemoveListener(cb);
        field.SetTextWithoutNotify(value.ToString());
        field.onValueChanged.AddListener(cb);
    }

    private void OnPreyChanged(string text)
    {
        var map = MapSession.Instance != null ? MapSession.Instance.CurrentMap : null;
        if (map == null) return;
        int v = ParsePositive(text);
        map.randomPreyCount = v;
        if (preyCountField != null) preyCountField.SetTextWithoutNotify(v.ToString());
        MapSession.Instance.MarkDirty();
    }

    private void OnPredatorChanged(string text)
    {
        var map = MapSession.Instance != null ? MapSession.Instance.CurrentMap : null;
        if (map == null) return;
        int v = ParsePositive(text);
        map.randomPredatorCount = v;
        if (predatorCountField != null) predatorCountField.SetTextWithoutNotify(v.ToString());
        MapSession.Instance.MarkDirty();
    }

    // Accetta solo interi >= 0 (testo non valido -> 0).
    private static int ParsePositive(string text)
        => int.TryParse(text, out int v) ? Mathf.Max(0, v) : 0;
}
