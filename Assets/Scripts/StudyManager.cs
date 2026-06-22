using System.IO;
using UnityEngine;

public class StudyManager : MonoBehaviour
{
    [SerializeField]
    private string pdfFileName = "T1-A AMP Flight manual 1T-1A.pdf";

    public void OpenStudyManual()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, "Study", pdfFileName);

        // iOS prefers an explicit file:// URL
        string url = "file://" + fullPath;

        Debug.Log($"Opening Study PDF: {url}");
        Application.OpenURL(url);
    }
}
