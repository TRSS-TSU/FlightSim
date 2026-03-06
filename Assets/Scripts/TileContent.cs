using UnityEngine;

public class TileContent : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer meshRenderer;

    public void SetTexture(Texture2D tex)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material.mainTexture = tex;
    }
}
