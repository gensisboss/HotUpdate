using UniFramework.Machine;
using UnityEngine;
using YooAsset;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using System;
using HybridCLR;

internal class FsmStartGame : IStateNode
{

     private Assembly _hotUpdateAss;


     private static List<string> AOTMetaAssemblyFiles {get; } = new List<string>(){
        "UniFramework.Event.dll",
		"UnityEngine.CoreModule.dll",
		"YooAsset.dll",
		"mscorlib.dll",
    };

    private static Dictionary<string, TextAsset> s_assetDatas = new Dictionary<string, TextAsset>();


    void IStateNode.OnCreate(StateMachine machine)
    {
    }

    void IStateNode.OnEnter()
    {
        GameManager.Instance.SetGamePackage(YooAssets.GetPackage("DefaultPackage"));
        GameManager.Instance.StartCoroutine(LoadDLLs());
    }

    void IStateNode.OnUpdate()
    {
    }

    void IStateNode.OnExit()
    {
    }


    
    private IEnumerator LoadDLLs(){
        var gamePackage = YooAssets.GetPackage("DefaultPackage");
        var assets = new List<string>() { "HotUpdate.dll" }.Concat(AOTMetaAssemblyFiles);
        foreach (var dllName in assets)
        {
            Debug.Log($"加载程序集: {dllName}");
            var handle = gamePackage.LoadAssetAsync<TextAsset>(dllName);
            yield return handle;
            if(handle.Status != EOperationStatus.Succeeded){
                Debug.LogError($"加载程序集: {dllName} 失败");
                yield break;
            }
            s_assetDatas.Add(dllName, handle.AssetObject as TextAsset);
            handle.Release();
        }
        LoadMetadataForAOTAssembly();
        LoadHotUpdateDlls(); 

        SceneChangeToHomeEvent.SendEventMessage();

    }



    private void LoadMetadataForAOTAssembly(){
        var mode = HomologousImageMode.SuperSet;
        foreach (var asset in AOTMetaAssemblyFiles){
            var bytes = s_assetDatas[asset].bytes;
            var error = RuntimeApi.LoadMetadataForAOTAssembly(bytes, mode);
            if (error != LoadImageErrorCode.OK)
            {
                Debug.LogError($"Load metadata for AOT assembly {asset} failed: {error}");
            }
        }
       
    }


    private void LoadHotUpdateDlls(){
        if(s_assetDatas.Count == 0){
            Debug.Log("程序集字节数据为空，跳过加载热更dll");
            return;
        }
#if !UNITY_EDITOR
        _hotUpdateAss = Assembly.Load(s_assetDatas["HotUpdate.dll"].bytes);
#else
        _hotUpdateAss = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "HotUpdate");
#endif
    }
}
