using System.Collections.Generic;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class VanquisherArrowProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Items/Ammo/VanquisherArrow";
        public ref float Time => ref Projectile.ai[0];
        public ref float ProjectileSpeed => ref Projectile.ai[1];
        public bool Phase2 = false;
        public float HomingTime = 0;
        public Color MainColor;
        public NPC targeted;
        public int rotDir = 0;
        public float rotSpeed = 0;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 35;
            Projectile.height = 35;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.arrow = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 7;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15 * Projectile.extraUpdates;
        }

        public override void AI()
        {
            //This is so Grape Beer works properly with them
            if (targeted != null && Projectile.localNPCImmunity[targeted.whoAmI] == -1)
                Projectile.localNPCImmunity[targeted.whoAmI] = 15 * Projectile.extraUpdates;
            float rate = Main.GlobalTimeWrappedHourly * 5;
            List<Color> eColors = new List<Color>()
            {
                Color.Cyan,
                Color.Magenta
            };

            int colorIndex = (int)(rate / 2 % eColors.Count);
            Color currentColor = eColors[colorIndex];
            Color nextColor = eColors[(colorIndex + 1) % eColors.Count];
            MainColor = Color.Lerp(currentColor, nextColor, rate % 2f > 1f ? 1f : rate % 1f);

            if (Time == 0)
            {
                rotDir = (Main.rand.NextBool() ? -1 : 1);
                rotSpeed = Main.rand.NextFloat(0.8f, 1.1f);
                Projectile.scale = 0.014f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 7f;
                ProjectileSpeed = 30;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (Time > 4f && Main.rand.NextBool(6))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 226 : 272, -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.15f, 0.35f);
                dust.noLightEmittence = true;
            }

            if (Time > 45)
            {
                if (targeted != null)
                {
                    Phase2 = true;
                    if (HomingTime == 0)
                        HomingTime = 1;
                    if (targeted.life <= 0)
                        targeted = null;
                }
                else
                    targeted = Projectile.Center.ClosestNPCAt(450);
            }

            if (HomingTime > 0 && HomingTime < 2 && targeted != null)
            {
                Projectile.timeLeft++;
                CalamityUtils.HomeInOnSelectedNPC(Projectile, targeted, true, 0.65f, 7, 0.98f, 0.95f, true);
            }
            else if (Projectile.velocity.Length() < 7)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.velocity.SafeNormalize(Vector2.UnitX) * 7f, 0.04f);
            if (HomingTime > 1)
            {
                HomingTime--;
                Projectile.velocity = Projectile.velocity.RotatedBy(0.2f * rotDir * rotSpeed * Utils.GetLerpValue(15, 5, HomingTime, true));
            }

            Time++;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HomingTime = Main.rand.Next(12, 15 + 1) * Projectile.extraUpdates;
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // First hit is 40% damage
            // Second hit is 100% base damage, it is the "Slash Hit"
            modifiers.SourceDamage *= (Projectile.numHits == 0 ? 0.4f : 1f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.numHits > 0)
            {
                Vector2 vel = new Vector2(0.1f, 0.1f).RotatedByRandom(100);
                VoidSparkParticle spark2 = new VoidSparkParticle(Projectile.Center, vel, false, 9, Main.rand.NextFloat(0.15f, 0.25f), Main.rand.NextBool() ? Color.Magenta : Color.Cyan);
                GeneralParticleHandler.SpawnParticle(spark2);

                for (int j = -1; j <= 1; j += 2)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), vel.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.1f) * Main.rand.NextFloat(2f, 12.5f) * j);
                        dust.noGravity = true;
                        dust.scale = Main.rand.NextFloat(1.2f, 1.7f);
                        dust.color = Main.rand.NextBool() ? Color.Magenta : Color.Cyan;
                        dust.noLightEmittence = true;
                        dust.fadeIn = 1;
                    }
                }

                SoundStyle onKill = new("CalamityMod/Sounds/Item/ScorpioHit");
                SoundEngine.PlaySound(onKill with { Volume = 0.25f, Pitch = 0.1f, PitchVariance = 0.3f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Volume = 0.4f, Pitch = -0.4f, PitchVariance = 0.3f }, Projectile.Center);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Time == 0)
                return false;
            Asset<Texture2D> arrow = ModContent.Request<Texture2D>("CalamityMod/Items/Ammo/VanquisherArrow");
            Asset<Texture2D> glow = ModContent.Request<Texture2D>("CalamityMod/Items/Ammo/VanquisherArrowGlow");
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;

            if (Time > 6)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], MainColor with { A = 0 } * 0.6f, 1, texture);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }
            Main.EntitySpriteDraw(arrow.Value, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, arrow.Size() / 2f, 1, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow.Value, Projectile.Center - Main.screenPosition, null, Color.Lerp(MainColor, Color.White, 0.6f), Projectile.rotation, glow.Size() / 2f, 1, SpriteEffects.None, 0);
            return false;
        }
    }
}
