using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects
{
    public abstract class LeonidMetalEffect
    {
        public abstract int EffectID { get; }

        protected virtual int EnergyVariant => 0;
        protected virtual float EnergySizeFactor => 1f;
        protected virtual int EnergyMoteCount => 2;
        protected virtual int EnergyDustInterval => 14;
        protected virtual float EnergyOpacity => 0.28f;
        protected virtual float EnergySpinOffset => 0f;

        public void UpdateInjectedEnergy(LeonidCometSmall meteor, Player owner)
        {
            if (Main.dedServ || meteor.Projectile.alpha > 200)
                return;

            string timerKey = "leonid_energy_timer_" + EffectID;
            float timer = meteor.GetState(timerKey) - 1f;
            if (timer > 0f)
            {
                meteor.SetState(timerKey, timer);
                return;
            }

            float speedFactor = MathHelper.Clamp(meteor.Projectile.velocity.Length() / 18f, 0.65f, 1.25f);
            timer = System.Math.Max(6f, EnergyDustInterval / speedFactor / (meteor.FromStealthRain ? 1.2f : 1f));
            meteor.SetState(timerKey, timer);
            SpawnInjectedEnergyDust(meteor);
        }

        public void DrawInjectedEnergy(LeonidCometSmall meteor, Player owner, SpriteBatch spriteBatch)
        {
            if (Main.dedServ || EnergyOpacity <= 0f)
                return;

            Projectile projectile = meteor.Projectile;
            float visibility = 1f - projectile.alpha / 255f;
            if (visibility <= 0f)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Color energyColor = GetEnergyColor(meteor);
            energyColor.A = 0;

            float radius = CalculateEnergyRadius(meteor);
            float scale = CalculateEnergyBloomScale(radius);
            float opacity = EnergyOpacity * visibility;
            float phase = Main.GlobalTimeWrappedHourly * (0.9f + PositiveMod(EnergyVariant, 9) * 0.07f) + EnergySpinOffset + EffectID * 0.37f;
            int pattern = PositiveMod(EnergyVariant, 6);
            int moteCount = System.Math.Max(1, System.Math.Min(EnergyMoteCount, 5));
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);

            switch (pattern)
            {
                case 0:
                    DrawEnergySprite(ring, projectile.Center, energyColor * opacity * 0.22f, phase, scale * 1.35f);
                    for (int i = 0; i < moteCount; i++)
                    {
                        float angle = phase + MathHelper.TwoPi * i / moteCount;
                        Vector2 offset = angle.ToRotationVector2() * radius * 0.46f;
                        DrawEnergySprite(bloom, projectile.Center + offset, energyColor * opacity, -phase, scale * 0.55f);
                    }
                    break;

                case 1:
                    for (int i = 0; i <= moteCount; i++)
                    {
                        float completion = (i + 1f) / (moteCount + 2f);
                        Vector2 offset = -direction * radius * (0.18f + completion * 0.72f) + side * (float)System.Math.Sin(phase * 2f + i) * radius * 0.2f;
                        DrawEnergySprite(bloom, projectile.Center + offset, energyColor * opacity * (1f - completion * 0.35f), phase, scale * (0.55f - completion * 0.12f));
                    }
                    break;

                case 2:
                    int spokes = System.Math.Max(3, moteCount + 1);
                    for (int i = 0; i < spokes; i++)
                    {
                        float angle = phase + MathHelper.TwoPi * i / spokes;
                        float pulse = 0.4f + 0.08f * (float)System.Math.Sin(phase * 3f + i);
                        DrawEnergySprite(bloom, projectile.Center + angle.ToRotationVector2() * radius * pulse, energyColor * opacity * 0.9f, angle, scale * 0.5f);
                    }
                    break;

                case 3:
                    DrawEnergySprite(ring, projectile.Center, energyColor * opacity * 0.26f, -phase, scale * 1.55f);
                    DrawEnergySprite(bloom, projectile.Center + direction * radius * 0.42f + side * (float)System.Math.Sin(phase) * radius * 0.12f, energyColor * opacity, phase, scale * 0.52f);
                    DrawEnergySprite(bloom, projectile.Center - direction * radius * 0.42f - side * (float)System.Math.Sin(phase) * radius * 0.12f, energyColor * opacity * 0.72f, phase, scale * 0.42f);
                    break;

                case 4:
                    for (int i = 0; i <= moteCount; i++)
                    {
                        float completion = (i + 1f) / (moteCount + 2f);
                        float angle = phase + i * 1.7f;
                        Vector2 offset = angle.ToRotationVector2() * radius * (0.18f + completion * 0.5f);
                        DrawEnergySprite(bloom, projectile.Center + offset, energyColor * opacity * (0.68f + completion * 0.25f), angle, scale * (0.34f + completion * 0.16f));
                    }
                    break;

                default:
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 offset = side * i * radius * (0.38f + 0.08f * (float)System.Math.Sin(phase * 2f)) + direction * (float)System.Math.Cos(phase + i) * radius * 0.14f;
                        DrawEnergySprite(bloom, projectile.Center + offset, energyColor * opacity * 0.9f, phase * i, scale * 0.5f);
                    }
                    if (moteCount > 2)
                        DrawEnergySprite(ring, projectile.Center, energyColor * opacity * 0.18f, phase, scale * 1.1f);
                    break;
            }
        }

        public virtual void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
        }

        public virtual void AI(LeonidCometSmall meteor, Player owner)
        {
        }

        public virtual void ModifyHitNPC(LeonidCometSmall meteor, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public virtual void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public virtual bool OnTileCollide(LeonidCometSmall meteor, Player owner, Vector2 oldVelocity)
        {
            return true;
        }

        public virtual void OnKill(LeonidCometSmall meteor, Player owner, int timeLeft)
        {
        }

        public virtual void PostDraw(LeonidCometSmall meteor, Player owner, SpriteBatch spriteBatch)
        {
        }

        protected float CalculateEnergyRadius(LeonidCometSmall meteor, float factor = 1f)
        {
            Projectile projectile = meteor.Projectile;
            float baseSize = System.Math.Max(projectile.width, projectile.height) * projectile.scale;
            float speedSize = projectile.velocity.Length() * 0.9f;
            float styleFactor = MathHelper.Clamp(EnergySizeFactor * factor, 0.55f, 1.45f);
            float radius = (baseSize * 0.42f + speedSize + 5f) * styleFactor;
            if (meteor.FromStealthRain)
                radius *= 1.12f;

            return MathHelper.Clamp(radius, 8f, 32f);
        }

        protected float CalculateEnergyBloomScale(float radius) => MathHelper.Clamp(radius / 118f, 0.06f, 0.22f);

        private void SpawnInjectedEnergyDust(LeonidCometSmall meteor)
        {
            Projectile projectile = meteor.Projectile;
            float radius = CalculateEnergyRadius(meteor, 0.92f);
            int pattern = PositiveMod(EnergyVariant, 6);
            float phase = projectile.timeLeft * 0.13f + EnergySpinOffset + EffectID * 0.61f;
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 offset = pattern switch
            {
                0 => phase.ToRotationVector2() * radius * Main.rand.NextFloat(0.25f, 0.52f),
                1 => -direction * radius * Main.rand.NextFloat(0.18f, 0.85f) + side * Main.rand.NextFloat(-0.18f, 0.18f) * radius,
                2 => (phase + Main.rand.NextFloat(MathHelper.TwoPi)).ToRotationVector2() * radius * Main.rand.NextFloat(0.22f, 0.44f),
                3 => direction * Main.rand.NextFloat(-0.5f, 0.5f) * radius + side * Main.rand.NextFloat(-0.24f, 0.24f) * radius,
                4 => (phase + Main.rand.NextFloat(-0.8f, 0.8f)).ToRotationVector2() * radius * Main.rand.NextFloat(0.16f, 0.5f),
                _ => side * (Main.rand.NextBool() ? 1f : -1f) * radius * Main.rand.NextFloat(0.28f, 0.5f)
            };

            Color color = Color.Lerp(GetEnergyColor(meteor), Color.White, 0.18f);
            Vector2 velocity = -direction * Main.rand.NextFloat(0.12f, 0.42f) + offset.SafeNormalize(side) * Main.rand.NextFloat(0.12f, 0.45f) + Main.rand.NextVector2Circular(0.35f, 0.35f);
            Dust dust = Dust.NewDustPerfect(projectile.Center + offset, ResolveEnergyDustID(), velocity, 100, color, Main.rand.NextFloat(0.45f, 0.78f) * MathHelper.Clamp(EnergySizeFactor, 0.75f, 1.2f));
            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.1f, 0.35f);
        }

        private Color GetEnergyColor(LeonidCometSmall meteor)
        {
            return LeonidVisualUtils.GetMetalEnergyColor(EffectID, meteor.MeteorColor);
        }

        private int ResolveEnergyDustID()
        {
            return PositiveMod(EnergyVariant, 8) switch
            {
                1 => DustID.Electric,
                2 => DustID.GoldFlame,
                3 => DustID.IceTorch,
                4 => DustID.GrassBlades,
                5 => DustID.Shadowflame,
                6 => DustID.PinkTorch,
                7 => DustID.BlueTorch,
                _ => DustID.TintableDustLighted
            };
        }

        private static void DrawEnergySprite(Texture2D texture, Vector2 drawPosition, Color color, float rotation, float scale)
        {
            Main.EntitySpriteDraw(
                texture,
                drawPosition - Main.screenPosition,
                null,
                color,
                rotation,
                texture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        private static int PositiveMod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
