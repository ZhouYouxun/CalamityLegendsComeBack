using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class Astral_ConstellationNode : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private float StartAngle => Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 142;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 20f;

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (target != null && Projectile.localAI[0] < 38f)
            {
                float radius = MathHelper.Lerp(126f, 78f, Projectile.localAI[0] / 38f);
                Vector2 desiredPosition = target.Center + (StartAngle + Projectile.localAI[0] * 0.075f).ToRotationVector2() * radius;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredPosition - Projectile.Center, 0.24f);
            }
            else if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 12f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
            }
            else
            {
                Projectile.velocity *= 0.985f;
            }

            Projectile.rotation += 0.18f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.28f, 0.14f, 0.34f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.BlueTorch : DustID.OrangeTorch,
                    Main.rand.NextVector2Circular(0.9f, 0.9f),
                    100,
                    Main.rand.NextBool() ? new Color(94, 216, 255) : new Color(255, 142, 70),
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }

            if (Projectile.localAI[0] == 38f && Main.myPlayer == Projectile.owner)
            {
                int blast = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Astral_Blast>(), System.Math.Max(1, Projectile.damage / 2), Projectile.knockBack, Projectile.owner, StartAngle, Main.rand.Next(2));
                if (blast >= 0 && blast < Main.maxProjectiles)
                    Main.projectile[blast].DamageType = Projectile.DamageType;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 210);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/FullStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 10f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Color orange = new Color(255, 142, 70, 0) * opacity;
            Color blue = new Color(94, 216, 255, 0) * opacity;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(bloom, oldDraw, null, Color.Lerp(orange, blue, completion) * completion * 0.16f, Projectile.rotation, bloom.Size() * 0.5f, 0.16f * completion, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, drawPosition, null, orange * 0.32f, Projectile.rotation, bloom.Size() * 0.5f, 0.18f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, blue * 0.62f, -Projectile.rotation, star.Size() * 0.5f, 0.34f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White * 0.3f * opacity, Projectile.rotation * 1.4f, star.Size() * 0.5f, 0.22f, SpriteEffects.None);
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
            float sqrRange = 680f * 680f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                closest = npc;
            }

            return closest;
        }
    }

    public class Uelibloom_Thorn : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "Terraria/Images/Projectile_638";

        private int TargetIndex => (int)Projectile.ai[0];
        private float StartAngle => Projectile.ai[1];
        private bool CrownVariant => Projectile.ai[2] > 0.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 132;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > (CrownVariant ? 16f : 6f);

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!CrownVariant)
                Projectile.penetrate = 1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (CrownVariant && target != null && Projectile.localAI[0] < 24f)
            {
                float radius = MathHelper.Lerp(132f, 72f, Projectile.localAI[0] / 24f);
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center + (StartAngle + Projectile.localAI[0] * 0.08f).ToRotationVector2() * radius, 0.2f);
                Projectile.velocity *= 0.84f;
            }
            else if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * (CrownVariant ? 14f : 12f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, CrownVariant ? 0.1f : 0.07f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.34f, 0.08f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.JungleGrass, -Projectile.velocity * 0.04f + Main.rand.NextVector2Circular(0.6f, 0.6f), 100, new Color(135, 255, 88), Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = new(135, 255, 88, 0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(bloom, oldDraw, null, color * completion * 0.16f, Projectile.rotation, bloom.Size() * 0.5f, 0.15f * completion, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, CrownVariant ? 0.82f : 0.65f, SpriteEffects.None);
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
            float sqrRange = 740f * 740f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                closest = npc;
            }

            return closest;
        }
    }

    public class Cosmilite_Rift : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private bool StrongRift => Projectile.ai[1] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 88;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool? CanDamage() => StrongRift && Projectile.localAI[0] > 16f;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!StrongRift)
                Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, 56);
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += StrongRift ? 0.1f : 0.065f;
            Projectile.scale = MathHelper.Lerp(0.2f, StrongRift ? 1.2f : 0.85f, Utils.GetLerpValue(0f, 20f, Projectile.localAI[0], true)) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.28f, 0.36f) * Projectile.scale);

            NPC target = GetTarget();
            if (target != null)
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center, StrongRift ? 0.035f : 0.015f);

            if (Main.myPlayer == Projectile.owner && Projectile.localAI[0] % (StrongRift ? 14f : 20f) == 0f)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(8f, 13f);
                int fragment = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * 26f, velocity, ModContent.ProjectileType<Cosmilite_Fragment>(), System.Math.Max(1, Projectile.damage / 2), Projectile.knockBack, Projectile.owner, TargetIndex, Main.rand.Next(6));
                if (fragment >= 0 && fragment < Main.maxProjectiles)
                    Main.projectile[fragment].DamageType = Projectile.DamageType;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = (Projectile.rotation + MathHelper.TwoPi * i / 3f).ToRotationVector2() * Main.rand.NextFloat(24f, 70f) * Projectile.scale;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GemSapphire, offset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 1.4f, 100, new Color(90, 230, 255), Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, 74f * Projectile.scale, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float opacity = Utils.GetLerpValue(0f, 14f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 14f, Projectile.timeLeft, true);
            Color color = new Color(82, 230, 255, 0) * opacity;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.2f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.52f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, color * 0.72f, Projectile.rotation, ring.Size() * 0.5f, Projectile.scale * 0.44f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, new Color(180, 120, 255, 0) * 0.48f * opacity, -Projectile.rotation * 1.4f, ring.Size() * 0.5f, Projectile.scale * 0.28f, SpriteEffects.None);
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

    public class Cosmilite_Fragment : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "Terraria/Images/Projectile_466";

        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 138;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 15.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, Projectile.localAI[0] < 18f ? 0.035f : 0.115f);
            }
            else if (Projectile.localAI[0] > 24f)
            {
                Projectile.velocity *= 0.99f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.26f, 0.34f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemSapphire, -Projectile.velocity * 0.045f, 100, new Color(82, 230, 255), Main.rand.NextFloat(0.7f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.MoonLeech, 90);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = new(82, 230, 255, 0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(bloom, oldDraw, null, color * completion * 0.2f, Projectile.rotation, bloom.Size() * 0.5f, 0.15f * completion, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, 0.62f, SpriteEffects.None);
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
            float sqrRange = 860f * 860f;
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

    public class Auric_SkyLance : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 96;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.ArmorPenetration = 80;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (target != null && Projectile.localAI[0] < 24f)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(Projectile.velocity.Length() + 0.35f, 18f, 27f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.055f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.34f, 0.26f, 0.08f) + new Vector3(0.03f, 0.12f, 0.16f));

            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric,
                    -Projectile.velocity * Main.rand.NextFloat(0.025f, 0.08f),
                    100,
                    Main.rand.NextBool() ? new Color(255, 214, 82) : new Color(82, 226, 255),
                    Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 42f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 54f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 18f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 210);
            target.AddBuff(ModContent.BuffType<AuricRebuke>(), 90);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                int pulse = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Auric_Pulse>(), System.Math.Max(1, Projectile.damage / 2), Projectile.knockBack, Projectile.owner);
                if (pulse >= 0 && pulse < Main.maxProjectiles)
                    Main.projectile[pulse].DamageType = Projectile.DamageType;
            }

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/AuricBulletHit") { Volume = 0.45f, Pitch = 0.12f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 8f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 14f, Projectile.timeLeft, true);
            Color gold = new Color(255, 216, 84, 0) * opacity;
            Color blue = new Color(84, 226, 255, 0) * opacity;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(line, oldDraw, null, Color.Lerp(gold, blue, i % 2) * completion * 0.32f, Projectile.rotation, new Vector2(line.Width * 0.5f, line.Height), new Vector2(0.1f, 0.42f * completion), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(line, drawPosition, null, gold, forward.ToRotation() + MathHelper.PiOver2, new Vector2(line.Width * 0.5f, line.Height), new Vector2(0.18f, 0.82f), SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, blue * 0.28f, Projectile.rotation, bloom.Size() * 0.5f, 0.2f, SpriteEffects.None);
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

    public class Auric_Pulse : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Particles/BloomRing";

        public override void SetDefaults()
        {
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0.28f, 1.25f, Projectile.localAI[0] / 18f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.32f, 0.26f, 0.08f));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, 58f * Projectile.scale, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AuricRebuke>(), 90);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            float opacity = Utils.GetLerpValue(0f, 5f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 6f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(255, 216, 84, 0) * opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.45f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(84, 226, 255, 0) * opacity * 0.55f, -Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.27f, SpriteEffects.None);
            return false;
        }
    }

    public class Shadowspec_Echo : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private float StartAngle => Projectile.ai[1];
        private bool Aggressive => Projectile.ai[2] > 0.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 22;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 118;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 12f;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!Aggressive)
            {
                Projectile.penetrate = 1;
                Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, 88);
            }
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC target = GetTarget();
            if (target != null && Aggressive && Projectile.localAI[0] < 22f)
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center + (StartAngle - Projectile.localAI[0] * 0.08f).ToRotationVector2() * 86f, 0.16f);
                Projectile.velocity *= 0.88f;
            }
            else if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * (Aggressive ? 13f : 10f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, Aggressive ? 0.12f : 0.07f);
            }
            else
            {
                Projectile.velocity *= 0.98f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.08f, 0.32f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), DustID.Shadowflame, -Projectile.velocity * 0.04f, 100, new Color(168, 90, 255), Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 240);
            if (Aggressive)
                target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 120);
        }

        public override void OnKill(int timeLeft)
        {
            if (Aggressive && Main.myPlayer == Projectile.owner)
            {
                int blast = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Astral_Blast>(), System.Math.Max(1, Projectile.damage / 2), Projectile.knockBack, Projectile.owner, StartAngle, 1f);
                if (blast >= 0 && blast < Main.maxProjectiles)
                    Main.projectile[blast].DamageType = Projectile.DamageType;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            float opacity = Utils.GetLerpValue(0f, 10f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Color purple = new Color(168, 90, 255, 0) * opacity;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(bloom, oldDraw, null, purple * completion * 0.18f, Projectile.rotation, bloom.Size() * 0.5f, 0.2f * completion, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(smear, drawPosition, null, purple * 0.72f, Projectile.rotation + MathHelper.PiOver2, new Vector2(smear.Width * 0.5f, smear.Height), new Vector2(0.12f, Aggressive ? 0.48f : 0.34f), SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, purple * 0.35f, Projectile.rotation, bloom.Size() * 0.5f, Aggressive ? 0.22f : 0.16f, SpriteEffects.None);
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
            float sqrRange = 880f * 880f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                closest = npc;
            }

            return closest;
        }
    }

    public class Shadowspec_Rift : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 184;
            Projectile.height = 184;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 92;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 18f;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation -= 0.075f;
            Projectile.scale = MathHelper.Lerp(0.25f, 1.18f, Utils.GetLerpValue(0f, 22f, Projectile.localAI[0], true)) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.04f, 0.28f) * Projectile.scale);

            NPC target = GetTarget();
            if (target != null)
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center, 0.05f);
                target.velocity += (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 0.08f;
            }

            if (Main.myPlayer == Projectile.owner && Projectile.localAI[0] % 16f == 0f)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 9f);
                int echo = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * 48f, velocity, ModContent.ProjectileType<Shadowspec_Echo>(), System.Math.Max(1, Projectile.damage / 2), Projectile.knockBack, Projectile.owner, TargetIndex, Main.rand.NextFloat(MathHelper.TwoPi), 1f);
                if (echo >= 0 && echo < Main.maxProjectiles)
                    Main.projectile[echo].DamageType = Projectile.DamageType;
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = (Projectile.rotation + MathHelper.TwoPi * i / 5f).ToRotationVector2() * Main.rand.NextFloat(26f, 78f) * Projectile.scale;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Shadowflame, offset.SafeNormalize(Vector2.UnitY).RotatedBy(-MathHelper.PiOver2) * 1.2f, 100, new Color(168, 90, 255), Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, 82f * Projectile.scale, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 240);
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 150);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float opacity = Utils.GetLerpValue(0f, 18f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Color purple = new Color(168, 90, 255, 0) * opacity;
            Color black = new Color(18, 12, 24, 0) * opacity;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(bloom, drawPosition, null, black * 0.7f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.82f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, purple * 0.72f, Projectile.rotation, ring.Size() * 0.5f, Projectile.scale * 0.48f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, purple * 0.42f, -Projectile.rotation * 1.7f, ring.Size() * 0.5f, Projectile.scale * 0.3f, SpriteEffects.None);
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
}

