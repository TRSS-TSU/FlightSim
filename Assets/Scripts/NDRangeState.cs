using System;
using UnityEngine;

public class NDRangeState : MonoBehaviour
{
    public int CurrentRangeNm { get; private set; } = 20;
    public event Action<int> OnRangeChanged;

    public void SetRangeNm(int nm)
    {
        if (nm == CurrentRangeNm)
            return;
        CurrentRangeNm = nm;
        OnRangeChanged?.Invoke(nm);
    }
}
