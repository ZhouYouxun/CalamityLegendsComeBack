using CalamityLegendsComeBack.Weapons.Vesuvius.Core;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.Passive
{
    public class VesuviusPassivePlayer : ModPlayer
    {
        public const int MaxAshSouls = 6;
        public const int AfterflameWindow = 90;
        private const int ManaPerAshSoul = 400;

        public int LeftClickCooldown;
        public int EmpoweredLeftTimer;
        public int MeteorFollowupTimer;
        public int AshSouls { get; private set; }

        private bool holdingVesuvius;
        private bool wasAirborne;
        private float fallPeakY;
        private int ashTimer;
        private int spentMana;

        public override void ResetEffects()
        {
            holdingVesuvius = false;
        }

        public override void PostUpdate()
        {
            if (LeftClickCooldown > 0)
                LeftClickCooldown--;
            if (EmpoweredLeftTimer > 0)
                EmpoweredLeftTimer--;
            if (MeteorFollowupTimer > 0)
                MeteorFollowupTimer--;

            if (!holdingVesuvius)
            {
                ashTimer = 0;
                TrackLanding(false);
                return;
            }

            // “移动火山”在玩家原本魔力不足 100 时额外补 100，而不是把任何角色都
            // 强行改成同一个上限。火块、岩浆和常见燃烧也在手持期间一并免疫。
            if (Player.statManaMax2 < 100)
                Player.statManaMax2 += 100;
            Player.fireWalk = true;
            Player.lavaImmune = true;
            Player.buffImmune[BuffID.OnFire] = true;
            Player.buffImmune[BuffID.OnFire3] = true;
            Player.noFallDmg = true;
            SpawnAmbientAsh();
            TrackLanding(true);
            MaintainAshSoulVisuals();
        }

        public override void UpdateDead()
        {
            LeftClickCooldown = 0;
            EmpoweredLeftTimer = 0;
            MeteorFollowupTimer = 0;
            AshSouls = 0;
            spentMana = 0;
            wasAirborne = false;
        }

        public void SetHoldingVesuvius()
        {
            holdingVesuvius = true;
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (item?.ModItem is not NewVesuvius || manaConsumed <= 0)
                return;

            spentMana += manaConsumed;
            while (spentMana >= ManaPerAshSoul)
            {
                spentMana -= ManaPerAshSoul;
                AddAshSoul();
            }
        }

        public void GrantAfterflameWindow()
        {
            EmpoweredLeftTimer = AfterflameWindow;
            MeteorFollowupTimer = AfterflameWindow;
        }

        public bool TryConsumeEmpoweredLeft()
        {
            if (EmpoweredLeftTimer <= 0)
                return false;

            EmpoweredLeftTimer = 0;
            return true;
        }

        public bool TryConsumeMeteorFollowup()
        {
            if (MeteorFollowupTimer <= 0)
                return false;

            MeteorFollowupTimer = 0;
            return true;
        }

        public bool TryConsumeAshVolley()
        {
            if (AshSouls < MaxAshSouls)
                return false;

            AshSouls = 0;
            return true;
        }

        public void AddAshSoul()
        {
            if (AshSouls >= MaxAshSouls)
                return;

            AshSouls++;
            if (Main.dedServ || Player.whoAmI != Main.myPlayer)
                return;

            Color soulColor = Color.Lerp(new Color(255, 72, 28), new Color(193, 78, 255), 0.34f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Player.Center,
                Vector2.Zero,
                soulColor,
                "CalamityMod/Particles/SmallBloomRingLayered",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.08f,
                0.72f,
                18,
                true));
            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.36f, Pitch = 0.18f + AshSouls * 0.035f }, Player.Center);
        }

        private void SpawnAmbientAsh()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            ashTimer++;
            if (ashTimer < 11)
                return;

            ashTimer = 0;
            int damage = 1;
            if (Player.HeldItem?.ModItem is NewVesuvius)
                damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.12f));

            // 火山灰只在贴身范围缓慢飘落，既能让玩家主动贴近敌人上标记，
            // 又不会像旧版那样覆盖半个屏幕并自动扫到远处目标。
            Vector2 spawnPosition = Player.Center + new Vector2(Main.rand.NextFloat(-86f, 86f), Main.rand.NextFloat(-74f, -28f));
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(0.45f, 1.05f));
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<VesuviusAshFall>(),
                damage,
                0f,
                Player.whoAmI,
                Main.rand.NextFloatDirection());
        }

        private void TrackLanding(bool active)
        {
            bool airborne = Math.Abs(Player.velocity.Y) > 0.05f || Player.jump > 0 || Player.fallStart < (int)(Player.position.Y / 16f);
            if (active && airborne)
            {
                if (!wasAirborne)
                    fallPeakY = Player.Bottom.Y;

                wasAirborne = true;
                fallPeakY = Player.gravDir > 0f
                    ? Math.Min(fallPeakY, Player.Bottom.Y)
                    : Math.Max(fallPeakY, Player.Top.Y);
                return;
            }

            if (active && wasAirborne && Player.velocity.Y == 0f && Player.whoAmI == Main.myPlayer)
            {
                float landingY = Player.gravDir > 0f ? Player.Bottom.Y : Player.Top.Y;
                float fallTiles = Math.Abs(landingY - fallPeakY) / 16f;
                if (fallTiles >= 7f)
                    SpawnLandingQuake(fallTiles, fallTiles >= 21f);
            }

            if (!airborne || !active)
            {
                wasAirborne = false;
                fallPeakY = Player.Bottom.Y;
            }
        }

        private void SpawnLandingQuake(float fallTiles, bool majorImpact)
        {
            int damage = 1;
            if (Player.HeldItem?.ModItem is NewVesuvius)
                damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * (majorImpact ? 1.4f : 0.7f)));

            float radius = majorImpact ? 300f : 150f;
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Bottom,
                Vector2.Zero,
                ModContent.ProjectileType<VesuviusLandingQuake>(),
                damage,
                majorImpact ? 9f : 5f,
                Player.whoAmI,
                radius,
                fallTiles,
                majorImpact ? 1f : 0f);
        }

        private void MaintainAshSoulVisuals()
        {
            if (Player.whoAmI != Main.myPlayer || AshSouls <= 0)
                return;

            int visualType = ModContent.ProjectileType<VesuviusAshSoulVisual>();
            for (int slot = 0; slot < AshSouls; slot++)
            {
                bool exists = false;
                foreach (Projectile projectile in Main.ActiveProjectiles)
                {
                    if (projectile.owner == Player.whoAmI && projectile.type == visualType && (int)projectile.ai[0] == slot)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, visualType, 0, 0f, Player.whoAmI, slot);
            }
        }
    }

    public class VesuviusAshFall : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.X += Projectile.ai[0] * 0.012f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.04f, 0.8f, 5.5f);
            Projectile.rotation += Projectile.velocity.X * 0.02f + 0.025f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 220f, Utils.GetLerpValue(42f, 0f, Projectile.timeLeft, true));

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Projectile.velocity * Main.rand.NextFloat(0.04f, 0.18f) + Main.rand.NextVector2Circular(0.12f, 0.12f),
                    Color.Lerp(new Color(58, 50, 44), new Color(92, 76, 60), Main.rand.NextFloat()),
                    Main.rand.Next(12, 24),
                    Main.rand.NextFloat(0.06f, 0.14f),
                    0.52f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false,
                    required: false);
                GeneralParticleHandler.SpawnParticle(smoke);

                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                        DustID.Smoke,
                        Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.18f, 0.18f),
                        175,
                        Color.Lerp(new Color(54, 46, 38), new Color(78, 64, 50), Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.14f, 0.32f));
                    d.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            VesuviusCombatSystem.ApplyVolcanicCalamity(target);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class VesuviusLandingQuake : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Radius => Projectile.ai[0] <= 0f ? 150f : Projectile.ai[0];
        private float FallTiles => Math.Max(7f, Projectile.ai[1]);
        private bool MajorImpact => Projectile.ai[2] > 0f;

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 28;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft >= 20;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.Resize((int)(Radius * 2f), (int)(Radius * 0.8f));
                Projectile.Damage();
                SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.85f, Pitch = -0.32f }, Projectile.Center);
                ApplyScreenShake();
                SpawnImpactParticles();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), Projectile.Center - new Vector2(Radius, Radius * 0.38f), Projectile.Center + new Vector2(Radius, Radius * 0.38f));
            return Vector2.Distance(closest, targetHitbox.Center.ToVector2()) < 12f ||
                   Math.Abs(targetHitbox.Center.X - Projectile.Center.X) <= Radius && Math.Abs(targetHitbox.Center.Y - Projectile.Center.Y) <= Radius * 0.5f;
        }

        private void SpawnImpactParticles()
        {
            if (Main.dedServ)
                return;

            Color deepSpace = new(43, 21, 92);
            Color stellarViolet = new(176, 83, 255);
            Color starCore = new(126, 226, 255);
            float power = MajorImpact ? 2f : 1f;

            // 保留 Leonid/星际践踏的“整片圆面被压亮”感，但地表喷出的东西改成星尘、
            // 紫蓝碎光和少量暗烟，彻底移除旧版橙色火环。
            int squareCount = MajorImpact ? 42 : 22;
            for (int i = 0; i < squareCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 0.5f) * Main.rand.NextFloat(2.5f, 8f) * power;
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.24f, 10f),
                    velocity - Vector2.UnitY * Main.rand.NextFloat(0.5f, 3.4f),
                    false,
                    Main.rand.Next(34, 58),
                    Main.rand.NextFloat(1.5f, 3.4f),
                    Color.Lerp(stellarViolet, starCore, Main.rand.NextFloat(0.18f, 0.72f))));
            }

            int sparkCount = MajorImpact ? 34 : 18;
            for (int i = 0; i < sparkCount; i++)
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                Vector2 velocity = new Vector2(side * Main.rand.NextFloat(3f, 14f) * power, -Main.rand.NextFloat(1.5f, 8f) * power);
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 5f),
                    velocity,
                    Main.rand.NextBool(3) ? starCore : stellarViolet,
                    deepSpace,
                    Main.rand.NextFloat(0.75f, 1.35f),
                    Main.rand.Next(16, 28)));
            }

            int smokeCount = MajorImpact ? 16 : 8;
            for (int i = 0; i < smokeCount; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(28f, 7f),
                    new Vector2(Main.rand.NextFloatDirection() * 2.4f, -Main.rand.NextFloat(0.8f, 3.4f)) * power,
                    Color.Lerp(deepSpace, new Color(68, 45, 92), Main.rand.NextFloat()),
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(0.35f, 0.75f),
                    0.62f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    true,
                    required: false));
            }
        }

        private void ApplyScreenShake()
        {
            if (Main.dedServ)
                return;

            float shake = MajorImpact ? 14f : MathHelper.Clamp(4f + (FallTiles - 7f) * 0.32f, 4f, 8f);
            float distanceFactor = Utils.GetLerpValue(1800f, 180f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shake * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            float opacity = Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true) * 0.78f;
            float scale = Radius / 78f;

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:CircularGradientWithEdge"]
                .UseOpacity(opacity)
                .UseColor(Color.Lerp(new Color(73, 31, 138), new Color(112, 211, 255), MajorImpact ? 0.48f : 0.3f))
                .UseSecondaryColor(new Color(224, 181, 255))
                .UseSaturation(scale)
                .Apply();
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition, null, Color.White,
                0f, pixel.Size() * 0.5f, scale * 156f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
