using UnityEditor;
using System.IO;

public class WebGLBuilder
{
    public static void BuildWebGL()
    {
        string[] scenes = { "Assets/Scenes/Main.unity" };
        string pathToBuild = "build/WebGL";

        if (!Directory.Exists(pathToBuild))
            Directory.CreateDirectory(pathToBuild);

        BuildPipeline.BuildPlayer(scenes, pathToBuild, BuildTarget.WebGL, BuildOptions.None);
        UnityEngine.Debug.Log("✅ WebGL build completed.");
    }
}