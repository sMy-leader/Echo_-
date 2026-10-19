using UnityEngine;
using Cysharp.Threading.Tasks;

public class TestUnitask : MonoBehaviour
{
    private void Start()
    {
        //// 调用异步方法，Forget()表示“不等待结果”（类似StartCoroutine）
        TestBasicAsync().Forget();
    }

    // 异步方法标记：async + 返回UniTask（核心格式）
    async UniTask TestBasicAsync()
    {
        Debug.Log("开始执行");

        // 1. 延迟2秒（无GC，替代WaitForSeconds）
        await UniTask.Delay(2000); // 单位：毫秒
                                   // 或按时间跨度：await UniTask.Delay(TimeSpan.FromSeconds(2));

        Debug.Log("延迟2秒后执行");

        // 2. 等待下一帧（替代yield return null）
        await UniTask.Yield();
        Debug.Log("下一帧执行");

        // 3. 等待LateUpdate后执行（替代yield return new WaitForEndOfFrame()）
        await UniTask.WaitForEndOfFrame();
        Debug.Log("LateUpdate后执行");
    }
}
