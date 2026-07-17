using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.CommandAscend;
using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.MilitaryCaller;
using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule;
using CalamityLegendsComeBack.Weapons.SHPC.EXSkill;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure;
using CalamityLegendsComeBack.LegendaryTooltipEffects;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;


namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class NewLegendSHPC : ModItem, ILocalizedModType
    {
        private const string MagazineIconTooltipLineName = "SHPCMagazineIconStrip";

        #region ===== 基础信息与运行时状态 =====

        #region ===== 资源与本地化 =====
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";
        public new string LocalizationCategory => "Items.Weapons";
        #endregion

        #region ===== 音效资源 =====

        // ==================== 音效部分 ====================
        public static readonly SoundStyle FireSound = new("CalamityMod/Sounds/Item/AnomalysNanogunMPFBShot");
        public static readonly SoundStyle VacuumStart = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumStart") { Volume = 0.5f };
        public static readonly SoundStyle VacuumLoop = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumLoop") { Volume = 0.5f };
        public static readonly SoundStyle VacuumEnd = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumEnd") { Volume = 0.5f };

        public static readonly SoundStyle RocketLaunch = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/解放者机甲左手火箭弹") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle LightningChainRelease = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/雷霆开火与换弹") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle EnergyMinigunFire = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/双刃镰开火音效") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle EnergyMinigunSpinUp = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/双刃镰启动音效") { Volume = 1f, Pitch = 0f };

        public static readonly SoundStyle MortarSentryShot = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/迫击哨戒炮单次攻击") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle FinalUltimatumExplosion = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/最后通牒爆炸") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle Eagle500kgExplosion = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/飞鹰500KG爆炸") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle AntiPersonnelMineExplosion = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/反步兵地雷爆炸") { Volume = 1f, Pitch = 0f };
        #endregion

        #region ===== 灌注与动画状态 =====

        public const int BaseMagazineCount = 3;
        public const int MagazineCount = BaseMagazineCount + 4;
        private const int BaseManaCost = 15;
        public const int MaxReservePerSlot = 9999;

        private readonly int[] magazineEffectPowers = new int[MagazineCount];
        private readonly int[] magazineAmmoTypes = new int[MagazineCount];
        private readonly int[] magazineEffectIDs = new int[MagazineCount];
        // 外置弹药库：每格最多存 MaxReservePerSlot 个相同材料，开火时自动补充内弹夹
        private readonly int[] magazineReserve = new int[MagazineCount];
        private int selectedMagazineIndex;
        private bool suppressWorldRightClickUntilRelease;
        private bool wasWorldRightClickInteractionActive;

        public int storedEffectPower
        {
            get => magazineEffectPowers[CurrentMagazineIndex];
            set
            {
                int index = CurrentMagazineIndex;
                magazineEffectPowers[index] = Math.Max(0, value);
                HandleDepletedMagazine(index);
            }
        }

        public int storedAmmoType
        {
            get => magazineAmmoTypes[CurrentMagazineIndex];
            set => magazineAmmoTypes[CurrentMagazineIndex] = value;
        }

        public int storedEffectID
        {
            get => magazineEffectIDs[CurrentMagazineIndex];
            set => magazineEffectIDs[CurrentMagazineIndex] = value;
        }

        public int CurrentMagazineIndex
        {
            get
            {
                selectedMagazineIndex = Utils.Clamp(selectedMagazineIndex, 0, MagazineCount - 1);
                return selectedMagazineIndex;
            }
        }

        public int GetActiveMagazineCount(Player player) => GetActiveMagazineCountForPlayer(player);

        public static int GetActiveMagazineCountForPlayer(Player player)
        {
            if (player == null)
                return BaseMagazineCount;

            int bonusMagazineCount = player.GetModPlayer<SHPCEnergyCorePlayer>().BonusMagazineCount;
            return Utils.Clamp(BaseMagazineCount + bonusMagazineCount, BaseMagazineCount, MagazineCount);
        }

        public static int GetAdjustedAmmoCapacity(Player player, int effectID)
        {
            // Cynosure 是唯一 Lore 材料。它始终只装填 999 发，不接受能源核心的容量倍率。
            if (effectID == CynosureEffect.CynosureEffectID)
                return 999;

            int baseCapacity = GetBaseAmmoCapacity(effectID);
            float multiplier = GetEnergyCoreCapacityMultiplier(player);
            if (multiplier <= 1f)
                return baseCapacity;

            int scaledCapacity = (int)Math.Ceiling(baseCapacity * multiplier);
            return RoundCapacityUpToFive(scaledCapacity);
        }

        private static int GetBaseAmmoCapacity(int effectID)
        {
            RulesOfEffect effect = EffectRegistry.GetEffectByID(effectID);
            return effect != null ? effect.ShotsPerAmmo : SHPCAmmoCapacity.GetCapacity(effectID);
        }

        private static int RoundCapacityUpToFive(int capacity)
        {
            capacity = Math.Max(1, capacity);
            return ((capacity + 4) / 5) * 5;
        }

        private static float GetEnergyCoreCapacityMultiplier(Player player)
        {
            if (player == null)
                return 1f;

            return player.GetModPlayer<SHPCEnergyCorePlayer>().AmmoCapacityMultiplier;
        }

        private void ClampSelectedMagazineToActiveCount(Player player)
        {
            int activeCount = GetActiveMagazineCount(player);
            selectedMagazineIndex = Utils.Clamp(selectedMagazineIndex, 0, activeCount - 1);
        }

        private static bool IsValidAmmoEffectPair(int ammoType, int effectID)
        {
            return ammoType > ItemID.None &&
                effectID > 0 &&
                EffectRegistry.IsRegisteredAmmoEffectPair(ammoType, effectID);
        }

        private void SanitizeMagazineSlot(int index)
        {
            index = Utils.Clamp(index, 0, MagazineCount - 1);

            int ammoType = magazineAmmoTypes[index];
            int effectID = magazineEffectIDs[index];
            bool empty = ammoType <= ItemID.None && effectID <= 0 && magazineEffectPowers[index] <= 0 && magazineReserve[index] <= 0;
            if (empty)
            {
                ClearMagazine(index);
                return;
            }

            if (!IsValidAmmoEffectPair(ammoType, effectID))
            {
                ClearMagazine(index);
                return;
            }

            magazineEffectPowers[index] = Math.Max(0, magazineEffectPowers[index]);
            magazineReserve[index] = Utils.Clamp(magazineReserve[index], 0, MaxReservePerSlot);
        }

        private void SanitizeAllMagazineSlots()
        {
            selectedMagazineIndex = Utils.Clamp(selectedMagazineIndex, 0, MagazineCount - 1);
            for (int i = 0; i < MagazineCount; i++)
                SanitizeMagazineSlot(i);
        }

        // 后坐力动画计数
        public int recoilProgress = 0;
        #endregion

        #region ===== 天顶世界补射状态 =====
        // ===== 天顶世界三连发控制 =====
        private int zenithBurstTimer;
        private int zenithBurstCount;
        #endregion

        private BalanceSHPC balance = new();
        #endregion


        #region ===== 基础物品设定 =====
        public override void SetDefaults()
        {
            Item.width = 124;
            Item.height = 52;
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.mana = BaseManaCost;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            if (Main.zenithWorld)
            {
                Item.UseSound = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/AWM开火")
                {
                    Volume = 1.5f,
                    Pitch = 0.1f
                };
            }
            else
            {
                Item.UseSound = FireSound;
            }
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<NewLegendSHPB>();
            Item.shootSpeed = 20f;
            Item.channel = false;

            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            //Item.rare = ItemRarityID.Pink;
        }
        #endregion


        #region ===== 灌注读取与通用查询 =====

        #region ===== 弹药与效果查询 =====

        // 查找当前玩家背包/弹药栏中，是否存在一个“被注册表认可的灌注弹药”
        // 现在虽然你只会注册 EnergyCore_Effect 对应的那一种，
        // 但结构上已经允许以后扩成很多种。
        public static int FindEffectAmmo(Player player)
        {
            List<SHPCAmmoCandidate> candidates = FindEffectAmmoCandidates(player, 1);
            return candidates.Count > 0 ? candidates[0].AmmoType : -1;
        }

        public readonly struct SHPCAmmoCandidate
        {
            public SHPCAmmoCandidate(int ammoType, int effectID, int inventoryIndex)
            {
                AmmoType = ammoType;
                EffectID = effectID;
                InventoryIndex = inventoryIndex;
            }

            public int AmmoType { get; }
            public int EffectID { get; }
            public int InventoryIndex { get; }
        }

        public readonly struct SHPCMagazineSlot
        {
            public SHPCMagazineSlot(int index, int ammoType, int effectID, int power, int reserve, bool selected)
            {
                Index = index;
                AmmoType = ammoType;
                EffectID = effectID;
                Power = power;
                Reserve = reserve;
                Selected = selected;
            }

            public int Index { get; }
            public int AmmoType { get; }
            public int EffectID { get; }
            /// <summary>内弹夹剩余发数（当前装填的一份材料还能打多少发）</summary>
            public int Power { get; }
            /// <summary>外置弹药库数量（最多 MaxReservePerSlot 个，供补充内弹夹用）</summary>
            public int Reserve { get; }
            public bool Selected { get; }
            public bool IsConfigured => AmmoType > ItemID.None && EffectID > 0;
            public bool HasAmmo => AmmoType > ItemID.None && EffectID > 0 && Power > 0;
            public bool HasReserve => Reserve > 0;
        }

        public static List<SHPCAmmoCandidate> FindEffectAmmoCandidates(Player player, int maxCount)
        {
            List<SHPCAmmoCandidate> candidates = new();
            HashSet<int> seenAmmoTypes = new();

            AddEffectAmmoCandidates(player, candidates, seenAmmoTypes, maxCount, 0, 54);
            AddEffectAmmoCandidates(player, candidates, seenAmmoTypes, maxCount, 54, 58);

            return candidates;
        }

        private static void AddEffectAmmoCandidates(Player player, List<SHPCAmmoCandidate> candidates, HashSet<int> seenAmmoTypes, int maxCount, int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex && candidates.Count < maxCount; i++)
            {
                Item item = player.inventory[i];
                if (item == null || item.stack <= 0 || !EffectRegistry.IsRegisteredAmmo(item.type))
                    continue;

                if (!seenAmmoTypes.Add(item.type))
                    continue;

                candidates.Add(new SHPCAmmoCandidate(item.type, EffectRegistry.GetEffectIDByAmmo(item.type), i));
            }
        }

        public bool TryLoadSelectedEffectAmmo(Player player, SHPCAmmoCandidate candidate)
        {
            int ammoType = candidate.AmmoType;
            if (ammoType <= ItemID.None || !EffectRegistry.IsRegisteredAmmo(ammoType))
                return false;

            if (candidate.InventoryIndex < 0 || candidate.InventoryIndex >= player.inventory.Length)
                return false;

            Item sourceItem = player.inventory[candidate.InventoryIndex];
            if (sourceItem == null || sourceItem.stack <= 0 || sourceItem.type != ammoType)
                return false;

            TryReturnStoredAmmo(player);
            sourceItem.stack--;
            if (sourceItem.stack <= 0)
                sourceItem.TurnToAir();

            storedAmmoType = ammoType;
            storedEffectID = EffectRegistry.GetEffectIDByAmmo(ammoType);
            storedEffectPower = GetAdjustedAmmoCapacity(player, storedEffectID);
            return true;
        }

        public SHPCMagazineSlot GetMagazineSlot(int index)
        {
            index = Utils.Clamp(index, 0, MagazineCount - 1);
            SanitizeMagazineSlot(index);
            return new SHPCMagazineSlot(index, magazineAmmoTypes[index], magazineEffectIDs[index], magazineEffectPowers[index], magazineReserve[index], index == CurrentMagazineIndex);
        }

        public SHPCMagazineSlot GetMagazineSlot(int index, Player player)
        {
            ClampSelectedMagazineToActiveCount(player);
            index = Utils.Clamp(index, 0, GetActiveMagazineCount(player) - 1);
            SanitizeMagazineSlot(index);
            return new SHPCMagazineSlot(index, magazineAmmoTypes[index], magazineEffectIDs[index], magazineEffectPowers[index], magazineReserve[index], index == CurrentMagazineIndex);
        }

        public bool SelectMagazine(int index)
        {
            if (index < 0 || index >= MagazineCount)
                return false;

            selectedMagazineIndex = index;
            return true;
        }

        public bool SelectMagazine(int index, Player player)
        {
            if (index < 0 || index >= GetActiveMagazineCount(player))
                return false;

            selectedMagazineIndex = index;
            return true;
        }

        private bool IsMagazineLoaded(int index)
        {
            return IsMagazineConfigured(index) &&
                   magazineEffectPowers[index] > 0;
        }

        private bool IsMagazineConfigured(int index)
        {
            SanitizeMagazineSlot(index);
            return IsValidAmmoEffectPair(magazineAmmoTypes[index], magazineEffectIDs[index]);
        }

        /// <summary>
        /// 供唯一材料检查使用：材料装入 SHPC 后不再作为背包物品存在，但仍然属于玩家。
        /// </summary>
        public bool HasLoadedAmmoType(int ammoType)
        {
            for (int i = 0; i < MagazineCount; i++)
            {
                if (IsMagazineConfigured(i) && magazineAmmoTypes[i] == ammoType)
                    return true;
            }

            return false;
        }

        public bool TryFillEmptyMagazines(Player player)
        {
            ClampSelectedMagazineToActiveCount(player);
            int activeMagazineCount = GetActiveMagazineCount(player);
            HashSet<int> unavailableAmmoTypes = new();
            for (int i = 0; i < MagazineCount; i++)
            {
                if (IsMagazineConfigured(i))
                    unavailableAmmoTypes.Add(magazineAmmoTypes[i]);
            }

            List<SHPCAmmoCandidate> candidates = FindEffectAmmoCandidates(player, activeMagazineCount);
            bool loadedAny = false;

            for (int magazineIndex = 0; magazineIndex < candidates.Count && magazineIndex < activeMagazineCount; magazineIndex++)
            {
                if (IsMagazineConfigured(magazineIndex))
                    continue;

                SHPCAmmoCandidate selectedCandidate = candidates[magazineIndex];
                if (unavailableAmmoTypes.Contains(selectedCandidate.AmmoType))
                    continue;

                if (selectedCandidate.InventoryIndex < 0 || selectedCandidate.InventoryIndex >= player.inventory.Length)
                    continue;

                Item consumedItem = player.inventory[selectedCandidate.InventoryIndex];
                if (consumedItem == null || consumedItem.stack <= 0 || consumedItem.type != selectedCandidate.AmmoType)
                    continue;

                consumedItem.stack--;
                if (consumedItem.stack <= 0)
                    consumedItem.TurnToAir();

                magazineAmmoTypes[magazineIndex] = selectedCandidate.AmmoType;
                magazineEffectIDs[magazineIndex] = selectedCandidate.EffectID;
                magazineEffectPowers[magazineIndex] = GetAdjustedAmmoCapacity(player, selectedCandidate.EffectID);
                unavailableAmmoTypes.Add(selectedCandidate.AmmoType);
                loadedAny = true;
            }

            return loadedAny;
        }

        private void ConsumeCurrentMagazineShot()
        {
            int index = CurrentMagazineIndex;
            if (!IsMagazineLoaded(index))
                return;

            magazineEffectPowers[index]--;
            HandleDepletedMagazine(index);
        }

        private void ConsumeCurrentMagazineShot(Player player)
        {
            int index = CurrentMagazineIndex;
            if (!IsMagazineLoaded(index))
                return;

            magazineEffectPowers[index]--;
            HandleDepletedMagazine(index, player);
        }

        public void ConsumeCurrentMagazineShots(int amount, Player player = null)
        {
            int index = CurrentMagazineIndex;
            if (!IsMagazineLoaded(index))
                return;

            magazineEffectPowers[index] -= Math.Max(1, amount);
            HandleDepletedMagazine(index, player);
        }

        private void HandleDepletedMagazine(int index, Player player = null)
        {
            if (magazineEffectPowers[index] > 0)
                return;

            magazineEffectPowers[index] = 0;
            // 槽位不自动清空：有外置储备时下次开火会自动补充；
            // 外置储备为零且背包也空时，保留配置供玩家手动补充（通过UI）
        }

        private bool TryPrepareCurrentMagazineForShot(Player player)
        {
            ClampSelectedMagazineToActiveCount(player);
            int index = CurrentMagazineIndex;
            if (IsMagazineLoaded(index))
                return true;

            if (IsMagazineConfigured(index))
            {
                // 优先从外置弹药库补充
                if (magazineReserve[index] > 0)
                {
                    magazineReserve[index]--;
                    magazineEffectPowers[index] = GetAdjustedAmmoCapacity(player, magazineEffectIDs[index]);
                    return magazineEffectPowers[index] > 0;
                }

                // 回退：从背包补充（兼容未使用UI的玩家）
                if (TryReloadMagazineFromInventory(player, index))
                    return true;

                // 无弹药：保留配置，本次开火无效果
                return false;
            }

            if (AreAllActiveMagazinesUnconfigured(player) && TryFillEmptyMagazines(player))
            {
                if (IsMagazineLoaded(index))
                    return true;

                int activeMagazineCount = GetActiveMagazineCount(player);
                for (int i = 0; i < activeMagazineCount; i++)
                {
                    if (!IsMagazineLoaded(i))
                        continue;

                    selectedMagazineIndex = i;
                    return true;
                }
            }

            return false;
        }

        private bool HasProjectileEffectAvailableForCost(Player player)
        {
            ClampSelectedMagazineToActiveCount(player);
            int index = CurrentMagazineIndex;
            if (IsMagazineLoaded(index) ||
                (IsMagazineConfigured(index) && (magazineReserve[index] > 0 || HasReloadMaterialInInventory(player, index))))
                return true;

            return !IsMagazineConfigured(index) &&
                   AreAllActiveMagazinesUnconfigured(player) &&
                   FindEffectAmmo(player) != -1;
        }

        private bool AreAllActiveMagazinesUnconfigured(Player player)
        {
            int activeMagazineCount = GetActiveMagazineCount(player);
            for (int i = 0; i < activeMagazineCount; i++)
            {
                if (IsMagazineConfigured(i))
                    return false;
            }

            return true;
        }

        private bool TryReloadMagazineFromInventory(Player player, int index)
        {
            index = Utils.Clamp(index, 0, MagazineCount - 1);
            if (!IsMagazineConfigured(index))
                return false;

            int inventoryIndex = FindReloadMaterialInventoryIndex(player, magazineAmmoTypes[index]);
            if (inventoryIndex < 0)
                return false;

            Item consumedItem = player.inventory[inventoryIndex];
            consumedItem.stack--;
            if (consumedItem.stack <= 0)
                consumedItem.TurnToAir();

            magazineEffectPowers[index] = GetAdjustedAmmoCapacity(player, magazineEffectIDs[index]);
            return magazineEffectPowers[index] > 0;
        }

        private bool HasReloadMaterialInInventory(Player player, int index)
        {
            index = Utils.Clamp(index, 0, MagazineCount - 1);
            return IsMagazineConfigured(index) &&
                   FindReloadMaterialInventoryIndex(player, magazineAmmoTypes[index]) >= 0;
        }

        private static int FindReloadMaterialInventoryIndex(Player player, int ammoType)
        {
            if (player == null || ammoType <= ItemID.None)
                return -1;

            for (int i = 0; i < 58 && i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && item.type == ammoType)
                    return i;
            }

            return -1;
        }

        private void ClearMagazine(int index)
        {
            index = Utils.Clamp(index, 0, MagazineCount - 1);
            magazineEffectPowers[index] = 0;
            magazineAmmoTypes[index] = ItemID.None;
            magazineEffectIDs[index] = 0;
            magazineReserve[index] = 0;
        }

        public void PublicClearMagazine(int index) => ClearMagazine(index);

        public void PublicClearMagazineWithReturn(Player player, int index) => ClearMagazineWithAmmoReturn(player, index);

        public void DirectLoadMagazine(int slotIndex, int ammoType, int effectID, int capacity)
        {
            slotIndex = Utils.Clamp(slotIndex, 0, MagazineCount - 1);
            if (!IsValidAmmoEffectPair(ammoType, effectID))
            {
                ClearMagazine(slotIndex);
                return;
            }

            magazineAmmoTypes[slotIndex] = ammoType;
            magazineEffectIDs[slotIndex] = effectID;
            magazineEffectPowers[slotIndex] = Math.Max(0, capacity);
        }

        /// <summary>
        /// 向外置弹药库追加材料。若槽位为空则同时初始化类型；追加上限 MaxReservePerSlot。
        /// </summary>
        public void AddToReserve(int slotIndex, int ammoType, int effectID, int count)
        {
            slotIndex = Utils.Clamp(slotIndex, 0, MagazineCount - 1);
            if (!IsValidAmmoEffectPair(ammoType, effectID) || count <= 0)
                return;

            SanitizeMagazineSlot(slotIndex);
            if (!IsMagazineConfigured(slotIndex))
            {
                magazineAmmoTypes[slotIndex] = ammoType;
                magazineEffectIDs[slotIndex] = effectID;
            }
            else if (magazineAmmoTypes[slotIndex] != ammoType || magazineEffectIDs[slotIndex] != effectID)
                return;

            magazineReserve[slotIndex] = Math.Min(MaxReservePerSlot, magazineReserve[slotIndex] + count);
        }

        /// <summary>直接设置外置库数量（UI取出时用）。</summary>
        public void SetReserve(int slotIndex, int count)
        {
            slotIndex = Utils.Clamp(slotIndex, 0, MagazineCount - 1);
            SanitizeMagazineSlot(slotIndex);
            if (!IsMagazineConfigured(slotIndex))
                return;

            magazineReserve[slotIndex] = Utils.Clamp(count, 0, MaxReservePerSlot);
        }

        private void TryReturnStoredAmmo(Player player)
        {
            TryReturnMagazineAmmo(player, CurrentMagazineIndex);
        }

        private void ClearMagazineWithAmmoReturn(Player player, int index)
        {
            TryReturnMagazineAmmo(player, index);
            ClearMagazine(index);
        }

        private void TryReturnMagazineAmmo(Player player, int index)
        {
            index = Utils.Clamp(index, 0, MagazineCount - 1);
            SanitizeMagazineSlot(index);
            int effectID = magazineEffectIDs[index];
            int ammoType = magazineAmmoTypes[index];
            int power = magazineEffectPowers[index];
            int reserve = magazineReserve[index];

            if (!IsValidAmmoEffectPair(ammoType, effectID))
            {
                ClearMagazine(index);
                return;
            }

            // 外置弹药库：全部直接返还（稳定数量）
            if (reserve > 0)
                player.QuickSpawnItem(player.GetSource_FromThis(), ammoType, reserve);

            // 内弹夹剩余：按 power/capacity 概率返还一份材料
            if (power > 0)
            {
                // 唯一 Lore 材料必须无条件返还
                if (effectID == CynosureEffect.CynosureEffectID && ammoType == ModContent.ItemType<LoreCynosure>())
                {
                    player.QuickSpawnItem(player.GetSource_FromThis(), ammoType, 1);
                }
                else
                {
                    int maxShots = GetAdjustedAmmoCapacity(player, effectID);
                    if (maxShots > 0)
                    {
                        float returnChance = MathHelper.Clamp(power / (float)maxShots, 0f, 1f);
                        if (Main.rand.NextFloat() < returnChance)
                            player.QuickSpawnItem(player.GetSource_FromThis(), ammoType, 1);
                    }
                }
            }

            magazineReserve[index] = 0;
        }

        // 根据当前效果ID获取主题色
        public Color FindColorForCurrentEffect()
        {
            RulesOfEffect effect = EffectRegistry.GetEffectByID(storedEffectID);
            if (effect != null)
                return effect.ThemeColor;

            return Color.DarkGray;
        }

        // 将当前效果ID传给弹幕
        public int TransferEffectToProj()
        {
            return storedEffectID;
        }

        // 获取当前灌注弹药显示名，用于 Tooltip / UI
        public string GetCurrentAmmoDisplayName()
        {
            SanitizeMagazineSlot(CurrentMagazineIndex);
            if (storedAmmoType <= ItemID.None)
                return "None";

            return Lang.GetItemNameValue(storedAmmoType);
        }
        #endregion
        #endregion


        #region ===== 左键开火与通用发射流程 =====

        #region ===== 创建与手持偏移 =====
        public override Vector2? HoldoutOffset() => new Vector2(-35f, -0f);

        public override void OnCreated(ItemCreationContext context)
        {
        }
        #endregion

        #region ===== 使用条件与消耗 =====
        public override bool CanUseItem(Player player)
        {
            ClampSelectedMagazineToActiveCount(player);

            // Keep the inventory slot's normal click behavior: while the cursor is over an
            // SHPC item, that click belongs to the inventory (including its right-click unload
            // action), not to the held weapon. Opening the inventory by itself must not block fire.
            if (Main.playerInventory && Main.HoverItem.type == Type)
                return false;

            if (IsUsingEX(player))
                return false;

            bool militaryCallerRightClick = player.altFunctionUse == 2 &&
                player.GetModPlayer<MilitaryCallerPlayer>().MilitaryCallerEquipped;
            SHPCRight_Player heatPlayer = player.GetModPlayer<SHPCRight_Player>();

            if (heatPlayer.IsForcedShutdownCooling() ||
                (heatPlayer.AttackLockoutTimer > 0 && !militaryCallerRightClick))
                return false;

            if (Main.myPlayer == player.whoAmI && KeybindSystem.LegendaryWeaponFormSwitch?.Current == true)
                return false;

            if (player.altFunctionUse == 2)
            {
                Item.mana = 0;
                Item.channel = true;         // Right-click channel
                Item.noUseGraphic = true;    // Holdout draws the weapon
                Item.UseSound = null;
            }
            else
            {
                Item.mana = BaseManaCost;
                Item.channel = false;
                Item.noUseGraphic = false;
                Item.UseSound = ShouldPlayDefaultLeftClickFireSound(GetProjectileEffectIDForShot()) ? FireSound : null;
            }

            // ===== 天顶世界三连发初始化 =====
            if (Main.zenithWorld)
            {
                zenithBurstCount = 2; // Two follow-up shots, three total
                zenithBurstTimer = 8;
            }

            // 只要当前还有灌注次数，或者玩家包里还能找到可灌注弹药，就允许使用
            //return storedEffectPower > 0 || FindEffectAmmo(player) != -1;
            // 改主意了，没有弹药也允许开火，只是一切为默认
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // ⭐ 右键：完全不参与弹药系统
            if (player.altFunctionUse == 2)
                return true;

            return base.UseItem(player);
        }
        #endregion

        #region ===== 使用参数覆写 =====
        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            ClampSelectedMagazineToActiveCount(player);
            SHPCEnergyCorePlayer energyCore = player.GetModPlayer<SHPCEnergyCorePlayer>();
            mult *= 1.5f * energyCore.LeftManaCostMultiplier;

            if (player.altFunctionUse != 2 && !HasProjectileEffectAvailableForCost(player))
                mult *= 0.5f;
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit += player.GetModPlayer<SHPCEnergyCorePlayer>().SHPCCritBonus;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            // 右键暂不做，保留默认速度
            return 1f;
        }
        #endregion

        #region ===== 左键发射与安全枪口 =====
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

            // ❌ 新增：左键冷却锁
            SHPCRight_Player heatPlayer = player.GetModPlayer<SHPCRight_Player>();
            if (leftClickCooldown > 0 || heatPlayer.AttackLockoutTimer > 0 || heatPlayer.IsForcedShutdownCooling())
                return false;

            // 右键 → 不发射左键弹幕
            if (player.altFunctionUse == 2)
            {
                return false;
            }

            var exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            if (player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped &&
                exPlayer.EXValue >= NewLegend_EXPlayer.GetCurrentEXMax(player) &&
                KeybindSystem.LegendarySkill.Current)
            {
                return false;
            }

            int shotEffectID = TryPrepareCurrentMagazineForShot(player) ? storedEffectID : -1;
            Projectile.NewProjectile(
                source,
                GetSafeFirePosition(player, velocity) + new Vector2(0f, -10f),
                velocity,
                ModContent.ProjectileType<NewLegendSHPB>(),
                damage,
                knockback,
                player.whoAmI,
                shotEffectID
            );

            // 生成左键手持弹幕（先清除旧的再重新生成以重置动画）
            if (player.whoAmI == Main.myPlayer)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == player.whoAmI && p.type == ModContent.ProjectileType<SHPCLeftClickHoldout>())
                        p.Kill();
                }

                RulesOfEffect shotEffect = EffectRegistry.GetEffectByID(shotEffectID);
                int burstCount = shotEffect?.LeftClickBurstCount ?? 1;
                Projectile.NewProjectile(
                    source,
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SHPCLeftClickHoldout>(),
                    0,
                    0f,
                    player.whoAmI,
                    shotEffectID,
                    burstCount
                );
            }

            SHPCLeftClickSounds.PlayForEffect(shotEffectID, player.Center);
            leftClickCooldown = shotEffectID == AshesofAnnEffect.AshesofAnnEffectID ? Math.Max(1, (int)MathF.Ceiling(Item.useTime * 0.6f)) : Item.useTime;
            if (!heatPlayer.IsForcedShutdownCooling())
                heatPlayer.PauseHeatDissipation(30);
            GainEXFromLeftShot(player);
            if (!player.GetModPlayer<SHPCEnergyCorePlayer>().ShouldSaveLeftClickAmmo())
                ConsumeCurrentMagazineShot(player);

            return false;
        }

        internal int GetProjectileEffectIDForShot()
        {
            return storedEffectPower > 0 && storedEffectID > 0 ? storedEffectID : -1;
        }

        private static bool ShouldPlayDefaultLeftClickFireSound(int effectID)
        {
            return EffectRegistry.GetEffectByID(effectID).PlayDefaultLeftClickFireSound;
        }

        public static void GainEXFromLeftShot(Player player, int multiplier = 1)
        {
            NewLegend_EXPlayer exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            if (!exPlayer.EXUnlocked || !HasNearbyChargeTarget(player))
                return;

            int currentMaxEX = NewLegend_EXPlayer.GetCurrentEXMax(player);
            int chargeMultiplier = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip.FastChipPlayer>().FastChipEquipped ? 2 : 1;
            exPlayer.EXValue += NewLegend_EXPlayer.GetBaseFramesPerDisplayUnit() * Math.Max(1, multiplier) * chargeMultiplier;

            if (exPlayer.EXValue > currentMaxEX)
                exPlayer.EXValue = currentMaxEX;
        }

        private static bool HasNearbyChargeTarget(Player player)
        {
            const float chargeRange = 900f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(player, false))
                    continue;

                if (Vector2.Distance(player.Center, npc.Center) <= chargeRange)
                    return true;
            }

            return false;
        }

        // ===== 左键安全开火位置 =====
        private Vector2 GetSafeFirePosition(Player player, Vector2 velocity)
        {
            // ===== 构造“虚拟枪口” =====
            Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float gunLength = 56f;

            Vector2 gunTip = player.Center + dir * gunLength;

            // ===== 1. 枪口卡墙 =====
            if (Collision.SolidCollision(gunTip, 1, 1))
                return player.Center;

            // ===== 2. 找最近敌人 =====
            NPC target = null;
            float maxDetect = 300f;
            float closestDist = maxDetect;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float dist = Vector2.Distance(player.Center, npc.Center);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = npc;
                }
            }

            // ===== 3. 贴脸判定 =====
            if (target != null && closestDist < gunLength)
                return player.Center;

            // ===== 4. 敌人在枪口前面 =====
            if (target != null)
            {
                float distToPlayer = Vector2.Distance(player.Center, target.Center);
                float distToGunTip = Vector2.Distance(gunTip, target.Center);

                if (distToPlayer < distToGunTip)
                    return player.Center;
            }

            return gunTip;
        }
        #endregion
        #endregion


        #region ===== 背包 UI 显示 =====
        // ==================== 背包UI显示 ====================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Player player = Main.LocalPlayer;
            ClampSelectedMagazineToActiveCount(player);

            float barScale = 2.5f;
            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            Vector2 drawPos = position + new Vector2((frame.Width - barBG.Width * 0.5f) * scale, (frame.Height + 45f) * scale);
            int maxShots = storedEffectID > 0 ? GetAdjustedAmmoCapacity(player, storedEffectID) : 1;
            float currentProgress = storedEffectID > 0 && storedEffectPower > 0
                ? MathHelper.Clamp(storedEffectPower / (float)maxShots, 0f, 1f)
                : 0f;
            Rectangle frameCrop = new Rectangle(
                0,
                0,
                (int)(currentProgress * barFG.Width),
                barFG.Height
            );

            Color colorBG = Color.Black;
            Color colorFG = Color.Lerp(Color.DarkGray, FindColorForCurrentEffect(), currentProgress);

            spriteBatch.Draw(barBG, drawPos, null, colorBG, 0f, origin, scale * barScale, 0, 0f);
            spriteBatch.Draw(barFG, drawPos, frameCrop, colorFG * 0.8f, 0f, origin, scale * barScale, 0, 0f);

            CalamityUtils.DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                storedEffectPower.ToString(),
                drawPos + new Vector2(-200f, -60f) * scale,
                Color.GreenYellow,
                Color.Black,
                scale * 2.5f
            );
        }
        #endregion

        
        #region ===== 手持表现、右键监听与 EX 技能 =====

        #region ===== 右键入口与阶段查询 =====
        public override bool AltFunctionUse(Player player) => true;

        // ===== 获取右键进度状态 =====
        public int GetRightClickProgressState()
        {
            return balance.GetRightClickProgressState();
        }
        #endregion

        #region ===== HoldItem 主流程 =====
        // 左键独立冷却
        private int leftClickCooldown = 0;
        public override void HoldItem(Player player)
        {
            ClampSelectedMagazineToActiveCount(player);

            if (leftClickCooldown > 0)
                leftClickCooldown--;

            // ===== EX条UI同步 =====
            var exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            SHPCRight_Player heatPlayer = player.GetModPlayer<SHPCRight_Player>();
            bool shpcAttackLocked = heatPlayer.IsForcedShutdownCooling() || heatPlayer.AttackLockoutTimer > 0;
            bool exUnlocked = exPlayer.EXUnlocked;
            bool legendaryEXUnlocked = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
            bool canUseEX = exUnlocked && legendaryEXUnlocked;

            if (canUseEX)
            {
                if (player.Calamity().cooldowns.TryGetValue(SHPC_EXCooldown.ID, out var cooldown))
                {
                    cooldown.timeLeft = exPlayer.EXValue;
                }
                else
                {
                    player.AddCooldown(SHPC_EXCooldown.ID, 0);
                }
            }
            else if (player.Calamity().cooldowns.TryGetValue(SHPC_EXCooldown.ID, out var cooldown))
            {
                cooldown.timeLeft = 0;
            }

            // ===== EX技能释放 =====
            if (canUseEX &&
                !shpcAttackLocked &&
                KeybindSystem.LegendarySkill.JustPressed &&
                exPlayer.EXValue >= NewLegend_EXPlayer.GetCurrentEXMax(player))
            {
                // 防止重复生成
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.owner == player.whoAmI &&
                        proj.type == ModContent.ProjectileType<NL_SHPC_EXWeapon>())
                    {
                        return;
                    }
                }

                InterruptNormalSHPCUse(player);

                Vector2 dir = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
                int exLaserDamage = GetCurrentLeftClickDamage(player, GetProjectileEffectIDForShot());

                int exIndex = Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    dir,
                    ModContent.ProjectileType<NL_SHPC_EXWeapon>(),
                    exLaserDamage,
                    Item.knockBack,
                    player.whoAmI
                );

                if (Main.projectile.IndexInRange(exIndex))
                {
                    Main.projectile[exIndex].CritChance = player.GetWeaponCrit(Item);
                    Main.projectile[exIndex].netUpdate = true;
                }

                // 清空EX条（如果你之后想改，可以删这句）
                exPlayer.EXValue = 0;
            }

            if (Main.myPlayer == player.whoAmI)
                EnsureMagazineStatusPanel(player);

            if (IsUsingEX(player))
                return;

            // ===== 天顶世界三连发补射 =====
            if (Main.zenithWorld && zenithBurstCount > 0)
            {
                zenithBurstTimer--;

                if (zenithBurstTimer <= 0)
                {
                    Vector2 shootDirection = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
                    Vector2 velocity = shootDirection * Item.shootSpeed;

                    Projectile.NewProjectile(
                        Item.GetSource_FromThis(),
                        player.Center + new Vector2(0f, -10f) + velocity * 3f,
                        velocity,
                        ModContent.ProjectileType<NewLegendSHPB>(),
                        GetCurrentRightDamage(player, ModContent.ProjectileType<SHPCRight_HoulOut>()),
                        Item.knockBack,
                        player.whoAmI,
                        storedEffectID > 0 ? storedEffectID : -1
                    );

                    zenithBurstCount--;
                    zenithBurstTimer = 8;

                    // ❗手动触发音效（否则不会响）
                    int burstEffectID = storedEffectID > 0 ? storedEffectID : -1;
                    if (ShouldPlayDefaultLeftClickFireSound(burstEffectID))
                        SoundEngine.PlaySound(FireSound, player.Center);
                    SHPCLeftClickSounds.PlayForEffect(burstEffectID, player.Center);
                }
            }

            // 鼠标监听（原有）
            player.Calamity().mouseWorldListener = true;

            // ===== 关键：开启右键监听 =====
            if (Main.myPlayer == player.whoAmI)
            {
                player.Calamity().rightClickListener = true;
                HandleAmmoSelectionKey(player);
            }

            // ===== 右键长按逻辑 =====
            if (CanStartRightClickHoldout(player))
            {
                bool useTurretRightClick = player.GetModPlayer<MilitaryCallerPlayer>().MilitaryCallerEquipped;
                if (heatPlayer.IsForcedShutdownCooling() ||
                    (heatPlayer.AttackLockoutTimer > 0 && !useTurretRightClick))
                    return;

                // 🔥 强制打断左键动画
                //player.itemAnimation = 0;
                //player.itemTime = 0;
                //recoilProgress = 0;

                int defaultRightClickType = ModContent.ProjectileType<SHPCRight_HoulOut>();
                int mortarRightClickType = ModContent.ProjectileType<RightClickMortar_HoldOut>();
                int turretRightClickType = ModContent.ProjectileType<MilitaryCaller_HoldOut>();
                int possessionRightClickType = ModContent.ProjectileType<ProjectilePossessionHoldout>();
                bool useMortarRightClick = player.GetModPlayer<CommandAscendPlayer>().CommandAscendEquipped;
                bool usePossessionRightClick = player.GetModPlayer<ProjectilePossessionModulePlayer>().ProjectilePossessionModuleEquipped;
                int rightClickHoldoutType = useTurretRightClick ? turretRightClickType : useMortarRightClick ? mortarRightClickType : usePossessionRightClick ? possessionRightClickType : defaultRightClickType;

                // ===== 防止重复生成；配件切换时清掉另一套右键手持 =====
                foreach (Projectile proj in Main.projectile)
                {
                    if (!proj.active || proj.owner != player.whoAmI)
                        continue;

                    if (proj.type == rightClickHoldoutType)
                    {
                        return;
                    }

                    if (proj.type == defaultRightClickType || proj.type == mortarRightClickType || proj.type == turretRightClickType || proj.type == possessionRightClickType)
                    {
                        proj.Kill();
                        proj.netUpdate = true;
                    }
                }

                // ===== 生成右键 Holdout =====
                Vector2 aimWorld = CtrlChipPlayer.GetAimWorld(player, player.Calamity().mouseWorld);
                Vector2 shootDirection = (aimWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

                int projIndex = Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    shootDirection,
                    rightClickHoldoutType,
                    GetCurrentRightDamage(player, rightClickHoldoutType),
                    Item.knockBack,
                    player.whoAmI,
                    GetRightClickProgressState(),                 // ai[0]
                    (storedEffectPower > 0 && storedEffectID > 0)
                        ? storedEffectID
                        : EffectRegistry.GetEffectIDByAmmo(FindEffectAmmo(player)));

                if (Main.projectile.IndexInRange(projIndex))
                {
                    SHPCEnergyCorePlayer energyCore = player.GetModPlayer<SHPCEnergyCorePlayer>();
                    Main.projectile[projIndex].CritChance = energyCore.AllowRightClickCrit ? player.GetWeaponCrit(Item) : 0;
                }
            }       

            if (player.itemAnimation > 0 && player.altFunctionUse != 2)
                return;
        }

        private bool CanStartRightClickHoldout(Player player)
        {
            if (player.whoAmI != Main.myPlayer ||
                IsUsingEX(player) ||
                KeybindSystem.LegendaryWeaponFormSwitch?.Current == true)
            {
                return false;
            }

            bool rightClickHeld = player.Calamity().mouseRight || Main.mouseRight;
            bool interactionActive = IsWorldRightClickInteractionActive(player);

            if (!rightClickHeld)
            {
                suppressWorldRightClickUntilRelease = false;
                wasWorldRightClickInteractionActive = interactionActive;
                return false;
            }

            if (interactionActive || wasWorldRightClickInteractionActive)
                suppressWorldRightClickUntilRelease = true;

            wasWorldRightClickInteractionActive = interactionActive;

            if (!player.Calamity().mouseRight)
                return false;

            if (suppressWorldRightClickUntilRelease)
                return false;

            if (!CanUseWorldRightClick(player) || IsRightClickBlockedByWorldInteraction())
            {
                suppressWorldRightClickUntilRelease = true;
                return false;
            }

            return true;
        }

        private void InterruptNormalSHPCUse(Player player)
        {
            player.itemAnimation = 0;
            player.itemTime = 0;
            leftClickCooldown = 0;
            zenithBurstCount = 0;
            zenithBurstTimer = 0;
            recoilProgress = 0;

            int rightHoldoutType = ModContent.ProjectileType<SHPCRight_HoulOut>();
            int mortarHoldoutType = ModContent.ProjectileType<RightClickMortar_HoldOut>();
            int turretHoldoutType = ModContent.ProjectileType<MilitaryCaller_HoldOut>();
            int possessionHoldoutType = ModContent.ProjectileType<ProjectilePossessionHoldout>();
            int wheelType = ModContent.ProjectileType<SHPCAmmoSelectionPanel>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type == rightHoldoutType ||
                    projectile.type == mortarHoldoutType ||
                    projectile.type == turretHoldoutType ||
                    projectile.type == possessionHoldoutType ||
                    projectile.type == wheelType)
                {
                    projectile.Kill();
                    projectile.netUpdate = true;
                }
            }
        }

        private static bool IsUsingEX(Player player)
        {
            int exType = ModContent.ProjectileType<NL_SHPC_EXWeapon>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == exType)
                    return true;
            }

            return false;
        }

        internal static bool CanUseWorldRightClick(Player player)
        {
            if (player.noItems ||
                player.CCed ||
                Main.mapFullscreen ||
                Main.blockMouse ||
                player.mouseInterface ||
                IsWorldRightClickInteractionActive(player))
            {
                return false;
            }

            return true;
        }

        private static bool IsWorldRightClickInteractionActive(Player player)
        {
            return (Main.playerInventory && Main.HoverItem.type == ModContent.ItemType<NewLegendSHPC>()) ||
                   player.chest != -1 ||
                   player.sleeping.isSleeping ||
                   player.TalkNPC != null;
        }

        private static bool IsRightClickBlockedByWorldInteraction()
        {
            if (!Main.mouseRightRelease)
                return false;

            if (!Main.npcChatRelease)
                return true;

            return Main.SmartInteractX != -1 ||
                   Main.SmartInteractY != -1 ||
                   Main.SmartInteractProj != -1;
        }

        private void HandleAmmoSelectionKey(Player player)
        {
            if (KeybindSystem.LegendaryWeaponFormSwitch?.JustPressed != true)
                return;

            if (player.noItems ||
                player.CCed ||
                Main.mapFullscreen)
            {
                return;
            }

            int panelType = ModContent.ProjectileType<SHPCAmmoSelectionPanel>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != panelType)
                    continue;

                return;
            }

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                panelType,
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Pitch = 0.06f, Volume = 0.62f }, player.Center);
        }

        private void EnsureMagazineStatusPanel(Player player)
        {
            int panelType = ModContent.ProjectileType<SHPCMagazineStatusPanel>();
            if (player.ownedProjectileCounts[panelType] > 0)
                return;

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                panelType,
                0,
                0f,
                player.whoAmI);
        }
        #endregion

        #region ===== 手持绘制与前臂姿态 =====
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // 始终朝向鼠标
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));

            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;
            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 35f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-35f, 0f);

            bool shouldHide = false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != player.whoAmI)
                    continue;

                if (proj.type == ModContent.ProjectileType<SHPCRight_HoulOut>() ||
                    proj.type == ModContent.ProjectileType<RightClickMortar_HoldOut>() ||
                    proj.type == ModContent.ProjectileType<MilitaryCaller_HoldOut>() ||
                    proj.type == ModContent.ProjectileType<ProjectilePossessionHoldout>() ||
                    proj.type == ModContent.ProjectileType<NL_SHPC_EXWeapon>() ||
                    proj.type == ModContent.ProjectileType<SHPCLeftClickHoldout>())
                {
                    shouldHide = true;
                    break;
                }
            }

            // 如果右键Holdout或大招已经存在，就把残留贴图扔到世界左上角
            if (shouldHide)
            {
                itemPosition = new Vector2(0f, 0f);
            }

            // 左键后坐力动画：完整保留
            recoilProgress++;
            if (recoilProgress < Item.useAnimation / 3)
            {
                itemPosition -= (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX) * (Item.useAnimation / 3 - recoilProgress) * 0.75f;
            }
            else
            {
                if (recoilProgress >= Item.useAnimation - 1)
                    recoilProgress = 0;
            }

            CalamityUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            // 前臂跟随鼠标方向，完整保留
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float rotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        #endregion
        #endregion


        #region ===== 传奇成长与伤害覆盖 =====

        private int GetCurrentRightDamage(Player player, int rightClickHoldoutType)
        {
            int baseDamage =
                rightClickHoldoutType == ModContent.ProjectileType<RightClickMortar_HoldOut>() ? balance.GetMortarRightClickBaseDamage() :
                rightClickHoldoutType == ModContent.ProjectileType<MilitaryCaller_HoldOut>() ? balance.GetTurretRightClickBaseDamage() :
                balance.GetRightClickBaseDamage();
            int damage = (int)player.GetTotalDamage(Item.DamageType).ApplyTo(baseDamage);
            float accessoryMultiplier = player.GetModPlayer<SHPCEnergyCorePlayer>().SHPCDamageMultiplier;
            return Math.Max(1, (int)Math.Round(damage * accessoryMultiplier * 1.25f));
        }

        internal int GetCurrentLeftClickDamage(Player player, int effectID)
        {
            int baseDamage = balance.GetLeftClickBaseDamageForEffect(effectID);
            int damage = (int)player.GetTotalDamage(Item.DamageType).ApplyTo(baseDamage);
            float accessoryMultiplier = player.GetModPlayer<SHPCEnergyCorePlayer>().SHPCDamageMultiplier;
            return Math.Max(1, (int)Math.Round(damage * accessoryMultiplier * 1.25f));
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            ClampSelectedMagazineToActiveCount(player);

            // 直接覆写面板基础伤害
            int targetDamage = balance.GetLeftClickBaseDamageForEffect(GetProjectileEffectIDForShot());
            damage.Base += targetDamage - Item.damage;
            damage *= player.GetModPlayer<SHPCEnergyCorePlayer>().SHPCDamageMultiplier;
            damage *= 1.25f;
        }
        #endregion


        #region ===== Tooltip 文本拼接 =====

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            bool isChinese = Language.ActiveCulture.Name.StartsWith("zh");
            bool shiftPressed = Main.keyState.PressingShift();

            string legendaryText = this.GetLocalizedValue("LegendaryText");
            string shiftHint = this.GetLocalizedValue("LegendaryHint");
            string legendarySection = shiftPressed ? legendaryText : shiftHint;

            if (shiftPressed)
            {
                // 按住Shift：隐藏所有功能描述，只显示传奇文本
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            }
            else
            {
                bool useCompactTooltip = true;
                if (useCompactTooltip)
                {
                    int compactState = GetRightClickProgressState();
                    string compactFormKeyText = KeybindSystem.LegendaryWeaponFormSwitch.GetAssignedKeys().FirstOrDefault() ?? (isChinese ? "未绑定" : "Unbound");
                    string compactLoadingKeyText = GetSHPCLoadingUIKeyText(isChinese);
                    string compactAmmoEffectText = BuildCurrentAmmoEffectTooltipText();
                    string compactUltimateKeyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? (isChinese ? "未绑定" : "Unbound");
                    bool compactLegendaryEmblemEquipped = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
                    string compactExHint = compactLegendaryEmblemEquipped
                        ? string.Format(this.GetLocalizedValue("SHPC_EXHint"), compactUltimateKeyText)
                        : this.GetLocalizedValue("SHPC_EXDisabledHint");

                    string compactPrefix =
                        this.GetLocalizedValue("SHPC_LeftIntro").TrimEnd('\r', '\n') + "\n" +
                        string.Format(this.GetLocalizedValue("SHPC_LoadingUIHint"), compactLoadingKeyText).TrimEnd('\r', '\n') + "\n" +
                        compactAmmoEffectText;

                    string compactSuffix =
                        string.Format(this.GetLocalizedValue("SHPC_AmmoWheelHint"), compactFormKeyText).TrimEnd('\r', '\n') + "\n" +
                        this.GetLocalizedValue($"SHPC_RightIntro{compactState + 1}").TrimEnd('\r', '\n') + "\n" +
                        this.GetLocalizedValue("SHPC_UnloadHint").TrimEnd('\r', '\n') + "\n" +
                        this.GetLocalizedValue("SHPC_Passive").TrimEnd('\r', '\n') + "\n" +
                        compactExHint.TrimEnd('\r', '\n') + "\n" +
                        this.GetLocalizedValue("SHPC_Final").TrimEnd('\r', '\n') + "\n";

                    int placeholderIndex = tooltips.FindIndex(line => line.Text == "[GFB]");
                    if (placeholderIndex >= 0)
                    {
                        tooltips[placeholderIndex].Text = compactPrefix;
                        tooltips.Insert(placeholderIndex + 1, new TooltipLine(Mod, MagazineIconTooltipLineName, new string(' ', 96) + "\n "));
                        tooltips.Insert(placeholderIndex + 2, new TooltipLine(Mod, "SHPCCompactTooltipRemainder", compactSuffix));
                    }
                }
                else
                {
                string leftIntro = this.GetLocalizedValue("SHPC_LeftIntro").TrimEnd('\r', '\n');
                string ammoText = BuildMagazineTooltipText(player);
                string unloadHintText = this.GetLocalizedValue("SHPC_UnloadHint").TrimEnd('\r', '\n');
                string passiveText = this.GetLocalizedValue("SHPC_Passive").TrimEnd('\r', '\n');

                int state = GetRightClickProgressState();
                string rightStateText = this.GetLocalizedValue($"SHPC_RightIntro{state + 1}");

                string finalLine = this.GetLocalizedValue("SHPC_Final");

                string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? (isChinese ? "未绑定" : "Unbound");
                string formKeyText = KeybindSystem.LegendaryWeaponFormSwitch.GetAssignedKeys().FirstOrDefault() ?? (isChinese ? "未绑定" : "Unbound");
                string loadingKeyText = GetSHPCLoadingUIKeyText(isChinese);

                bool legendaryEmblemEquipped = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
                string exHint = legendaryEmblemEquipped
                    ? string.Format(this.GetLocalizedValue("SHPC_EXHint"), keyText)
                    : this.GetLocalizedValue("SHPC_EXDisabledHint");

                string ammoWheelHint = string.Format(this.GetLocalizedValue("SHPC_AmmoWheelHint"), formKeyText);
                string loadingUIHint = string.Format(this.GetLocalizedValue("SHPC_LoadingUIHint"), loadingKeyText);

                string finalText =
                    leftIntro + ammoText +
                    "\n\n" +
                    loadingUIHint + "\n" +
                    ammoWheelHint +
                    "\n\n" +
                    rightStateText + "\n\n" +
                    unloadHintText + "\n\n" +
                    passiveText + "\n\n" +
                    exHint + "\n\n" +
                    finalLine + "\n";

                tooltips.FindAndReplace("[GFB]", finalText);
                }
            }

            tooltips.Add(new TooltipLine(Mod, SHPCMatrixLegendaryTooltip.TooltipLineName, legendarySection));
        }

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Mod != Mod.Name || line.Name != MagazineIconTooltipLineName)
                return true;

            DrawMagazineIconTooltipLine(line);
            return false;
        }

        private void DrawMagazineIconTooltipLine(DrawableTooltipLine line)
        {
            Player player = Main.LocalPlayer;
            int slotCount = GetActiveMagazineCount(player);
            float uiScale = line.BaseScale.X;
            int iconSize = Math.Max(18, (int)(22f * uiScale));
            int slotWidth = Math.Max(58, (int)(72f * uiScale));
            int slotHeight = iconSize + Math.Max(13, (int)(14f * uiScale)) + 7;
            int gap = Math.Max(7, (int)(10f * uiScale));
            int y = (int)line.Y + 1;
            int x = (int)line.X + 10;

            for (int i = 0; i < slotCount; i++)
            {
                SHPCMagazineSlot slot = GetMagazineSlot(i, player);
                Rectangle box = new(x + i * (slotWidth + gap), y, slotWidth, slotHeight);
                Color frameColor = slot.Selected ? new Color(255, 221, 90) : new Color(126, 150, 176);

                DrawTooltipRectangle(box, new Color(11, 16, 24, 220));
                DrawTooltipBorder(box, frameColor, 1);

                if (slot.IsConfigured)
                {
                    Texture2D texture = SHPCAmmoSelectionPanel.TryGetAmmoTexture(slot.EffectID, slot.AmmoType);
                    if (texture != null)
                    {
                        Rectangle source = SHPCAmmoSelectionPanel.GetCurrentFrame(texture, SHPCAmmoSelectionPanel.GetFrameCount(slot.EffectID));
                        Vector2 sourceSize = source.Size();
                        float textureScale = Math.Min((iconSize - 4f) / Math.Max(1f, sourceSize.X), (iconSize - 4f) / Math.Max(1f, sourceSize.Y));
                        Vector2 iconPosition = new(box.Center.X, box.Y + 3 + iconSize * 0.5f);
                        Main.EntitySpriteDraw(texture, iconPosition, source, Color.White, 0f, sourceSize * 0.5f, textureScale, SpriteEffects.None, 0f);
                    }
                }

                string materialName = slot.IsConfigured ? Lang.GetItemNameValue(slot.AmmoType) : "-";
                DrawTooltipItemName(materialName, box, uiScale);

                if (slot.Selected)
                {
                    DrawTooltipTriangle(new Point(box.Left - 6, box.Y + iconSize / 2 + 3), true, new Color(255, 221, 76));
                    DrawTooltipTriangle(new Point(box.Right + 6, box.Y + iconSize / 2 + 3), false, new Color(255, 221, 76));
                }
            }
        }

        private static void DrawTooltipItemName(string text, Rectangle box, float uiScale)
        {
            const float baseScale = 0.42f;
            float scale = baseScale * uiScale;
            string displayText = text;
            float maxWidth = box.Width - 6f;
            while (displayText.Length > 1 && FontAssets.MouseText.Value.MeasureString(displayText).X * scale > maxWidth)
                displayText = displayText[..^1];

            if (displayText != text)
                displayText += "…";

            Vector2 size = FontAssets.MouseText.Value.MeasureString(displayText) * scale;
            Vector2 position = new(box.Center.X - size.X * 0.5f, box.Bottom - size.Y - 2f);
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, FontAssets.MouseText.Value, displayText, position, Color.White, Color.Black, scale);
        }

        private static void DrawTooltipTriangle(Point center, bool pointRight, Color color)
        {
            const int halfHeight = 4;
            for (int column = 0; column <= halfHeight; column++)
            {
                int height = (halfHeight - column) * 2 + 1;
                int x = pointRight ? center.X - halfHeight + column : center.X + halfHeight - column;
                DrawTooltipRectangle(new Rectangle(x, center.Y - height / 2, 1, height), color);
            }
        }

        private static void DrawTooltipRectangle(Rectangle rectangle, Color color) => Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);

        private static void DrawTooltipBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawTooltipRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawTooltipRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawTooltipRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawTooltipRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        private string GetSHPCLoadingUIKeyText(bool isChinese)
        {
            return InventoryActivationInput.GetDisplayKeyOrDefault(
                KeybindSystem.SHPCLoadingUI,
                isChinese ? "鼠标中键" : "Middle Mouse");
        }

        private string BuildCurrentAmmoEffectTooltipText()
        {
            string effectDescription = IsMagazineConfigured(CurrentMagazineIndex)
                ? Language.GetTextValue($"Mods.CalamityLegendsComeBack.AMMO.SHPCAmmo{magazineEffectIDs[CurrentMagazineIndex]}")
                : this.GetLocalizedValue("SHPC_DefaultAmmoEffect");

            return string.Format(this.GetLocalizedValue("SHPC_CurrentAmmoEffect"), effectDescription).TrimEnd('\r', '\n');
        }

        private string BuildMagazineTooltipText(Player player)
        {
            ClampSelectedMagazineToActiveCount(player);
            int activeMagazineCount = GetActiveMagazineCount(player);
            List<string> slots = new();
            bool isChinese = Language.ActiveCulture.Name.StartsWith("zh");
            string emptyText = isChinese ? "空" : "Empty";
            string prefix = isChinese ? "号" : "";

            for (int i = 0; i < activeMagazineCount; i++)
            {
                bool isActive = (i == CurrentMagazineIndex);
                string slotName = $"{i + 1}{prefix}";
                string contentText;

                if (!IsMagazineConfigured(i))
                {
                    contentText = emptyText;
                }
                else
                {
                    int ammoType = magazineAmmoTypes[i];
                    int power = magazineEffectPowers[i];
                    int reserve = magazineReserve[i];
                    int maxShots = GetAdjustedAmmoCapacity(player, magazineEffectIDs[i]);
                    contentText = reserve > 0
                        ? $"[i:{ammoType}]({power}/{maxShots})+{reserve}"
                        : $"[i:{ammoType}]({power}/{maxShots})";
                }

                if (isActive)
                {
                    slots.Add($"[c/FFD700:▶]{slotName}:{contentText}[c/FFD700:◀]");
                }
                else
                {
                    slots.Add($"{slotName}:{contentText}");
                }
            }

            return string.Join("   ", slots);
        }
        #endregion


        #region ===== 背包右键清空灌注 =====

        #region ===== 背包右键入口 =====
        // 背包里点击右键，倒掉左键材料
        public override bool CanRightClick()
        {
            return true;
        }
        #endregion

        #region ===== 清空与返还逻辑 =====
        public override void RightClick(Player player)
        {
            ClampSelectedMagazineToActiveCount(player);

            if (Main.keyState.PressingShift())
            {
                for (int i = 0; i < MagazineCount; i++)
                    ClearMagazineWithAmmoReturn(player, i);
            }
            else
                ClearMagazineWithAmmoReturn(player, CurrentMagazineIndex);

            // ===== 音效 =====
            SoundEngine.PlaySound(SoundID.MenuClose, player.Center);
        }
        #endregion

        #region ===== 阻止物品自身被消耗 =====
        public override bool ConsumeItem(Player player)
        {
            return false;
        }
        #endregion
        #endregion


        #region ===== 克隆、存档与联机同步 =====

        #region ===== 合成表 =====
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(8)
                .AddIngredient<DubiousPlating>(8)
                .AddIngredient<PlasmaDriveCore>()
                .AddIngredient(ItemID.SpaceGun)
                .AddTile(TileID.Anvils)
                .AddDecraftCondition(Condition.DownedEowOrBoc)
                .Register();
        }
        #endregion

        #region ===== 复制、存档与网络同步 =====
        // ==================== 物品复制 / 存档 / 联机同步（完整保留） ====================
        public override ModItem Clone(Item item)
        {
            ModItem clone = base.Clone(item);

            if (clone is NewLegendSHPC newItem && item.ModItem is NewLegendSHPC oldItem)
            {
                oldItem.SanitizeAllMagazineSlots();
                newItem.selectedMagazineIndex = oldItem.CurrentMagazineIndex;
                for (int i = 0; i < MagazineCount; i++)
                {
                    newItem.magazineEffectPowers[i] = oldItem.magazineEffectPowers[i];
                    newItem.magazineAmmoTypes[i] = oldItem.magazineAmmoTypes[i];
                    newItem.magazineEffectIDs[i] = oldItem.magazineEffectIDs[i];
                    newItem.magazineReserve[i] = oldItem.magazineReserve[i];
                }
                newItem.SanitizeAllMagazineSlots();
            }

            return clone;
        }

        public override void SaveData(TagCompound tag)
        {
            SanitizeAllMagazineSlots();
            tag["selectedMagazineIndex"] = CurrentMagazineIndex;
            for (int i = 0; i < MagazineCount; i++)
            {
                tag[$"magazineEffectPower{i}"] = magazineEffectPowers[i];
                tag[$"magazineAmmoType{i}"] = magazineAmmoTypes[i];
                tag[$"magazineEffectID{i}"] = magazineEffectIDs[i];
                tag[$"magazineReserve{i}"] = magazineReserve[i];
            }
        }

        public override void LoadData(TagCompound tag)
        {
            selectedMagazineIndex = Utils.Clamp(tag.GetInt("selectedMagazineIndex"), 0, MagazineCount - 1);

            bool hasMagazineData = tag.ContainsKey("magazineEffectPower0");
            if (hasMagazineData)
            {
                for (int i = 0; i < MagazineCount; i++)
                {
                    magazineEffectPowers[i] = tag.GetInt($"magazineEffectPower{i}");
                    magazineAmmoTypes[i] = tag.GetInt($"magazineAmmoType{i}");
                    magazineEffectIDs[i] = tag.GetInt($"magazineEffectID{i}");
                    magazineReserve[i] = tag.GetInt($"magazineReserve{i}"); // 旧存档没有此键时默认 0
                }

                return;
            }

            magazineEffectPowers[CurrentMagazineIndex] = tag.GetInt("storedEffectPower");
            magazineAmmoTypes[CurrentMagazineIndex] = tag.GetInt("storedAmmoType");
            magazineEffectIDs[CurrentMagazineIndex] = tag.GetInt("storedEffectID");
            // 旧存档无外置储备，reserve 保持 0
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(CurrentMagazineIndex);
            for (int i = 0; i < MagazineCount; i++)
            {
                writer.Write(magazineEffectPowers[i]);
                writer.Write(magazineAmmoTypes[i]);
                writer.Write(magazineEffectIDs[i]);
                writer.Write(magazineReserve[i]);
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            selectedMagazineIndex = Utils.Clamp(reader.ReadInt32(), 0, MagazineCount - 1);
            for (int i = 0; i < MagazineCount; i++)
            {
                magazineEffectPowers[i] = reader.ReadInt32();
                magazineAmmoTypes[i] = reader.ReadInt32();
                magazineEffectIDs[i] = reader.ReadInt32();
                magazineReserve[i] = reader.ReadInt32();
            }
        }
        #endregion
        #endregion
    }
}












