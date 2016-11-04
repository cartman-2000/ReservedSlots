using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Permissions;
using SDG.Unturned;
using Steamworks;
using Rocket.Core;
using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.RCON;
using Rocket.Unturned.Chat;
using Rocket.Unturned;
using Rocket.Unturned.Player;

namespace ReservedSlots
{
    public class ReservedSlots : RocketPlugin<ReservedSlotsConfig>
    {
        public static ReservedSlots Instance;
        private byte lastMaxSlotCount;

        protected override void Load()
        {
            Instance = this;
            UnturnedPermissions.OnJoinRequested += Events_OnJoinRequested;
            Logger.Log(string.Format("Reserved Slots enabled: {0}, Count: {1}, Allowfill: {2}, DynamicSlots: {3}, min: {4}, max:{5}.", Instance.Configuration.Instance.ReservedSlotEnable, Instance.Configuration.Instance.ReservedSlotCount, Instance.Configuration.Instance.AllowFill, Instance.Configuration.Instance.AllowDynamicMaxSlot, Instance.Configuration.Instance.MinSlotCount, Instance.Configuration.Instance.MaxSlotCount));
            if (Instance.Configuration.Instance.AllowDynamicMaxSlot)
            {
                if (Instance.Configuration.Instance.MinSlotCount < 2)
                {
                    Logger.LogError("Reserved Slots Config Error: Minimum slots is set to 0, changing to the Unturned server default of 8 slots.");
                    Instance.Configuration.Instance.MinSlotCount = 8;
                }
                if (Instance.Configuration.Instance.MaxSlotCount > 48)
                {
                    Logger.LogWarning("Reserved Slots Config Error: Maximum slots is set to something higher than 48, limiting to 48.");
                    Instance.Configuration.Instance.MaxSlotCount = 48;
                }
                if (Instance.Configuration.Instance.MaxSlotCount < Instance.Configuration.Instance.MinSlotCount)
                {
                    Logger.LogError("Reserved Slots Config Error: Max slot count is less than initial slot count, Setting max slot count to min slot count + reserved slots, or max slot count, if over 48.");
                    byte tmp = (byte)(Instance.Configuration.Instance.MinSlotCount + Instance.Configuration.Instance.ReservedSlotCount);
                    Instance.Configuration.Instance.MaxSlotCount = tmp > Instance.Configuration.Instance.MaxSlotCount ? Instance.Configuration.Instance.MaxSlotCount : tmp;
                }
                SetMaxPlayers();
                U.Events.OnPlayerConnected += Events_OnPlayerConnected;
                U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            }
            UnturnedPermissions.OnJoinRequested += Events_OnJoinRequested;
            Instance.Configuration.Save();
        }

        protected override void Unload()
        {
            UnturnedPermissions.OnJoinRequested -= Events_OnJoinRequested;
            if (Instance.Configuration.Instance.AllowDynamicMaxSlot)
            {
                U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
                U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
            }
        }

        private void SetMaxPlayers(bool onDisconnect = false)
        {
            // Minus one if it is coming from disconnect, they are still accounted towards the total player count at this time.
            int curPlayerNum = Provider.clients.Count - (onDisconnect ? 1 : 0);
            byte curPlayerMax = Provider.maxPlayers;
            if (curPlayerNum + Instance.Configuration.Instance.ReservedSlotCount < Instance.Configuration.Instance.MinSlotCount)
                curPlayerMax = Instance.Configuration.Instance.MinSlotCount;
            else if (curPlayerNum + Instance.Configuration.Instance.ReservedSlotCount > Instance.Configuration.Instance.MaxSlotCount)
                curPlayerMax = Instance.Configuration.Instance.MaxSlotCount;
            else if (curPlayerNum + Instance.Configuration.Instance.ReservedSlotCount >= Instance.Configuration.Instance.MinSlotCount && curPlayerNum + Instance.Configuration.Instance.ReservedSlotCount <= Instance.Configuration.Instance.MaxSlotCount)
            {
                curPlayerMax = (byte)(curPlayerNum + Instance.Configuration.Instance.ReservedSlotCount);
                if (curPlayerMax > lastMaxSlotCount)
                    UnturnedChat.Say(CSteamID.Nil, "Max slots increased to: " + curPlayerMax);
                if (curPlayerMax < lastMaxSlotCount)
                    UnturnedChat.Say(CSteamID.Nil, "Max slots Decreased to: " + curPlayerMax);
            }
            if (lastMaxSlotCount != curPlayerMax)
            {
                Provider.maxPlayers = curPlayerMax;
                lastMaxSlotCount = curPlayerMax;
            }
        }

        private void Events_OnJoinRequested(CSteamID CSteamID, ref ESteamRejection? rejectionReason)
        {
            if (Instance.Configuration.Instance.ReservedSlotEnable && Instance.Configuration.Instance.ReservedSlotCount > 0 && Instance.Configuration.Instance.Groups != null && Instance.Configuration.Instance.Groups.Count > 0)
            {
                int numPlayers = Provider.clients.Count;
                byte maxPlayers = Provider.maxPlayers;
                // Run slot fill calculations, if it is enabled.
                if (Instance.Configuration.Instance.AllowFill)
                {
                    foreach (SteamPlayer player in Provider.clients)
                    {
                        if (CheckReserved(player.playerID.steamID))
                        {
                            numPlayers--;
                        }
                    }
                }

                // Check to see if dynamic slots are enabled, and adjust the max slot count on the server if they are.
                if ((!Instance.Configuration.Instance.AllowDynamicMaxSlot && numPlayers + Instance.Configuration.Instance.ReservedSlotCount >= maxPlayers) || (Instance.Configuration.Instance.AllowDynamicMaxSlot && numPlayers + Instance.Configuration.Instance.ReservedSlotCount >= Instance.Configuration.Instance.MaxSlotCount))
                {
                    // Kick if they aren't a reserved player.
                    if (!CheckReserved(CSteamID))
                    {
                        rejectionReason = ESteamRejection.SERVER_FULL;
                    }
                }
            }
        }

        // Adjust the max player count on player connect and disconnect, if the dynamic slots feature is enabled.
        private void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            if (Instance.Configuration.Instance.AllowDynamicMaxSlot)
                SetMaxPlayers();
        }

        private void Events_OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (Instance.Configuration.Instance.AllowDynamicMaxSlot)
                SetMaxPlayers(true);
        }

        private bool CheckReserved(CSteamID CSteamID)
        {
            if (SteamAdminlist.checkAdmin(CSteamID))
            {
                return true;
            }
            else
            {
                foreach (RocketPermissionsGroup group in R.Permissions.GetGroups(new RocketPlayer(CSteamID.ToString()), true))
                {
                    if (Instance.Configuration.Instance.Groups.Contains(group.Id))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
