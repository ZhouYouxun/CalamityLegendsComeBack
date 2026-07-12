using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordCapeFinisher : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const int Duration = 210;
        private const float CapeRadius = 150f;

        private static readonly Color SoulPurple = new(80, 30, 130);
        private static readonly Color BloodRed = new(165, 8, 36);

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private float spinRotation;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = (int)(CapeRadius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 11;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            timer++;
            Projectile.Center = Owner.Center;
            Projectile.timeLeft = 4;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 1.4f);

            float spinSpeed = MathHelper.Lerp(0.21f, 0.42f, Utils.GetLerpValue(0f, 60f, timer, true));
            spinRotation += spinSpeed;
            Projectile.rotation = spinRotation;

            EmitCapeParticles();
            FireDarkSouls();

            if (timer == Duration - 12)
                ReleaseFinaleBurst();

            if (timer >= Duration)
                Projectile.Kill();
        }

        private void ReleaseFinaleBurst()
        {
            // 斗篷旋至终点并不悄然散去，而是把积攒的血与火一次性掀出去。
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 9f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.35f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.65f, Pitch = -0.4f }, Projectile.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, new Color(238, 214, 250) * 0.8f, 1.6f, 18));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                    BloodRed, new Vector2(1f, 1f), 0f, 0.3f, 1.4f, 26));

                for (int i = 0; i < 24; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2().RotatedByRandom(0.16f) * Main.rand.NextFloat(4f, 12f);
                    GeneralParticleHandler.SpawnParticle(new CritSpark(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f),
                        velocity, Color.White, Main.rand.NextBool() ? BloodRed : SoulPurple,
                        Main.rand.NextFloat(0.45f, 0.95f), Main.rand.Next(12, 20)));
                }
            }

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), Math.Max(1, (int)(Projectile.damage * 1.45f)),
                Projectile.knockBack + 3f, Projectile.owner, 1f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = (spinRotation + MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(9f, 12f);
                int soulType = i % 2 == 0
                    ? ModContent.ProjectileType<GaelGreatswordDarkSoul>()
                    : ModContent.ProjectileType<GaelGreatswordVengefulSoul>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * 40f,
                    velocity, soulType, Math.Max(1, (int)(Projectile.damage * 0.5f)), 1.5f, Projectile.owner);
            }
        }

        public override bool? CanDamage() => timer >= 12 && timer <= Duration - 10 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = CapeRadius + MathF.Sin(timer * 0.11f) * 22f;
            Vector2 closest = targetHitbox.ClosestPointInRect(Projectile.Center);
            return closest.Distance(Projectile.Center) <= radius ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.42f, MathHelper.Clamp(Projectile.numHits / 12f, 0f, 1f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != Projectile.owner || Projectile.numHits % 4 != 0)
                return;

            int echoDamage = Math.Max(1, (int)(Projectile.damage * 0.32f));
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Center, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), echoDamage, 1.5f, Projectile.owner, 1f);
        }

        private void FireDarkSouls()
        {
            int interval = Math.Max(4, 8 - GaelGreatswordProgression.GetStage());
            if (Main.myPlayer != Projectile.owner || timer % interval != 0)
                return;

            NPC target = FindTarget();
            Vector2 forward = target != null
                ? Projectile.Center.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY)
                : (spinRotation + Main.rand.NextFloat(-0.7f, 0.7f)).ToRotationVector2();

            Vector2 spawnPosition = Projectile.Center - forward * Main.rand.NextFloat(80f, 140f) + Main.rand.NextVector2Circular(50f, 50f);
            Vector2 velocity = forward.RotatedBy(Main.rand.NextFloat(-0.35f, 0.35f)) * Main.rand.NextFloat(8f, 13f);
            int damage = Math.Max(1, (int)(Projectile.damage * 0.55f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, velocity,
                ModContent.ProjectileType<GaelGreatswordDarkSoul>(), damage, 1.2f, Projectile.owner, target?.whoAmI ?? -1);
        }

        private NPC FindTarget()
        {
            NPC closest = null;
            float closestDistance = 1200f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = npc.Distance(Projectile.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void EmitCapeParticles()
        {
            if (Main.dedServ)
                return;

            if (timer % 2 == 0)
            {
                Vector2 edge = Projectile.Center + spinRotation.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-1.3f, 1.3f)) * Main.rand.NextFloat(80f, CapeRadius);
                Vector2 velocity = edge.DirectionFrom(Projectile.Center).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(edge, velocity, false,
                    Main.rand.Next(14, 24), Main.rand.NextFloat(0.45f, 0.78f), Main.rand.NextBool(3) ? BloodRed : SoulPurple));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(CapeRadius, CapeRadius),
                    DustID.Shadowflame, Main.rand.NextVector2Circular(2f, 2f), 120, SoulPurple, Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D capeTexture = GetCapeTexture().Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Vector2 origin = capeTexture.Size() * 0.5f;
            float fadeIn = Utils.GetLerpValue(0f, 18f, timer, true);
            float fadeOut = Utils.GetLerpValue(Duration, Duration - 24f, timer, true);
            float opacity = fadeIn * fadeOut;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < 6; i++)
            {
                float angle = spinRotation + MathHelper.TwoPi * i / 6f;
                Vector2 offset = angle.ToRotationVector2() * 34f;
                Color color = i % 2 == 0 ? BloodRed : SoulPurple;
                Main.EntitySpriteDraw(capeTexture, center + offset, null, color with { A = 0 } * opacity * 0.28f,
                    angle + MathHelper.PiOver2, origin, 1.25f + i * 0.035f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, center, null, SoulPurple with { A = 0 } * opacity * 0.46f,
                0f, bloom.Size() * 0.5f, 3.1f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private static Asset<Texture2D> GetCapeTexture()
        {
            string[] candidates =
            {
                "CalamityMod/Items/Armor/Empyrean/EmpyreanCloak_Back",
                "CalamityMod/Items/Accessories/SandCloak",
                "CalamityMod/Items/Accessories/Wings/SilvaWings_Wings",
                "CalamityMod/Items/Accessories/Wings/TarragonWings_Wings",
                "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword",
            };

            foreach (string candidate in candidates)
            {
                if (ModContent.RequestIfExists(candidate, out Asset<Texture2D> asset))
                    return asset;
            }

            return ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword");
        }
    }
}
