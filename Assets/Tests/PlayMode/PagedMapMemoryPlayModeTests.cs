using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PagedMapMemoryPlayModeTests
{
    [UnityTest]
    public IEnumerator MasterFmsLoadsBoundedPagedChunks()
    {
        yield return SceneManager.LoadSceneAsync("Master_FMS", LoadSceneMode.Single);

        Type gridType = Type.GetType("LocalTileChunkGrid, Assembly-CSharp");
        Component grid = null;
        for (int frame = 0; frame < 600; frame++)
        {
            grid = gridType == null ? null : UnityEngine.Object.FindFirstObjectByType(gridType) as Component;
            if (grid && !(bool)gridType.GetProperty("IsLoading").GetValue(grid))
                break;
            yield return null;
        }

        Assert.IsNotNull(gridType);
        Assert.IsNotNull(grid);
        Assert.IsTrue((bool)gridType.GetProperty("IsLoaded").GetValue(grid));
        int resident = (int)gridType.GetProperty("ResidentChunkCount").GetValue(grid);
        int maxResident = (int)gridType.GetField("maxResidentChunks").GetValue(grid);
        Transform parent = (Transform)gridType.GetField("chunkParent").GetValue(grid);
        long bytes = (long)gridType.GetProperty("EstimatedResidentTextureBytes").GetValue(grid);
        Assert.LessOrEqual(resident, maxResident);
        Assert.AreEqual(resident, parent.childCount);
        Assert.Greater(bytes, 0L);
    }
}
