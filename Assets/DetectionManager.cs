using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetectionManager : MonoBehaviour
{
    public static DetectionManager Instance;

    [Header("UI")]
    public Slider detectionSlider;

    private List<FieldOfView> enemies = new List<FieldOfView>();
    private bool hasLost = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterEnemy(FieldOfView enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(FieldOfView enemy)
    {
        enemies.Remove(enemy);
    }

    private void Update()
    {
        if (hasLost || enemies.Count == 0)
            return;

        float maxDetection = 0f;
        float maxTimeToLose = 0f;

        foreach (var enemy in enemies)
        {
            maxDetection = Mathf.Max(maxDetection, enemy.GetCurrentDetection());
            maxTimeToLose = Mathf.Max(maxTimeToLose, enemy.GetTimeToLose());
        }

        float normalized = maxDetection / maxTimeToLose;
        detectionSlider.value = normalized;

        if (normalized >= 1f)
        {
            hasLost = true;
            GameManager.Instance.lostGame = true;
        }
    }
}
