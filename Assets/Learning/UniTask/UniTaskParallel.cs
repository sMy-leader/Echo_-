using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System;

public class UniTaskParallel : MonoBehaviour
{
    public Image img1;
    public Image img2;

    private void Start()
    {
        LoadResourceParallel().Forget();
    }

    async UniTask LoadResourceParallel()
    {
        Debug.Log("开始并行加载资源");

        //启动2个异步任务（不等待，并行执行）
        var loadImg1 =Resources.LoadAsync<Sprite>("Img1").ToUniTask();
        var loadImg2 = Resources.LoadAsync<Sprite>("Img2").ToUniTask();


        // 等待所有任务完成（并行执行，总耗时=最长任务耗时）
        await UniTask.WhenAll(loadImg1, loadImg2);


        // 赋值结果
        //img1.sprite = loadImg1.Result;
        //img2.sprite = loadImg2.Result;

        Debug.Log("所有资源加载完成");

        // 若只需等待任意一个完成：await UniTask.WhenAny(loadImg1, loadImg2);
    }
}
