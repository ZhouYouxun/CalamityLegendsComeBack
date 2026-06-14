using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    // 集中存放叶流所有音效，方便在此处统一调整音量、音高和其它属性。
    internal static class BlossomFluxSounds
    {
        #region 左键开火与战术音效

        // 突破形态左键普通射击音效（使用绿叶法杖的射击声，包含3种变种，带音量与音高微调）
        public static readonly SoundStyle LeftBreakthroughFire = new("CalamityMod/Sounds/Item/SylvestaffFire", 3) { Volume = 0.52f, PitchVariance = 0.12f };
        
        // 突破形态激活“过去余音”被动时的左键射击音效（音量稍低的绿叶法杖射击声，包含3种变种）
        public static readonly SoundStyle LeftPastLingeringFire = new("CalamityMod/Sounds/Item/SylvestaffFire", 3) { Volume = 0.48f, PitchVariance = 0.12f };
        
        // 突破形态“过去余音”连射结束或爆发时的强力音效（使用原版 Item92 增加高亢音高）
        public static readonly SoundStyle LeftPastLingeringBurst = SoundID.Item92 with { Volume = 0.45f, Pitch = 0.35f };

        // 复苏形态左键战术连发的基础音效（使用原版 Item8 魔法声，后续会动态调整音高）
        public static readonly SoundStyle LeftRecoveryFire = SoundID.Item8 with { Volume = 0.58f };
        
        // 播放复苏形态左键战术连发音效的方法（音高随着连击段数逐渐升高，表现蓄能感）
        public static void PlayLeftRecoveryFire(Vector2 position, int burstGroupsStarted) =>
            SoundEngine.PlaySound(LeftRecoveryFire with { Pitch = 0.2f + burstGroupsStarted * 0.04f }, position);

        // 侦察形态左键战术散射的基础音效（使用灾厄 LunicShot2 科技风枪声）
        public static readonly SoundStyle LeftReconFire = new("CalamityMod/Sounds/Item/LunicShot2") { Volume = 0.48f };
        
        // 播放侦察形态左键战术散射音效的方法（音高随着当前射击段数微幅递增）
        public static void PlayLeftReconFire(Vector2 position, int reconShotsFired) =>
            SoundEngine.PlaySound(LeftReconFire with { Pitch = 0.15f + reconShotsFired * 0.03f }, position);

        // 轰炸形态左键战术发射第一种交替音效（使用灾厄 ImpalerLaunch 刺枪发射声，重低音）
        public static readonly SoundStyle LeftBombardFire1 = new("CalamityMod/Sounds/Item/ImpalerLaunch") { Volume = 0.58f, PitchVariance = 0.15f };
        
        // 轰炸形态左键战术发射第二种交替音效（使用原版 Item5 弓箭声，调低音高作为辅音）
        public static readonly SoundStyle LeftBombardFire2 = SoundID.Item5 with { Volume = 0.3f, Pitch = -0.22f, PitchVariance = 0.08f };

        // 瘟疫形态左键天顶世界专属的多变蜜蜂射击音效
        public static readonly SoundStyle LeftPlagueFire = new("CalamityMod/Sounds/Custom/BEES/bees") { Volume = 0.55f, PitchVariance = 0.12f };
        
        // 瘟疫形态左键非天顶世界的普通射击音效（使用原版 Item14 爆炸声）
        public static readonly SoundStyle LeftPlagueFireNonZenith = SoundID.Item14 with { Volume = 0.55f, PitchVariance = 0.12f };

        // 播放瘟疫形态左键射击音效的方法（天顶世界下随机播放12种蜜蜂嗡嗡声，普通世界下播放爆炸声）
        public static void PlayLeftPlagueFire(Vector2 position)
        {
            if (Main.zenithWorld)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/BEES/bees" + Main.rand.Next(1, 13)) { Volume = LeftPlagueFire.Volume, PitchVariance = LeftPlagueFire.PitchVariance }, position);
            }
            else
            {
                SoundEngine.PlaySound(LeftPlagueFireNonZenith, position);
            }
        }

        #endregion

        #region 右键蓄力与释放音效

        // 右键开始蓄力时的音效（使用原版 Item149 传送声并调低音高，营造充能开始的效果）
        public static readonly SoundStyle RightBeginCharge = SoundID.Item149 with { Volume = 0.55f, Pitch = -0.2f };
        
        // 轰炸形态右键在冷却或被阻止使用时的提示音效（使用原版 MenuTick 菜单滴答声并调低音高）
        public static readonly SoundStyle RightBombardStrikeBlocked = SoundID.MenuTick with { Volume = 0.42f, Pitch = -0.3f };
        
        // 右键蓄力完成时播放的叠加音效1（使用原版 Item4 升变声）
        public static readonly SoundStyle RightChargeReadyBurst1 = SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f };
        
        // 右键蓄力完成时播放的叠加音效2（使用原版 Item29 尖锐声）
        public static readonly SoundStyle RightChargeReadyBurst2 = SoundID.Item29 with { Volume = 0.4f, Pitch = -0.15f };

        // 突破形态右键蓄力过程中，每根箭矢上弦装填时的音效（使用原版 DD2弩车发射音效 DD2_BallistaTowerShot）
        public static readonly SoundStyle RightBreakthroughArrowLoaded = SoundID.DD2_BallistaTowerShot with { Volume = 0.35f, Pitch = 0.1f };

        // 轰炸形态右键释放蓄力、发射重炮弹幕时的音效（使用灾厄 LauncherHeavyShot 重型发射声）
        public static readonly SoundStyle RightReleaseBombard = new("CalamityMod/Sounds/Item/LauncherHeavyShot") { Volume = 0.82f, Pitch = -0.08f, PitchVariance = 0.08f };
        
        // 其他所有形态右键释放蓄力发射时的通用音效（使用原版 Item5 弓箭声）
        public static readonly SoundStyle RightReleaseOthers = SoundID.Item5 with { Volume = 0.72f, Pitch = -0.05f };

        // 突破形态右键释放后，队列中后续积攒 of 箭矢自动开火时的轻微射击声（使用原版 Item5）
        public static readonly SoundStyle RightBreakthroughQueuedFire = SoundID.Item5 with { Volume = 0.28f };
        
        // 播放突破形态队列自动开火音效的方法（音高随开火索引增加而逐步升高）
        public static void PlayRightBreakthroughQueuedFire(Vector2 position, int breakthroughQueuedShotIndex) =>
            SoundEngine.PlaySound(RightBreakthroughQueuedFire with { Pitch = 0.16f + breakthroughQueuedShotIndex * 0.04f }, position);

        #endregion

        #region 左键弹幕命中与消散音效

        // 突破形态左键叶片弹幕自然消散或撞击反弹时的音效（使用灾厄 SylvestaffProjectileBounce 绿叶法杖反弹声，包含3种变种）
        public static readonly SoundStyle LeftBreakthroughProjKill = new("CalamityMod/Sounds/Item/SylvestaffProjectileBounce", 3) { Volume = 0.32f, PitchVariance = 0.12f };
        
        // 复苏形态左键叶片击中敌人时的治疗回馈提示音（使用原版 Item29 尖锐声调高音高）
        public static readonly SoundStyle LeftRecoveryProjHit = SoundID.Item29 with { Volume = 0.42f, Pitch = 0.12f };
        
        // 复苏形态左键叶片自然消散或打满消失时的音效（使用原版 Item8 魔法消散声）
        public static readonly SoundStyle LeftRecoveryProjKill = SoundID.Item8 with { Volume = 0.28f, Pitch = 0.24f };

        // 侦察形态左键叶片击中敌人时的提示音1（使用原版 Item4 亮色击中声）
        public static readonly SoundStyle LeftReconProjHit1 = SoundID.Item4 with { Volume = 0.28f, Pitch = 0.36f };
        
        // 侦察形态左键叶片击中敌人时的提示音2（使用原版 Item114 科技击中声，与Hit1同时播放合成清脆音效）
        public static readonly SoundStyle LeftReconProjHit2 = SoundID.Item114 with { Volume = 0.45f, Pitch = 0.22f };
        
        // 侦察形态左键叶片自然死亡或飞过终点时的音效（使用原版 Item115 清脆消散声）
        public static readonly SoundStyle LeftReconProjKill = SoundID.Item115 with { Volume = 0.32f, Pitch = 0.12f };

        // 轰炸形态左键叶片落地或直接命中敌人的爆裂声（使用灾厄 PlantyMushMine 植物粘性炸弹引爆声，包含3种变种）
        public static readonly SoundStyle LeftBombardProjHit = new("CalamityMod/Sounds/Custom/PlantyMushMine", 3) { Volume = 0.45f, PitchVariance = 0.18f };
        
        // 轰炸形态左键落地爆炸产生气流爆炸伤害时的音效（使用灾厄 SubsumingVortexExplosion 漩涡爆炸声）
        public static readonly SoundStyle LeftBombardExplosion = new("CalamityMod/Sounds/Custom/SubsumingVortexExplosion") { Volume = 0.35f, Pitch = 0.15f };

        // 瘟疫形态左键弹幕命中敌人造成瘟疫感染的音效（使用灾厄 PBG 瘟疫使者歌莉娅攻击切换短声）
        public static readonly SoundStyle LeftPlagueProjHit = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort") { Volume = 0.35f, Pitch = 0.05f };
        
        // 瘟疫形态左键弹幕击杀或剧烈爆炸的基础音效（使用原版 Item14 爆炸声）
        public static readonly SoundStyle LeftPlagueProjKillBase = SoundID.Item14;
        
        // 瘟疫形态左键弹幕在天顶世界击杀或剧烈消散时叠加的蜜蜂嗡嗡声
        public static readonly SoundStyle LeftPlagueProjKillBees = new("CalamityMod/Sounds/Custom/BEES/bees") { Volume = 0.45f, PitchVariance = 0.15f };
        
        // 播放瘟疫形态左键弹幕消散/击杀音效的方法（非天顶只放爆炸声，天顶世界会额外叠加随机蜜蜂声）
        public static void PlayLeftPlagueProjKill(Vector2 position)
        {
            SoundEngine.PlaySound(LeftPlagueProjKillBase, position);
            if (Main.zenithWorld)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/BEES/bees" + Main.rand.Next(1, 13)) { Volume = LeftPlagueProjKillBees.Volume, PitchVariance = LeftPlagueProjKillBees.PitchVariance }, position);
            }
        }

        #endregion

        #region 右键弹幕命中与消散音效

        // 突破形态右键蓄力箭撞击物块时的音效（使用原版 Dig 挖矿撞击声）
        public static readonly SoundStyle RightBreakthroughTileCollide = SoundID.Dig with { Volume = 0.35f, Pitch = 0.2f };
        
        // 突破形态右键蓄力箭击中敌人的硬物撞击音效（使用原版 Item17）
        public static readonly SoundStyle RightBreakthroughProjHit = SoundID.Item17 with { Volume = 0.22f, Pitch = 0.25f };

        // 复苏形态右键落樱花瓣或叶片击中敌人时的治疗音效（使用原版 Item8 魔法声）
        public static readonly SoundStyle RightRecoveryProjHit = SoundID.Item8 with { Volume = 0.42f, Pitch = 0.3f };
        
        // 复苏形态右键箭矢撞击墙壁碎裂成治疗花瓣的音效（使用原版 Item27 玻璃碎裂声）
        public static readonly SoundStyle RightRecoveryTileCollide = SoundID.Item27 with { Volume = 0.26f, Pitch = 0.18f };
        
        // 复苏形态右键落樱花瓣能量耗尽消散时的清纯声音（使用原版 Item29）
        public static readonly SoundStyle RightRecoveryProjKill = SoundID.Item29 with { Volume = 0.36f, Pitch = 0.22f };
        
        // 复苏形态右键治疗粒子飞回玩家并执行生命值回复瞬间的音效（使用原版 Item29 亮色治愈回馈）
        public static readonly SoundStyle RightRecoveryTransfer = SoundID.Item29 with { Volume = 0.56f, Pitch = 0.38f };

        // 侦察形态右键雷达箭矢在中途改变方向、执行偏转锁定时播放的音效（使用原版 Item9）
        public static readonly SoundStyle RightReconProjAction = SoundID.Item9 with { Volume = 0.12f, Pitch = 0.72f };
        
        // 侦察形态右键雷达箭击中敌人触发标记刷新时的回波声音（使用原版 Item122 机械声）
        public static readonly SoundStyle RightReconProjHit = SoundID.Item122 with { Volume = 0.28f, Pitch = 0.24f };
        
        // 侦察形态右键雷达箭撞击墙壁弹刀消失的音效（使用原版 Dig 挖土声）
        public static readonly SoundStyle RightReconTileCollide = SoundID.Dig with { Volume = 0.25f, Pitch = 0.35f };
        
        // 侦察形态右键标记敌人或敌人物理雷达折射侦测成功的音效（使用原版 Item25 亮声）
        public static readonly SoundStyle RightReconProjEvent = SoundID.Item25 with { Volume = 0.38f, Pitch = 0.34f };

        // 轰炸形态右键落地产生强力破土撞击的音效1（使用原版 Item62 大爆炸声）
        public static readonly SoundStyle RightBombardImpact1 = SoundID.Item62 with { Volume = 0.65f, Pitch = -0.2f };
        
        // 轰炸形态右键落地产生强力破土撞击的音效2（使用原版 Item14 普通爆炸声，与Impact1叠加合成重低音泥石流效果）
        public static readonly SoundStyle RightBombardImpact2 = SoundID.Item14 with { Volume = 0.5f, Pitch = -0.18f };
        
        // 轰炸形态右键射向空中、召唤箭雨从天而降时的气流掠过音效（使用原版 Item5）
        public static readonly SoundStyle RightBombardSkyRain = SoundID.Item5 with { Volume = 0.2f, Pitch = 0.42f };
        
        // 轰炸形态右键落地诱发的连环泥石流/孢子孢子炸弹起爆的爆破音效（使用原版 Item14）
        public static readonly SoundStyle RightBombardExplosion = SoundID.Item14 with { Volume = 0.55f, Pitch = 0.15f };

        // 瘟疫形态右键瘟疫箭矢击中敌人造成病毒爆发的音效1（使用灾厄 PBG 瘟疫使者歌莉娅攻击切换声）
        public static readonly SoundStyle RightPlagueProjHit1 = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort") { Volume = 0.42f, Pitch = 0.05f };
        
        // 瘟疫形态右键瘟疫箭矢击中敌人造成病毒爆发的音效2（使用原版 NPCDeath13 绿泥软泥怪死亡声，体现粘稠毒液感）
        public static readonly SoundStyle RightPlagueProjHit2 = SoundID.NPCDeath13 with { Volume = 0.38f, Pitch = 0.1f };
        
        // 瘟疫形态右键瘟疫箭矢击中敌人造成病毒爆发的音效3（使用原版 Item14 低沉爆炸声，合成最终的瘟疫毒素起爆效果）
        public static readonly SoundStyle RightPlagueProjHit3 = SoundID.Item14 with { Volume = 0.36f, Pitch = -0.24f };
        
        // 瘟疫形态右键毒爆蜂巢分裂并寻找周围宿主时的嗡嗡启动声（使用原版 Item10）
        public static readonly SoundStyle RightPlagueProjAction = SoundID.Item10 with { Volume = 0.22f, Pitch = 0.1f };
        
        // 瘟疫形态右键最终剧烈毒气爆弹起爆的基础音效（使用灾厄 PBG 瘟疫爆炸声）
        public static readonly SoundStyle RightPlagueProjExplode = new("CalamityMod/Sounds/Custom/PlagueSounds/PlagueBoom") { Volume = 0.46f, PitchVariance = 0.14f };
        
        // 播放瘟疫形态右键毒气爆弹音效的方法（在4种 PBG 瘟疫起爆音效变种中随机选择一种播放）
        public static void PlayRightPlagueProjExplode(Vector2 position) =>
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/PlagueSounds/PlagueBoom" + Main.rand.Next(1, 5)) { Volume = RightPlagueProjExplode.Volume, PitchVariance = RightPlagueProjExplode.PitchVariance }, position);
        
        // 瘟疫形态右键毒爆释放出的瘟疫毒气向周围扩散时的沙沙声（使用原版 Item30）
        public static readonly SoundStyle RightPlagueGas = SoundID.Item30 with { Volume = 0.22f, Pitch = 0.42f };

        #endregion

        #region 大招与通用音效

        // 释放终极 EX 大招「万物复苏 / 春晖箭雨 / Vernal Shot」神级武器弹幕召唤时的震耳魔音（使用原版 Item164）
        public static readonly SoundStyle EXWeaponProjSpawn = SoundID.Item164 with { Volume = 0.8f, Pitch = -0.35f };
        
        // 终极 EX 武器（万木之源）普通攒能射击时的破空音效（使用原版 Item5）
        public static readonly SoundStyle EXWeaponNormalFire = SoundID.Item5 with { Volume = 0.58f };
        
        // 播放终极 EX 武器普通射击音效的方法（带有微小幅度的随机音高偏移）
        public static void PlayEXWeaponNormalFire(Vector2 position) =>
            SoundEngine.PlaySound(EXWeaponNormalFire with { Pitch = 0.2f + Main.rand.NextFloat(-0.08f, 0.08f) }, position);
        
        // 终极 EX 武器蓄力到第一阶段（中等能量）发射时的重音效1（使用原版 Item92 升音）
        public static readonly SoundStyle EXWeaponChargedFire1 = SoundID.Item92 with { Volume = 0.78f, Pitch = -0.08f };
        
        // 终极 EX 武器蓄力到第一阶段（中等能量）发射时的重音效2（使用原版 Item29，叠加让发射声音更清脆）
        public static readonly SoundStyle EXWeaponChargedFire2 = SoundID.Item29 with { Volume = 0.42f, Pitch = -0.2f };
        
        // 终极 EX 武器蓄力到第二阶段（满能量）发射时的超重压破空声1（使用原版 Item163 巨炮声）
        public static readonly SoundStyle EXWeaponFullChargedFire1 = SoundID.Item163 with { Volume = 1.05f, Pitch = -0.22f };
        
        // 终极 EX 武器蓄力到第二阶段（满能量）发射时的重力震荡声2（使用原版 Item122 机械共鸣声，微调音高变差）
        public static readonly SoundStyle EXWeaponFullChargedFire2 = SoundID.Item122 with { Volume = 0.88f, Pitch = -0.34f, PitchVariance = 0.08f };
        
        // 终极 EX 武器蓄力到第二阶段（满能量）发射时的终焉撕裂声3（使用原版 Item74 恶魔叹息声，三重叠加创造震撼效果）
        public static readonly SoundStyle EXWeaponFullChargedFire3 = SoundID.Item74 with { Volume = 0.6f, Pitch = -0.45f };

        // 终极 EX 大招「万物复苏」第一波春晖落英箭雨在空中成型并向地面降落时的神圣音效（使用原版 Item27 冰晶声）
        public static readonly SoundStyle EXVernalShotFire1 = SoundID.Item27 with { Volume = 0.26f, Pitch = 0.3f };
        
        // 终极 EX 大招「万物复苏」第二波更密集的落英箭雨降临时的神圣辅音效（使用原版 Item27，低音高以拉开层次感）
        public static readonly SoundStyle EXVernalShotFire2 = SoundID.Item27 with { Volume = 0.34f, Pitch = -0.08f };

        // 第五层被动「突破 / Breakthrough」触发瞬间玩家身上闪烁金色斗志光辉的音效（使用原版 Item1）
        public static readonly SoundStyle Pa5BreakthroughSound = SoundID.Item1 with { Volume = 0.5f, Pitch = 0.32f };
        
        // 被动技能激活、产生全屏玩家属性增益（Buff）时的圣洁提示音（使用原版 Item4）
        public static readonly SoundStyle PassivePlayerBuffSpawn = SoundID.Item4 with { Volume = 0.58f, Pitch = 0.18f };
        
        // 被动技能触发特殊强力机制（突击/轰炸重置等）时的重音效（使用原版 Item29）
        public static readonly SoundStyle PassivePlayerSpecialAction1 = SoundID.Item29 with { Volume = 0.75f, Pitch = -0.2f };
        
        // 被动技能触发第二种增幅回馈时的闪光音效（使用原版 Item4）
        public static readonly SoundStyle PassivePlayerSpecialAction2 = SoundID.Item4 with { Volume = 0.65f, Pitch = -0.08f };
        
        // 被动技能触发第三种充能/转换成功时的泄能音效（使用原版 Item30）
        public static readonly SoundStyle PassivePlayerSpecialAction3 = SoundID.Item30 with { Volume = 0.58f, Pitch = 0.22f };

        // 复苏被动大招「复苏生态之境 / Recovery Field」领域展开瞬间的魔法音效（使用原版 Item74）
        public static readonly SoundStyle PassiveRecoveryFieldSpawn = SoundID.Item74 with { Volume = 0.3f, Pitch = -0.25f };
        
        // 复苏生态领域在场上持续存在并定时向四周释放治愈脉冲波时的亮声音效1（使用原版 Item29）
        public static readonly SoundStyle PassiveRecoveryFieldPulse1 = SoundID.Item29 with { Volume = 0.62f, Pitch = -0.12f };
        
        // 复苏生态领域定时释放治愈脉冲波时的机械共鸣辅音效2（使用原版 Item122，令脉冲音效更有质感）
        public static readonly SoundStyle PassiveRecoveryFieldPulse2 = SoundID.Item122 with { Volume = 0.54f, Pitch = -0.24f };

        // 右键瞄准镜（Aim Scope）充能锁定完成、发出清脆锁定声的音效（使用原版 Item82 激光声，乘算调整音量）
        public static readonly SoundStyle AimScopeReady = SoundID.Item82 with { Volume = SoundID.Item82.Volume * 0.7f };
        
        // 绿叶弹匣装填/换弹结束，提示可以重新开火时的清脆装弹卡扣声（使用原版 Item37 砧板声）
        public static readonly SoundStyle ReloadComplete = SoundID.Item37 with { Volume = 0.45f, Pitch = 0.1f };
        
        // 长按右键或快捷键打开叶流专属五维战术选择面板时的UI打开音效（使用原版 MenuOpen）
        public static readonly SoundStyle SelectionPanelOpen = SoundID.MenuOpen with { Volume = 0.55f, Pitch = 0.1f };

        #endregion
    }
}
