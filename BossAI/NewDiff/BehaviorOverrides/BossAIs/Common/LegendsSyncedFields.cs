using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    /// <summary>
    /// Boss 实例字段的过网声明表。
    ///
    /// 要解决的问题：Registry 里每个 Boss 类型只有一个 AI 实例，它的实例字段默认【完全不过网】——
    /// 服务端和各客户端各算各的。只要这些字段被用来 gate 伤害（护盾）或 gate 招式选择（部位血量），
    /// 联机就会出现"我明明在打却不掉血"、甚至各端走出不同的招式序列。
    ///
    /// 为什么不让各 Boss 自己写 SendExtraAI/ReceiveExtraAI：那样要求作者手工保证收发两端
    /// 【同样的字段、同样的顺序、同样的宽度】。这个约定一旦写错不会报错、不会崩，只会读串成一堆
    /// 乱码状态，是极难排查的一类 bug。这里改成"声明一次，框架双向执行"——顺序和宽度由同一份
    /// 声明推导出来，物理上不可能不对称。
    ///
    /// 用法（在 Boss AI 里覆写 DeclareSyncedFields）：
    /// <code>
    /// protected override void DeclareSyncedFields(LegendsSyncedFields f) => f
    ///     .Bool(() => shieldActive,   v => shieldActive = v)
    ///     .Int(() => shieldHealth,    v => shieldHealth = v)
    ///     .Float(() => chargeProgress, v => chargeProgress = v);
    /// </code>
    ///
    /// 只声明"另一端算不出来、但又会影响玩法判定"的字段。纯表现层的东西（拖尾坐标、闪光 alpha、
    /// 粒子计时器）不要塞进来——它们每帧都在变，过网只会白白吃带宽，各端自己算就够了。
    /// </summary>
    public sealed class LegendsSyncedFields
    {
        private readonly List<Action<BinaryWriter>> writers = new();
        private readonly List<Action<BinaryReader>> readers = new();

        public LegendsSyncedFields Float(Func<float> get, Action<float> set)
        {
            writers.Add(w => w.Write(get()));
            readers.Add(r => set(r.ReadSingle()));
            return this;
        }

        public LegendsSyncedFields Int(Func<int> get, Action<int> set)
        {
            writers.Add(w => w.Write(get()));
            readers.Add(r => set(r.ReadInt32()));
            return this;
        }

        public LegendsSyncedFields Bool(Func<bool> get, Action<bool> set)
        {
            writers.Add(w => w.Write(get()));
            readers.Add(r => set(r.ReadBoolean()));
            return this;
        }

        public LegendsSyncedFields Vec2(Func<Vector2> get, Action<Vector2> set)
        {
            writers.Add(w =>
            {
                Vector2 v = get();
                w.Write(v.X);
                w.Write(v.Y);
            });
            readers.Add(r => set(new Vector2(r.ReadSingle(), r.ReadSingle())));
            return this;
        }

        /// <summary>定长数组。长度取声明时的 length，收发两端必然一致，不写长度前缀。</summary>
        public LegendsSyncedFields FloatArray(float[] array)
        {
            int length = array.Length;
            writers.Add(w =>
            {
                for (int i = 0; i < length; i++)
                    w.Write(array[i]);
            });
            readers.Add(r =>
            {
                for (int i = 0; i < length; i++)
                    array[i] = r.ReadSingle();
            });
            return this;
        }

        public LegendsSyncedFields BoolArray(bool[] array)
        {
            int length = array.Length;
            writers.Add(w =>
            {
                for (int i = 0; i < length; i++)
                    w.Write(array[i]);
            });
            readers.Add(r =>
            {
                for (int i = 0; i < length; i++)
                    array[i] = r.ReadBoolean();
            });
            return this;
        }

        /// <summary>
        /// 以 NPC whoAmI 为键的部位血量表（AstrumDeus 那种"节段各自有核心"的结构）。
        /// 变长，但写了长度前缀，收发依然对称。读端整表重建而不是逐项合并 —— 服务端的表才是真相，
        /// 客户端上多出来的陈旧条目（上一场留下的 whoAmI）必须一并消失。
        /// </summary>
        public LegendsSyncedFields IntFloatDict(Dictionary<int, float> dict)
        {
            writers.Add(w =>
            {
                w.Write(dict.Count);
                foreach (KeyValuePair<int, float> kv in dict)
                {
                    w.Write(kv.Key);
                    w.Write(kv.Value);
                }
            });
            readers.Add(r =>
            {
                int count = r.ReadInt32();
                dict.Clear();
                for (int i = 0; i < count; i++)
                {
                    int key = r.ReadInt32();
                    dict[key] = r.ReadSingle();
                }
            });
            return this;
        }

        internal void Write(BinaryWriter writer)
        {
            for (int i = 0; i < writers.Count; i++)
                writers[i](writer);
        }

        internal void Read(BinaryReader reader)
        {
            for (int i = 0; i < readers.Count; i++)
                readers[i](reader);
        }

        // --- 变化检测 -------------------------------------------------------------------------------------
        // 光声明字段还不够：tModLoader 只在 npc.netUpdate 被置位的那一帧才会调 SendExtraAI。以前这要求
        // 每个 Boss 作者在每一处改动后手写 netUpdate —— 和"两端手工保持对称"一样，是漏一处就静默失效
        // 的约定。这里把它也收进框架：每帧对声明过的字段做一次快照，值真的变了才推送。
        //
        // 用序列化后的字节做 FNV-1a 哈希，因此不需要为每种类型单独写比较逻辑，新增字段类型自动生效。
        // MemoryStream 复用，稳态下不分配。
        private readonly MemoryStream probeStream = new();
        private BinaryWriter probeWriter;
        private ulong lastHash;
        private bool hasSnapshot;

        internal bool HasChanged()
        {
            if (writers.Count == 0)
                return false;

            probeWriter ??= new BinaryWriter(probeStream);
            probeStream.SetLength(0);
            Write(probeWriter);

            ulong hash = 14695981039346656037UL;
            byte[] buffer = probeStream.GetBuffer();
            int length = (int)probeStream.Length;
            for (int i = 0; i < length; i++)
            {
                hash ^= buffer[i];
                hash *= 1099511628211UL;
            }

            if (hasSnapshot && hash == lastHash)
                return false;

            lastHash = hash;
            hasSnapshot = true;
            return true;
        }
    }
}
