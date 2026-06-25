using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    internal sealed class ElementalCodexGlobalNPC : GlobalNPC
    {
        private ElementalCodexElement primedElement;
        private int primedTimeLeft;
        private int primedPower;
        private int primedOwner = -1;
        private int reactionCooldown;

        private int meltingTimer;
        private int meltingPower;
        private int scorchTimer;
        private int scorchPower;
        private int paralysisTimer;
        private int freezeTimer;
        private int electrifiedTimer;
        private int electrifiedPower;
        private int witherTimer;
        private int condensationHasteTimer;
        private int flourishTimer;
        private int flourishOwner = -1;
        private int controlTimer;
        private int controlOwner = -1;
        private int growthTimer;
        private int growthRollTimer;
        private float growthVelocityMultiplier = 1f;

        private bool natureValueBoosted;
        private bool coldStorageApplied;

        public override bool InstancePerEntity => true;

        public bool ActiveFlourishFor(Player player)
        {
            return flourishTimer > 0 &&
                player != null &&
                player.active &&
                (flourishOwner < 0 || flourishOwner == player.whoAmI);
        }

        public static void TryApplyWeaponElement(NPC target, Player owner, Item weapon)
        {
            if (!CanReceiveElement(target) ||
                owner == null ||
                !owner.active ||
                owner.dead ||
                weapon == null ||
                weapon.IsAir ||
                !owner.GetModPlayer<ElementalCodexPlayer>().ElementalCodexEquipped ||
                !ElementalCodexWeaponDatabase.TryGetDefinition(weapon.type, out ElementalCodexWeaponDefinition definition))
                return;

            ElementalCodexElement element = definition.PickElementForHit(weapon.type);
            int panelDamage = Math.Max(1, owner.GetWeaponDamage(weapon));
            int duration = ElementalCodexBalance.GetElementDurationFrames(owner, weapon, panelDamage);
            target.GetGlobalNPC<ElementalCodexGlobalNPC>().ApplyElement(target, owner, element, panelDamage, duration);
        }

        public static NPC FindFlourishTarget(Player owner, Vector2 origin)
        {
            NPC best = null;
            float bestDistanceSquared = 1200f * 1200f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanReceiveElement(npc))
                    continue;

                ElementalCodexGlobalNPC state = npc.GetGlobalNPC<ElementalCodexGlobalNPC>();
                if (!state.ActiveFlourishFor(owner))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(origin, npc.Center);
                if (distanceSquared >= bestDistanceSquared)
                    continue;

                best = npc;
                bestDistanceSquared = distanceSquared;
            }

            return best;
        }

        public void ApplyElement(NPC npc, Player owner, ElementalCodexElement element, int panelDamage, int duration)
        {
            if (element == ElementalCodexElement.None)
                return;

            if (reactionCooldown <= 0 &&
                primedTimeLeft > 0 &&
                primedElement != ElementalCodexElement.None &&
                primedElement != element)
            {
                ElementalCodexElement first = primedElement;
                ClearPrimedElement(npc);
                TriggerReaction(npc, owner, first, element, panelDamage, duration);
                return;
            }

            PrimeElement(npc, owner, element, panelDamage, duration);
        }

        public override void PostAI(NPC npc)
        {
            TickTimers();
            ApplyMovementEffects(npc);
            TickPeriodicDamage(npc);
            EmitElementVisuals(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (primedElement == ElementalCodexElement.Fire && primedTimeLeft > 0)
            {
                int dot = Math.Max(12, primedPower / 2);
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;
                npc.lifeRegen -= dot;
                damage = Math.Max(damage, Math.Max(1, primedPower / 8));
            }
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (freezeTimer > 0)
            {
                modifiers.SourceDamage *= 0f;
                return;
            }

            if (primedTimeLeft > 0)
            {
                if (primedElement == ElementalCodexElement.Water)
                {
                    modifiers.DefenseEffectiveness *= 0.72f;
                    modifiers.ScalingArmorPenetration += 0.08f;
                }
                else if (primedElement == ElementalCodexElement.Disease)
                    modifiers.FinalDamage *= 1.09f;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!Main.player.IndexInRange(projectile.owner))
                return;

            ApplyOwnerReactionDamageBonuses(npc, Main.player[projectile.owner], ref modifiers);
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyOwnerReactionDamageBonuses(npc, player, ref modifiers);
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (paralysisTimer > 0)
            {
                modifiers.SourceDamage *= 0f;
                return;
            }

            if (primedElement == ElementalCodexElement.Lightning && primedTimeLeft > 0)
                modifiers.FinalDamage *= 0.90f;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (freezeTimer > 0)
                drawColor = Color.Lerp(drawColor, ElementalCodexContent.GetReactionColor(ElementalCodexReaction.Freeze), 0.55f);
            else if (flourishTimer > 0)
                drawColor = Color.Lerp(drawColor, ElementalCodexContent.GetReactionColor(ElementalCodexReaction.Flourish), 0.32f);
            else if (primedTimeLeft > 0)
                drawColor = Color.Lerp(drawColor, ElementalCodexContent.GetElementColor(primedElement), 0.22f);
        }

        private static bool CanReceiveElement(NPC npc)
        {
            return npc != null &&
                npc.active &&
                !npc.friendly &&
                npc.lifeMax > 5 &&
                !npc.dontTakeDamage;
        }

        private void PrimeElement(NPC npc, Player owner, ElementalCodexElement element, int panelDamage, int duration)
        {
            primedElement = element;
            primedTimeLeft = Math.Max(primedTimeLeft, duration);
            primedPower = Math.Max(primedPower, panelDamage);
            primedOwner = owner?.whoAmI ?? -1;
            npc.AddBuff(ElementalCodexContent.GetBuffType(element), duration);

            if (element == ElementalCodexElement.Nature && !natureValueBoosted)
            {
                natureValueBoosted = true;
                npc.value *= 1.12f;
            }
        }

        private void TriggerReaction(NPC npc, Player owner, ElementalCodexElement first, ElementalCodexElement second, int panelDamage, int duration)
        {
            ElementalCodexReaction reaction = ElementalCodexContent.GetReaction(first, second);
            if (reaction == ElementalCodexReaction.None)
                return;

            reactionCooldown = Math.Max(reactionCooldown, ElementalCodexBalance.GetReactionCooldownFrames(duration, reaction));

            if (!Main.dedServ)
                CombatText.NewText(npc.Hitbox, ElementalCodexContent.GetReactionColor(reaction), ElementalCodexContent.GetReactionName(reaction));

            switch (reaction)
            {
                case ElementalCodexReaction.SteamBurst:
                    StrikeNPC(npc, panelDamage * 4, owner);
                    SpawnBurst(npc.Center, ElementalCodexContent.GetReactionColor(reaction), 28, 7f);
                    break;

                case ElementalCodexReaction.MeltingImpact:
                    StrikeNPC(npc, Math.Max(1, panelDamage / 2), owner);
                    meltingTimer = Math.Max(meltingTimer, 3 * 60);
                    meltingPower = Math.Max(meltingPower, panelDamage);
                    break;

                case ElementalCodexReaction.Overload:
                    StrikeArea(npc.Center, ElementalCodexBalance.OverloadRadius, panelDamage * 5, owner, 9f);
                    SpawnBurst(npc.Center, ElementalCodexContent.GetReactionColor(reaction), 54, 10f);
                    break;

                case ElementalCodexReaction.Scorch:
                    scorchTimer = Math.Max(scorchTimer, 6 * 60);
                    scorchPower = Math.Max(scorchPower, panelDamage);
                    break;

                case ElementalCodexReaction.Paralysis:
                    paralysisTimer = Math.Max(paralysisTimer, 2 * 60);
                    break;

                case ElementalCodexReaction.Freeze:
                    freezeTimer = Math.Max(freezeTimer, 90);
                    break;

                case ElementalCodexReaction.Electrified:
                    electrifiedTimer = Math.Max(electrifiedTimer, 5 * 60);
                    electrifiedPower = Math.Max(electrifiedPower, panelDamage);
                    break;

                case ElementalCodexReaction.Growth:
                    growthTimer = Math.Max(growthTimer, 6 * 60);
                    growthRollTimer = 0;
                    break;

                case ElementalCodexReaction.Wither:
                    witherTimer = Math.Max(witherTimer, 5 * 60);
                    break;

                case ElementalCodexReaction.Condensation:
                    StrikeNPC(npc, GetCondensationDamage(npc, panelDamage), owner);
                    condensationHasteTimer = Math.Max(condensationHasteTimer, 4 * 60);
                    break;

                case ElementalCodexReaction.ColdStorage:
                    if (!coldStorageApplied)
                    {
                        coldStorageApplied = true;
                        npc.value *= 3f;
                    }
                    break;

                case ElementalCodexReaction.CorruptFreeze:
                    if (Main.rand.NextFloat() < 0.62f)
                        StrikeNPC(npc, panelDamage * 8, owner);
                    else
                        npc.life = Math.Min(npc.lifeMax, npc.life + Math.Max(1, panelDamage * 3));
                    SpawnBurst(npc.Center, ElementalCodexContent.GetReactionColor(reaction), 34, 7f);
                    break;

                case ElementalCodexReaction.Flourish:
                    flourishTimer = Math.Max(flourishTimer, 8 * 60);
                    flourishOwner = owner?.whoAmI ?? -1;
                    break;

                case ElementalCodexReaction.Control:
                    controlTimer = Math.Max(controlTimer, 5 * 60);
                    controlOwner = owner?.whoAmI ?? -1;
                    owner?.GetModPlayer<ElementalCodexPlayer>().ApplyControl(npc, 5 * 60);
                    break;

                case ElementalCodexReaction.Neutralization:
                    owner?.GetModPlayer<ElementalCodexPlayer>().ApplyNeutralization(7 * 60);
                    break;
            }
        }

        private void ClearPrimedElement(NPC npc)
        {
            primedElement = ElementalCodexElement.None;
            primedTimeLeft = 0;
            primedPower = 0;
            primedOwner = -1;

            for (int i = 0; i < npc.buffType.Length; i++)
            {
                int buffType = npc.buffType[i];
                foreach (ElementalCodexElement element in ElementalCodexContent.AllElements)
                {
                    if (buffType != ElementalCodexContent.GetBuffType(element))
                        continue;

                    npc.DelBuff(i);
                    i--;
                    break;
                }
            }
        }

        private void TickTimers()
        {
            if (primedTimeLeft > 0 && --primedTimeLeft <= 0)
            {
                primedElement = ElementalCodexElement.None;
                primedPower = 0;
                primedOwner = -1;
            }

            if (reactionCooldown > 0)
                reactionCooldown--;
            if (meltingTimer > 0)
                meltingTimer--;
            if (scorchTimer > 0)
                scorchTimer--;
            if (paralysisTimer > 0)
                paralysisTimer--;
            if (freezeTimer > 0)
                freezeTimer--;
            if (electrifiedTimer > 0)
                electrifiedTimer--;
            if (witherTimer > 0)
                witherTimer--;
            if (condensationHasteTimer > 0)
                condensationHasteTimer--;
            if (flourishTimer > 0)
                flourishTimer--;
            if (controlTimer > 0)
                controlTimer--;
            else
                controlOwner = -1;
            if (growthTimer > 0)
                growthTimer--;
        }

        private void ApplyMovementEffects(NPC npc)
        {
            if (freezeTimer > 0)
                npc.velocity = Vector2.Zero;

            if (growthTimer > 0 && npc.knockBackResist > 0f)
            {
                if (growthRollTimer-- <= 0)
                {
                    growthRollTimer = 20;
                    growthVelocityMultiplier = Main.rand.NextFloat(0.60f, 1f);
                }

                npc.velocity *= growthVelocityMultiplier;
            }

            if (condensationHasteTimer > 0)
                npc.velocity *= 1.20f;

            if (primedElement == ElementalCodexElement.Ice && primedTimeLeft > 0 && npc.knockBackResist > 0f && Main.rand.NextBool(240))
                npc.velocity *= 0.45f;
        }

        private void TickPeriodicDamage(NPC npc)
        {
            Player owner = GetOwner(primedOwner);

            if (meltingTimer > 0 && meltingTimer % 12 == 0)
                StrikeNPC(npc, Math.Max(1, meltingPower / 5), owner);

            if (scorchTimer > 0 && scorchTimer % 15 == 0)
                StrikeNPC(npc, Math.Max(1, scorchPower / 6), owner);

            if (electrifiedTimer > 0 && electrifiedTimer % 30 == 0)
            {
                StrikeNPC(npc, Math.Max(1, (int)(electrifiedPower * 0.40f)), owner);
                TrySpreadElectrified(npc);
            }

            if (witherTimer > 0 && witherTimer % 60 == 0)
                StrikeNPC(npc, Math.Max(1, npc.lifeMax / 100), owner);
        }

        private void TrySpreadElectrified(NPC npc)
        {
            if (!Main.rand.NextBool(5))
                return;

            foreach (NPC other in Main.ActiveNPCs)
            {
                if (other.whoAmI == npc.whoAmI || !CanReceiveElement(other))
                    continue;

                if (Vector2.DistanceSquared(npc.Center, other.Center) > 420f * 420f)
                    continue;

                ElementalCodexGlobalNPC state = other.GetGlobalNPC<ElementalCodexGlobalNPC>();
                state.electrifiedTimer = Math.Max(state.electrifiedTimer, 3 * 60);
                state.electrifiedPower = Math.Max(state.electrifiedPower, Math.Max(1, electrifiedPower / 2));
                SpawnArc(npc.Center, other.Center, ElementalCodexContent.GetReactionColor(ElementalCodexReaction.Electrified));
                return;
            }
        }

        private void ApplyOwnerReactionDamageBonuses(NPC npc, Player owner, ref NPC.HitModifiers modifiers)
        {
            if (owner == null || !owner.active)
                return;

            ElementalCodexPlayer codexPlayer = owner.GetModPlayer<ElementalCodexPlayer>();
            if (!codexPlayer.ElementalCodexEquipped)
                return;

            if (codexPlayer.NeutralizationTimer > 0)
                modifiers.SourceDamage *= 1.25f;

            if (controlTimer > 0 && (controlOwner < 0 || controlOwner == owner.whoAmI) && codexPlayer.IsControllingTarget(npc))
                modifiers.SourceDamage *= 1.35f;
        }

        private static int GetCondensationDamage(NPC npc, int panelDamage)
        {
            float percent = npc.boss ? 0.012f : 0.035f;
            int percentDamage = Math.Max(1, (int)(npc.lifeMax * percent));
            return Math.Clamp(percentDamage, panelDamage, panelDamage * 12);
        }

        private static void StrikeArea(Vector2 center, float radius, int damage, Player owner, float knockback)
        {
            float radiusSquared = radius * radius;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!CanReceiveElement(npc) || Vector2.DistanceSquared(center, npc.Center) > radiusSquared)
                    continue;

                Vector2 push = npc.Center - center;
                if (push.LengthSquared() > 0f)
                    npc.velocity += push.SafeNormalize(Vector2.UnitY) * knockback;

                StrikeNPC(npc, damage, owner, knockback);
            }
        }

        private static void StrikeNPC(NPC npc, int damage, Player owner, float knockback = 0f)
        {
            if (!CanReceiveElement(npc) || damage <= 0)
                return;

            int hitDirection = owner == null ? 0 : Math.Sign(npc.Center.X - owner.Center.X);
            NPC.HitInfo hit = npc.CalculateHitInfo(damage, hitDirection, false, knockback, DamageClass.Magic);
            npc.StrikeNPC(hit);
        }

        private static Player GetOwner(int ownerIndex)
        {
            if (!Main.player.IndexInRange(ownerIndex))
                return null;

            Player owner = Main.player[ownerIndex];
            return owner.active ? owner : null;
        }

        private void EmitElementVisuals(NPC npc)
        {
            if (Main.dedServ || Main.GameUpdateCount % 5 != 0)
                return;

            if (primedTimeLeft > 0)
                EmitBaseElementVisual(npc, primedElement);

            if (scorchTimer > 0)
                EmitBaseElementVisual(npc, ElementalCodexElement.Fire);
            if (electrifiedTimer > 0)
                EmitBaseElementVisual(npc, ElementalCodexElement.Lightning);
            if (flourishTimer > 0)
                EmitBaseElementVisual(npc, ElementalCodexElement.Nature);
            if (freezeTimer > 0)
                EmitBaseElementVisual(npc, ElementalCodexElement.Ice);
        }

        private static void EmitBaseElementVisual(NPC npc, ElementalCodexElement element)
        {
            Color color = ElementalCodexContent.GetElementColor(element);
            Vector2 center = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.45f, npc.height * 0.45f);
            Vector2 velocity;
            int dustType;

            switch (element)
            {
                case ElementalCodexElement.Fire:
                    dustType = DustID.Torch;
                    velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f);
                    break;

                case ElementalCodexElement.Water:
                    dustType = DustID.BlueTorch;
                    velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 4.2f);
                    break;

                case ElementalCodexElement.Ice:
                    dustType = DustID.IceTorch;
                    velocity = new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-0.35f, 0.35f));
                    break;

                case ElementalCodexElement.Lightning:
                    dustType = DustID.PurpleTorch;
                    velocity = new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-5.2f, 5.2f));
                    break;

                case ElementalCodexElement.Nature:
                    dustType = Main.rand.NextBool() ? DustID.GrassBlades : DustID.GemEmerald;
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    velocity = new Vector2((float)Math.Cos(angle * 2.3f), (float)Math.Sin(angle * 1.7f)) * Main.rand.NextFloat(1.5f, 4f);
                    break;

                case ElementalCodexElement.Disease:
                    dustType = Main.rand.NextBool() ? DustID.Shadowflame : DustID.Smoke;
                    velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 5.6f);
                    color = new Color(32, 28, 36);
                    break;

                default:
                    return;
            }

            Dust dust = Dust.NewDustPerfect(center, dustType, velocity, 120, color, Main.rand.NextFloat(0.85f, 1.35f));
            dust.noGravity = true;
            Lighting.AddLight(npc.Center, color.ToVector3() * 0.18f);
        }

        private static void SpawnBurst(Vector2 center, Color color, int count, float speed)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / count).ToRotationVector2() * Main.rand.NextFloat(speed * 0.45f, speed);
                Dust dust = Dust.NewDustPerfect(center, DustID.Electric, velocity, 90, color, Main.rand.NextFloat(0.9f, 1.7f));
                dust.noGravity = true;
            }
        }

        private static void SpawnArc(Vector2 from, Vector2 to, Color color)
        {
            if (Main.dedServ)
                return;

            Vector2 line = to - from;
            int steps = Math.Max(4, (int)(line.Length() / 42f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)steps) + Main.rand.NextVector2Circular(10f, 10f);
                Dust dust = Dust.NewDustPerfect(point, DustID.Electric, Main.rand.NextVector2Circular(1.2f, 1.2f), 110, color, 1.1f);
                dust.noGravity = true;
            }
        }
    }
}
