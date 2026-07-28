using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 冥蚀贯穿矛 —— 左键第三段冲刺贯穿在突入瞬间射出的音速虚空激光。
    /// 极高更新次数下近乎瞬移地贯穿全部敌人，拖出一道宽亮的黑→荧绿光束；命中叠 2 层蚀痕并划一道事件视界。
    /// 这是三段连招的"射击"收尾：不是匀速直线飞镖，而是一记贴脸拉爆的贯穿光矛。
    /// </summary>
    public class UmbralNadirVoidLance : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color Green = UmbralNadirPalette.MeldGreen;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 18;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 26;
            Projectile.extraUpdates = 5;              // 音速：6 次/帧
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunRicochet") with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.01f;
            Lighting.AddLight(Projectile.Center, 0.3f, 0.9f, 0.45f);

            if (Projectile.FinalExtraUpdate())
            {
                Vector2 perp = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                for (int i = -1; i <= 1; i += 2)
                {
                    Dust vd = Dust.NewDustPerfect(Projectile.Center + perp * i * Main.rand.NextFloat(2f, 8f),
                        ModContent.DustType<VoidDustInverted>(), perp * i * Main.rand.NextFloat(1f, 3f), 0, Green, Main.rand.NextFloat(0.7f, 1.2f));
                    vd.noGravity = true;
                    vd.color = Green;
                }
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, Color.Black,
                    Main.rand.NextFloat(0.2f, 0.34f), Main.rand.Next(6, 10), true, false));
            }
        }

        // 拉长判定，读作贯穿光束
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = Projectile.Center - dir * 70f;
            Vector2 tl = Vector2.Min(Projectile.Center, back) - new Vector2(14f);
            Vector2 br = Vector2.Max(Projectile.Center, back) + new Vector2(14f);
            hitbox = new Rectangle((int)tl.X, (int)tl.Y, (int)(br.X - tl.X), (int)(br.Y - tl.Y));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 150);
            UmbralCorrosionGlobalNPC.AddStacks(target, 2);
            UmbralNadirVisuals.EventHorizon(target.Center, 0.42f, false);
        }

        // ===== 黑→荧绿激光束 =====

        private float BeamWidth(float completionRatio, Vector2 _)
            => MathHelper.Lerp(46f, 4f, completionRatio) * Utils.GetLerpValue(0f, 6f, Projectile.timeLeft, true);

        private Color BeamColor(float completionRatio, Vector2 _)
            => Color.Lerp(UmbralNadirPalette.MeldGreenBright, Color.Lerp(Green, Color.Black, completionRatio), completionRatio) * (1f - completionRatio * 0.5f);

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            Vector2 offset = Projectile.Size * 0.5f;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(BeamWidth, BeamColor, (_, _) => offset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]), 60);

            // 亮绿弹头闪
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, UmbralNadirPalette.MeldGreenBright with { A = 0 }, 0f,
                bloom.Size() * 0.5f, 0.3f, SpriteEffects.None, 0);
            return false;
        }
    }
}
