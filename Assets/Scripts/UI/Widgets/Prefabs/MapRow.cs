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

    public void Initialize(MapMetadata meta,
                           Action onPlay,
                           Action onEdit,
                           Action onDelete)
    {
        nameLabel.text = meta.mapName;

        playButton.onClick.AddListener(() => onPlay());
        editButton.onClick.AddListener(() => onEdit());
        deleteButton.onClick.AddListener(() => onDelete());
    }
}