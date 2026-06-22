using UnityEngine;

[ExecuteAlways]
public class NDRangeLock : MonoBehaviour
{
    public NDRangeState rangeState;
    public Camera ndCam;
    public float fallbackWidthNm = 20f; // used if no rangeState

    void OnEnable()
    {
        if (!ndCam)
            ndCam = GetComponent<Camera>();
        if (rangeState != null)
            rangeState.OnRangeChanged += OnRangeChanged;
        Apply(rangeState ? rangeState.CurrentRangeNm : Mathf.RoundToInt(fallbackWidthNm));
    }

    void OnDisable()
    {
        if (rangeState != null)
            rangeState.OnRangeChanged -= OnRangeChanged;
    }

    void OnValidate()
    {
        if (!ndCam)
            ndCam = GetComponent<Camera>();
        Apply(rangeState ? rangeState.CurrentRangeNm : Mathf.RoundToInt(fallbackWidthNm));
    }

    void OnRangeChanged(int nm) => Apply(nm);

    void Apply(int widthNm)
    {
        if (!ndCam)
            return;
        ndCam.orthographic = true;
        float widthM = widthNm * 1852f;
        float aspect = 1f; // 785x785 RT
        ndCam.orthographicSize = (widthM / aspect) * 0.5f;
    }
}
