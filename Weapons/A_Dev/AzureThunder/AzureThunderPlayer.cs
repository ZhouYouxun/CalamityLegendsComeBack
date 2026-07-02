using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 青霆剑的玩家态中心：记录充能、终极能量、右键冷却、终极滤镜和被动计时。
    internal sealed class AzureThunderPlayer : ModPlayer
    {
        // 主要资源与节奏常量，所有弹幕和物品入口统一从这里读取。
        public const int LeftAttackManaCost = 9;
        public const int RightClickManaCost = 100;
        public const int AttackManaCost = LeftAttackManaCost;
        public const int ThunderChargeMax = 3;
        public const int UltimateEnergyMax = 240;
        public const int HarmonyDuration = 25 * 60;
        public const int RightClickCooldownMax = 3 * 60;
        public const int AutoGroundSwordInterval = 10 * 60;
        public const int UltimateAutoGainInterval = 2 * 60;

        // 对外暴露的运行时状态：UI、右键、终极技都会读取这些字段。
        public int ThunderCharge;
        public int UltimateEnergy;
        public int RightClickCooldown;
        public int ActiveHarmonyDuration;
        public bool GreenUltimateFilterActive;
        public float GreenUltimateFilterOpacity;

        private static int harmonyHeadSlot = -1;
        private static int harmonyBodySlot = -1;
        private static int harmonyLegsSlot = -1;

        // holdingAzureThunder 每帧重置，只有物品 HoldItem 会重新置 true。
        private bool holdingAzureThunder;
        private bool dashHeavyStrikeReady;
        private bool wasUltimateReady;
        private int retainedComboIndex;
        private int retainedComboTimer;
        private int autoGroundSwordTimer = AutoGroundSwordInterval;
        private int ultimateAutoGainTimer;

        // 双重确认玩家仍然真实手持青霆剑，防止状态标记滞留。
        public bool HoldingAzureThunder =>
            holdingAzureThunder &&
            Player.HeldItem != null &&
            !Player.HeldItem.IsAir &&
            Player.HeldItem.type == ModContent.ItemType<AzureThunder>();

        public bool HarmonyActive => Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>());

        public override void Load()
        {
            if (Main.dedServ)
                return;

            harmonyHeadSlot = EquipLoader.AddEquipTexture(
                Mod,
                "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/天理真和头部",
                EquipType.Head,
                name: "AzureThunderHarmonyHead");
            harmonyBodySlot = EquipLoader.AddEquipTexture(
                Mod,
                "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/天理真和身体",
                EquipType.Body,
                name: "AzureThunderHarmonyBody");
            harmonyLegsSlot = EquipLoader.AddEquipTexture(
                Mod,
                "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/天理真和腿部",
                EquipType.Legs,
                name: "AzureThunderHarmonyLegs");

        }

        public override void SetStaticDefaults()
        {
            if (Main.dedServ)
                return;

            int headSlot = EquipLoader.GetEquipSlot(Mod, "AzureThunderHarmonyHead", EquipType.Head);
            if (headSlot >= 0)
                ArmorIDs.Head.Sets.DrawHead[headSlot] = false;
        }

        public override void Unload()
        {
            harmonyHeadSlot = -1;
            harmonyBodySlot = -1;
            harmonyLegsSlot = -1;
        }

        public override void ResetEffects()
        {
            // 每帧先清空，由 AzureThunder.HoldItem 在本帧重新写入。
            holdingAzureThunder = false;
            GreenUltimateFilterActive = false;
        }

        public override void FrameEffects()
        {
            // 终极期间只替换玩家外观装备槽，不提供任何装备属性。
            if (!HarmonyActive)
                return;

            if (harmonyHeadSlot >= 0)
                Player.head = harmonyHeadSlot;
            if (harmonyBodySlot >= 0)
                Player.body = harmonyBodySlot;
            if (harmonyLegsSlot >= 0)
                Player.legs = harmonyLegsSlot;

            for (int i = 0; i < Player.hideVisibleAccessory.Length; i++)
                Player.hideVisibleAccessory[i] = true;

            Player.wings = 0;
            Player.cWings = 0;
        }

        public override void UpdateDead()
        {
            // 死亡清空所有青霆资源和计时器。
            ThunderCharge = 0;
            UltimateEnergy = 0;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, false);
            RightClickCooldown = 0;
            ActiveHarmonyDuration = 0;
            autoGroundSwordTimer = AutoGroundSwordInterval;
            ultimateAutoGainTimer = 0;
            dashHeavyStrikeReady = false;
            retainedComboIndex = 0;
            retainedComboTimer = 0;
        }

        public override void PostUpdate()
        {
            // 所有资源先钳制，避免同步或外部写入导致越界。
            ThunderCharge = Utils.Clamp(ThunderCharge, 0, ThunderChargeMax);
            UltimateEnergy = Utils.Clamp(UltimateEnergy, 0, UltimateEnergyMax);
            CancelHarmonyIfWeaponChanged();

            // 右键冷却每帧递减。
            if (RightClickCooldown > 0)
                RightClickCooldown--;
            if (retainedComboTimer > 0)
                retainedComboTimer--;

            if (HarmonyActive)
            {
                // 终极期间本地开启绿色滤镜并确保头顶时间条存在。
                if (Player.whoAmI == Main.myPlayer)
                {
                    GreenUltimateFilterActive = true;
                    EnsureHarmonyBar();
                }

                SpawnHarmonyPlayerParticles();
            }
            else if (!HarmonyActive)
                // Buff 不在时清空持续时间，避免下一次 UI 读取旧值。
                ActiveHarmonyDuration = 0;

            // 绿色终极滤镜用 lerp 淡入淡出，避免瞬间闪屏。
            float targetFilterOpacity = GreenUltimateFilterActive ? 0.25f : 0f;
            GreenUltimateFilterOpacity = MathHelper.Lerp(GreenUltimateFilterOpacity, targetFilterOpacity, GreenUltimateFilterActive ? 0.06f : 0.045f);
            if (!GreenUltimateFilterActive && GreenUltimateFilterOpacity < 0.01f)
                GreenUltimateFilterOpacity = 0f;

            if (HoldingAzureThunder)
            {
                // 手持期间才增长能量、同步 UI、读取终极键和处理自动地剑。
                if (Player.whoAmI == Main.myPlayer)
                    EnsureGroundSwordMatrix();

                HandleUltimateAutoGain();
                SyncCooldownDisplays();
                HandleUltimateInput();
                HandleAutomaticGroundSword();
            }
            else if (ThunderCharge > 0 || UltimateEnergy > 0)
            {
                // 切走武器时保留充能/能量，但重置自动地剑节奏。
                autoGroundSwordTimer = AutoGroundSwordInterval;
                dashHeavyStrikeReady = false;
            }
            else
            {
                autoGroundSwordTimer = AutoGroundSwordInterval;
                dashHeavyStrikeReady = false;
            }

            if (HoldingAzureThunder)
                ApplyPassiveStatGrowth();
        }

        public void SetHoldingAzureThunder()
        {
            holdingAzureThunder = true;
        }

        public void ArmDashHeavyStrike()
        {
            dashHeavyStrikeReady = true;
        }

        public bool ConsumeDashHeavyStrike()
        {
            if (!dashHeavyStrikeReady)
                return false;

            dashHeavyStrikeReady = false;
            return true;
        }

        public void RetainLeftCombo(int nextComboIndex)
        {
            retainedComboIndex = Utils.Clamp(nextComboIndex, 0, 3);
            retainedComboTimer = 90;
        }

        public bool TryConsumeRetainedLeftCombo(out int comboIndex)
        {
            comboIndex = 0;
            if (retainedComboTimer <= 0)
                return false;

            comboIndex = retainedComboIndex;
            retainedComboTimer = 0;
            return true;
        }

        public bool TrySpendMana(int manaCost = LeftAttackManaCost)
        {
            // CheckMana 第三个参数为 true，会真正扣除魔力并播放缺魔反馈。
            return Player.CheckMana(Player.HeldItem, manaCost, true, false);
        }

        public void AddThunderCharge(int amount)
        {
            // 天理真和锁住一息万变获取，避免终极期间边打边回满层数。
            if (HarmonyActive)
                return;

            if (amount <= 0)
                return;

            ThunderCharge = Utils.Clamp(ThunderCharge + amount, 0, ThunderChargeMax);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, Pitch = 0.35f }, Player.Center);
        }

        public int ConsumeThunderCharge()
        {
            // 右键一次性消费所有层数，返回值用于决定召剑数和终结倍率。
            int consumed = ThunderCharge;
            ThunderCharge = 0;
            return consumed;
        }

        public void AddUltimateEnergy(int amount)
        {
            if (HarmonyActive || amount <= 0)
                return;

            UltimateEnergy = Utils.Clamp(UltimateEnergy + amount, 0, UltimateEnergyMax);
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, UltimateEnergy >= UltimateEnergyMax);
        }
        public void TryGainThunderChargeFromTarget(NPC target)
        {
            // 终极期间或无效目标不允许获取雷息层数。
            if (HarmonyActive)
                return;

            if (target == null || !target.active || target.friendly || target.dontTakeDamage)
                return;

            // 层数来自目标身上的电系 debuff 数量，最多一次给 3 层。
            int stacks = CountElectroDebuffs(target);
            if (stacks <= 0)
                return;

            AddThunderCharge(Math.Min(3, stacks));
        }

        public void RestoreManaForOwnedSwords(bool includeLeftClickGrowth = false)
        {
            // 每把青霆剑系弹幕返还 1 点魔力，机械后左键最终段额外按地剑规模返还。
            int amount = CountOwnedAzureThunderSwords(Player);
            if (includeLeftClickGrowth && AzureThunderProgression.DownedAnyMech)
                amount += CountOwnedAzureThunderSwords(Player) / 3 * 5;

            if (amount <= 0)
                return;

            // 手动改 statMana 后调用 ManaEffect 显示实际回复量。
            int oldMana = Player.statMana;
            Player.statMana = Math.Min(Player.statManaMax2, Player.statMana + amount);
            int restored = Player.statMana - oldMana;
            if (restored > 0)
                Player.ManaEffect(restored);
        }

        public void RestoreManaFromConsumedCharge(int consumedCharge)
        {
            // 机械后右键消耗雷息会按层数返还魔力。
            if (!AzureThunderProgression.DownedAnyMech || consumedCharge <= 0)
                return;

            int amount = consumedCharge * 5;
            int oldMana = Player.statMana;
            Player.statMana = Math.Min(Player.statManaMax2, Player.statMana + amount);
            int restored = Player.statMana - oldMana;
            if (restored > 0)
                Player.ManaEffect(restored);
        }

        public void RestoreLifeFromFourSymbols()
        {
            // 猪鲨前 FourSymbolsLifeRestore 为 0，因此这里自然跳过。
            int amount = AzureThunderProgression.FourSymbolsLifeRestore;
            if (amount <= 0)
                return;

            int healAmount = Math.Min(amount, Player.statLifeMax2 - Player.statLife);
            if (healAmount <= 0)
                return;

            Player.statLife += healAmount;
            Player.HealEffect(healAmount, true);
        }

        private void HandleUltimateInput()
        {
            // 终极键只由本地玩家读取，且必须满能量。
            if (Main.myPlayer != Player.whoAmI || !KeybindSystem.LegendarySkill.JustPressed)
                return;

            if (UltimateEnergy < UltimateEnergyMax)
                return;

            int harmonyDuration = AzureThunderAccessoryPlayer.GetHarmonyDuration(Player);
            if (!Player.GetModPlayer<ZhuangFangYiPetPlayer>().TryStartHarmonyTransform(harmonyDuration))
                return;

            // 启动终极演出：清能量，但真正的 Buff 等小庄变身动画播完后再给予。
            UltimateEnergy = 0;
            ThunderCharge = 0;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, false);
            ultimateAutoGainTimer = 0;
            ActiveHarmonyDuration = harmonyDuration;
        }

        public void StartHarmonyFromPet(int duration)
        {
            ActiveHarmonyDuration = Math.Max(1, duration);
            Player.AddBuff(ModContent.BuffType<AzureThunderHarmonyBuff>(), ActiveHarmonyDuration);
            EnsureHarmonyBar();
            // 启动爆发粒子和提示音。
            for (int i = 0; i < 36; i++)
            {
                Vector2 velocity = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2.5f, 8.5f);
                Dust dust = Dust.NewDustPerfect(Player.Center, DustID.FireworksRGB, velocity, 0, AzureThunderColors.Yellow, Main.rand.NextFloat(1.15f, 1.8f));
                dust.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.85f, Pitch = 0.15f }, Player.Center);
        }

        private void HandleAutomaticGroundSword()
        {
            // 自动地剑只在本地拥有者生成，避免多人重复生成。
            if (Main.myPlayer != Player.whoAmI)
                return;

            // 和合四象解锁前不进行自动召剑。
            if (!AzureThunderProgression.FourSymbolsUnlocked)
                return;

            // 饰品可能缩短自动召剑间隔，现有计时不能超过新上限。
            int autoSwordInterval = AzureThunderAccessoryPlayer.GetAutoGroundSwordInterval(Player);
            if (autoGroundSwordTimer > autoSwordInterval)
                autoGroundSwordTimer = autoSwordInterval;

            int groundSwordCount = CountOwnedGroundSwords(Player);
            if (groundSwordCount >= AzureThunderProgression.AutomaticSwordLimit)
            {
                // 达到进度上限时重置计时，等场上少剑后再重新倒计时。
                autoGroundSwordTimer = autoSwordInterval;
                return;
            }

            if (autoGroundSwordTimer > 0)
            {
                // 还没到时间，只递减计时器。
                autoGroundSwordTimer--;
                return;
            }

            // 计时结束后在玩家附近边缘生成一把地剑，并触发四象回血。
            SpawnGroundSword(Player, Player.Center + Main.rand.NextVector2CircularEdge(160f, 80f), Player.GetWeaponDamage(Player.HeldItem), Player.HeldItem.knockBack);
            RestoreLifeFromFourSymbols();
            autoGroundSwordTimer = autoSwordInterval;
        }

        private void ApplyPassiveStatGrowth()
        {
            // 天地造化：每把青霆剑系弹幕提供魔力再生。
            if (AzureThunderProgression.DownedDesertScourge)
                Player.manaRegenBonus += CountOwnedAzureThunderSwords(Player);

            // 猪鲨后补生命再生，犽戎后补魔法伤害。
            if (AzureThunderProgression.DownedFishron)
                Player.lifeRegen += 2;

            if (AzureThunderProgression.DownedYharon)
                Player.GetDamage(DamageClass.Magic) += 0.02f;
        }

        private void HandleUltimateAutoGain()
        {
            // 终极期间禁用被动回能，并重置计时器。
            if (HarmonyActive)
            {
                ultimateAutoGainTimer = 0;
                return;
            }

            if (UltimateEnergy >= UltimateEnergyMax)
            {
                // 满能后不继续累计计时，防止释放后立刻多跳一次能量。
                ultimateAutoGainTimer = 0;
                return;
            }

            ultimateAutoGainTimer++;
            if (ultimateAutoGainTimer < UltimateAutoGainInterval)
                return;

            ultimateAutoGainTimer = 0;
            AddUltimateEnergy(1);
        }

        private void SyncCooldownDisplays()
        {
            // 灾厄 cooldown 字典只负责显示；真实数值仍存放在本 ModPlayer。
            if (Player.Calamity().cooldowns.TryGetValue(AzureThunderChargeCooldown.ID, out var chargeCooldown))
                chargeCooldown.timeLeft = ThunderCharge;
            else
                Player.AddCooldown(AzureThunderChargeCooldown.ID, ThunderCharge);

            if (Player.Calamity().cooldowns.TryGetValue(AzureThunderUltimateCooldown.ID, out var ultimateCooldown))
                ultimateCooldown.timeLeft = UltimateEnergy;
            else
                Player.AddCooldown(AzureThunderUltimateCooldown.ID, UltimateEnergy);
        }

        private void EnsureHarmonyBar()
        {
            // 已经有时间条时不重复生成。
            int barType = ModContent.ProjectileType<AzureThunderHarmonyBar>();
            if (Player.ownedProjectileCounts[barType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Top,
                Vector2.Zero,
                barType,
                0,
                0f,
                Player.whoAmI);
        }

        private void EnsureGroundSwordMatrix()
        {
            int matrixType = ModContent.ProjectileType<AzureThunderGroundSwordMatrix>();
            if (Player.ownedProjectileCounts[matrixType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                Vector2.Zero,
                matrixType,
                0,
                0f,
                Player.whoAmI);
        }

        public static int CountElectroDebuffs(NPC target)
        {
            // 统计所有被青霆剑视为“电系”的 debuff，用于最后雷击结算雷息层数。
            int stacks = 0;
            if (target.HasBuff(BuffID.Electrified))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<AuricRebuke>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<GalvanicCorrosion>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<StaticDischarge>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<VermillionFlux>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<ElementalMix>()))
                stacks++;

            return stacks;
        }

        public static int CountOwnedAzureThunderSwords(Player player)
        {
            // 天地造化把地剑和飞剑都算作“青霆剑”数量。
            int count = 0;
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            int flyingType = ModContent.ProjectileType<AzureThunderFlyingSword>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type == groundType || projectile.type == flyingType)
                    count++;
            }

            return count;
        }

        public static int CountOwnedGroundSwords(Player player)
        {
            // 单独统计地剑，用于右键、自动召剑和终极终结条件。
            int count = 0;
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == groundType)
                    count++;
            }

            return count;
        }

        public static int CountGroundSwordsNear(Player player, Vector2 center, float radius)
        {
            // 右键和饰品效果只关心一定范围内的地剑。
            int count = 0;
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            float radiusSquared = radius * radius;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != groundType)
                    continue;

                if (projectile.DistanceSQ(center) <= radiusSquared)
                    count++;
            }

            return count;
        }

        public static NPC FindMouseNearestTarget(Player player, float maxDistance = 1600f)
        {
            // 灾厄 mouseWorld 优先，缺失时回退到 Main.MouseWorld。
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            if (mouseWorld == Vector2.Zero)
                mouseWorld = Main.MouseWorld;

            return FindNearestTarget(mouseWorld, maxDistance);
        }

        public static NPC FindNearestTarget(Vector2 point, float maxDistance = 1600f)
        {
            // 简单最近敌人搜索，只接受可被追踪的 NPC。
            NPC bestTarget = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(point, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public static Vector2 GetMouseWorld(Player player)
        {
            // 所有青霆输入统一通过此方法取鼠标，保持多人和灾厄监听一致。
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        public static void SpawnGroundSword(Player player, Vector2 position, int damage, float knockback)
        {
            // 达到地剑硬上限时拒绝生成，防止右键和自动召剑超量。
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            if (CountOwnedGroundSwords(player) >= AzureThunderGroundSword.MaxGroundSwords)
                return;

            int sword = Projectile.NewProjectile(
                player.GetSource_FromThis(),
                position,
                Vector2.Zero,
                groundType,
                Math.Max(1, damage),
                knockback,
                player.whoAmI);

            if (Main.projectile.IndexInRange(sword))
                ApplyProjectileGrowth(Main.projectile[sword]);
        }

        public static void MakeRoomForGroundSwords(Player player, int desiredCount)
        {
            int overflow = CountOwnedGroundSwords(player) + desiredCount - AzureThunderGroundSword.MaxGroundSwords;
            if (overflow <= 0)
                return;

            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (overflow <= 0)
                    break;

                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == groundType)
                {
                    projectile.Kill();
                    overflow--;
                }
            }
        }

        public static void ApplyProjectileGrowth(Projectile projectile)
        {
            // 邪恶二阶后所有青霆子弹获得护甲穿透成长。
            if (AzureThunderProgression.DownedEvilTier2)
                projectile.ArmorPenetration += 2;
        }

        public static void ApplyUltimateDot(NPC target, int duration)
        {
            // 终极 DoT 使用青霆剑的完整电系减益包，后续叠层也按同一组 buff 统计。
            if (target == null || !target.active)
                return;

            target.AddBuff(BuffID.Electrified, duration);
            target.AddBuff(ModContent.BuffType<StaticDischarge>(), duration);

            if (AzureThunderProgression.DownedWallOfFlesh)
                target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), duration);
            if (AzureThunderProgression.DownedDragonfolly)
                target.AddBuff(ModContent.BuffType<VermillionFlux>(), duration);
            if (AzureThunderProgression.DownedMoonLord)
                target.AddBuff(ModContent.BuffType<ElementalMix>(), duration);
            if (AzureThunderProgression.DownedYharon)
                target.AddBuff(ModContent.BuffType<AuricRebuke>(), duration);
        }

        public static void SpawnHarmonyHitMark(IEntitySource source, Vector2 position, int owner, int targetIndex = -1, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            int mark = Projectile.NewProjectile(
                source,
                position,
                Vector2.Zero,
                ModContent.ProjectileType<AzureThunderHarmonyImpactMark>(),
                0,
                0f,
                owner,
                targetIndex,
                MathHelper.Clamp(scale, 0.45f, 2.6f));

            if (Main.projectile.IndexInRange(mark))
                Main.projectile[mark].netImportant = false;
        }

        public static void SpawnVerticalLightning(
            IEntitySource source,
            Vector2 impactPosition,
            NPC target,
            int damage,
            float knockback,
            int owner,
            bool gainCharge = false,
            bool applyStaticDischarge = false,
            bool big = false,
            int ultimateEnergyGain = 0,
            bool applyCrumbling = false,
            float spawnHeightMultiplier = 1f,
            bool visualOnly = false,
            float? fixedTiltRadians = null,
            bool applyBaseElectricDebuff = true,
            bool weak = false,
            bool speedLines = false,
            bool normalVisualIntensity = false,
            bool oneThirdVisualIntensity = false,
            float lightningScale = 1f,
            int additionalFlags = 0)
        {
            // 竖直雷通过目标点向上偏移生成，再朝落点移动。
            Vector2 targetPosition = target?.Center ?? impactPosition;
            float spawnDistance = 1000f * Math.Max(0.1f, spawnHeightMultiplier);
            Vector2 fallDirection = fixedTiltRadians.HasValue ?
                Vector2.UnitY.RotatedBy(fixedTiltRadians.Value) :
                Vector2.UnitY.RotatedByRandom(0.2f);
            Vector2 spawnPosition = targetPosition - fallDirection * spawnDistance;
            Vector2 velocity = targetPosition - spawnPosition;
            int flags = 0;

            // 将布尔参数压缩到 FlatLightning 的 bit flag。
            if (gainCharge)
                flags |= AzureThunderFlatLightning.GainChargeFlag;
            if (applyStaticDischarge)
                flags |= AzureThunderFlatLightning.StaticDischargeFlag;
            if (big)
                flags |= AzureThunderFlatLightning.BigLightningFlag;
            if (applyCrumbling)
                flags |= AzureThunderFlatLightning.CrumblingFlag;
            if (visualOnly)
                flags |= AzureThunderFlatLightning.VisualOnlyFlag;
            if (!applyBaseElectricDebuff)
                flags |= AzureThunderFlatLightning.NoBaseElectricDebuffFlag;
            if (weak)
                flags |= AzureThunderFlatLightning.WeakLightningFlag;
            if (speedLines)
                flags |= AzureThunderFlatLightning.SpeedLineFlag;
            if (normalVisualIntensity)
                flags |= AzureThunderFlatLightning.NormalVisualIntensityFlag;
            if (oneThirdVisualIntensity)
                flags |= AzureThunderFlatLightning.OneThirdVisualIntensityFlag;
            flags |= additionalFlags;

            SpawnDirectionalLightning(
                source,
                spawnPosition,
                velocity,
                Math.Max(1, damage),
                knockback,
                owner,
                flags,
                ultimateEnergyGain,
                big,
                lightningScale);
        }

        public static void SpawnDirectionalLightning(
            IEntitySource source,
            Vector2 spawnPosition,
            Vector2 velocity,
            int damage,
            float knockback,
            int owner,
            int flags = 0,
            int ultimateEnergyGain = 0,
            bool big = false,
            float size = 1f)
        {
            // 通用方向雷生成函数，竖直雷和平雷最终都走这里。
            int lightning = Projectile.NewProjectile(
                source,
                spawnPosition,
                velocity.SafeNormalize(Vector2.UnitY) * 24f,
                ModContent.ProjectileType<AzureThunderFlatLightning>(),
                Math.Max(1, damage),
                knockback,
                owner,
                flags,
                Math.Max(0.1f, size),
                ultimateEnergyGain);

            if (Main.projectile.IndexInRange(lightning))
            {
                // 生成后同步玩家当前暴击率，并应用进度成长。
                Projectile projectile = Main.projectile[lightning];
                projectile.CritChance = Main.player[owner].GetWeaponCrit(Main.player[owner].HeldItem);
                ApplyProjectileGrowth(projectile);
                if (big)
                {
                    Vector2 center = projectile.Center;
                    int hitboxSize = (int)(70f * Math.Max(0.1f, size));
                    projectile.width = hitboxSize;
                    projectile.height = hitboxSize;
                    projectile.Center = center;
                }
            }
        }

        public static void SpawnFlatLightning(
            IEntitySource source,
            Vector2 position,
            Vector2 velocity,
            int damage,
            float knockback,
            int owner,
            float size = 1f,
            int flags = 0,
            int ultimateEnergyGain = 0)
        {
            // 平雷直接从 position 按给定方向飞行，常用于命中补刀和终极演出。
            int lightning = Projectile.NewProjectile(
                source,
                position,
                velocity.SafeNormalize(Vector2.UnitX) * 24f,
                ModContent.ProjectileType<AzureThunderFlatLightning>(),
                Math.Max(1, damage),
                knockback,
                owner,
                flags,
                Math.Max(0.1f, size),
                ultimateEnergyGain);

            if (Main.projectile.IndexInRange(lightning))
                ApplyProjectileGrowth(Main.projectile[lightning]);
        }

        public static void SpawnUpwardThunderBoltBurst(Vector2 position, int count, float scale)
        {
            // 终结爆点使用向上的灾厄闪电束，不再向四周散射平雷。
            if (Main.dedServ)
                return;

            count = Math.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float centeredIndex = i - (count - 1) * 0.5f;
                float rotation = MathHelper.ToRadians(centeredIndex * 4f + Main.rand.NextFloat(-3f, 3f));
                Vector2 spawnOffset = new(Main.rand.NextFloat(-22f, 22f), Main.rand.NextFloat(-8f, 8f));
                Color color = Main.rand.NextBool(3) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure;

                GeneralParticleHandler.SpawnParticle(new ThunderBoltVFX(
                    position + spawnOffset,
                    rotation,
                    scale * Main.rand.NextFloat(0.88f, 1.12f),
                    color,
                    Main.rand.Next(18, 25),
                    Main.rand.NextFloat(4f, 7f),
                    0.9f,
                    new Vector2(Main.rand.NextFloat(0.72f, 0.95f), Main.rand.NextFloat(1.05f, 1.35f))));
            }
        }

        private void CancelHarmonyIfWeaponChanged()
        {
            // Buff.Update 已经会删 Buff，这里额外兜底处理玩家态和滤镜。
            bool stillHoldingAzureThunder =
                Player.HeldItem != null &&
                !Player.HeldItem.IsAir &&
                Player.HeldItem.type == ModContent.ItemType<AzureThunder>();

            if (!HarmonyActive || stillHoldingAzureThunder)
                return;

            int buffIndex = Player.FindBuffIndex(ModContent.BuffType<AzureThunderHarmonyBuff>());
            if (buffIndex >= 0)
                Player.DelBuff(buffIndex);

            ActiveHarmonyDuration = 0;
            GreenUltimateFilterActive = false;
        }

        private void SpawnHarmonyPlayerParticles()
        {
            // 终极状态下围绕玩家生成绿色上升粒子；服务端不做视觉。
            if (Main.dedServ || !HarmonyActive || !Main.rand.NextBool(18))
                return;

            Vector2 haloCenter = Player.Center - Vector2.UnitY * (Player.height * 0.88f);
            Color haloColor = Color.Lerp(AzureThunderColors.Azure, new Color(110, 255, 140), 0.55f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                haloCenter,
                Vector2.Zero,
                haloColor,
                Vector2.One * 0.7f,
                0f,
                0.025f,
                0.78f,
                24));
        }
    }

    // 青霆剑共享调色板，避免各弹幕重复写颜色常量。
    internal static class AzureThunderColors
    {
        public static readonly Color Yellow = new(255, 232, 66);
        public static readonly Color PaleYellow = new(255, 255, 194);
        public static readonly Color Azure = new(52, 192, 255);
        public static readonly Color DeepAzure = new(20, 88, 190);
    }
}
