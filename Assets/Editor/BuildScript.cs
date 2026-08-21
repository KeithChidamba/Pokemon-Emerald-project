using UnityEditor;
using System.IO;

    public class WebGLBuilder
    {
        public static void BuildGame()
        {
            string[] scenes = { "Assets/Scenes/Main.unity" };
            string pathToBuild = "Build/";

            if (!Directory.Exists(pathToBuild))
                Directory.CreateDirectory(pathToBuild);

            BuildPipeline.BuildPlayer(scenes, pathToBuild, BuildTarget.WebGL, BuildOptions.None);
        }
    }

