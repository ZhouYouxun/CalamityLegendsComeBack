using CalamityLegendsComeBack.Accssory;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    public class SeasSearing : ModItem, ILocalizedModType
    {
        private static int HoldoutType => ModContent.ProjectileType<SeasSearingHoldout>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SeasSearing/NewLegendSeasSearing";

        // Stage 0 = pre-hardmode, 1 = hardmode, 2 = post-Plantera, 3 = post-Moonlord
        public static int GetProgressionStage()
        {
            if (NPC.downedMoonlord) return 3;
            if (NPC.downedPlantBoss) return 2;
            if (Main.hardMode) return 1;
            return 0;
        }

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 74;
            Item.height = 34;
            Item.damage = 0;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 5f;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = 34f;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
        }

        public override bool AltFunctionUse(Player player) => true;

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = SS_Balance.GetBaseDamage();
        }

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override bool ConsumeItem(Player player) => false;

        public override void HoldItem(Player player)
        {
            SeasSearingPlayer ssPlayer = player.GetModPlayer<SeasSearingPlayer>();
            ssPlayer.SetHoldingSeasSearing();

            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (Main.myPlayer != player.whoAmI || HasActiveHoldout(player))
                return;

            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int holdoutIndex = Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.MountedCenter,
                aimDirection,
                HoldoutType,
                player.GetWeaponDamage(Item),
                Item.knockBack,
                player.whoAmI);

            if (Main.projectile.IndexInRange(holdoutIndex))
                Main.projectile[holdoutIndex].CritChance = player.GetWeaponCrit(Item);
        }

        public override void UpdateInventory(Player player)
        {
            Item.noUseGraphic = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string intro = this.GetLocalizedValue("Intro");
            string left = this.GetLocalizedValue("LeftClick");
            string right = this.GetLocalizedValue("RightClick");
            string passive = this.GetLocalizedValue("Passive");
            string ultimate = string.Format(this.GetLocalizedValue("Ultimate"), keyText);
            string legendarySection = Main.keyState.PressingShift()
                ? this.GetLocalizedValue("LegendaryText")
                : this.GetLocalizedValue("LegendaryHint");

            string finalText =
                intro + "\n\n" +
                left + "\n" +
                right + "\n" +
                passive + "\n" +
                ultimate + "\n\n" +
                legendarySection + "\n";

            tooltips.FindAndReplace("[GFB]", finalText);
        }

        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity) ||
                !calamity.TryFind("SeasSearing", out ModItem originalSeaSearing))
            {
                return;
            }

            CreateRecipe()
                .AddIngredient(originalSeaSearing.Type)
                .AddIngredient<DepthCells>(25)
                .AddIngredient<Lumenyl>(18)
                .AddIngredient<InfectedArmorPlating>(12)
                .AddIngredient(ItemID.IllegalGunParts)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        internal static bool CanUseWorldInput(Player player)
        {
            if (player.noItems || player.CCed || Main.mapFullscreen || player.mouseInterface)
                return false;

            if (Main.blockMouse)
                return false;

            if (Main.playerInventory && !Main.HoverItem.IsAir)
                return false;

            return true;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        private static bool HasActiveHoldout(Player player)
        {
            int holdoutType = HoldoutType;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == holdoutType)
                    return true;
            }

            return false;
        }
    }

    public sealed class SeasSearingPlayer : ModPlayer
    {
        public const int UltimateCooldownFrames = 75 * 60;
        private const float BasePressureRadius = 430f;
        private const float MaxPressureRadiusBonus = 170f;

        public bool HoldingSeasSearing { get; private set; }
        public int UltimateCooldown { get; private set; }
        public float PressureVisualPower { get; private set; }

        public override void ResetEffects()
        {
            HoldingSeasSearing = false;
        }

        public override void PostUpdate()
        {
            if (UltimateCooldown > 0)
                UltimateCooldown--;

            if (!HoldingSeasSearing)
            {
                PressureVisualPower = MathHelper.Clamp(PressureVisualPower - 0.04f, 0f, 1f);
                return;
            }

            int totalPollution = SeasSearingPollutionNPC.CountPollutionForOwner(Player.whoAmI);
            float pollutionFactor = MathHelper.Clamp(totalPollution / 200f, 0f, 1f);
            PressureVisualPower = MathHelper.Lerp(PressureVisualPower, 0.32f + pollutionFactor * 0.68f, 0.08f);
            ApplyPressureField(pollutionFactor);
            EmitPressureAtmosphere(pollutionFactor);
        }

        public void SetHoldingSeasSearing()
        {
            HoldingSeasSearing = true;
        }

        public bool CanUseUltimate => UltimateCooldown <= 0;

        public void StartUltimateCooldown()
        {
            UltimateCooldown = UltimateCooldownFrames;
        }

        private void ApplyPressureField(float pollutionFactor)
        {
            float radius = BasePressureRadius + MaxPressureRadiusBonus * pollutionFactor;
            float radiusSquared = radius * radius;
            int owner = Player.whoAmI;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;

                float distanceSquared = Vector2.DistanceSquared(Player.Center, npc.Center);
                if (distanceSquared > radiusSquared)
                    continue;

                float proximity = 1f - MathHelper.Clamp((float)Math.Sqrt(distanceSquared) / radius, 0f, 1f);
                float slowPower = MathHelper.Lerp(0.035f, 0.13f, proximity) * MathHelper.Lerp(0.6f, 1.25f, pollutionFactor);
                if (npc.knockBackResist <= 0f || npc.boss)
                    slowPower *= 0.38f;

                npc.position -= npc.velocity * slowPower;
                npc.velocity *= 1f - slowPower * 0.35f;

                SeasSearingPollutionNPC pollution = npc.GetGlobalNPC<SeasSearingPollutionNPC>();
                pollution.ExposeToPressure(npc, owner, proximity);
            }
        }

        private void EmitPressureAtmosphere(float pollutionFactor)
        {
            if (Main.dedServ)
                return;

            Lighting.AddLight(Player.Center, new Vector3(0.03f, 0.16f, 0.22f) * (0.8f + pollutionFactor));

            int interval = pollutionFactor > 0.6f ? 4 : 7;
            if (Main.GameUpdateCount % interval != 0)
                return;

            float radius = BasePressureRadius * Main.rand.NextFloat(0.58f, 1.04f);
            Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius * Main.rand.NextFloat(0.5f, 0.95f));
            Vector2 position = Player.Center + offset;
            Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.35f, 1.1f);
            Color color = Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat(0.12f, 0.75f));

            Dust dust = Dust.NewDustPerfect(
                position,
                Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                velocity,
                135,
                color,
                Main.rand.NextFloat(0.55f, 1.05f));
            dust.noGravity = true;
        }
    }

    internal static class SeasSearingPalette
    {
        public static readonly Color AbyssBlack = new(4, 9, 18);
        public static readonly Color DeepBlue = new(20, 72, 122);
        public static readonly Color PressureBlue = new(34, 120, 185);
        public static readonly Color RadioactiveCyan = new(88, 255, 218);
        public static readonly Color ToxicGreen = new(68, 210, 104);
        public static readonly Color BiohazardLime = new(140, 255, 60);
        public static readonly Color FalloutAsh = new(90, 112, 120);
        public static readonly Color WarningOrange = new(255, 132, 48);

        public static Color PollutionColor(float completion)
        {
            if (completion < 0.45f)
                return Color.Lerp(DeepBlue, RadioactiveCyan, completion / 0.45f);

            return Color.Lerp(RadioactiveCyan, ToxicGreen, (completion - 0.45f) / 0.55f);
        }

        public static Color GradeColor(int grade) => grade switch
        {
            1 => PressureBlue,
            2 => RadioactiveCyan,
            3 => ToxicGreen,
            4 => BiohazardLime,
            5 => new Color(210, 255, 140),
            _ => DeepBlue
        };
    }

    internal static class SeasSearingVisualUtility
    {
        public static void ShakeAt(Vector2 center, float power, float range = 1600f)
        {
            if (Main.dedServ)
                return;

            Player player = Main.LocalPlayer;
            float distanceFactor = 1f - MathHelper.Clamp(Vector2.Distance(player.Center, center) / range, 0f, 1f);
            if (distanceFactor <= 0f)
                return;

            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public static void SpawnAbyssDust(Vector2 center, int count, float speed, float radius, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Vector2 velocity = offset.SafeNormalize(Main.rand.NextVector2CircularEdge(1f, 1f)) * Main.rand.NextFloat(speed * 0.25f, speed);
                Color color = SeasSearingPalette.PollutionColor(Main.rand.NextFloat());
                Dust dust = Dust.NewDustPerfect(
                    center + offset,
                    Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                    velocity,
                    125,
                    color,
                    Main.rand.NextFloat(0.55f, 1.15f) * scale);
                dust.noGravity = true;
                dust.fadeIn = scale;
            }
        }

        public static void SpawnPressureRing(Vector2 center, float speed, float radius, int count, Color color)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / count).ToRotationVector2() * speed;
                Dust dust = Dust.NewDustPerfect(
                    center + velocity.SafeNormalize(Vector2.UnitY) * radius,
                    DustID.GemDiamond,
                    velocity,
                    110,
                    color,
                    Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        public static void SpawnGradeBurst(Vector2 center, int grade, int count)
        {
            if (Main.dedServ)
                return;

            Color baseColor = SeasSearingPalette.GradeColor(grade);
            for (int i = 0; i < count; i++)
            {
                int dustType = grade >= 4 ? DustID.Vortex : (grade >= 3 ? 89 : (Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald));
                Vector2 velocity = Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1.5f + grade * 0.8f, 3f + grade * 1.4f);
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    dustType, velocity, 100,
                    Color.Lerp(baseColor, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.6f, 1.1f + grade * 0.1f));
                d.noGravity = true;
            }
        }

        public static void PlayDeepShot(Vector2 position, float pitch = 0f)
        {
            SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.74f, Pitch = -0.18f + pitch, PitchVariance = 0.08f, MaxInstances = 6 }, position);
            SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.34f, Pitch = -0.45f + pitch, PitchVariance = 0.05f, MaxInstances = 6 }, position);
        }
    }
}
