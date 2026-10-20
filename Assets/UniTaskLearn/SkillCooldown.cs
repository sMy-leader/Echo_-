using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

public class Skill
{
    public string SkillName;
    public float CooldownTime;
    private bool isCooling;
    public bool isReady => !isCooling;

    private CancellationTokenSource coolcts;

    /// <summary>
    /// 开始冷却
    /// </summary>
    /// <param name="ownerToken"></param>
    public void StartCooldown(CancellationToken ownerToken)
    {
        if (isCooling)
            return;
        isCooling = true;
        coolcts = CancellationTokenSource.CreateLinkedTokenSource(ownerToken);//创建一个与外部token关联的CancellationTokenSource

        CooldownAsync(coolcts.Token).Forget();

    }

    async UniTask CooldownAsync(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(CooldownTime), cancellationToken: token);//等待冷却时间
        }
        catch (OperationCanceledException)
        {
            // 冷却被取消
        }
        finally
        {
            isCooling = false;
            coolcts.Dispose();
            coolcts = null;
        }
    }

    /// <summary>
    /// 中断冷却（死亡、切换武器等场景调用）
    /// </summary>
    public void Interrupt()
    {
        coolcts?.Cancel();
    }


    /// <summary>
    /// 重置冷却，立即就绪
    /// </summary>
    public void Reset()
    {
        Interrupt();
    }
}

