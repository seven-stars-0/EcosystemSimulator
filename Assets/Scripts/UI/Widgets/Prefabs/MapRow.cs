using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;

    [Header("Logging")]
    [Tooltip("Se attivo, la run di questa mappa scrive il CSV di log. " +
             "Default spento per non riempire la memoria senza volerlo.")]
    [SerializeField] private Toggle logToggle;

    /// <summary>onPlay riceve true se il toggle LOG e' attivo.</summary>
    public void Initialize(MapMetadata meta,
                           Action<bool> onPlay,
                           Action onEdit,
                           Action onDelete)
    {
        nameLabel.text = meta.mapName;

        playButton.onClick.AddListener(() => onPlay(logToggle != null && logToggle.isOn));
        editButton.onClick.AddListener(() => onEdit());
        deleteButton.onClick.AddListener(() => onDelete());
    }
}
