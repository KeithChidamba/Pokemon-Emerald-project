
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

public class TestGeneratorWindow : EditorWindow
{
    private const string TemplateFolder = "Assets/Scripts/Testing/TestTemplates/";
    private const string TestDataDestinationFolder = "Assets/Resources/Pokemon_project_assets/Tests/";
    private const string TestDataTemplateFolder = "Assets/Resources/Pokemon_project_assets/Tests/Template";
    
    private string className = "";
    private string destinationFolder = "Assets/Scripts/Testing";

    private string[] templateNames;
    private string[] templatePaths;
    private int selectedTemplateIndex;

    [MenuItem("Tools/Test Class Generator")]
    public static void ShowWindow()
    {
        GetWindow<TestGeneratorWindow>("Class Generator");
    }

    private void OnEnable()
    {
        RefreshTemplates();
    }

    private void OnGUI()
    {
        GUILayout.Label("C# Class Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // -------------------------
        // Template
        // -------------------------

        GUILayout.Label("Template", EditorStyles.boldLabel);

        if (templateNames == null || templateNames.Length == 0)
        {
            EditorGUILayout.HelpBox(
                $"No templates found in:\n{TemplateFolder}",
                MessageType.Warning
            );

            if (GUILayout.Button("Refresh Templates"))
            {
                RefreshTemplates();
            }
        }
        else
        {
            selectedTemplateIndex = EditorGUILayout.Popup(
                "Template",
                selectedTemplateIndex,
                templateNames
            );
        }

        EditorGUILayout.Space();

        // -------------------------
        // Class Name
        // -------------------------

        GUILayout.Label("Class", EditorStyles.boldLabel);

        className = EditorGUILayout.TextField(
            "Class Name",
            className
        );

        EditorGUILayout.Space();

        // -------------------------
        // Destination
        // -------------------------

        GUILayout.Label("Destination", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        destinationFolder = EditorGUILayout.TextField(
            destinationFolder
        );

        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            BrowseForFolder();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // -------------------------
        // Create Folder
        // -------------------------

        if (GUILayout.Button("Create Destination Folder"))
        {
            CreateDestinationFolder();
        }

        EditorGUILayout.Space();

        // -------------------------
        // Create Class
        // -------------------------

        GUI.enabled =
            !string.IsNullOrWhiteSpace(className) &&
            templateNames != null &&
            templateNames.Length > 0;

        if (GUILayout.Button("Create Class", GUILayout.Height(30)))
        {
            CreateClass();
        }

        GUI.enabled = true;
    }

    // ============================================================
    // Template Handling
    // ============================================================

    private void RefreshTemplates()
    {
        if (!AssetDatabase.IsValidFolder(TemplateFolder))
        {
            templateNames = Array.Empty<string>();
            templatePaths = Array.Empty<string>();
            return;
        }

        string absolutePath = Path.GetFullPath(TemplateFolder);

        string[] files = Directory
            .GetFiles(absolutePath, "*.cs", SearchOption.TopDirectoryOnly);

        templatePaths = files
            .Select(ConvertToAssetPath)
            .ToArray();

        templateNames = templatePaths
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        if (selectedTemplateIndex >= templateNames.Length)
        {
            selectedTemplateIndex = 0;
        }
    }

    private string ConvertToAssetPath(string absolutePath)
    {
        absolutePath = absolutePath.Replace("\\", "/");

        string projectPath = Application.dataPath
            .Substring(0, Application.dataPath.Length - "Assets".Length)
            .Replace("\\", "/");

        return absolutePath.Replace(projectPath, "");
    }

    // ============================================================
    // Folder Handling
    // ============================================================

    private void BrowseForFolder()
    {
        string absolutePath = EditorUtility.OpenFolderPanel(
            "Select Destination Folder",
            destinationFolder,
            ""
        );

        if (string.IsNullOrEmpty(absolutePath))
            return;

        string projectPath = Directory.GetParent(Application.dataPath).FullName;

        projectPath = projectPath.Replace("\\", "/");
        absolutePath = absolutePath.Replace("\\", "/");

        if (!absolutePath.StartsWith(projectPath))
        {
            EditorUtility.DisplayDialog(
                "Invalid Folder",
                "The destination folder must be inside the Unity project.",
                "OK"
            );

            return;
        }

        destinationFolder = absolutePath
            .Substring(projectPath.Length)
            .TrimStart('/');

        Repaint();
    }

    private void CreateDestinationFolder()
    {
        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please specify a destination folder.",
                "OK"
            );

            return;
        }

        if (AssetDatabase.IsValidFolder(destinationFolder))
        {
            EditorUtility.DisplayDialog(
                "Folder Exists",
                $"The folder already exists:\n{destinationFolder}",
                "OK"
            );

            return;
        }

        string absolutePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            destinationFolder
        );

        Directory.CreateDirectory(absolutePath);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Success",
            $"Created folder:\n{destinationFolder}",
            "OK"
        );
    }

    // ============================================================
    // Class Creation
    // ============================================================

    private void CreateClass()
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please enter a class name.",
                "OK"
            );

            return;
        }

        if (!IsValidClassName(className))
        {
            EditorUtility.DisplayDialog(
                "Invalid Class Name",
                $"'{className}' is not a valid C# class name.",
                "OK"
            );

            return;
        }

        if (templatePaths == null || templatePaths.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No templates are available.",
                "OK"
            );

            return;
        }

        if (!AssetDatabase.IsValidFolder(destinationFolder))
        {
            bool createFolder = EditorUtility.DisplayDialog(
                "Folder Doesn't Exist",
                $"The destination folder does not exist:\n\n{destinationFolder}\n\nWould you like to create it?",
                "Create",
                "Cancel"
            );

            if (!createFolder)
                return;

            string absolutePath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                destinationFolder
            );

            Directory.CreateDirectory(absolutePath);

            AssetDatabase.Refresh();
        }

        string templatePath = templatePaths[selectedTemplateIndex];

        string template = File.ReadAllText(templatePath);

        // Replace the class name placeholder.
        var setClassName = template.Replace(
            RemoveFileExtension(Path.GetFileName(templatePath)),
            className
        );

        // Convert "StatusEffectTest" -> "Status Effect Test"
        string formattedClassName = Regex.Replace(
            className,
            "(?<!^)([A-Z])",
            " $1"
        );

       
        // Set testName variable
        string generatedClass = setClassName.Replace(
            "TestNameVariable",
            formattedClassName
        );

        string filePath = Path.Combine(
            destinationFolder,
            $"{className}.cs"
        );

        if (File.Exists(filePath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Class Already Exists",
                $"A file already exists at:\n\n{filePath}\n\nDo you want to overwrite it?",
                "Overwrite",
                "Cancel"
            );

            if (!overwrite)
                return;
        }

        File.WriteAllText(
            filePath,
            generatedClass
        );
        
        //Setup test data 
        CopyTestDataTemplate(formattedClassName);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Class Created",
            $"Successfully created:\n\n{filePath}",
            "OK"
        );

        // Select the newly created file.
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(filePath);

        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        return;
        string RemoveFileExtension(string filename)
        {
            return filename.Split('.')[0];
        }
    }

    // ============================================================
    // Validation
    // ============================================================

    private bool IsValidClassName(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return false;

        if (!char.IsLetter(className[0]) && className[0] != '_')
            return false;

        for (int i = 1; i < className.Length; i++)
        {
            char character = className[i];

            if (!char.IsLetterOrDigit(character) && character != '_')
                return false;
        }

        // Check C# keywords.
        string[] keywords =
        {
            "class",
            "public",
            "private",
            "protected",
            "internal",
            "void",
            "int",
            "float",
            "double",
            "string",
            "bool",
            "new",
            "return",
            "if",
            "else",
            "for",
            "while",
            "namespace",
            "using",
            "static",
            "const",
            "readonly",
            "struct",
            "enum",
            "interface",
            "abstract",
            "virtual",
            "override"
        };

        return !keywords.Contains(className);
    }
    private void CopyTestDataTemplate(string formattedTestName)
    {
        if (!AssetDatabase.IsValidFolder(TestDataTemplateFolder))
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Test data template folder was not found:\n{TestDataTemplateFolder}",
                "OK"
            );

            return;
        }

        string destinationPath = Path.Combine(
            TestDataDestinationFolder,
            formattedTestName
        );

        destinationPath = destinationPath.Replace("\\", "/");

        if (Directory.Exists(destinationPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Test Data Folder Already Exists",
                $"A test data folder already exists at:\n\n{destinationPath}\n\nDo you want to replace it?",
                "Replace",
                "Cancel"
            );

            if (!overwrite)
                return;

            Directory.Delete(destinationPath, true);
        }

        string sourceAbsolutePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            TestDataTemplateFolder
        );

        string destinationAbsolutePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            destinationPath
        );

        FileUtil.CopyFileOrDirectory(
            sourceAbsolutePath,
            destinationAbsolutePath
        );

        AssetDatabase.Refresh();
    }
}


