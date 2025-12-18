using UnityEngine;
using TMPro;

public class BestTimesUI : MonoBehaviour
{
    public TextMeshProUGUI level1BestText;
    public TextMeshProUGUI level2BestText;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        level1BestText.text = GetBestTimeText("ApartmentV2");
        level2BestText.text = GetBestTimeText("HouseV2");
    }

    private string GetBestTimeText(string levelName)
    {
        string key = $"BestTime_{levelName}";

        if (!PlayerPrefs.HasKey(key))
            return "--:--.--";

        float time = PlayerPrefs.GetFloat(key);

        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return string.Format("{0:0}:{1:00.00}", minutes, seconds);
    }
}
