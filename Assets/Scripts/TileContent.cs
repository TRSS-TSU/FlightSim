using UnityEngine;

public class TileContent : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer meshRenderer;

    public Texture2D RuntimeTexture { get; private set; }
    public Material RuntimeMaterial { get; private set; }

    public void SetTexture(Texture2D tex)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        ReleaseRuntimeAssets();
        RuntimeTexture = tex;
        RuntimeMaterial = meshRenderer.material;
        RuntimeMaterial.mainTexture = tex;
    }

    public void ReleaseRuntimeAssets()
    {
        if (RuntimeMaterial != null)
        {
            Destroy(RuntimeMaterial);
            RuntimeMaterial = null;
        }

        if (RuntimeTexture != null)
        {
            Destroy(RuntimeTexture);
            RuntimeTexture = null;
        }
    }

    private void OnDestroy() => ReleaseRuntimeAssets();
}
