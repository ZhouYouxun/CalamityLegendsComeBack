using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick
{
    public class IceBladeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/ChronomancersScythe";

        private float rotationOffset = 0f;
        private int attackTimer = 0;
        private const int PrepareDelay = 10;
        private const int SpinDuration = 30;

        // 旋转圈数，可调整（2圈或3圈）
        private const float SpinRotations = 2f;

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PrepareDelay + SpinDuration;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
        }

        public override bool? CanDamage() => attackTimer >= PrepareDelay ? null : false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            if (attackTimer == 0)
                rotationOffset = Projectile.ai[0];

            float bladeRadius = Projectile.ai[1] <= 0f ? 150f : Projectile.ai[1];

            attackTimer++;
            if (attackTimer < PrepareDelay)
            {
                // 准备期：在鼠标方位颤抖蓄能
                float jitter = MathF.Sin(Main.GlobalTimeWrappedHourly * 42f) * MathHelper.ToRadians(2.5f);
                float prepareAngle = rotationOffset + jitter;
                Projectile.Center = player.Center + prepareAngle.ToRotationVector2() * bladeRadius;
                // 贴图倾斜45°，旋转时加 PiOver4 补偿
                Projectile.rotation = prepareAngle + MathHelper.PiOver2 + MathHelper.PiOver4;

                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Ice);
                    d.velocity = (player.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 2f;
                    d.noGravity = true;
                }
            }
            else
            {
                if (attackTimer == PrepareDelay)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.1f, Volume = 1f }, Projectile.Center);
                    player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 2.2f);
                }

                float progress = (float)(attackTimer - PrepareDelay) / SpinDuration;
                float angle = progress * MathHelper.TwoPi * SpinRotations;
                float spinAngle = rotationOffset + angle;
                Projectile.Center = player.Center + spinAngle.ToRotationVector2() * bladeRadius;
                Projectile.rotation = spinAngle + MathHelper.PiOver2 + MathHelper.PiOver4;

                for (int i = 0; i < 4; i++)
                {
                    float dist = 20f + i * 25f;
                    Vector2 pos = Projectile.Center + (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * dist;
                    int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                    Dust dDust = Dust.NewDustDirect(pos, 0, 0, dType);
                    dDust.velocity = (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2()
                        .RotatedBy(MathHelper.PiOver2) * (3f + i);
                    dDust.scale = 1.0f + Main.rand.NextFloat(0.6f);
                    dDust.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 300);
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            modPlayer.UltimateCharge = Math.Min(GlacialEmbracePlayer.MaxUltimateCharge, modPlayer.UltimateCharge + 4);
            modPlayer.OnSpecialHitNPC();
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi * i / 18f;
                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType, vel.X, vel.Y);
                d.scale = Main.rand.NextFloat(1f, 1.8f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;

            Color col = Color.Lerp(new Color(0, 195, 255, 0), new Color(100, 40, 255, 0),
                0.5f + 0.5f * MathF.Sin(time * 8f));
            float opacity = attackTimer < PrepareDelay ? 0.4f : 0.92f;
            float scale = attackTimer < PrepareDelay ? 0.7f : 1.05f;

            // 发光叠加
            Main.spriteBatch.Draw(tex, drawPos, null, col * opacity,
                Projectile.rotation, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, drawPos, null, Color.White * 0.3f * opacity,
                Projectile.rotation, tex.Size() * 0.5f, scale * 0.82f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
