using UnityEngine;
using UnityEngine.UI;

public class NDRangeStepper : MonoBehaviour
{
    public LocalTileGrid tileGrid;

    [Header("UI")]
    public Button plusButton;
    public Button minusButton;
    public Image plusImage;
    public Image minusImage;
    public Sprite plusEnabled,
        plusDisabled;
    public Sprite minusEnabled,
        minusDisabled;

    [Header("Ranges (NM)")]
    public int[] rangesNm = new[] { 5, 10, 20 };
    public int startIndex = 2; // 20NM

    int idx;

    void Start()
    {
        idx = Mathf.Clamp(startIndex, 0, rangesNm.Length - 1);
        Apply();
    }

    public void OnPlus()
    {
        if (idx < rangesNm.Length - 1)
        {
            idx++;
            Apply();
        }
    }

    public void OnMinus()
    {
        if (idx > 0)
        {
            idx--;
            Apply();
        }
    }

    public NDRangeState rangeState;

    void Apply()
    {
        int nm = rangesNm[idx];
        rangeState.SetRangeNm(nm);

        bool canPlus = idx < rangesNm.Length - 1;
        bool canMinus = idx > 0;

        if (plusButton)
            plusButton.interactable = canPlus;
        if (minusButton)
            minusButton.interactable = canMinus;

        if (plusImage && plusEnabled && plusDisabled)
            plusImage.sprite = canPlus ? plusEnabled : plusDisabled;
        if (minusImage && minusEnabled && minusDisabled)
            minusImage.sprite = canMinus ? minusEnabled : minusDisabled;
    }
}
