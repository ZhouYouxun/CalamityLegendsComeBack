using CalamityMod;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class Aerialite_Feather : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "Terraria/Images/Projectile_585";

        private Vector2 ReturnDirection => new(Projectile.ai[0], Projectile.ai[1]);
        private float CurveDelay => Projectile.ai[2];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 126;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 returnDirection = ReturnDirection.SafeNormalize(Vector2.UnitY);
            if (Projectile.localAI[0] > CurveDelay)
            {
                NPC target = FindClosestNPC(640f);
                Vector2 desiredVelocity = target != null
                    ? (target.Center - Projectile.Center).SafeNormalize(returnDirection) * 13.5f
                    : returnDirection * 12f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.015f * (returnDirection.X >= 0f ? 1f : -1f)) * 0.995f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.32f, 0.36f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Cloud, -Projectile.velocity * 0.05f, 140, new Color(150, 238, 255), Main.rand.NextFloat(0.55f, 0.85f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 8f;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 90);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = new(150, 238, 255, 0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(bloom, oldDraw, null, color * completion * 0.18f, Projectile.rotation, bloom.Size() * 0.5f, 0.1f * completion, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, 0.68f, SpriteEffects.None);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            return false;
        }

        private NPC FindClosestNPC(float range)
        {
            NPC target = null;
            float sqrRange = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                target = npc;
            }

            return target;
        }
    }

    public class Aerialite_Gale : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 168;
            Projectile.height = 168;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 78;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.18f;
            Projectile.scale = MathHelper.Lerp(0.55f, 1.18f, Utils.GetLerpValue(0f, 24f, Projectile.localAI[0], true)) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Vector3(0.14f, 0.28f, 0.32f) * Projectile.scale);

            NPC target = GetTarget();
            if (target != null)
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center, 0.05f);
                Vector2 pull = (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * -0.12f;
                target.velocity += pull;
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = Projectile.rotation + MathHelper.TwoPi * i / 4f + Main.rand.NextFloat(-0.1f, 0.1f);
                Vector2 position = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(26f, 78f) * Projectile.scale;
                Dust dust = Dust.NewDustPerfect(position, DustID.Cloud, angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 1.8f, 130, new Color(144, 235, 255), Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.38f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 22; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Cloud, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 7f), 130, new Color(144, 235, 255), Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = new Color(144, 235, 255, 0) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.18f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.55f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, color * 0.58f, Projectile.rotation, ring.Size() * 0.5f, Projectile.scale * 0.36f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, color * 0.36f, -Projectile.rotation * 0.8f, ring.Size() * 0.5f, Projectile.scale * 0.24f, SpriteEffects.None);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[TargetIndex];
            return target.active && target.CanBeChasedBy(Projectile) ? target : null;
        }
    }

    public class Cryonic_PrismShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "Terraria/Images/Projectile_507";

        private int TargetIndex => (int)Projectile.ai[0];
        private float StartAngle => Projectile.ai[1];
        private float ReleaseDelay => Projectile.ai[2];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > ReleaseDelay + 4f;

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (target != null && Projectile.localAI[0] <= ReleaseDelay)
            {
                float radius = MathHelper.Lerp(132f, 82f, Projectile.localAI[0] / System.Math.Max(ReleaseDelay, 1f));
                float angle = StartAngle + Projectile.localAI[0] * 0.055f;
                Vector2 desiredCenter = target.Center + angle.ToRotationVector2() * radius;
                Projectile.velocity = desiredCenter - Projectile.Center;
            }
            else
            {
                if (Projectile.localAI[1] == 0f)
                {
                    Projectile.localAI[1] = 1f;
                    Vector2 destination = target?.Center ?? Projectile.Center + StartAngle.ToRotationVector2() * -120f;
                    Projectile.velocity = (destination - Projectile.Center).SafeNormalize(Vector2.UnitY) * 15f;
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.35f, Pitch = 0.18f }, Projectile.Center);
                }

                Projectile.velocity *= 1.012f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.26f, 0.36f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, -Projectile.velocity * 0.04f, 100, new Color(152, 236, 255), Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 210);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, Main.rand.NextVector2Circular(3.5f, 3.5f), 100, new Color(152, 236, 255), Main.rand.NextFloat(0.7f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color color = new(152, 236, 255, 0);
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDraw, null, color * completion * 0.45f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.5f * completion, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, 0.62f, SpriteEffects.None);
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[TargetIndex];
            return target.active && target.CanBeChasedBy(Projectile) ? target : null;
        }
    }

    public class Perennial_BloomSeed : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "Terraria/Images/Projectile_227";

        private int TargetIndex => (int)Projectile.ai[0];
        private float StartAngle => Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 156;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 28f;

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (Projectile.localAI[0] < 28f)
            {
                Projectile.velocity *= 0.92f;
                if (target != null)
                {
                    float angle = StartAngle + Projectile.localAI[0] * 0.08f;
                    Vector2 orbit = target.Center + angle.ToRotationVector2() * MathHelper.Lerp(88f, 58f, Projectile.localAI[0] / 28f);
                    Projectile.Center = Vector2.Lerp(Projectile.Center, orbit, 0.18f);
                }
            }
            else
            {
                if (Projectile.localAI[1] == 0f)
                {
                    Projectile.localAI[1] = 1f;
                    Vector2 destination = target?.Center ?? Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 120f;
                    Projectile.velocity = (destination - Projectile.Center).SafeNormalize(Vector2.UnitY) * 11.5f;
                }

                if (target != null)
                {
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 12.5f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.05f);
                }
            }

            Projectile.rotation += 0.19f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.34f, 0.14f));

            if (Projectile.localAI[0] == 28f && Main.myPlayer == Projectile.owner)
            {
                int petalCount = 3;
                for (int i = 0; i < petalCount; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / petalCount + StartAngle).ToRotationVector2() * 5.5f;
                    int petal = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<Perennial_Petal>(), System.Math.Max(1, Projectile.damage / 2), Projectile.knockBack, Projectile.owner, TargetIndex);
                    if (petal >= 0 && petal < Main.maxProjectiles)
                        Main.projectile[petal].DamageType = Projectile.DamageType;
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GrassBlades, Main.rand.NextVector2Circular(1.1f, 1.1f), 100, new Color(132, 255, 148), Main.rand.NextFloat(0.75f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = new(132, 255, 148, 0);
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, color * 0.26f, Projectile.rotation, bloom.Size() * 0.5f, 0.16f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, 0.72f, SpriteEffects.None);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex >= 0 && TargetIndex < Main.maxNPCs)
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target;
            }

            NPC closest = null;
            float sqrRange = 560f * 560f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                closest = npc;
            }

            return closest;
        }
    }

    public class Perennial_Petal : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "Terraria/Images/Projectile_221";

        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (target != null && Projectile.localAI[0] > 10f)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 13f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.32f, 0.12f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 150);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(128, 255, 118, 0), Projectile.rotation, texture.Size() * 0.5f, 0.65f, SpriteEffects.None);
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[TargetIndex];
            return target.active && target.CanBeChasedBy(Projectile) ? target : null;
        }
    }

    public class Scoria_Geyser : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 86;
            Projectile.height = 240;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 48;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (target != null && Projectile.localAI[0] < 18f)
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center + new Vector2(0f, 28f), 0.2f);

            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.16f, 0.04f));
            for (int i = 0; i < 8; i++)
            {
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-34f, 34f), Main.rand.NextFloat(-20f, 20f));
                Vector2 velocity = new(Main.rand.NextFloat(-1.9f, 1.9f), Main.rand.NextFloat(-12f, -4.4f));
                Dust dust = Dust.NewDustPerfect(position, Main.rand.NextBool(3) ? DustID.LavaMoss : DustID.Torch, velocity, 80, new Color(255, 122, 58), Main.rand.NextFloat(1.05f, 1.75f));
                dust.noGravity = false;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 start = Projectile.Bottom;
            Vector2 end = Projectile.Top;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 38f * Projectile.scale, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            float opacity = Utils.GetLerpValue(0f, 8f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Color orange = new Color(255, 115, 52, 0) * opacity;
            Vector2 drawPosition = Projectile.Bottom - Main.screenPosition;

            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            Main.EntitySpriteDraw(smear, drawPosition, null, orange * 0.72f, 0f, new Vector2(smear.Width * 0.5f, smear.Height), new Vector2(0.28f, 1.25f), SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, orange * 0.3f, 0f, bloom.Size() * 0.5f, new Vector2(0.32f, 1.15f), SpriteEffects.None);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[TargetIndex];
            return target.active && target.CanBeChasedBy(Projectile) ? target : null;
        }
    }

    public class LifeAlloy_ReconstructionPulse : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Particles/BloomRing";

        private bool StrongPulse => Projectile.ai[0] > 0.5f;
        private int ColorStyle => (int)Projectile.ai[1];
        private Color PulseColor => ColorStyle switch
        {
            0 => new Color(90, 245, 255, 0),
            1 => new Color(255, 92, 215, 0),
            _ => new Color(126, 255, 118, 0)
        };

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float progress = Projectile.localAI[0] / 22f;
            Projectile.scale = MathHelper.Lerp(0.2f, StrongPulse ? 1.6f : 0.95f, progress);
            Projectile.rotation += StrongPulse ? 0.13f : 0.08f;
            Lighting.AddLight(Projectile.Center, PulseColor.ToVector3() * (StrongPulse ? 0.8f : 0.4f));

            if (Projectile.localAI[0] == 1f)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = StrongPulse ? 0.42f : 0.22f, Pitch = 0.3f }, Projectile.Center);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.35f * Projectile.scale, Projectile.height * 0.35f * Projectile.scale), DustID.RainbowTorch, Main.rand.NextVector2Circular(1.2f, 1.2f), 100, PulseColor, Main.rand.NextFloat(0.7f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 4f && StrongPulse;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = Projectile.width * Projectile.scale * 0.48f;
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, radius, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float opacity = Utils.GetLerpValue(0f, 5f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            Main.EntitySpriteDraw(bloom, drawPosition, null, PulseColor * 0.18f * opacity, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, PulseColor * 0.7f * opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.42f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * 0.24f * opacity, -Projectile.rotation * 1.2f, texture.Size() * 0.5f, Projectile.scale * 0.22f, SpriteEffects.None);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            return false;
        }
    }
}
