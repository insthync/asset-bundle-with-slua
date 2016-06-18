using UnityEngine;
using System.Collections;
using AssetBundles;
using SLua;

public class LoadLuaFiles : MonoBehaviour
{

    public string luaAssetBundle;
    public string luaAssetFile;
    TextAsset luaScript;
    LuaSvr svr;
    LuaTable self;
    LuaFunction update;

    // Use this for initialization
    IEnumerator Start()
    {
        yield return StartCoroutine(Initialize());

        // Load script files
        yield return StartCoroutine(InitializeLuaAsync());

        // Then after retreived scrip files, Start lua scripts
        StartLuaScript();
    }

    // Initialize the downloading url and AssetBundleManifest object.
    protected IEnumerator Initialize()
    {
        // Don't destroy this gameObject as we depend on it to run the loading script.
        DontDestroyOnLoad(gameObject);

        // With this code, when in-editor or using a development builds: Always use the AssetBundle Server
        // (This is very dependent on the production workflow of the project. 
        // 	Another approach would be to make this configurable in the standalone player.)
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        AssetBundleManager.SetDevelopmentAssetBundleServer();
#else
		// Use the following code if AssetBundles are embedded in the project for example via StreamingAssets folder etc:
		AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/");
		// Or customize the URL based on your deployment or configuration
		//AssetBundleManager.SetSourceAssetBundleURL("http://www.MyWebsite/MyAssetBundles");
#endif

        // Initialize AssetBundleManifest which loads the AssetBundleManifest object.
        var request = AssetBundleManager.Initialize();

        if (request != null)
            yield return StartCoroutine(request);
    }

    protected IEnumerator InitializeLuaAsync()
    {
        // This is simply to get the elapsed time for this phase of AssetLoading.
        float startTime = Time.realtimeSinceStartup;

        // Load level from assetBundle.
        AssetBundleLoadAssetOperation request = request = AssetBundleManager.LoadAssetAsync(luaAssetBundle, luaAssetFile, typeof(TextAsset));
        if (request == null)
            yield break;
        yield return StartCoroutine(request);
        luaScript = request.GetAsset<TextAsset>();

        // Calculate and display the elapsed time.
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        Debug.Log("Finished loading file " + luaAssetFile + "  in " + elapsedTime + " seconds");
    }

    void StartLuaScript()
    {
        // Make sure that we got script file before start
        if (luaScript != null)
        {
            // Initializing
            svr = new LuaSvr();
            svr.init(null, () =>
            {
                object obj;
                // Reading lua scripts
                if (svr.luaState.doBuffer(luaScript.bytes, "Here can be any name but should unique", out obj))
                {
                    // Calling main function from lua script
                    LuaFunction func = (LuaFunction)svr.luaState["main"];
                    if (func != null)
                    {
                        self = (LuaTable)func.call();
                        update = (LuaFunction)self["update"];
                    }
                    else
                    {
                        Debug.LogError("No main function");
                    }
                }
                else
                {
                    Debug.LogError("Can not read scripts");
                }
            });
        }
    }

    void Update()
    {
        if (update != null) update.call(self);
    }
}
