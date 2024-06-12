#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Timers;
using UnityEditor;
using UnityEngine;

//TODO Configuration mode, beautify editor windows, See if Other Timer methods are valid

public class SaveSceneReminder : EditorWindow
{
    private static SaveSceneData data;
    private const string dataPath = "Assets/Editor/SaveSceneReminder/SaveSceneData.asset";
    
    private static int timerDuration;
    private static Timer timer;

    //public static bool isActive = false;

    [MenuItem("Window/SaveSceneReminder")]
    public static void ShowWindow()
    {
        GetData();
        
        Debug.Log("Show Window Called");
        var window = GetWindow<SaveSceneReminder>("Save Scene Reminder Tool");
        window.Show();

        //EditorApplication.playModeStateChanged += Test;
    }

    public static void SaveTimerData()
    {
        data.ChangeTimerInterval(30000);
        data.TimerDuration = timerDuration * 1000 * 60;
    }
    
    public static SaveSceneData GetData()
    {
        if (data == null)
        {
            data = (SaveSceneData)AssetDatabase.LoadAssetAtPath(dataPath, typeof(SaveSceneData));
            if (data == null)
            {
                data = new SaveSceneData();
                AssetDatabase.CreateAsset(data, dataPath);
                
                if (data == null)
                {
                    Debug.LogError("SaveSceneData object could not be found or created.");
                }
            }
        }
        
        return data;
    }

    void DrawWindowContents(int windowID)
    {
        GUILayout.Label("Testing Window!");
    }
    
    void OnGUI()
    {
        /*if (GUILayout.Button("Play Notification"))
        {
            SaveSceneReminderPopup.ShowWindow();
        }*/
        
        GUILayout.BeginHorizontal();
        
        Texture texture =
           (Texture) AssetDatabase.LoadAssetAtPath("Assets/Editor/SaveSceneReminder/settings-icon.png", typeof(Texture));
        GUISkin skin =
            (GUISkin)AssetDatabase.LoadAssetAtPath("Assets/Editor/SaveSceneReminder/CustomSkin.guiskin",
                typeof(GUISkin));
        
        var defaultSkin = GUI.skin;
        GUI.skin = skin;
        
        GUILayout.Label("Save Scene Reminder Tool");
        if (GUILayout.Button(texture))
        {
            GUILayout.Window(0,new Rect(Screen.currentResolution.width/2, Screen.currentResolution.height/2, 120 , 50), DrawWindowContents, "Configuration");
        }
        GUI.skin = defaultSkin;
        
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        //Creating Time field / Label
        
        if (data.IsActive)
        {
            //Show a greyed out field when timer is active
            GUI.enabled = false;
            timerDuration = EditorGUILayout.IntField("Timer Duration (in minutes): ", timerDuration);
            GUI.enabled = true;
        }
        else
        {
            timerDuration = EditorGUILayout.IntField("Timer Duration (in minutes): ", timerDuration);
        }
        
        if (data.IsActive)
        {
            if (GUILayout.Button("STOP")) StopButton();
            /*
            var oldGUIColor = GUI.color;
            GUI.color = Color.green;
            if (GUILayout.Button("Save Scene and Restart Timer")) SaveSceneAndStartTimerButton();

            GUI.color = oldGUIColor;
            if (GUILayout.Button("Save Scene and Stop Timer")) SaveSceneAndStopTimerButton();
            if (GUILayout.Button("Close")) CloseButton();
            */
        }
        else
        {
            if (GUILayout.Button("Start"))
            {
                if (timerDuration > 0)
                {
                    timerDuration = timerDuration;
                    StartTimer();
                }
            }
        }
    }
    
    #region BUTTON FUNCTIONS
    void StartButton()
    {
        SaveTimerData();
        StartTimer();
    }

    void StopButton()
    {
        StopTimer();
    }

    void CloseButton()
    {
        SaveTimerData();
        Close();
    }

    void SaveSceneAndStartTimerButton()
    {
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        StartTimer();
        CloseButton();
    }

    void SaveSceneAndStopTimerButton()
    {
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        StopButton();
    }
    #endregion

    public static void TimerElapsed()
    {
        EditorApplication.delayCall += TimerElapsedMainThread;
    }

    private static void TimerElapsedMainThread()
    {
        StopTimer();
        
        if (EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged += QueuePopup;
        }
        else
        {
            SaveSceneReminderPopup.ShowWindow();
        }
        
        Debug.Log("Timer Elapsed");
    }
    
    //Crude logic, Scripts do not recompile when exiting play mode.. I think
    private static void QueuePopup(PlayModeStateChange newState)
    {
        if (newState == PlayModeStateChange.EnteredEditMode)
        {
            SaveSceneReminderPopup.ShowWindow();
        }
    }
    
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (GetData().IsActive)
        {
            StartTimer();
        }
        Debug.Log("Scripts Reloaded");
        //EditorApplication.playModeStateChanged += Test;
    }

    public static void StartTimer()
    {
        GetData();
        SaveTimerData();
        
        data.IsActive = true;

        if (timer != null)
        {
            StopTimer();
        }
        
        //Timers count in MS
        timer = new System.Timers.Timer(data.TimeInterval);
        timer.Elapsed += data.AddTime;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    public static void StopTimer()
    {
        GetData().IsActive = false;
        timer?.Stop();
        timer?.Dispose();
        data.Reset();
        Debug.Log("Timer Stopped");
    }

    private void OnDestroy()
    {
        SaveTimerData();
    }
}

public class SaveSceneData : ScriptableObject
{
    [HideInInspector] public bool IsActive;
    [HideInInspector] public int TimerDuration;
    private AudioClip clip;
    public AudioClip AlertClip
    {
        get
        {
            if (clip != null) return clip;
            else
            {
                clip = (AudioClip)AssetDatabase.LoadAssetAtPath(default_path +"DefaultAlert.wav", typeof(AudioClip));
                return clip;
            }
        }
    }
    private int timeInterval = 30000;
    private int currentTimerDuration;

    public string FolderPath
    {
        get =>  default_path;
    }

    private string default_path = "Assets/Editor/SaveSceneReminder/";

    public int TimeInterval
    {
        get => timeInterval;
    }

    public void AddTime(object obj, ElapsedEventArgs e)
    {
        Debug.Log("Time Added");
        currentTimerDuration += timeInterval;
        if (currentTimerDuration >= TimerDuration) SaveSceneReminder.TimerElapsed();
    }

    /// <summary>
    /// Updates the time step of the timer, clamped between 1-60 seconds.
    /// </summary>
    /// <param name="ms"></param>
    public void ChangeTimerInterval(int ms)
    {
        timeInterval = Mathf.Clamp(ms, 1000, 60000);
    }

    public void Reset()
    {
        currentTimerDuration = 0;
    }
}

public class SaveSceneReminderPopup : EditorWindow
{
    public static void ShowWindow()
    {
        var window = ScriptableObject.CreateInstance<SaveSceneReminderPopup>();
        
        //For now, always puts popup in top left corner b/c mac has a bug with Screen.currentResolution
        //https://forum.unity.com/threads/get-screen-resolution-not-window-resolution.319511/
        
        #if UNITY_EDITOR_WIN
            window.position = new Rect(Screen.currentResolution.width/2, Screen.currentResolution.height/2, 200, 125);
        #else
            //Bug with Mac where Screen.currentResolution is weird
            //https://forum.unity.com/threads/get-screen-resolution-not-window-resolution.319511/
            window.position = new Rect(0, 0, 200, 125);
        #endif

        window.ShowPopup();
        PlayAlertSound(SaveSceneReminder.GetData().AlertClip);
    }

    /// <summary>
    /// From https://github.com/JohannesDeml/EditorAudioUtils
    /// </summary>
    public static void PlayAlertSound(AudioClip clip)
    {
        //No idea what this is doing
        ////https://discussions.unity.com/t/how-to-play-audioclip-in-edit-mode/10943/4
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
     
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );
 
        Debug.Log(method);
        method.Invoke(
            null,
            new object[] { clip, 0, false }
        );
    }

    private void OnGUI()
    {
        GUILayout.Label("Remember to save your scene!!!");
        
        GUILayout.Space(10);

        if (GUILayout.Button("Save Scene and Restart Timer"))
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            SaveSceneReminder.StartTimer();
            Close();
        }
        
        GUILayout.Space(10);

        if (GUILayout.Button("Save Scene and Open Menu"))
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Close();
            SaveSceneReminder.ShowWindow();
        }
        
        GUILayout.Space(10);

        if (GUILayout.Button("Close without Saving"))
        {
            Close();
        }
    }
}


public class SaveSceneReminderConfiguration : EditorWindow
{
    private int miliseconds;
    private AudioClip audioClip;
    private int volume;
    
    public SerializedProperty clip
    {
        get;
        set;
    }
    public static void ShowWindow()
    {
        var window = GetWindow<SaveSceneReminderConfiguration>("Configuration");
    }

    public void SaveConfiguration()
    {
        
    }

    private void OnGUI()
    {
        miliseconds = EditorGUILayout.IntField("Don't know: ", miliseconds);
        
        //audioClip = EditorGUILayout.ObjectField("Clip",obj, typeof(AudioClip));

        volume = EditorGUILayout.IntSlider("Volume",volume, 0, 100);
    }
}
#endif
