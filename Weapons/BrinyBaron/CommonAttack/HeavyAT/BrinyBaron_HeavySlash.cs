using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.Particles;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.HeavyAT
{
    /// <summary>
    /// 回退复原的经典回旋打击（360度正圆圆盘自转 + VFX刀盘 + 纯净水流粒子）。
    /// 无椭圆轨道变形，无龙卷风绘制，无复杂的 Exoblade 着色器拖尾。
    /// </summary>
    public class BrinyBaron_HeavySlash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        public Player Owner => Main.player[Projectile.owner];

        public ref float SpinAngle => ref Projectile.ai[0];
        public ref float OrbitDirection => ref Projectile.ai[1];

        public const int DefaultDuration = 30;
        private float fadeIn = 0f;
        private bool soundPlayed = false;

        public override void SetDefaults()
        {
            Projectile.width = 130;
            Projectile.height = 130;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1;
            Projectile.timeLeft = DefaultDuration;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            if (Owner == null || !Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (OrbitDirection == 0)
                OrbitDirection = Owner.direction != 0 ? Owner.direction : 1;

            if (!soundPlayed)
            {
                soundPlayed = true;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.82f, Pitch = Main.rand.NextFloat(-0.06f, 0.06f) }, Owner.Center);
            }

            fadeIn = MathHelper.Lerp(fadeIn, 1f, 0.2f);

            // 1. 正圆 360° 自转轨迹
            float spinSpeed = MathHelper.PiOver4 * 0.35f * OrbitDirection;
            SpinAngle += spinSpeed;

            Vector2 spinDir = SpinAngle.ToRotationVector2();
            float reach = 60f * Projectile.scale;

            Projectile.Center = Owner.Center + spinDir * (reach * 0.5f);
            Projectile.rotation = SpinAngle + MathHelper.PiOver4;
            Owner.heldProj = Projectile.whoAmI;

            // 2. 水流 / 霜冻粒子
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 spawnPos = Owner.Center + spinDir * reach;
                Vector2 tangentDir = spinDir.RotatedBy(MathHelper.PiOver2 * OrbitDirection);
                Vector2 vel = tangentDir * Main.rand.NextFloat(1.2f, 2.5f) + spinDir * Main.rand.NextFloat(0.2f, 0.8f);

                Dust dust = Dust.NewDustPerfect(spawnPos + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextBool() ? DustID.Water : DustID.Frost, vel, 100, new Color(90, 205, 255), Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 spinDir = SpinAngle.ToRotationVector2();
            Vector2 tip = Owner.Center + spinDir * (60f * Projectile.scale);
            float collisionWidth = 32f * Projectile.scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Owner.Center, tip, collisionWidth, ref Projectile.localAI[0]);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.DamageOverTime.WindChilled>(), 180);

            if (!Main.dedServ)
            {
                Vector2 spinDir = SpinAngle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    target.Center, Vector2.Zero, Color.Cyan, new Vector2(1.05f, 0.58f),
                    spinDir.ToRotation(), 0.12f, 0.76f, 14));

                for (int i = 0; i < 8; i++)
                {
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.IceTorch, Main.rand.NextVector2Circular(6f, 6f), 0, Color.White, Main.rand.NextFloat(1f, 1.4f));
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D ghost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaronGoest").Value;
            Texture2D swoosh = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BrinyBaron/CommonAttack/BBSwing_Wave_Effect").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            // 1. 绘制正圆 VFX 刀盘贴图 (无椭圆变形)
            Vector2 swooshCenter = Owner.Center - Main.screenPosition;
            Vector2 spinDir = SpinAngle.ToRotationVector2();
            float swooshRotation = spinDir.ToRotation() + MathHelper.PiOver2 * OrbitDirection;
            float swooshScale = Projectile.scale * 1.05f;

            Main.EntitySpriteDraw(
                swoosh,
                swooshCenter,
                null,
                Color.DeepSkyBlue with { A = 0 } * fadeIn * 0.65f,
                swooshRotation,
                swoosh.Size() * 0.5f,
                swooshScale,
                SpriteEffects.None);

            // 2. 残影与主武器绘制
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 3.5f;
                Main.EntitySpriteDraw(ghost, drawPos + offset, null, Color.Cyan with { A = 0 } * 0.25f * fadeIn, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPos, null, lightColor * fadeIn, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
