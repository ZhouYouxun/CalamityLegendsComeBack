using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    internal static class SHPCLeftClickSounds
    {
        private static readonly SoundStyle ArtAttackCast = new("CalamityMod/Sounds/Item/ArtAttackCast");
        private static readonly SoundStyle ArcNovaSmallShot = new("CalamityMod/Sounds/Item/ArcNovaDiffuserSmallShot");
        private static readonly SoundStyle AwmFire = new("CalamityLegendsComeBack/Sound/SHPC/AWM开火");
        private static readonly SoundStyle HadalUrnOpen = new("CalamityMod/Sounds/Item/HadalUrnOpen");
        private static readonly SoundStyle IceBarrageCast = new("CalamityMod/Sounds/Item/IceBarrageCast");
        private static readonly SoundStyle MechGaussRifle = new("CalamityMod/Sounds/Item/MechGaussRifle");
        private static readonly SoundStyle MeldExplosion = new("CalamityMod/Sounds/Item/MeldExplosion");
        private static readonly SoundStyle OmicronBeam = new("CalamityMod/Sounds/Item/OmicronBeam");
        private static readonly SoundStyle OracleHum = new("CalamityMod/Sounds/Item/OracleHum");
        private static readonly SoundStyle TeslaCannonFire = new("CalamityMod/Sounds/Item/TeslaCannonFire");

        public static void PlayForEffect(int effectID, Vector2 position)
        {
            if (Main.dedServ)
                return;

            if (UsesDefaultOnlyLeftClickSound(effectID))
                return;

            switch (effectID)
            {
                // 1: 钨钢能源核心 (Wulfrum Energy Core)
                case 1:
                    Play(SoundID.Item15 with { Volume = 0.24f, Pitch = 0.18f, MaxInstances = 5 }, position);
                    Play(SoundID.Item93 with { Volume = 0.18f, Pitch = 0.32f, MaxInstances = 5 }, position);
                    break;
                // 2: 风暴之颚 (Storm Lion Mandible)
                case 2:
                    Play(SoundID.Item122 with { Volume = 0.34f, Pitch = 0.22f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 3: 硫磺鳞片 (Sulphuric Scale)
                case 3:
                    Play(SoundID.Item13 with { Volume = 0.32f, Pitch = -0.16f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 4: 纯净凝胶 (Purified Gel)
                case 4:
                    Play(SoundID.Item75 with { Volume = 0.22f, Pitch = 0.34f, MaxInstances = 5 }, position);
                    Play(SoundID.Item93 with { Volume = 0.24f, Pitch = 0.48f, MaxInstances = 5 }, position);
                    break;
                // 5: 混沌精华 (Essence of Havoc)
                case 5:
                    Play(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 6: 冰川精华 (Essence of Snow)
                case 6:
                    Play(SoundID.Item30 with { Volume = 0.34f, Pitch = 0.16f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 7: 日光精华 (Essence of Sunlight)
                case 7:
                    Play(SoundID.Item72 with { Volume = 0.32f, Pitch = 0.18f, MaxInstances = 5 }, position);
                    Play(SoundID.Item34 with { Volume = 0.18f, Pitch = 0.32f, MaxInstances = 5 }, position);
                    break;
                // 8: 泰坦之星 (Starblight Soot)
                case 8:
                    Play(SoundID.Item14 with { Volume = 0.34f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                // 9: 光明之魂 (Soul of Light)
                case 9:
                    Play(SoundID.Item9 with { Volume = 0.28f, Pitch = 0.36f, MaxInstances = 5 }, position);
                    break;
                // 10: 暗影之魂 (Soul of Night)
                case 10:
                    Play(SoundID.Item8 with { Volume = 0.28f, Pitch = -0.22f, MaxInstances = 5 }, position);
                    break;
                // 11: 飞翔之魂 (Soul of Flight)
                case 11:
                    Play(SoundID.Item24 with { Volume = 0.24f, Pitch = 0.28f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 12: 恐惧之魂 (Soul of Fright)
                case 12:
                    Play(SoundID.Item103 with { Volume = 0.28f, Pitch = -0.32f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 13: 力量之魂 (Soul of Might)
                case 13:
                    Play(SoundID.DD2_DefenseTowerSpawn with { Volume = 0.55f, Pitch = 0f, MaxInstances = 4 }, position);
                    break;
                // 14: 视觉之魂 (Soul of Sight)
                case 14:
                    Play(SoundID.Item33 with { Volume = 0.3f, Pitch = 0.1f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 15: 生命碎片 (Living Shard)
                case 15:
                    Play(SoundID.Item60 with { Volume = 0.26f, Pitch = 0.22f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 17: 深渊细胞 (Depth Cells)
                case 17:
                    Play(HadalUrnOpen with { Volume = 0.24f, Pitch = 0.08f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 18: 瘟疫细胞罐 (Plague Cell Canister)
                case 18:
                    Play(SoundID.Item43 with { Volume = 0.28f, Pitch = -0.08f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item14 with { Volume = 0.16f, Pitch = 0.2f, MaxInstances = 4 }, position);
                    break;
                // 19: 灾厄尘 (Ashes of Calamity)
                case 19:
                    Play(ArtAttackCast with { Volume = 0.28f, Pitch = -0.08f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 21: 日耀碎片 (Solar Fragment)
                case 21:
                    Play(SoundID.Item68 with { Volume = 0.32f, Pitch = 0.02f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 22: 漩涡碎片 (Vortex Fragment)
                case 22:
                    Play(SoundID.Item61 with { Volume = 0.3f, Pitch = -0.04f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 23: 星云碎片 (Nebula Fragment)
                case 23:
                    Play(SoundID.Item88 with { Volume = 0.26f, Pitch = 0.2f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 24: 星尘碎片 (Stardust Fragment)
                case 24:
                    Play(SoundID.Item44 with { Volume = 0.26f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 25: 冥思溶剂 (Meld Construct)
                case 25:
                    Play(MeldExplosion with { Volume = 0.24f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                // 26: 浊火精华 (Unholy Essence)
                case 26:
                    Play(SoundID.DD2_FlameburstTowerShot with { Volume = 0.3f, Pitch = 0.12f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 28: 神圣晶石 (Divine Geode)
                case 28:
                    Play(SoundID.Item72 with { Volume = 0.28f, Pitch = 0.32f, MaxInstances = 5 }, position);
                    Play(SoundID.Item68 with { Volume = 0.18f, Pitch = 0.28f, MaxInstances = 5 }, position);
                    break;
                // 29: 血石核心 (Bloodstone Core)
                case 29:
                    Play(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.28f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 30: 毁灭之灵 (Ruinous Soul)
                case 30:
                    Play(SoundID.Item105 with { Volume = 0.24f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 31: 灵质 (Necroplasm)
                case 31:
                    Play(SoundID.Item8 with { Volume = 0.3f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item109 with { Volume = 0.18f, Pitch = 0.22f, MaxInstances = 5 }, position);
                    break;
                // 32: 暗离子体 (Dark Plasma)
                case 32:
                    Play(SoundID.Item84 with { Volume = 0.28f, Pitch = -0.16f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 33: 扭曲虚空 (Twisting Nether)
                case 33:
                    Play(SoundID.Item45 with { Volume = 0.26f, Pitch = -0.12f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item68 with { Volume = 0.18f, Pitch = -0.06f, MaxInstances = 5 }, position);
                    break;
                // 34: 恒温能量 (Endothermic Energy)
                case 34:
                    Play(IceBarrageCast with { Volume = 0.32f, Pitch = 0.1f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                // 35: 梦魇魔能 (Nightmare Fuel)
                case 35:
                    Play(MechGaussRifle with { Volume = 0.34f, Pitch = -0.06f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                // 36: 化神魂晶 (Ascendant Spirit Substance)
                case 36:
                    Play(TeslaCannonFire with { Volume = 0.28f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item92 with { Volume = 0.2f, Pitch = 0.22f, MaxInstances = 5 }, position);
                    break;
                // 37: 龙魂碎片 (Yharon Soul Fragment)
                case 37:
                    Play(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.24f, PitchVariance = 0.04f, MaxInstances = 4 }, position);
                    Play(AwmFire with { Volume = 1.15f, Pitch = 0.42f, PitchVariance = 0.03f, MaxInstances = 4 }, position);
                    Play(OmicronBeam with { Volume = 0.5f, Pitch = 0.36f, PitchVariance = 0.03f, MaxInstances = 4 }, position);
                    Play(SoundID.Item68 with { Volume = 1.15f, Pitch = -0.42f, PitchVariance = 0.05f, MaxInstances = 4 }, position);
                    break;
                // 38: 星流棱晶 (Exo Prism)
                case 38:
                    Play(OracleHum with { Pitch = 0.25f }, position);
                    Play(OmicronBeam with { Volume = 0.75f, Pitch = -0.35f }, position);
                    break;
                // 39: 湮灭余烬 (Ashes of Annihilation)
                case 39:
                    Play(SoundID.Item103 with { Volume = 0.32f, Pitch = -0.3f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                // 40: 装甲外壳 (Armored Shell)
                case 40:
                    Play(SoundID.Item38 with { Volume = 0.3f, Pitch = -0.12f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item122 with { Volume = 0.18f, Pitch = 0.16f, MaxInstances = 5 }, position);
                    break;
                // 41: 珍珠碎片 (Pearl Shard)
                case 41:
                    Play(new SoundStyle("CalamityMod/Sounds/Item/OpalFire") { Volume = 0.34f, Pitch = 0.08f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    Play(new SoundStyle("CalamityMod/Sounds/Item/GunShotSmall") { Volume = 0.18f, Pitch = 0.28f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                // 43: Cynosure唯一材料 (Cynosure)
                case 43:
                    Play(new SoundStyle("CalamityLegendsComeBack/Sound/Other/Helldiver2/反坦克炮-开火与换弹"), position);
                    Play(SoundID.Item92, position);
                    Play(MechGaussRifle, position);
                    Play(TeslaCannonFire, position);
                    Play(SoundID.Item68, position);
                    Play(SoundID.Item122, position);
                    Play(OmicronBeam, position);
                    break;
                // 44: 灾劫核心 (Core of Calamity)
                case 44:
                    Play(ArcNovaSmallShot with { Volume = 0.42f, Pitch = -0.2f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item84 with { Volume = 0.2f, Pitch = -0.34f, MaxInstances = 4 }, position);
                    break;
                default:
                    break;
            }
        }

        private static bool UsesDefaultOnlyLeftClickSound(int effectID)
        {
            return (effectID >= 9 && effectID <= 12) || effectID == 14;
        }

        private static void Play(SoundStyle sound, Vector2 position)
        {
            SoundEngine.PlaySound(sound, position);
        }
    }
}
