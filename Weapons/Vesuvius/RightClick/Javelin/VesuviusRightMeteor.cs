using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick.Javelin
{
    public sealed class VesuviusRightReturningMeteor : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/Magic/AsteroidMolten";

        private const int ReturningState = 0;
        private const int CirclingState = 1;
        private const int FiringState = 2;
        private const float OrbitRadius = 104f;

        private int State
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private int Slot
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[2] <= 0f ? 1f : Projectile.ai[2], 1f, 5f);

        public bool ReadyForVolley => State == CirclingState && Projectile.Opacity >= 0.95f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 30;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.Opacity = 1f;
        }

        public override bool ShouldUpdatePosition() => State != CirclingState;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (State != FiringState)
            {
                Projectile.timeLeft = 2;
                EnsureSlot(owner);
                if (!Projectile.active)
                    return;
            }

            if (Projectile.localAI[2] > 0f)
                Projectile.localAI[2]--;

            Lighting.AddLight(Projectile.Center, 0.82f * Projectile.Opacity, 0.34f * Projectile.Opacity, 0.08f * Projectile.Opacity);

            switch (State)
            {
                case ReturningState:
                    DoReturning(owner);
                    break;
                case CirclingState:
                    DoCircling(owner);
                    break;
                case FiringState:
                    DoFiring(owner);
                    break;
            }
        }

        public void LaunchFromOrbit(Vector2 direction, int damage, float knockBack, int sequence)
        {
            State = FiringState;
            Projectile.damage = Math.Max(1, (int)(damage * 0.78f));
            Projectile.knockBack = knockBack;
            Projectile.velocity = direction.SafeNormalize(Vector2.UnitX) * (34f + sequence * 1.35f);
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 1f;
            Projectile.netUpdate = true;

            if (!Main.dedServ)
            {
                Color color = VesuviusProjectileVisuals.LavaGold;
                SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.48f, Pitch = -0.05f + sequence * 0.025f }, Projectile.Center);
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), color, new Vector2(0.75f, 1.55f), Projectile.velocity.ToRotation(), 0.06f, 0.9f, 18));
            }
        }

        private void EnsureSlot(Player owner)
        {
            if (Slot >= 0)
                return;

            int slot = owner.GetModPlayer<VesuviusRightMeteorPlayer>().RegisterMeteor(Projectile.whoAmI);
            if (slot < 0)
            {
                Projectile.Kill();
                return;
            }

            Slot = slot;
            Projectile.netUpdate = true;
            SpawnArrivalBirthEffects();
        }

        private void DoReturning(Player owner)
        {
            Projectile.localAI[0]++;
            Projectile.Opacity = 1f;

            Vector2 targetPosition = OrbitPosition(owner, Slot);
            Vector2 toTarget = targetPosition - Projectile.Center;
            float distance = toTarget.Length();
            float speed = MathHelper.Clamp(distance / 12f, 7f, 28f + Stage * 1.2f);
            Vector2 desiredVelocity = toTarget.SafeNormalize(Vector2.UnitY) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.16f);
            Projectile.rotation += 0.12f + Projectile.velocity.Length() * 0.01f;

            if (!Main.dedServ && Projectile.localAI[0] % 4f == 0f)
                SpawnMeteorEmber(0.55f);

            if (distance < 18f && Projectile.localAI[0] > 24f)
            {
                State = CirclingState;
                Projectile.Center = targetPosition;
                Projectile.velocity = Vector2.Zero;
                Projectile.Opacity = 1f;
                Projectile.localAI[0] = 0f;
                Projectile.netUpdate = true;
                SpawnReadyEffects();
            }
        }

        private void DoCircling(Player owner)
        {
            Vector2 oldCenter = Projectile.Center;
            Vector2 targetPosition = OrbitPosition(owner, Slot);
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPosition, 0.42f);
            Projectile.velocity = Projectile.Center - oldCenter;
            Projectile.rotation += 0.055f + Stage * 0.006f;
            Projectile.Opacity = 1f;

            if (!Main.dedServ && Main.rand.NextBool(12))
                SpawnMeteorEmber(0.38f);
        }

        private void DoFiring(Player owner)
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.3f + Projectile.velocity.Length() * 0.004f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 35f, Projectile.timeLeft, true);

            NPC target = FindTarget(1200f);
            float speed = MathHelper.Clamp(Projectile.velocity.Length() * 1.006f, 30f, 44f);
            if (target != null)
            {
                Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center + target.velocity * 8f) * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.055f);
            }
            else
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction) * speed;

            if (!Main.dedServ)
            {
                if (Projectile.localAI[0] % 2f == 0f)
                    SpawnMeteorEmber(0.72f);

                if (Main.rand.NextBool(5))
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                        -Projectile.velocity * Main.rand.NextFloat(0.035f, 0.09f),
                        false,
                        Main.rand.Next(10, 17),
                        Main.rand.NextFloat(0.28f, 0.58f),
                        Main.rand.NextBool(4) ? Color.White : VesuviusProjectileVisuals.LavaGold));
            }
        }

        private Vector2 OrbitPosition(Player owner, int slot)
        {
            float angle = -(owner.GetModPlayer<VesuviusRightMeteorPlayer>().OrbitTimer / 48f + MathHelper.TwoPi * slot / VesuviusRightMeteorPlayer.MaxMeteors);
            float breathing = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f + slot) * 5f;
            return owner.MountedCenter + angle.ToRotationVector2() * (OrbitRadius + breathing);
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);

            if (State != FiringState || Projectile.localAI[2] > 0f || Projectile.owner != Main.myPlayer)
                return;

            Projectile.localAI[2] = 10f;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<VesuviusRightMeteorExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 0.62f)),
                Projectile.knockBack * 0.45f,
                Projectile.owner,
                Stage);
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<VesuviusRightMeteorPlayer>().ClearSlot(Slot, Projectile.whoAmI);

            if (State == FiringState && Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusRightMeteorExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 0.5f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                    Stage);
            }
        }

        private void SpawnArrivalBirthEffects()
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.32f, Pitch = -0.32f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(2) ? DustID.InfernoFork : DustID.Smoke,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 6.5f),
                    90,
                    Main.rand.NextBool(3) ? Color.White : VesuviusProjectileVisuals.LavaOrange,
                    Main.rand.NextFloat(0.72f, 1.25f));
                dust.noGravity = true;
            }
        }

        private void SpawnReadyEffects()
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaGold, 0.52f, 15));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaOrange, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.08f, 0.62f, 16));
        }

        private void SpawnMeteorEmber(float strength)
        {
            if (Main.dedServ)
                return;

            Vector2 drift = -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.8f, 2.6f) + Main.rand.NextVector2Circular(0.5f, 0.5f);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                Main.rand.NextBool(3) ? DustID.Torch : DustID.InfernoFork,
                drift,
                90,
                Main.rand.NextBool(4) ? Color.White : VesuviusProjectileVisuals.LavaGold,
                Main.rand.NextFloat(0.42f, 0.92f) * strength);
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Color glowColor = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, Color.White, State == FiringState ? 0.36f : 0.18f) * Projectile.Opacity;
            float drawScale = Projectile.scale * (State == FiringState ? 1.05f : 0.92f);

            // Additive pass is glow only. The old "border" here stamped 10-14 rotating copies of
            // the whole asteroid sprite a few pixels apart, which produced a muddy halo around
            // the rock rather than an outline.
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, VesuviusProjectileVisuals.AdditiveColor(glowColor) * 0.5f, 0f, bloom.Size() * 0.5f, 0.34f + drawScale * 0.28f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // Calamity AsteroidMolten draw order: opaque rock body first, then the glowmask in
            // white on top. The body draw used to sit inside a comment whose trailing byte was
            // corrupted and ate the terminating newline, and the glowmask was being painted
            // underneath the rock rather than over it.
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, glow.Size() * 0.5f, drawScale, SpriteEffects.None);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (State == CirclingState || Projectile.Opacity <= 0f)
                return;

            Vector2[] points = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (points.Length == 0)
                points = new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (points[0] != Projectile.Center)
                points = new[] { Projectile.Center }.Concat(points).ToArray();

            if (points.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(TrailWidth, TrailColor, TrailOffset, true, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                points.Length * 4);
        }

        private float TrailWidth(float completion, Vector2 _)
        {
            float baseWidth = State == FiringState ? 24f : 15f;
            return Projectile.scale * baseWidth * MathF.Sin((1f - completion) * MathHelper.PiOver2) * Projectile.Opacity;
        }

        private Color TrailColor(float completion, Vector2 _)
        {
            Color hot = Color.Lerp(Color.White, VesuviusProjectileVisuals.LavaGold, 0.42f);
            Color cool = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, Color.DarkRed, completion * 0.55f);
            return Color.Lerp(hot, cool, completion) * (1f - completion) * Projectile.Opacity;
        }

        private Vector2 TrailOffset(float completion, Vector2 _)
        {
            Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float wave = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f + completion * MathHelper.TwoPi) * 0.8f;
            return normal * wave;
        }
    }

    public sealed class VesuviusRightMeteorExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0], 1f, 5f);

        public override void SetDefaults()
        {
            Projectile.width = 126;
            Projectile.height = 126;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 4;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0f && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.38f, Pitch = -0.24f }, Projectile.Center);
                Color color = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, Color.White, 0.2f);
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, color, 0.78f + Stage * 0.08f, 16));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.08f, 1f + Stage * 0.08f, 18));

                for (int i = 0; i < 12; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        Main.rand.NextBool(2) ? DustID.InfernoFork : DustID.Torch,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 8.5f),
                        80,
                        Main.rand.NextBool(3) ? Color.White : VesuviusProjectileVisuals.LavaGold,
                        Main.rand.NextFloat(0.72f, 1.45f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = Projectile.width * 0.5f;
            return Vector2.Distance(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center)) <= radius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Utils.GetLerpValue(0f, 4f, Projectile.timeLeft, true);
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 116, 32) * 0.34f * fade,
                0f,
                bloom.Size() * 0.5f,
                1.12f + Stage * 0.08f,
                SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
