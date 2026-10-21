using System.Collections.Generic;
using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 残影列表管理：3 个上限、FIFO 淘汰、统一重置。
    /// 只管 Domain 数据，不碰 GameObject；表现层根据列表增删视图。
    /// </summary>
    public sealed class EchoManager
    {
        private readonly List<EchoPlayback> _echoes = new List<EchoPlayback>();
        private int _nextId = 1;

        public IReadOnlyList<EchoPlayback> Echoes => _echoes;
        public int Count => _echoes.Count;

        /// <summary>
        /// 用当前录制创建一个残影。超过上限时移除最旧的一个。
        /// 新残影的 Id 单调递增，供表现层做视图映射/调试。
        /// </summary>
        public EchoPlayback Create(EchoRecorder recorder)
        {
            while (_echoes.Count >= GameConstants.MaxEchoes)
            {
                _echoes.RemoveAt(0);
            }

            //回放残影时，Id 递增，加入到列表中
            EchoPlayback echo = new EchoPlayback(_nextId++, recorder);
            _echoes.Add(echo);
            return echo;
        }

        /// <summary>
        /// 前进所有残影。先移除“上一 tick 已结束”的残影，
        /// 这样刚播完最后一帧的残影还能被渲染一次。
        /// </summary>
        public void Tick()
        {
            for (int i = _echoes.Count - 1; i >= 0; i--)
            {
                EchoPlayback echo = _echoes[i];
                if (echo.IsFinished)
                {
                    _echoes.RemoveAt(i);
                    continue;
                }

                echo.Tick();
            }
        }

        /// <summary>
        /// 回溯时调用：所有残影回到 t=0，包括刚创建的新残影。
        /// </summary>
        public void ResetAll()
        {
            for (int i = 0; i < _echoes.Count; i++)
            {
                _echoes[i].Reset();
            }
        }

        public void Clear()
        {
            _echoes.Clear();
        }

        public bool Contains(EchoPlayback echo)
        {
            return _echoes.Contains(echo);
        }
    }
}
