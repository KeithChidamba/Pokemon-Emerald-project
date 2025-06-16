using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class ZipUploader : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void UploadZipAndStoreToIDBFS();
    public static ZipUploader Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UploadSaveZip()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
                UploadZipAndStoreToIDBFS();
#else
        Debug.Log("This only works in WebGL build.");
#endif
    }
}