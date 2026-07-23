using System;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRMetalJetShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1; // 只能造成一次伤害
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 180;
            Projectile.scale = 0.9f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage()
        {
            // 出现之后一段时间 (ai[0] 帧) 才能造成伤害，防止瞬间重叠伤害同一个目标
            return Projectile.ai[0] <= 0 ? null : false;
        }

        public override void AI()
        {
            bool finalUpdate = CalamityUtils.FinalExtraUpdate(Projectile);
            if (Projectile.ai[0] > 0f && finalUpdate)
                Projectile.ai[0]--;

            Projectile.velocity *= 0.91f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ && finalUpdate)
            {
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Color metalColor = Main.rand.NextBool()
                    ? Color.Lerp(Color.Silver, Color.Gold, Main.rand.NextFloat(0.35f, 0.8f))
                    : Color.Lerp(Color.Gold, Color.Orange, Main.rand.NextFloat(0.2f, 0.65f));

                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center,
                    backward.RotatedByRandom(0.16f) * Main.rand.NextFloat(1.2f, 2.8f),
                    false,
                    Main.rand.Next(18, 27),
                    Main.rand.NextFloat(0.72f, 1.08f),
                    metalColor));

                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    backward * 0.4f,
                    false,
                    10,
                    Main.rand.NextFloat(0.24f, 0.34f),
                    metalColor,
                    true,
                    false,
                    true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnMetalImpact(target.Center);

            // Stage 0 真实伤害机制 (10% 基础，Boss 0.5%，受 DR 加成)
            if (target.active && target.lifeMax > 5)
            {
                bool isBoss = target.boss || target.type == NPCID.TargetDummy || target.realLife >= 0;
                float trueDamageRatio = isBoss ? 0.005f : 0.10f;
                float trueDamage = target.lifeMax * trueDamageRatio;

                // DR (Damage Reduction) 提升真实伤害
                float dr = target.Calamity().DR;
                if (dr > 0f)
                    trueDamage *= (1f + dr);

                int finalTrueDamage = Math.Max(1, (int)trueDamage);

                // 造成独立真实伤害
                hit.HideCombatText = false;
                target.life -= finalTrueDamage;
                CombatText.NewText(target.getRect(), new Color(255, 140, 40), finalTrueDamage, true);

                if (target.life <= 0)
                    target.checkDead();
            }

            // Stage 1 克眼强化：防御力永久降低 60%
            if (AMRBalance.DeathMarkUnlocked)
            {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 5 * 60);
                int defenseLoss = Math.Max(25, (int)(target.defense * 0.6f));
                target.Calamity().miscDefenseLoss = Math.Max(target.Calamity().miscDefenseLoss, defenseLoss);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnMetalImpact(Projectile.Center);
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.35f, Pitch = 0.18f }, Projectile.Center);
            return true;
        }

        private void SpawnMetalImpact(Vector2 impactPoint)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new ImpactParticle(
                impactPoint,
                Main.rand.NextFloat(-0.18f, 0.18f),
                20,
                0.62f,
                Color.Lerp(Color.Silver, Color.Gold, 0.55f)));

            for (int i = 0; i < 6; i++)
            {
                float spread = i / 5f - 0.5f;
                Vector2 velocity = forward.RotatedBy(MathHelper.ToRadians(62f) * spread) * Main.rand.NextFloat(4.5f, 10.5f);
                Color color = Color.Lerp(Color.Silver, Color.Orange, Main.rand.NextFloat(0.25f, 0.75f));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    impactPoint,
                    velocity,
                    true,
                    Main.rand.Next(22, 36),
                    Main.rand.NextFloat(0.82f, 1.25f),
                    color));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 180, 50, 0), 0f,
                bloom.Size() * 0.5f, 0.12f * Projectile.scale, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(texture, drawPos, null, new Color(255, 230, 160, 0),
                Projectile.rotation, origin, new Vector2(0.8f, 1.4f) * Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
