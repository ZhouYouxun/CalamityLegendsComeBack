using CalamityMod;
using CalamityMod.Items;
using CalamityMod.TileEntities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Systems
{
    // Keeps the two Draedon machines separate: codebreakers decrypt schematics,
    // while charging stations only fast-charge the six lab-seeking mechanisms.
    internal sealed class LightningDecryptionSystem : ModSystem
    {
        private const int OneSecondInTicks = 60;

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !CalamityLegendsComeBackConfig.Instance.LightningDecryption)
                return;

            foreach (TileEntity tileEntity in TileEntity.ByID.Values)
            {
                if (tileEntity is TECodebreaker codebreaker)
                    ShortenDecryption(codebreaker);
                else if (tileEntity is TEChargingStation chargingStation)
                    FullyChargeLabSeeker(chargingStation);
            }
        }

        private static void ShortenDecryption(TECodebreaker codebreaker)
        {
            // The normal UI starts a 7200/900-tick countdown. Clamp it once so
            // the original Calamity completion, battery consumption, and sync flow remain intact.
            if (codebreaker.DecryptionCountdown > OneSecondInTicks)
            {
                codebreaker.DecryptionCountdown = OneSecondInTicks;
                codebreaker.SyncDecryptCountdown();
            }
        }

        private static void FullyChargeLabSeeker(TEChargingStation chargingStation)
        {
            Item pluggedItem = chargingStation.PluggedItem;
            if (pluggedItem.IsAir || pluggedItem.ModItem?.GetType().Namespace != "CalamityMod.Items.LabFinders" ||
                chargingStation.CellStack <= 0 || Main.GameUpdateCount % OneSecondInTicks != 0)
                return;

            var calamityItem = pluggedItem.Calamity();
            if (!calamityItem.UsesCharge || calamityItem.Charge >= calamityItem.MaxCharge)
                return;

            // Power cells are worth one charge each. Preserve the normal cell cost
            // while making an entire locator charge complete on the next one-second tick.
            int cellCost = (int)System.Math.Ceiling(calamityItem.MaxCharge - calamityItem.Charge);
            int cellsConsumed = System.Math.Min(chargingStation.CellStack, cellCost);
            calamityItem.Charge += cellsConsumed;
            chargingStation.CellStack -= (short)cellsConsumed;

            if (calamityItem.Charge < calamityItem.MaxCharge)
                return;

            calamityItem.Charge = calamityItem.MaxCharge;

            // CellStack sync does not include the plugged item's charge. Tile-entity
            // sharing does, so multiplayer clients immediately receive the full item state.
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, chargingStation.ID, chargingStation.Position.X, chargingStation.Position.Y);
        }
    }
}
