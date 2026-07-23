using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive
{
    /// <summary>
    /// When the player is in the left-click blade form, this small crystal floats behind
    /// the player and periodically fires a thin laser at nearby enemies.
    /// </summary>
    internal sealed class YC_BackgroundCrystal : ModProjectile, ILocalizedModType
    {
        private static readonly Color CrystalGold = new(255, 218, 88);
        private static readonly Color CrystalOrange = new(255, 104, 36);

        private const int AttackInterval = 90;
        private const float AttackRange = 600f;
        private const float DamageRatio = 0.28f;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YharimsCrystalPrism";

        private ref float Timer => ref Projectile.localAI[0];
        private float bobPhase;
        private float spinAngle;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = AttackInterval;
            Projectile.hide = false;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<NewLegendYharimsCrystal>())
            {
                Projectile.Kill();
                return;
            }

            YharimsCrystalStatePlayer state = owner.GetModPlayer<YharimsCrystalStatePlayer>();
            if (state.LastWeapon != YCWeaponForm.Blade)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Timer++;
            bobPhase += 0.038f;
            spinAngle += 0.05f;

            // Animate frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            // Position: float behind the player
            float bobY = (float)Math.Sin(bobPhase) * 10f;
            Vector2 offset = new Vector2(-owner.direction * 80f, -24f + bobY);
            Projectile.Center = owner.MountedCenter + offset;
            Projectile.rotation = spinAngle;
            Projectile.direction = owner.direction;
            Projectile.spriteDirection = owner.direction;

            Lighting.AddLight(Projectile.Center, CrystalGold.ToVector3() * 0.3f);

            // Periodic emit sparkles
            if (!Main.dedServ && (int)Timer % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.12f, 0.24f),
                    Main.rand.NextBool(3) ? Color.White : CrystalGold));
            }

            // Periodic auto-attack
            if (Projectile.owner == Main.myPlayer && (int)Timer % AttackInterval == AttackInterval - 1)
                TryAutoAttack(owner);
        }

        private void TryAutoAttack(Player owner)
        {
            NPC target = FindNearestTarget(owner, AttackRange);
            if (target == null)
                return;

            int baseDamage = owner.HeldItem.ModItem is NewLegendYharimsCrystal yc
                ? yc.GetScaledDamage(owner, new BalanceYharimsCrystal().GetRightClickBaseDamage())
                : owner.HeldItem.damage;
            int attackDamage = Math.Max(1, (int)(baseDamage * DamageRatio));

            Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);

            // Fire a background crystal mini-laser bolt
            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                dir * 26f,
                ModContent.ProjectileType<YC_BackgroundCrystalBolt>(),
                attackDamage,
                2f,
                Projectile.owner,
                target.whoAmI);

            if (Main.projectile.IndexInRange(proj))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[proj], YCWeaponForm.Crystal);
                Main.projectile[proj].CritChance = owner.GetWeaponCrit(owner.HeldItem);
            }

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, CrystalGold, Vector2.One, dir.ToRotation(), 0.03f, 0.65f, 10));
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.3f, Pitch = 0.35f }, Projectile.Center);
            }
        }

        private static NPC FindNearestTarget(Player owner, float range)
        {
            NPC nearest = null;
            float maxDistSq = range * range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;
                float distSq = Vector2.DistanceSquared(owner.Center, npc.Center);
                if (distSq < maxDistSq)
                {
                    maxDistSq = distSq;
                    nearest = npc;
                }
            }
            return nearest;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            int frameCount = Main.projFrames[Type] <= 0 ? 1 : Main.projFrames[Type];
            int frame = Projectile.frame % frameCount;
            int frameHeight = texture.Height / frameCount;
            Rectangle sourceRect = new Rectangle(0, frameHeight * frame, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float scale = 0.72f;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f);
            float opacity = 0.62f;

            // Soft bloom
            Main.EntitySpriteDraw(bloom, drawPos, null, CrystalGold with { A = 0 } * 0.32f * opacity, 0f, bloom.Size() * 0.5f, scale * 1.4f * pulse, SpriteEffects.None);

            // Crystal prism (semi-transparent)
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, lightColor * opacity, Projectile.rotation, origin, scale, effects);

            return false;
        }
    }

    /// <summary>Quick laser bolt fired by YC_BackgroundCrystal.</summary>
    internal sealed class YC_BackgroundCrystalBolt : ModProjectile, ILocalizedModType
    {
        private static readonly Color BoltGold = new(255, 218, 88);
        private static readonly Color BoltOrange = new(255, 104, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 50;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, BoltGold.ToVector3() * 0.38f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.GoldFlame, -Projectile.velocity * 0.3f + Main.rand.NextVector2Circular(0.8f, 0.8f), 0, default, 0.85f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 120);
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero, BoltGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.03f, 0.55f, 8));
                for (int i = 0; i < 5; i++)
                {
                    Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.GoldFlame, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f), 0, default, 0.9f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Utils.GetLerpValue(0f, 6f, Projectile.timeLeft < 6 ? Projectile.timeLeft : 50f - Projectile.timeLeft, false);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, BoltGold with { A = 0 } * 0.65f, 0f, bloom.Size() * 0.5f, 0.12f, SpriteEffects.None);
            return false;
        }
    }
}
