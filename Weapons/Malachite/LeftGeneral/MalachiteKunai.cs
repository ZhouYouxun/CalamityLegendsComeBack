using CalamityLegendsComeBack.Accssory.MC.General;
using CalamityLegendsComeBack.Accssory.MC.PeacockDart;
using CalamityLegendsComeBack.Weapons.Malachite.LeftGeneral;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal enum MalachiteKunaiMode
    {
        NormalThrown = 0,
        StoredFrenzy = 1,
        StoredPeacock = 2,
        FiredFrenzy = 3,
        FiredPeacock = 4,
        ActivatedPeacock = 5,
        ActivatedAce = 6,
        StagedNormal = 7,
        StuckToNPC = 8
    }

    internal enum MalachiteKunaiVariant
    {
        Normal = 0,
        Frenzy = 1,
        Peacock = 2,
        Ace = 3
    }

    public class MalachiteKunai : ModProjectile, ILocalizedModType
    {
        private const int FrenzyCount = 3;
        private const int StagedNormalLaunchDelay = 10;
        private const float StoredLifeRefresh = 2f;
        private const float StagedNormalLaunchSpeed = 44f;
        private const float PeacockHomingTurnRate = MathHelper.Pi / 60f;
        private const int StoredPeacockOpenFrames = 12;

        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private MalachiteKunaiMode Mode
        {
            get => (MalachiteKunaiMode)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        private int SlotIndex => (int)Projectile.ai[1];

        private MalachiteKunaiVariant Variant
        {
            get => (MalachiteKunaiVariant)(int)Projectile.ai[2];
            set => Projectile.ai[2] = (float)value;
        }

        private bool IsStored => Mode == MalachiteKunaiMode.StoredFrenzy || Mode == MalachiteKunaiMode.StoredPeacock;

        private bool WasActivated
        {
            get => Projectile.localAI[2] == 1f;
            set => Projectile.localAI[2] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 360;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition()
        {
            if (IsStored)
                return false;

            if (Mode == MalachiteKunaiMode.StagedNormal && Projectile.localAI[0] <= StagedNormalLaunchDelay)
                return false;

            if (Mode == MalachiteKunaiMode.StuckToNPC)
                return false;

            if (Mode == MalachiteKunaiMode.ActivatedAce && Projectile.localAI[1] <= 0f)
                return false;

            return true;
        }

        public override bool? CanDamage()
        {
            if (IsStored || Mode == MalachiteKunaiMode.StuckToNPC)
                return false;

            if (Mode == MalachiteKunaiMode.StagedNormal && Projectile.localAI[0] <= StagedNormalLaunchDelay)
                return false;

            return null;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.alpha = Math.Max(0, Projectile.alpha - 24);
            Projectile.localAI[0]++;

            switch (Mode)
            {
                case MalachiteKunaiMode.StoredFrenzy:
                    AIStoredFrenzy(owner);
                    break;

                case MalachiteKunaiMode.StoredPeacock:
                    AIStoredPeacock(owner);
                    break;

                case MalachiteKunaiMode.FiredFrenzy:
                    AIFiredFrenzy();
                    break;

                case MalachiteKunaiMode.FiredPeacock:
                    AIFiredPeacock();
                    break;

                case MalachiteKunaiMode.ActivatedPeacock:
                    AIActivatedPeacock(owner);
                    break;

                case MalachiteKunaiMode.ActivatedAce:
                    AIActivatedAce(owner);
                    break;

                case MalachiteKunaiMode.StagedNormal:
                    AIStagedNormal(owner);
                    break;

                case MalachiteKunaiMode.StuckToNPC:
                    AIStuckToNPC();
                    break;

                default:
                    AINormalThrown(owner);
                    break;
            }

            ApplyPeacockDartGravity(owner);

            if (Projectile.velocity.LengthSquared() > 0.01f && !IsStored && Mode != MalachiteKunaiMode.StuckToNPC)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!IsStored && Mode != MalachiteKunaiMode.StuckToNPC && Variant == MalachiteKunaiVariant.Ace)
            {
                SpawnAceSpecialVFX();
            }

            SpawnMotionDust();
        }

        public static bool HasStoredKunai(Player player) => CountStoredKunai(player) > 0;

        public static bool HasFullStoredPeacockFan(Player player) => CountStoredPeacockKunai(player) >= MalachiteBalance.StealthKunaiCount;

        public static int CountStoredKunai(Player player)
        {
            int count = 0;
            int type = ModContent.ProjectileType<MalachiteKunai>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    MalachiteKunaiMode mode = (MalachiteKunaiMode)(int)projectile.ai[0];
                    if (mode == MalachiteKunaiMode.StoredPeacock)
                        count++;
                }
            }

            return count;
        }

        public static int CountStoredPeacockKunai(Player player)
        {
            int count = 0;
            int type = ModContent.ProjectileType<MalachiteKunai>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    if ((MalachiteKunaiMode)(int)projectile.ai[0] == MalachiteKunaiMode.StoredPeacock)
                        count++;
                }
            }

            return count;
        }

        public static int CountStoredFrenzyKunai(Player player)
        {
            int count = 0;
            int type = ModContent.ProjectileType<MalachiteKunai>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    if ((MalachiteKunaiMode)(int)projectile.ai[0] == MalachiteKunaiMode.StoredFrenzy)
                        count++;
                }
            }

            return count;
        }

        public static void PrepareForNewFrenzyFan(Player player, Vector2 mouseWorld)
        {
        }

        public static bool FireStoredPeacockKunaiAsLeftThrows(Player player, Vector2 mouseWorld, int damage = -1, float knockback = 0f)
        {
            bool firedAny = false;
            int type = ModContent.ProjectileType<MalachiteKunai>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    if ((MalachiteKunaiMode)(int)projectile.ai[0] == MalachiteKunaiMode.StoredPeacock)
                    {
                        Vector2 direction = (mouseWorld - projectile.Center).SafeNormalize(Vector2.UnitX * player.direction);
                        RefreshStoredKunaiStats(projectile, damage, knockback);
                        FireKunai(projectile, direction, MalachiteKunaiMode.FiredPeacock, leftThrow: true, stealthStrike: projectile.Calamity().stealthStrike);
                        firedAny = true;
                    }
                }
            }

            if (firedAny)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.28f }, player.Center);

            return firedAny;
        }

        public static bool TryThrowStoredKunai(Player player, Vector2 mouseWorld, int damage = -1, float knockback = 0f)
        {
            int throwCount = Math.Max(1, MalachiteBalance.StoredPeacockKunaiPerLeftClick);
            bool firedAny = false;

            for (int i = 0; i < throwCount; i++)
            {
                Projectile selected = FindNextStoredKunai(player, MalachiteKunaiMode.StoredPeacock);
                if (selected == null)
                    break;

                Vector2 direction = (mouseWorld - selected.Center).SafeNormalize(Vector2.UnitX * player.direction);
                RefreshStoredKunaiStats(selected, damage, knockback);
                FireKunai(selected, direction, MalachiteKunaiMode.FiredPeacock, leftThrow: true, stealthStrike: selected.Calamity().stealthStrike);
                firedAny = true;
            }

            if (!firedAny)
                return false;

            if (CountStoredKunai(player) <= 0)
                player.GetModPlayer<MalachitePlayer>().StartDepletionBurst();

            return true;
        }

        private static Projectile FindNextStoredKunai(Player player, MalachiteKunaiMode wantedMode)
        {
            Projectile selected = null;
            Projectile aceFallback = null;
            int selectedSlot = int.MaxValue;
            int aceSlot = int.MaxValue;
            int type = ModContent.ProjectileType<MalachiteKunai>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    if ((MalachiteKunaiMode)(int)projectile.ai[0] != wantedMode)
                        continue;

                    int slot = (int)projectile.ai[1];
                    if ((MalachiteKunaiVariant)(int)projectile.ai[2] == MalachiteKunaiVariant.Ace)
                    {
                        if (slot < aceSlot)
                        {
                            aceSlot = slot;
                            aceFallback = projectile;
                        }

                        continue;
                    }

                    if (slot < selectedSlot)
                    {
                        selectedSlot = slot;
                        selected = projectile;
                    }
                }
            }

            return selected ?? aceFallback;
        }

        public static void SpawnNormalLeftClickVolley(
            Player player,
            IEntitySource source,
            int damage,
            float knockback,
            Vector2 mouseWorld,
            int count,
            bool depletionBurst,
            int launchDelayFrames = 0)
        {
            count = Utils.Clamp(count, 1, MalachiteBalance.DepletionBurstKunaiCount);
            Vector2 aimDirection = (mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            float speedMultiplier = player.GetModPlayer<MCGeneralPlayer>().ProjectileSpeedMult;
            float curve = Main.rand.NextFloat(-1f, 1f);
            if (MathF.Abs(curve) < 0.25f)
                curve += 0.35f * MathF.Sign(curve == 0f ? player.direction : curve);

            float damageFactor = depletionBurst ? 0.78f : 1f;
            for (int i = 0; i < count; i++)
            {
                Projectile projectile = Projectile.NewProjectileDirect(
                    source,
                    player.MountedCenter,
                    aimDirection * StagedNormalLaunchSpeed * speedMultiplier,
                    ModContent.ProjectileType<MalachiteKunai>(),
                    Math.Max(1, (int)(damage * damageFactor)),
                    knockback,
                    player.whoAmI,
                    (float)MalachiteKunaiMode.StagedNormal,
                    count * 10 + i,
                    curve);

                projectile.Calamity().stealthStrike = false;
                projectile.alpha = 150;
                projectile.friendly = false;
                projectile.tileCollide = false;
                projectile.extraUpdates = 0;
                projectile.penetrate = 1;
                projectile.localAI[0] = -Math.Max(0, launchDelayFrames);
                projectile.localAI[1] = depletionBurst ? 2f : 0f;
                projectile.netUpdate = true;
            }
        }

        public static void SpawnFrenzyFan(Player player, IEntitySource source, int damage, float knockback)
        {
            MalachiteRightFeather.TrySpawnStoredRightFeather(player, source, damage, knockback);
        }

        public static void SpawnSingleFrenzyKunai(Player player, IEntitySource source, int damage, float knockback)
        {
            MalachiteRightFeather.TrySpawnStoredRightFeather(player, source, damage, knockback);
        }

        public static void SpawnPeacockFan(Player player, IEntitySource source, int damage, float knockback)
        {
            int count = MalachiteBalance.StealthKunaiCount;
            bool aceUnlocked = MalachiteBalance.StealthAceKunaiUnlocked;

            for (int i = 0; i < count; i++)
            {
                bool ace = aceUnlocked && i == count / 2;
                Projectile projectile = Projectile.NewProjectileDirect(
                    source,
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<MalachiteKunai>(),
                    (int)(damage * (ace ? 2.05f : 1.28f)),
                    knockback,
                    player.whoAmI,
                    (float)MalachiteKunaiMode.StoredPeacock,
                    i,
                    (float)(ace ? MalachiteKunaiVariant.Ace : MalachiteKunaiVariant.Peacock));

                projectile.Calamity().stealthStrike = true;
                projectile.alpha = 135;
                projectile.netUpdate = true;
            }
        }

        private static void ActivateStoredFrenzyKunai(Player player, Vector2 mouseWorld)
        {
            bool activatedAny = false;
            int type = ModContent.ProjectileType<MalachiteKunai>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    MalachiteKunaiMode mode = (MalachiteKunaiMode)(int)projectile.ai[0];
                    if (mode == MalachiteKunaiMode.StoredFrenzy)
                    {
                        Vector2 direction = (mouseWorld - projectile.Center).SafeNormalize(Vector2.UnitX * player.direction);
                        projectile.damage = (int)(projectile.damage * 1.85f);
                        FireKunai(projectile, direction, MalachiteKunaiMode.FiredFrenzy, leftThrow: false, stealthStrike: projectile.Calamity().stealthStrike);
                        projectile.penetrate = -1;
                        projectile.tileCollide = false;
                        projectile.localNPCHitCooldown = 4;
                        activatedAny = true;
                    }
                }
            }

            if (activatedAny)
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = 0.15f }, player.Center);
        }

        private static void KillStoredKunai(Player player, MalachiteKunaiMode wantedMode)
        {
            int type = ModContent.ProjectileType<MalachiteKunai>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    if ((MalachiteKunaiMode)(int)projectile.ai[0] == wantedMode)
                        projectile.Kill();
                }
            }
        }

        public static bool ActivateStoredKunaiAsAces(Player player, Vector2 mouseWorld)
        {
            bool activatedAny = false;
            int type = ModContent.ProjectileType<MalachiteKunai>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                {
                    MalachiteKunaiMode mode = (MalachiteKunaiMode)(int)projectile.ai[0];
                    if (mode != MalachiteKunaiMode.StoredFrenzy && mode != MalachiteKunaiMode.StoredPeacock)
                        continue;

                    projectile.damage = (int)(projectile.damage * 1.9f);
                    projectile.ai[0] = (float)MalachiteKunaiMode.ActivatedAce;
                    projectile.ai[1] = mouseWorld.X;
                    projectile.ai[2] = mouseWorld.Y;
                    projectile.localAI[0] = 0f;
                    projectile.localAI[1] = 0f;
                    projectile.localAI[2] = 1f;
                    projectile.friendly = true;
                    projectile.hostile = false;
                    projectile.penetrate = 1;
                    projectile.tileCollide = false;
                    projectile.timeLeft = 240;
                    projectile.extraUpdates = 1;
                    projectile.netUpdate = true;
                    activatedAny = true;
                }
            }

            if (activatedAny)
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.8f, Pitch = -0.05f }, player.Center);

            return activatedAny;
        }

        private static void RefreshStoredKunaiStats(Projectile projectile, int currentDamage, float currentKnockback)
        {
            if (currentDamage > 0)
            {
                bool isAce = (MalachiteKunaiVariant)(int)projectile.ai[2] == MalachiteKunaiVariant.Ace;
                projectile.damage = Math.Max(1, (int)(currentDamage * (isAce ? 2.05f : 1.28f)));
            }

            if (currentKnockback > 0f)
                projectile.knockBack = currentKnockback;
        }

        private static void FireKunai(Projectile projectile, Vector2 direction, MalachiteKunaiMode mode, bool leftThrow, bool stealthStrike)
        {
            projectile.ai[0] = (float)mode;
            projectile.localAI[0] = 0f;
            projectile.localAI[1] = 1f;
            projectile.localAI[2] = leftThrow ? 0f : 1f;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.timeLeft = mode == MalachiteKunaiMode.FiredFrenzy ? 180 : 240;
            projectile.extraUpdates = mode == MalachiteKunaiMode.FiredFrenzy && !leftThrow ? 2 : 1;
            
            bool isAce = (MalachiteKunaiVariant)(int)projectile.ai[2] == MalachiteKunaiVariant.Ace;
            projectile.tileCollide = isAce ? false : true;
            projectile.penetrate = isAce ? -1 : (leftThrow ? 1 : (mode == MalachiteKunaiMode.FiredFrenzy ? -1 : 1));
            projectile.localNPCHitCooldown = isAce ? 6 : (leftThrow ? 12 : (mode == MalachiteKunaiMode.FiredFrenzy ? 4 : 8));
            projectile.Calamity().stealthStrike = stealthStrike;
            Player owner = Main.player[projectile.owner];
            float speedMultiplier = owner.active ? owner.GetModPlayer<MCGeneralPlayer>().ProjectileSpeedMult : 1f;
            float baseSpeed = mode == MalachiteKunaiMode.FiredFrenzy ? 27f : 23f;
            if (isAce)
                baseSpeed *= 1.5f;

            projectile.velocity = direction * baseSpeed * speedMultiplier;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.netUpdate = true;
        }

        private void AIStoredFrenzy(Player owner)
        {
            if (!OwnerCanKeepStoredKunai(owner))
                return;

            Projectile.timeLeft = (int)StoredLifeRefresh;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;

            float open = GetStoredOpenInterpolant();
            float fanAngle = MathHelper.Lerp(-0.55f, 0.55f, SlotIndex / 2f) * open;
            Vector2 behind = new Vector2(-owner.direction, -0.18f).SafeNormalize(Vector2.UnitX * -owner.direction);
            Vector2 offset = behind.RotatedBy(fanAngle * owner.direction) * MathHelper.Lerp(10f, 58f, open) + Vector2.UnitY * MathHelper.Lerp(2f, -8f, open);
            Projectile.Center = Vector2.Lerp(Projectile.Center, owner.MountedCenter + offset, 0.35f);
            Projectile.rotation = offset.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(0.62f, 1f, open);
            SpawnStoredRevealDust(owner, open);
        }

        private void AIStoredPeacock(Player owner)
        {
            if (!OwnerCanKeepStoredKunai(owner))
                return;

            Projectile.timeLeft = (int)StoredLifeRefresh;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;

            int total = Math.Max(1, MalachiteBalance.StealthKunaiCount);
            int slot = Utils.Clamp(SlotIndex, 0, total - 1);
            float progress = total <= 1 ? 0.5f : slot / (float)(total - 1);
            float open = GetStoredPeacockOpenInterpolant();
            float spread = MathHelper.Lerp(-1.92f, 1.92f, progress);
            float crown = MathF.Sin(progress * MathHelper.Pi);
            Vector2 behind = new Vector2(-owner.direction, -0.2f).SafeNormalize(Vector2.UnitX * -owner.direction);
            Vector2 stackOffset = behind * 112f - Vector2.UnitY * 36f;
            Vector2 targetOffset = behind.RotatedBy(spread * owner.direction) * (76f + crown * 36f);
            targetOffset.Y -= 18f + crown * 18f;
            Vector2 offset = Vector2.Lerp(stackOffset, targetOffset, open);

            Projectile.Center = Vector2.Lerp(Projectile.Center, owner.MountedCenter + offset, 0.32f);
            Projectile.rotation = offset.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(0.58f, Variant == MalachiteKunaiVariant.Ace ? 1.18f : 0.86f + crown * 0.18f, open);
            SpawnStoredRevealDust(owner, open);
        }

        private float GetStoredOpenInterpolant()
        {
            float open = Utils.GetLerpValue(0f, 18f, Projectile.localAI[0], true);
            return open * open * (3f - 2f * open);
        }

        private float GetStoredPeacockOpenInterpolant()
        {
            float open = Utils.GetLerpValue(1f, StoredPeacockOpenFrames + 1f, Projectile.localAI[0], true);
            return 1f - MathF.Pow(1f - open, 3f);
        }

        private void SpawnStoredRevealDust(Player owner, float open)
        {
            if (open >= 1f || Projectile.localAI[0] > 18f || !Main.rand.NextBool(3))
                return;

            Vector2 velocity = (Projectile.Center - owner.MountedCenter).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.8f, 2.2f);
            Dust dust = Dust.NewDustPerfect(
                owner.MountedCenter + Main.rand.NextVector2Circular(10f, 12f),
                DustID.Terra,
                velocity,
                100,
                GetKunaiColor(),
                Main.rand.NextFloat(0.55f, 0.95f));
            dust.noGravity = true;
        }

        private bool OwnerCanKeepStoredKunai(Player owner)
        {
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return false;
            }

            return true;
        }

        private void AINormalThrown(Player owner)
        {
            Projectile.extraUpdates = 1;
            if (!MalachiteBalance.NormalKunaiIgnoresGravity(owner))
            {
                float gravityFade = Utils.GetLerpValue(110f, 20f, Projectile.localAI[0], true);
                Projectile.velocity.Y += 0.15f * gravityFade;
                Projectile.velocity *= 0.995f;
                return;
            }

            Projectile.velocity *= 1.001f;
            Lighting.AddLight(Projectile.Center, 0.04f, 0.28f, 0.08f);
        }

        private void ApplyPeacockDartGravity(Player owner)
        {
            if (!owner.active ||
                !owner.GetModPlayer<PeacockDartPlayer>().PeacockDartEquipped ||
                IsStored ||
                Mode == MalachiteKunaiMode.StuckToNPC)
            {
                return;
            }

            if (Mode == MalachiteKunaiMode.NormalThrown && !MalachiteBalance.NormalKunaiIgnoresGravity(owner))
                return;

            if (Mode == MalachiteKunaiMode.StagedNormal && Projectile.localAI[0] <= StagedNormalLaunchDelay)
                return;

            if (Mode == MalachiteKunaiMode.ActivatedAce && Projectile.localAI[1] <= 0f)
                return;

            Projectile.velocity.Y += 0.16f;
            Projectile.velocity.X *= 0.996f;
        }

        private void AIStagedNormal(Player owner)
        {
            Vector2 launchDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            int encoded = Math.Max(10, (int)Projectile.ai[1]);
            int count = Utils.Clamp(encoded / 10, 1, MalachiteBalance.DepletionBurstKunaiCount);
            int slot = Utils.Clamp(encoded % 10, 0, count - 1);

            if (Projectile.localAI[0] <= StagedNormalLaunchDelay)
            {
                Projectile.friendly = false;
                Projectile.tileCollide = false;
                Projectile.extraUpdates = 0;
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 90);

                float progress = count <= 1 ? 0.5f : slot / (float)(count - 1);
                float centered = slot - (count - 1) * 0.5f;
                float open = Utils.GetLerpValue(0f, StagedNormalLaunchDelay, Projectile.localAI[0], true);
                open = open * open * (3f - 2f * open);
                float curve = Projectile.ai[2];
                Vector2 side = launchDirection.RotatedBy(MathHelper.PiOver2);
                float spacing = Projectile.localAI[1] >= 2f ? 19f : 26f;
                float arcHeight = (Projectile.localAI[1] >= 2f ? 44f : 34f) * MathF.Sin(progress * MathHelper.Pi);
                Vector2 targetOffset =
                    launchDirection * MathHelper.Lerp(-10f, 38f + MathF.Abs(curve) * 14f, open) +
                    side * centered * spacing * open -
                    launchDirection * arcHeight * curve * open -
                    Vector2.UnitY * MathHelper.Lerp(0f, 18f + arcHeight * 0.22f, open);

                Projectile.Center = Vector2.Lerp(Projectile.Center, owner.MountedCenter + targetOffset, 0.48f);
                Projectile.rotation = launchDirection.ToRotation() + MathHelper.PiOver2;
                Projectile.scale = MathHelper.Lerp(0.62f, Projectile.localAI[1] >= 2f ? 1.08f : 0.98f, open);
                SpawnStagedNormalDust();
                return;
            }

            if (Projectile.localAI[0] == StagedNormalLaunchDelay + 1f)
            {
                Projectile.friendly = true;
                Projectile.tileCollide = true;
                Projectile.extraUpdates = 1;
                Projectile.penetrate = 1;
                Projectile.velocity = launchDirection * Math.Max(StagedNormalLaunchSpeed, Projectile.velocity.Length());
                Projectile.netUpdate = true;
            }

            Projectile.velocity *= 1.002f;
            Lighting.AddLight(Projectile.Center, 0.06f, 0.36f, 0.1f);
        }

        private void AIStuckToNPC()
        {
            int targetIndex = (int)Projectile.ai[1];
            if (!Main.npc.IndexInRange(targetIndex) || !Main.npc[targetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[targetIndex];
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.Center = target.Center + Projectile.velocity;
            Projectile.rotation = Projectile.ai[2];
            Projectile.alpha = (int)MathHelper.Lerp(35f, 215f, Utils.GetLerpValue(28f, 50f, Projectile.localAI[0], true));
            Projectile.scale *= 0.996f;
        }

        private void SpawnStagedNormalDust()
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                DustID.Terra,
                Main.rand.NextVector2Circular(0.8f, 0.8f),
                100,
                Projectile.localAI[1] >= 2f ? new Color(210, 255, 150) : new Color(105, 245, 125),
                Main.rand.NextFloat(0.58f, 0.9f));
            dust.noGravity = true;
        }

        private void AIFiredFrenzy()
        {
            Projectile.tileCollide = Projectile.penetrate != -1;
            Projectile.velocity *= 1.002f;
            Lighting.AddLight(Projectile.Center, 0.05f, 0.5f, 0.14f);
        }

        private void AIFiredPeacock()
        {
            Projectile.tileCollide = false;

            if (Variant == MalachiteKunaiVariant.Ace)
                return;

            if (Projectile.localAI[0] < 12f)
                return;

            NPC target = FindTarget(720f, requireLineOfSight: false);
            if (target == null)
                return;

            float baseTurnRate = Projectile.Calamity().stealthStrike ? PeacockHomingTurnRate * 2f : PeacockHomingTurnRate;
            float homingGrowth = Utils.GetLerpValue(0f, 80f, Projectile.localAI[0] - 12f, true);
            float turnRate = MathHelper.Lerp(baseTurnRate, baseTurnRate * 5f, homingGrowth);
            HomeTowardsWithTurnLimit(target.Center, 34f, turnRate);
        }

        private void AIActivatedPeacock(Player owner)
        {
            Projectile.tileCollide = false;

            if (Variant == MalachiteKunaiVariant.Ace)
                return;

            if (Projectile.localAI[0] < 32f)
            {
                Projectile.velocity *= 0.985f;
                return;
            }

            NPC target = FindTarget(1500f, requireLineOfSight: false);
            if (target != null)
            {
                float homingGrowth = Utils.GetLerpValue(0f, 80f, Projectile.localAI[0] - 32f, true);
                float turnRate = MathHelper.Lerp(PeacockHomingTurnRate * 2f, PeacockHomingTurnRate * 10f, homingGrowth);
                HomeTowardsWithTurnLimit(target.Center, 38f, turnRate);
                return;
            }

            Vector2 fallback = (GetMouseWorld(owner) - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, fallback * 22f, 0.08f);
        }

        private void AIActivatedAce(Player owner)
        {
            Vector2 target = new(Projectile.ai[1], Projectile.ai[2]);
            if (target == Vector2.Zero)
                target = GetMouseWorld(owner);

            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.scale = 1.35f;
            Projectile.penetrate = Variant == MalachiteKunaiVariant.Ace ? -1 : 1;

            if (Projectile.localAI[0] <= 46f)
            {
                float angle = Projectile.identity * 0.77f + Projectile.localAI[0] * 0.22f;
                float radius = MathHelper.Lerp(116f, 34f, Utils.GetLerpValue(0f, 46f, Projectile.localAI[0], true));
                Projectile.Center = target + angle.ToRotationVector2() * radius;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = (target - Projectile.Center).ToRotation() + MathHelper.PiOver2;
                return;
            }

            if (Projectile.localAI[1] <= 0f)
            {
                Projectile.localAI[1] = 1f;
                float speed = Variant == MalachiteKunaiVariant.Ace ? 51f : 34f;
                Projectile.velocity = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * speed;
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.75f, Pitch = 0.15f }, Projectile.Center);
                Projectile.netUpdate = true;
                return;
            }

            Projectile.velocity *= 1.006f;
        }

        private NPC FindTarget(float range, bool requireLineOfSight)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                if (requireLineOfSight && !Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void HomeTowardsWithTurnLimit(Vector2 target, float speed, float maxTurn)
        {
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target - Projectile.Center).SafeNormalize(currentDirection);
            float newRotation = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn);
            Projectile.velocity = newRotation.ToRotationVector2() * MathHelper.Lerp(Projectile.velocity.Length(), speed, 0.18f);

            if (Projectile.velocity.Length() > speed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * speed;
        }

        private void SpawnMotionDust()
        {
            if (IsStored || Mode == MalachiteKunaiMode.StuckToNPC)
                return;

            if (Variant == MalachiteKunaiVariant.Ace)
                return;

            if (UsesOriginalMalachiteTrail)
                SpawnOriginalMalachiteGlowDust();

            SpawnPlagueFlightVisuals();

            if ((Mode == MalachiteKunaiMode.FiredPeacock || Mode == MalachiteKunaiMode.ActivatedPeacock) && Main.rand.NextBool(2))
            {
                Vector2 side = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                Dust arcDust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.36f + side * Main.rand.NextFloat(-12f, 12f),
                    DustID.Terra,
                    -Projectile.velocity * 0.025f + side * Main.rand.NextFloat(-1.4f, 1.4f),
                    80,
                    new Color(72, 255, 145),
                    Main.rand.NextFloat(0.72f, 1.05f));
                arcDust.noGravity = true;
            }

            if (Main.rand.NextBool(3))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 8f,
                DustID.Terra,
                -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                100,
                IsAceVisual ? new Color(210, 255, 155) : new Color(92, 245, 124),
                IsAceVisual ? 1.25f : 0.9f);
                dust.noGravity = true;
        }

        private void SpawnPlagueFlightVisuals()
        {
            Lighting.AddLight(Projectile.Center, 0.07f, 0.15f, 0.01f);

            if (Main.rand.NextBool(3))
            {
                Dust toxicDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    Main.rand.NextBool(30) ? DustID.Terra : DustID.GreenTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.015f, 0.045f) + Main.rand.NextVector2Circular(0.55f, 0.55f),
                    120,
                    default,
                    Main.rand.NextBool(30) ? Main.rand.NextFloat(0.9f, 1.05f) : Main.rand.NextFloat(0.3f, 0.42f));
                toxicDust.noGravity = true;
            }

            if (Projectile.localAI[0] % 18f != 0f)
                return;

            Color ringColor = Main.rand.NextBool(3) ? Color.LimeGreen : Color.Green;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                ringColor * 0.42f,
                Vector2.One,
                Projectile.rotation,
                Main.rand.NextFloat(0.045f, 0.09f),
                0f,
                15));
        }

        private void SpawnOriginalMalachiteGlowDust()
        {
            if (Projectile.localAI[0] <= 4f)
                return;

            int dustCount = Mode == MalachiteKunaiMode.NormalThrown ? 2 : 1;
            for (int i = 0; i < dustCount; i++)
            {
                int dustIndex = Dust.NewDust(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Terra,
                    0f,
                    0f,
                    100,
                    GetOriginalMalachiteGlowColor(255),
                    0.75f);

                Dust dust = Main.dust[dustIndex];
                dust.noGravity = true;
                dust.velocity *= 0f;
            }
        }

        private void SpawnAceSpecialVFX()
        {
            float time = Projectile.localAI[0];
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.Zero);
            if (forward == Vector2.Zero)
                return;

            Vector2 perpendicular = forward.RotatedBy(MathHelper.PiOver2);

            // 1. Double Helix Math Trail (Terra green)
            float angle = time * 0.35f;
            float radius = 18f * MathF.Sin(time * 0.08f);
            for (int i = 0; i < 2; i++)
            {
                float offsetAngle = angle + i * MathHelper.Pi;
                Vector2 offset = perpendicular * MathF.Sin(offsetAngle) * radius + forward * MathF.Cos(offsetAngle) * 6f;
                Vector2 spawnPos = Projectile.Center + offset;

                Dust d = Dust.NewDustPerfect(
                    spawnPos,
                    DustID.Terra,
                    Projectile.velocity * 0.05f,
                    100,
                    new Color(90, 255, 140),
                    1.15f
                );
                d.noGravity = true;
            }

            // 2. Diffusive ring of light every 8 frames
            if (time % 8 == 0)
            {
                int ringCount = 16;
                for (int i = 0; i < ringCount; i++)
                {
                    float ringAngle = i * MathHelper.TwoPi / ringCount;
                    Vector2 vel = ringAngle.ToRotationVector2() * 3.2f;
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.Terra,
                        vel + Projectile.velocity * 0.12f,
                        110,
                        new Color(180, 255, 110),
                        0.9f
                    );
                    d.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 6 * 60);
            target.AddBuff(ModContent.BuffType<Plague>(), 4 * 60);
            if (ShouldUseLeftKunaiHitVisual)
                SpawnLeftKunaiHitVisual(target);

            if (Mode == MalachiteKunaiMode.ActivatedAce || HasAceVariant)
                SpawnMiniExplosion();

            if (ShouldStickOnHit)
                StickToTarget(target);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
                return;

            int kunaiArmorPen = owner.GetModPlayer<MCGeneralPlayer>().KunaiArmorPen;
            if (kunaiArmorPen > 0)
                modifiers.ArmorPenetration += kunaiArmorPen;

            if (owner.GetModPlayer<PeacockDartPlayer>().PeacockDartEquipped)
                modifiers.SourceDamage *= 1.3f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (IsStored || Mode == MalachiteKunaiMode.StuckToNPC)
                return false;

            float collisionPoint = 0f;
            Vector2 start = Projectile.Center - Projectile.velocity;
            Vector2 end = Projectile.Center;
            float width = 18f * Projectile.scale;

            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref collisionPoint))
            {
                return true;
            }

            return null;
        }

        public override void OnKill(int timeLeft)
        {
            if (Mode == MalachiteKunaiMode.StuckToNPC)
                return;

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Terra,
                    Main.rand.NextVector2Circular(3.5f, 3.5f),
                    90,
                IsAceVisual ? new Color(230, 255, 165) : new Color(70, 230, 110),
                    Main.rand.NextFloat(0.75f, 1.35f));
                dust.noGravity = Main.rand.NextBool();
            }
        }

        private bool ShouldUseLeftKunaiHitVisual =>
            !WasActivated &&
            (Mode == MalachiteKunaiMode.NormalThrown ||
            Mode == MalachiteKunaiMode.StagedNormal ||
            Mode == MalachiteKunaiMode.FiredFrenzy ||
            Mode == MalachiteKunaiMode.FiredPeacock);

        private bool ShouldStickOnHit =>
            !WasActivated &&
            Variant != MalachiteKunaiVariant.Ace &&
            (Mode == MalachiteKunaiMode.NormalThrown ||
            Mode == MalachiteKunaiMode.StagedNormal ||
            Mode == MalachiteKunaiMode.FiredFrenzy ||
            Mode == MalachiteKunaiMode.FiredPeacock);

        private void SpawnLeftKunaiHitVisual(NPC target)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize((target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX));
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                direction,
                ModContent.ProjectileType<MalachiteHitCutVisual>(),
                0,
                0f,
                Projectile.owner,
                direction.ToRotation(),
                Projectile.localAI[1] >= 2f ? 1f : 0f);
        }

        private void StickToTarget(NPC target)
        {
            MalachiteKunaiVariant storedVariant = Variant;
            Vector2 offset = Projectile.Center - target.Center;
            float maxOffset = Math.Max(target.width, target.height) * 0.46f + 12f;
            if (offset.Length() > maxOffset)
                offset = offset.SafeNormalize(Vector2.Zero) * maxOffset;

            Projectile.ai[0] = (float)MalachiteKunaiMode.StuckToNPC;
            Projectile.ai[1] = target.whoAmI;
            Projectile.ai[2] = Projectile.rotation;
            Projectile.localAI[0] = 0f;
            Projectile.localAI[1] = (float)storedVariant;
            Projectile.localAI[2] = 0f;
            Projectile.velocity = offset;
            Projectile.damage = 0;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 50;
            Projectile.netUpdate = true;
        }

        private void SpawnMiniExplosion()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MalachiteGreenExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 0.65f)),
                0f,
                Projectile.owner,
                0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (!IsStored && Mode != MalachiteKunaiMode.StuckToNPC)
            {
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            }

            Color drawColor = Color.Lerp(lightColor, GetKunaiColor(), 0.75f);
            if (UsesOriginalMalachiteTrail)
                drawColor = Color.Lerp(drawColor, GetOriginalMalachiteGlowColor(Projectile.alpha), 0.18f);

            if (IsStored)
                drawColor *= 0.82f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + SlotIndex);

            if (UsesOriginalMalachiteTrail)
            {
                float glowPulse = 1.15f + MathF.Sin(Main.GlobalTimeWrappedHourly * 10f + Projectile.identity) * 0.07f;
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, GetOriginalMalachiteGlowColor(90) * 0.38f, Projectile.rotation, origin, Projectile.scale * glowPulse, effects);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, drawColor, Projectile.rotation, origin, Projectile.scale, effects);

            if (IsAceVisual)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, new Color(190, 255, 100, 0) * 0.5f, Projectile.rotation, origin, Projectile.scale * 1.45f, effects);
            }

            return false;
        }

        private void DrawStoredTintLayers(Texture2D texture, Rectangle frame, Vector2 origin, SpriteEffects effects)
        {
            Color kunaiColor = GetKunaiColor();
            float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4.2f + SlotIndex * 0.53f);
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            Vector2 side = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2().RotatedBy(MathHelper.PiOver2);
            float stackOffset = Variant == MalachiteKunaiVariant.Peacock ? 2.4f : 1.4f;

            Main.EntitySpriteDraw(
                texture,
                drawCenter,
                frame,
                kunaiColor * (0.16f + pulse * 0.06f),
                Projectile.rotation,
                origin,
                Projectile.scale * 1.36f,
                effects);

            Main.EntitySpriteDraw(
                texture,
                drawCenter + side * stackOffset,
                frame,
                GetOriginalMalachiteGlowColor(100) * 0.24f,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.13f,
                effects);

            Main.EntitySpriteDraw(
                texture,
                drawCenter - side * stackOffset,
                frame,
                new Color(35, 255, 165, 0) * (0.18f + pulse * 0.08f),
                Projectile.rotation,
                origin,
                Projectile.scale * 1.06f,
                effects);
        }



        public override Color? GetAlpha(Color lightColor) => GetKunaiColor();

        private Color GetKunaiColor()
        {
            if (IsAceVisual)
                return new Color(220, 255, 130);

            MalachiteKunaiVariant variant = Mode == MalachiteKunaiMode.StuckToNPC
                ? (MalachiteKunaiVariant)(int)Projectile.localAI[1]
                : Variant;

            return variant switch
            {
                MalachiteKunaiVariant.Frenzy => new Color(90, 255, 145),
                MalachiteKunaiVariant.Peacock => new Color(70, 230, 125),
                _ => new Color(115, 245, 125)
            };
        }

        private float GetTrailRotation(int oldPositionIndex)
        {
            if (!IsStored)
                return Projectile.rotation;

            float oldRotation = Projectile.oldRot[oldPositionIndex];
            return oldRotation == 0f ? Projectile.rotation : oldRotation;
        }

        private static Color GetOriginalMalachiteGlowColor(int alpha) => new(Main.DiscoR, 203, 103, alpha);

        private bool IsAceVisual =>
            Mode == MalachiteKunaiMode.ActivatedAce ||
            (Mode == MalachiteKunaiMode.StuckToNPC && (MalachiteKunaiVariant)(int)Projectile.localAI[1] == MalachiteKunaiVariant.Ace) ||
            HasAceVariant;

        private bool HasAceVariant =>
            Mode != MalachiteKunaiMode.StagedNormal &&
            Mode != MalachiteKunaiMode.StuckToNPC &&
            Variant == MalachiteKunaiVariant.Ace;

        private bool UsesOriginalMalachiteTrail =>
            Mode == MalachiteKunaiMode.NormalThrown ||
            (Mode == MalachiteKunaiMode.StagedNormal && Projectile.localAI[0] > StagedNormalLaunchDelay) ||
            (!WasActivated && (Mode == MalachiteKunaiMode.FiredFrenzy || Mode == MalachiteKunaiMode.FiredPeacock));

        private bool IsPeacockOrAceKunai =>
            Mode == MalachiteKunaiMode.FiredPeacock ||
            Mode == MalachiteKunaiMode.ActivatedPeacock ||
            Mode == MalachiteKunaiMode.ActivatedAce ||
            Variant == MalachiteKunaiVariant.Peacock ||
            Variant == MalachiteKunaiVariant.Ace;

        private static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }

  
}

