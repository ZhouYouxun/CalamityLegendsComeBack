using CalamityLegendsComeBack.Accssory.MC;
using CalamityMod;
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
        private const int PeacockCount = 23;
        private const int FrenzyCount = 3;
        private const int StagedNormalLaunchDelay = 10;
        private const float StoredLifeRefresh = 2f;
        private const float StagedNormalLaunchSpeed = 44f;
        private const float PeacockHomingTurnRate = MathHelper.Pi / 60f;

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
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 0;
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

            if (WasActivated && owner.active && owner.GetModPlayer<MalachiteAccessoryPlayer>().PrecisionEmblemEquipped)
                Projectile.ArmorPenetration = Math.Max(Projectile.ArmorPenetration, 10);

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
                    AINormalThrown();
                    break;
            }

            if (Projectile.velocity.LengthSquared() > 0.01f && !IsStored && Mode != MalachiteKunaiMode.StuckToNPC)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            SpawnMotionDust();
        }

        public static bool HasStoredKunai(Player player) => CountStoredKunai(player) > 0;

        public static bool HasFullStoredPeacockFan(Player player) => CountStoredPeacockKunai(player) >= PeacockCount;

        public static int CountStoredKunai(Player player)
        {
            int count = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

                MalachiteKunaiMode mode = (MalachiteKunaiMode)(int)projectile.ai[0];
                if (mode == MalachiteKunaiMode.StoredFrenzy || mode == MalachiteKunaiMode.StoredPeacock)
                    count++;
            }

            return count;
        }

        public static int CountStoredPeacockKunai(Player player)
        {
            int count = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

                if ((MalachiteKunaiMode)(int)projectile.ai[0] == MalachiteKunaiMode.StoredPeacock)
                    count++;
            }

            return count;
        }

        public static int CountStoredFrenzyKunai(Player player)
        {
            int count = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

                if ((MalachiteKunaiMode)(int)projectile.ai[0] == MalachiteKunaiMode.StoredFrenzy)
                    count++;
            }

            return count;
        }

        public static void PrepareForNewFrenzyFan(Player player, Vector2 mouseWorld)
        {
            int storedCount = CountStoredFrenzyKunai(player);
            if (storedCount >= FrenzyCount)
            {
                ActivateStoredFrenzyKunai(player, mouseWorld);
                return;
            }

            KillStoredKunai(player, MalachiteKunaiMode.StoredFrenzy);
        }

        public static bool FireStoredPeacockKunaiAsLeftThrows(Player player, Vector2 mouseWorld)
        {
            bool firedAny = false;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

                if ((MalachiteKunaiMode)(int)projectile.ai[0] != MalachiteKunaiMode.StoredPeacock)
                    continue;

                Vector2 direction = (mouseWorld - projectile.Center).SafeNormalize(Vector2.UnitX * player.direction);
                FireKunai(projectile, direction, MalachiteKunaiMode.FiredPeacock, leftThrow: true);
                firedAny = true;
            }

            if (firedAny)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.28f }, player.Center);

            return firedAny;
        }

        public static bool TryThrowStoredKunai(Player player, Vector2 mouseWorld)
        {
            Projectile selected = FindNextStoredKunai(player, MalachiteKunaiMode.StoredPeacock) ??
                FindNextStoredKunai(player, MalachiteKunaiMode.StoredFrenzy);
            if (selected == null)
                return false;

            Vector2 direction = (mouseWorld - selected.Center).SafeNormalize(Vector2.UnitX * player.direction);
            MalachiteKunaiMode modeToFire = (MalachiteKunaiMode)(int)selected.ai[0] == MalachiteKunaiMode.StoredFrenzy
                ? MalachiteKunaiMode.FiredFrenzy
                : MalachiteKunaiMode.FiredPeacock;

            FireKunai(selected, direction, modeToFire, leftThrow: true);
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

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

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

            return selected ?? aceFallback;
        }

        public static void SpawnNormalLeftClickVolley(
            Player player,
            IEntitySource source,
            int damage,
            float knockback,
            Vector2 mouseWorld,
            int count,
            bool depletionBurst)
        {
            count = Utils.Clamp(count, 1, MalachiteProgression.DepletionBurstKunaiCount);
            Vector2 aimDirection = (mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            float speedMultiplier = player.GetModPlayer<MalachiteAccessoryPlayer>().MalachiteProjectileVelocityMultiplier;
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
                projectile.localAI[1] = depletionBurst ? 2f : 0f;
                projectile.netUpdate = true;
            }
        }

        public static void SpawnFrenzyFan(Player player, IEntitySource source, int damage, float knockback)
        {
            for (int i = 0; i < FrenzyCount; i++)
            {
                Projectile projectile = Projectile.NewProjectileDirect(
                    source,
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<MalachiteKunai>(),
                    (int)(damage * 1.55f),
                    knockback,
                    player.whoAmI,
                    (float)MalachiteKunaiMode.StoredFrenzy,
                    i,
                    (float)MalachiteKunaiVariant.Frenzy);

                projectile.Calamity().stealthStrike = false;
                projectile.alpha = 120;
                projectile.netUpdate = true;
            }
        }

        public static void SpawnSingleFrenzyKunai(Player player, IEntitySource source, int damage, float knockback)
        {
            Projectile projectile = Projectile.NewProjectileDirect(
                source,
                player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MalachiteKunai>(),
                (int)(damage * 1.55f),
                knockback,
                player.whoAmI,
                (float)MalachiteKunaiMode.StoredFrenzy,
                FrenzyCount / 2,
                (float)MalachiteKunaiVariant.Frenzy);

            projectile.Calamity().stealthStrike = false;
            projectile.alpha = 120;
            projectile.netUpdate = true;
        }

        public static void SpawnPeacockFan(Player player, IEntitySource source, int damage, float knockback)
        {
            for (int i = 0; i < PeacockCount; i++)
            {
                bool ace = i == PeacockCount / 2;
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

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

                MalachiteKunaiMode mode = (MalachiteKunaiMode)(int)projectile.ai[0];
                if (mode == MalachiteKunaiMode.StoredFrenzy)
                {
                    Vector2 direction = (mouseWorld - projectile.Center).SafeNormalize(Vector2.UnitX * player.direction);
                    projectile.damage = (int)(projectile.damage * 1.85f);
                    FireKunai(projectile, direction, MalachiteKunaiMode.FiredFrenzy, leftThrow: false);
                    projectile.penetrate = -1;
                    projectile.tileCollide = false;
                    projectile.localNPCHitCooldown = 4;
                    activatedAny = true;
                }
            }

            if (activatedAny)
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = 0.15f }, player.Center);
        }

        private static void KillStoredKunai(Player player, MalachiteKunaiMode wantedMode)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

                if ((MalachiteKunaiMode)(int)projectile.ai[0] == wantedMode)
                    projectile.Kill();
            }
        }

        public static bool ActivateStoredKunaiAsAces(Player player, Vector2 mouseWorld)
        {
            bool activatedAny = false;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != ModContent.ProjectileType<MalachiteKunai>())
                    continue;

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

            if (activatedAny)
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.8f, Pitch = -0.05f }, player.Center);

            return activatedAny;
        }

        private static void FireKunai(Projectile projectile, Vector2 direction, MalachiteKunaiMode mode, bool leftThrow)
        {
            projectile.ai[0] = (float)mode;
            projectile.localAI[0] = 0f;
            projectile.localAI[1] = 1f;
            projectile.localAI[2] = leftThrow ? 0f : 1f;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.timeLeft = mode == MalachiteKunaiMode.FiredFrenzy ? 180 : 240;
            projectile.extraUpdates = mode == MalachiteKunaiMode.FiredFrenzy && !leftThrow ? 2 : 1;
            projectile.tileCollide = true;
            projectile.penetrate = leftThrow ? 1 : (mode == MalachiteKunaiMode.FiredFrenzy ? -1 : 1);
            projectile.localNPCHitCooldown = leftThrow ? 12 : (mode == MalachiteKunaiMode.FiredFrenzy ? 4 : 8);
            projectile.Calamity().stealthStrike = false;
            Player owner = Main.player[projectile.owner];
            float speedMultiplier = owner.active
                ? owner.GetModPlayer<MalachiteAccessoryPlayer>().MalachiteProjectileVelocityMultiplier
                : 1f;
            projectile.velocity = direction * (mode == MalachiteKunaiMode.FiredFrenzy ? 27f : 23f) * speedMultiplier;
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

            float progress = PeacockCount <= 1 ? 0.5f : SlotIndex / (float)(PeacockCount - 1);
            float open = GetStoredOpenInterpolant();
            float spread = MathHelper.Lerp(-1.92f, 1.92f, progress) * open;
            float crown = MathF.Sin(progress * MathHelper.Pi);
            Vector2 behind = new Vector2(-owner.direction, -0.2f).SafeNormalize(Vector2.UnitX * -owner.direction);
            Vector2 offset = behind.RotatedBy(spread * owner.direction) * MathHelper.Lerp(12f, 76f + crown * 36f, open);
            offset.Y -= MathHelper.Lerp(0f, 18f + crown * 18f, open);

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

        private void AINormalThrown()
        {
            Projectile.extraUpdates = 1;
            if (!MalachiteProgression.NormalKunaiIgnoresGravity)
            {
                float gravityFade = Utils.GetLerpValue(110f, 20f, Projectile.localAI[0], true);
                Projectile.velocity.Y += 0.15f * gravityFade;
                Projectile.velocity *= 0.995f;
                return;
            }

            Projectile.velocity *= 1.001f;
            Lighting.AddLight(Projectile.Center, 0.04f, 0.28f, 0.08f);
        }

        private void AIStagedNormal(Player owner)
        {
            Vector2 launchDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            int encoded = Math.Max(10, (int)Projectile.ai[1]);
            int count = Utils.Clamp(encoded / 10, 1, MalachiteProgression.DepletionBurstKunaiCount);
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

            if (Projectile.localAI[0] < 12f)
                return;

            NPC target = FindTarget(720f, requireLineOfSight: false);
            if (target == null)
                return;

            HomeTowardsWithTurnLimit(target.Center, 34f, PeacockHomingTurnRate);
        }

        private void AIActivatedPeacock(Player owner)
        {
            Projectile.tileCollide = false;

            if (Projectile.localAI[0] < 32f)
            {
                Projectile.velocity *= 0.985f;
                return;
            }

            NPC target = FindTarget(1500f, requireLineOfSight: false);
            if (target != null)
            {
                HomeTowardsWithTurnLimit(target.Center, 38f, PeacockHomingTurnRate);
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
            Projectile.penetrate = 1;

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
                Projectile.velocity = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * 34f;
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

            if (UsesOriginalMalachiteTrail)
                SpawnOriginalMalachiteGlowDust();

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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 6 * 60);
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
            if (!owner.active || !owner.GetModPlayer<MalachiteAccessoryPlayer>().PrecisionEmblemEquipped)
                return;

            if (WasActivated)
                modifiers.ArmorPenetration += 10f;

            if (IsPeacockOrAceKunai)
                modifiers.SourceDamage *= 1.05f;
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
                if (UsesOriginalMalachiteTrail)
                    DrawAfterimageTrail(texture, frame, origin, effects, GetOriginalMalachiteGlowColor(130) * 0.62f, glowTrail: true);

                Color trailColor = GetKunaiColor() * 0.45f;
                DrawAfterimageTrail(texture, frame, origin, effects, trailColor, glowTrail: false);
            }

            Color drawColor = Color.Lerp(lightColor, GetKunaiColor(), 0.75f);
            if (UsesOriginalMalachiteTrail)
                drawColor = Color.Lerp(drawColor, GetOriginalMalachiteGlowColor(Projectile.alpha), 0.18f);

            if (IsStored)
                drawColor *= 0.82f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + SlotIndex);

            if (IsStored)
                DrawStoredTintLayers(texture, frame, origin, effects);

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

        private void DrawAfterimageTrail(Texture2D texture, Rectangle frame, Vector2 origin, SpriteEffects effects, Color color, bool glowTrail)
        {
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldPosition = Projectile.oldPos[i];
                if (oldPosition == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = oldPosition + Projectile.Size * 0.5f - Main.screenPosition;
                float scale = glowTrail
                    ? Projectile.scale * MathHelper.Lerp(0.85f, 1.32f, completion)
                    : Projectile.scale * (0.55f + completion * 0.35f);

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    frame,
                    color * completion,
                    GetTrailRotation(i),
                    origin,
                    scale,
                    effects);
            }
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

    public sealed class MalachiteHitCutVisual : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private bool Enhanced => Projectile.ai[1] >= 1f;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == 1f)
                SpawnParallelCutParticles();

            Lighting.AddLight(Projectile.Center, 0.04f, 0.18f, 0.04f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        private void SpawnParallelCutParticles()
        {
            Vector2 forward = Projectile.ai[0].ToRotationVector2();
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            int count = Enhanced ? 24 : 15;
            float spread = Enhanced ? 72f : 46f;
            Color baseColor = Enhanced ? new Color(198, 255, 112) : new Color(96, 255, 135);

            Particle sparkle = new GenericSparkle(
                Projectile.Center,
                Vector2.Zero,
                Color.White,
                baseColor,
                Enhanced ? 1.15f : 0.82f,
                12,
                0.025f,
                1.05f,
                false);
            GeneralParticleHandler.SpawnParticle(sparkle);

            for (int i = 0; i < count; i++)
            {
                float centered = i - (count - 1) * 0.5f;
                Vector2 position =
                    Projectile.Center -
                    forward * Main.rand.NextFloat(56f, 104f) +
                    normal * (centered / Math.Max(1f, count - 1f) * spread + Main.rand.NextFloat(-2.5f, 2.5f));
                Vector2 velocity = forward * Main.rand.NextFloat(7.5f, Enhanced ? 13.5f : 10.5f);
                Color color = Color.Lerp(baseColor, Color.White, Main.rand.NextFloat(0.08f, 0.22f)) * Main.rand.NextFloat(0.68f, 0.9f);

                Particle line = Main.rand.NextBool()
                    ? new AltSparkParticle(position, velocity, false, Main.rand.Next(10, 15), Main.rand.NextFloat(0.46f, 0.72f), color)
                    : new LineParticle(position, velocity * 0.38f, false, Main.rand.Next(11, 16), Main.rand.NextFloat(0.55f, 0.86f), color);
                GeneralParticleHandler.SpawnParticle(line);

                if (i % 4 != 0)
                    continue;

                Particle softLine = new CustomSpark(
                    position,
                    velocity * 0.14f,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    2,
                    Main.rand.NextFloat(0.36f, 0.52f),
                    color * 0.62f,
                    new Vector2(1.75f, 0.34f),
                    true,
                    true,
                    0f,
                    false,
                    false,
                    0.54f,
                    0.82f,
                    0.82f,
                    false,
                    false,
                    0f);
                GeneralParticleHandler.SpawnParticle(softLine);
            }
        }
    }
}
