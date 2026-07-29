using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public class LightingSlidersSetup : EditorWindow
{
    private GameObject avatarTarget;
    private List<Material> customMaterials = new List<Material>();
    private List<string> logMessages = new List<string>();
    private Vector2 scrollPos, customMaterialsScrollPos;
    private bool showDetailedLog = false;
    private bool isProcessing = false;

    private int lastFoundCount = 0;
    private int lastProcessedCount = 0;
    private int lastErrorCount = 0;

    private readonly string[] propertiesToAnimate = new string[]
    {
        "_LightingMonochromatic",
        "_PPFinalColorMultiplier",
        "_PPLightingAddition",
        "_PPLightingMultiplier",
        "_LightingMinLightBrightness",
        "_LightingCap",
        "_PPEmissionMultiplier"
    };

    [MenuItem("Tools/UpInPixels/Lighting Sliders Setup")]
    public static void ShowWindow()
    {
        GetWindow<LightingSlidersSetup>("Lighting Sliders Setup");
    }

    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 14;
        EditorGUILayout.LabelField("Lighting Sliders Setup", titleStyle);
        EditorGUILayout.LabelField("by UpInPixels", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(8);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Avatar Settings", EditorStyles.boldLabel);
        avatarTarget = (GameObject)EditorGUILayout.ObjectField("Avatar Root", avatarTarget, typeof(GameObject), true);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Extra Materials", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Drag Poiyomi materials onto the Add button to include them.", EditorStyles.miniLabel);

        if (customMaterials.Count > 0)
        {
            customMaterialsScrollPos = EditorGUILayout.BeginScrollView(customMaterialsScrollPos, GUILayout.Height(Mathf.Min(80, customMaterials.Count * 22)));
            for (int i = 0; i < customMaterials.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                customMaterials[i] = (Material)EditorGUILayout.ObjectField(customMaterials[i], typeof(Material), false);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    customMaterials.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        Rect addButtonRect = GUILayoutUtility.GetRect(new GUIContent("Add Material"), GUI.skin.button, GUILayout.Height(25));
        if (GUI.Button(addButtonRect, "Add Material"))
        {
            customMaterials.Add(null);
        }

        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (addButtonRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            Material mat = obj as Material;
                            if (mat != null && mat.shader != null && mat.shader.name.ToLower().Contains("poiyomi"))
                            {
                                customMaterials.Add(mat);
                            }
                        }
                    }
                    evt.Use();
                }
                break;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        bool hasAvatar = avatarTarget != null;
        bool hasCustomMaterials = customMaterials.Exists(m => m != null);
        bool canRun = !isProcessing && (hasAvatar || hasCustomMaterials);

        bool wasEnabled = GUI.enabled;
        GUI.enabled = canRun;
        string buttonText = isProcessing ? "Processing..." : 
                            (hasAvatar ? "Setup Materials & Add Sliders" : "Setup Only Materials");
        if (GUILayout.Button(buttonText, GUILayout.Height(36)))
        {
            if (!hasAvatar && !hasCustomMaterials)
            {
                AddLog("Please assign an Avatar Root or add at least one material to the Extra list.", true);
                return;
            }

            logMessages.Clear();
            lastFoundCount = 0;
            lastProcessedCount = 0;
            lastErrorCount = 0;

            HashSet<Material> poiyomiMaterials = new HashSet<Material>();

            if (hasAvatar)
            {
                poiyomiMaterials = GatherPoiyomiMaterials(avatarTarget);
            }

            foreach (Material m in customMaterials)
            {
                if (m != null && m.shader != null && m.shader.name.ToLower().Contains("poiyomi"))
                    poiyomiMaterials.Add(m);
            }

            lastFoundCount = poiyomiMaterials.Count;
            string sourceInfo = hasAvatar ? "avatar and extra list" : "extra list";
            AddLog($"Found {lastFoundCount} unique Poiyomi materials from {sourceInfo}.");

            if (poiyomiMaterials.Count == 0)
            {
                AddLog("No Poiyomi materials found.");
            }
            else
            {
                isProcessing = true;
                EditorApplication.delayCall += () => ProcessPipeline(poiyomiMaterials, hasAvatar);
            }
        }
        GUI.enabled = wasEnabled;

        EditorGUILayout.Space(8);

        if (lastFoundCount > 0 || lastProcessedCount > 0 || lastErrorCount > 0)
        {
            string summaryText = $"Found {lastFoundCount}   |   Processed {lastProcessedCount}   |   Errors {lastErrorCount}";
            MessageType msgType = lastErrorCount > 0 ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(summaryText, msgType);
        }
        else if (!isProcessing)
        {
            EditorGUILayout.HelpBox("No run yet. Assign an avatar or add extra materials, then press Setup.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("Processing materials...", MessageType.Info);
        }

        showDetailedLog = EditorGUILayout.Foldout(showDetailedLog, "Detailed Log", true, EditorStyles.foldoutHeader);
        if (showDetailedLog)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(180));

            string fullLog = logMessages.Count > 0 ? string.Join("\n", logMessages) : "No log entries.";
            GUIStyle readOnlyArea = new GUIStyle(EditorStyles.textArea);
            readOnlyArea.wordWrap = true;
            GUI.enabled = false;
            EditorGUILayout.TextArea(fullLog, readOnlyArea, GUILayout.ExpandHeight(true));
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    private void AddLog(string message, bool isError = false)
    {
        string prefix = isError ? "[Error] " : "";
        string fullMessage = prefix + message;
        logMessages.Add(fullMessage);
        if (isError) Debug.LogError("[Lighting Setup] " + message);
        else Debug.Log("[Lighting Setup] " + message);
    }

    private void ProcessPipeline(HashSet<Material> materials, bool addPrefab)
    {
        int processedCount = 0;
        int errorCount = 0;

        foreach (Material mat in materials)
        {
            Shader originalShader = mat.shader;
            if (originalShader == null)
            {
                AddLog($"Material '{mat.name}' has no shader – skipping.", true);
                errorCount++;
                continue;
            }

            string shaderName = originalShader.name;
            bool wasLocked = shaderName.Contains("Locked");

            AddLog($"Processing material: {mat.name} (locked = {wasLocked})");

            if (wasLocked)
            {
                bool unlockCallOk = SafeSetLockState(mat, false, originalShader);
                if (!unlockCallOk)
                {
                    AddLog($"Unlock call failed for '{mat.name}' – skipping.", true);
                    errorCount++;
                    continue;
                }

                if (mat.shader == null || mat.shader.name.Contains("Locked"))
                {
                    AddLog($"Material '{mat.name}' did not unlock to a base shader. Restoring locked shader and skipping.", true);
                    if (mat.shader == null || mat.shader != originalShader)
                    {
                        mat.shader = originalShader;
                        EditorUtility.SetDirty(mat);
                    }
                    errorCount++;
                    continue;
                }
            }

            ApplyAnimationFlags(mat);

            bool lockSucceeded = true;
            if (wasLocked)
            {
                lockSucceeded = SafeSetLockState(mat, true, originalShader);
                if (!lockSucceeded)
                {
                    AddLog($"Failed to re-lock material '{mat.name}'. It will remain unlocked.", true);
                    errorCount++;
                }
            }

            if (!wasLocked || (wasLocked && lockSucceeded))
                processedCount++;

            AddLog($"Finished material: {mat.name}");
        }

        if (addPrefab && avatarTarget != null)
        {
            AddLog("Adding Lighting Sliders prefab to avatar...");
            AddLightingSlidersPrefab(avatarTarget);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ActiveEditorTracker.sharedTracker.ForceRebuild();

        Type inspectorType = Type.GetType("UnityEditor.InspectorWindow, UnityEditor");
        if (inspectorType != null)
        {
            foreach (var win in Resources.FindObjectsOfTypeAll(inspectorType))
            {
                MethodInfo repaintMethod = inspectorType.GetMethod("Repaint", BindingFlags.Public | BindingFlags.Instance);
                repaintMethod?.Invoke(win, null);
            }
        }

        lastProcessedCount = processedCount;
        lastErrorCount = errorCount;
        isProcessing = false;

        AddLog($"Pipeline finished! Processed {processedCount} materials.");
        Repaint();
    }

    private bool SafeSetLockState(Material mat, bool lockMaterial, Shader fallbackShader)
    {
        try
        {
            SetMaterialLockState(mat, lockMaterial);
        }
        catch (Exception e)
        {
            AddLog($"Exception during lock/unlock: {e.Message}", true);
            RestoreShaderIfNeeded(mat, fallbackShader);
            return false;
        }

        if (mat.shader == null)
        {
            AddLog($"Material '{mat.name}' lost its shader after lock/unlock. Restoring original shader.", true);
            RestoreShaderIfNeeded(mat, fallbackShader);
            return false;
        }

        return true;
    }

    private void RestoreShaderIfNeeded(Material mat, Shader fallbackShader)
    {
        if (mat.shader == null && fallbackShader != null)
        {
            mat.shader = fallbackShader;
            EditorUtility.SetDirty(mat);
        }
    }

    private void AddLightingSlidersPrefab(GameObject avatar)
    {
        string prefabPath = GetPrefabPath();
        if (string.IsNullOrEmpty(prefabPath))
        {
            AddLog("Could not determine prefab location.", true);
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            AddLog($"Could not find Lighting Sliders.prefab at: {prefabPath}", true);
            return;
        }

        Transform existing = avatar.transform.Find(prefab.name);
        if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) == prefab)
        {
            AddLog("Lighting Sliders prefab already added to avatar.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, avatar.transform);
        instance.name = prefab.name;
        EditorUtility.SetDirty(avatar);
        AddLog("Added Lighting Sliders prefab to avatar.");
    }

    private string GetPrefabPath()
    {
        string scriptPath = GetScriptPath();
        if (string.IsNullOrEmpty(scriptPath))
            return null;

        string scriptFolder = Path.GetDirectoryName(scriptPath).Replace("\\", "/");

        if (Path.GetFileName(scriptFolder).Equals("Editor", StringComparison.OrdinalIgnoreCase))
        {
            string parentFolder = Path.GetDirectoryName(scriptFolder).Replace("\\", "/");
            if (string.IsNullOrEmpty(parentFolder))
                return null;

            string prefabPath = Path.Combine(parentFolder, "Lighting Sliders.prefab").Replace("\\", "/");
            if (File.Exists(prefabPath))
                return MakeRelative(prefabPath);

            string editorPrefabPath = Path.Combine(scriptFolder, "Lighting Sliders.prefab").Replace("\\", "/");
            if (File.Exists(editorPrefabPath))
                return MakeRelative(editorPrefabPath);

            return null;
        }

        string directPath = Path.Combine(scriptFolder, "Lighting Sliders.prefab").Replace("\\", "/");
        if (File.Exists(directPath))
            return MakeRelative(directPath);

        return null;
    }

    private string MakeRelative(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        return absolutePath;
    }

    private string GetScriptPath()
    {
        MonoScript script = MonoScript.FromScriptableObject(this);
        if (script != null)
            return AssetDatabase.GetAssetPath(script);
        return null;
    }

    private HashSet<Material> GatherPoiyomiMaterials(GameObject root)
    {
        HashSet<Material> mats = new HashSet<Material>();

        if (root == null) return mats;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader != null && mat.shader.name.ToLower().Contains("poiyomi"))
                    mats.Add(mat);
            }
        }

        AnimationClip[] clips = AnimationUtility.GetAnimationClips(root);
        foreach (AnimationClip clip in clips)
        {
            if (clip == null) continue;
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.propertyName.StartsWith("m_Materials.Array.data["))
                {
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    foreach (ObjectReferenceKeyframe keyframe in keyframes)
                    {
                        Material swapMat = keyframe.value as Material;
                        if (swapMat != null && swapMat.shader != null && swapMat.shader.name.ToLower().Contains("poiyomi"))
                            mats.Add(swapMat);
                    }
                }
            }
        }

        return mats;
    }

    private bool ApplyAnimationFlags(Material mat)
    {
        bool anyModified = false;
        foreach (string propName in propertiesToAnimate)
        {
            if (!mat.HasProperty(propName)) continue;

            string tagName = propName + "Animated";
            string current = mat.GetTag(tagName, false, "");

            if (current != "1")
            {
                mat.SetOverrideTag(tagName, "1");
                anyModified = true;
            }
        }

        if (anyModified)
            EditorUtility.SetDirty(mat);

        return anyModified;
    }

    private void SetMaterialLockState(Material mat, bool lockMaterial)
    {
        Type optimizerType = null;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            optimizerType = assembly.GetType("Thry.ThryEditor.ShaderOptimizer");
            if (optimizerType != null) break;
        }

        if (optimizerType == null)
            throw new InvalidOperationException("Thry.ThryEditor.ShaderOptimizer not found.");

        MethodInfo method = optimizerType.GetMethod("SetLockedForAllMaterials", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new InvalidOperationException("SetLockedForAllMaterials method not found.");

        method.Invoke(null, new object[]
        {
            new Material[] { mat },
            lockMaterial ? 1 : 0,
            false,
            false,
            false,
            null
        });
    }
}