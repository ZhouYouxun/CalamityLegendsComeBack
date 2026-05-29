using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 青霆剑音效集中入口，方便弹幕之间共享音量、音高和实例上限。
    internal static class AzureThunderSounds
    {
        // 通用电击命中音，平雷、光球和剑命中都会复用。
        private static readonly SoundStyle ElectricHit = new("CalamityMod/Sounds/Item/ElectricHit")
        {
            Volume = 0.62f,
            PitchVariance = 0.12f,
            MaxInstances = 6
        };

        // 重型发射音专门给终极巨剑下坠和爆炸使用。
        private static readonly SoundStyle LauncherHeavyShot = new("CalamityMod/Sounds/Item/LauncherHeavyShot")
        {
            Volume = 0.62f,
            PitchVariance = 0.12f,
            MaxInstances = 3
        };

        public static void PlayLightningSpawn(Vector2 position, bool big = false)
        {
            // big 雷更低沉、更响，普通雷带轻微随机音高。
            SoundEngine.PlaySound(CommonCalamitySounds.LightningSound with
            {
                Volume = big ? 0.72f : 0.44f,
                Pitch = big ? -0.18f : Main.rand.NextFloat(0.08f, 0.22f),
                PitchVariance = 0.06f,
                MaxInstances = 6
            }, position);
        }

        public static void PlayLightningImpact(Vector2 position, bool big = false)
        {
            // 雷击命中叠加电击声和爆裂声，让伤害反馈更明确。
            SoundEngine.PlaySound(ElectricHit with
            {
                Volume = big ? 0.78f : 0.58f,
                Pitch = big ? -0.12f : 0.08f,
                MaxInstances = 8
            }, position);

            SoundEngine.PlaySound(SoundID.Item94 with
            {
                Volume = big ? 0.52f : 0.34f,
                Pitch = big ? -0.1f : 0.16f,
                MaxInstances = 8
            }, position);
        }

        public static void PlayOrbRelease(Vector2 position)
        {
            // 光球释放使用较轻的双层起手音。
            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.38f, Pitch = 0.26f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.24f, Pitch = 0.42f, MaxInstances = 4 }, position);
        }

        public static void PlayOrbImpact(Vector2 position)
        {
            // 光球命中只保留电击短音，避免多颗光球同时命中过吵。
            SoundEngine.PlaySound(ElectricHit with { Volume = 0.46f, Pitch = 0.22f, MaxInstances = 8 }, position);
        }

        public static void PlaySwordMaterialize(Vector2 position)
        {
            // 飞剑/地剑生成时的凝聚音效。
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.42f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 6 }, position);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.28f, Pitch = 0.34f, MaxInstances = 6 }, position);
        }

        public static void PlaySwordLaunch(Vector2 position)
        {
            // 剑体发射或地剑俯冲时播放，音色比生成更锐利。
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.46f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 6 }, position);
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.3f, Pitch = 0.05f, MaxInstances = 6 }, position);
        }

        public static void PlaySwordHit(Vector2 position)
        {
            // 剑体命中同时强调金属冲击和电击。
            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 0.34f, Pitch = 0.16f, PitchVariance = 0.1f, MaxInstances = 8 }, position);
            SoundEngine.PlaySound(ElectricHit with { Volume = 0.42f, Pitch = 0.1f, MaxInstances = 8 }, position);
        }

        public static void PlaySwordBurst(Vector2 position, bool heavy = false)
        {
            // 地剑消散或俯冲结束爆开；heavy=true 时用于更重的终极俯冲。
            SoundEngine.PlaySound(SoundID.Item94 with { Volume = heavy ? 0.62f : 0.46f, Pitch = heavy ? -0.08f : 0.16f, MaxInstances = 6 }, position);
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = heavy ? 0.48f : 0.3f, Pitch = heavy ? -0.12f : 0.12f, MaxInstances = 6 }, position);
        }

        public static void PlayCommandPulse(Vector2 position)
        {
            // 右键序列器发令时的脉冲音。
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = 0.22f, MaxInstances = 3 }, position);
        }

        public static void PlayHeavyDrop(Vector2 position)
        {
            // 终极巨剑开始下坠，使用重型射击和大型武器声叠加。
            SoundEngine.PlaySound(LauncherHeavyShot with { Volume = 0.7f, Pitch = -0.18f, MaxInstances = 3 }, position);
            SoundEngine.PlaySound(CommonCalamitySounds.LargeWeaponFireSound with { Volume = 0.48f, Pitch = -0.12f, MaxInstances = 3 }, position);
        }

        public static void PlayHeavyImpact(Vector2 position)
        {
            // 终极巨剑落地爆发，最后再调用大雷出生音强化冲击。
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.78f, Pitch = -0.08f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
            SoundEngine.PlaySound(LauncherHeavyShot with { Volume = 0.82f, Pitch = -0.28f, MaxInstances = 3 }, position);
            PlayLightningSpawn(position, true);
        }
    }
}
