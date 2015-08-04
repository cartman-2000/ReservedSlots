using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Permissions;
using SDG.Unturned;
using Steamworks;
using Rocket.Core.Serialisation;
using Rocket.Core;
using Rocket.API;

namespace ReservedSlots
{
    public class RocketPlayer : IRocketPlayer
    {
        public string Id
        {
            get; set;
        }

        public string DisplayName
        {
            get; set;
        }
    }

    public class ReservedSlots : RocketPlugin<ReservedSlotsConfig>
    {
        public ReservedSlots Instance;

        protected override void Load()
        {
            Instance = this;
            UnturnedPermissions.OnJoinRequested += Events_OnJoinRequested;
            Logger.Log(string.Format("Reserved Slots enabled: {0}, Count: {1}, Allowfill: {2}.", Instance.Configuration.Instance.ReservedSlotEnable, Instance.Configuration.Instance.ReservedSlotCount, Instance.Configuration.Instance.AllowFill));
        }

        private void Events_OnJoinRequested(CSteamID CSteamID, ref ESteamRejection? rejectionReason)
        {
            if (Instance.Configuration.Instance.ReservedSlotEnable && Instance.Configuration.Instance.ReservedSlotCount > 0 && Instance.Configuration.Instance.Groups != null && Instance.Configuration.Instance.Groups.Count > 0)
            {
                int numPlayers = Steam.Players.Count;
                int maxPlayers = Steam.maxPlayers;
                if (!Instance.Configuration.Instance.AllowFill)
                {
                    // Don't allow the slots to fill.
                    if (numPlayers + Instance.Configuration.Instance.ReservedSlotCount >= maxPlayers)
                    {
                        if (!CheckReserved(CSteamID))
                        {
                            rejectionReason = ESteamRejection.SERVER_FULL;
                        }
                    }
                }
                else
                {
                    // Allow them to fill.
                    foreach (SteamPlayer player in Steam.Players)
                    {
                        if (CheckReserved(player.SteamPlayerID.CSteamID))
                        {
                            numPlayers--;
                        }
                    }
                    if (numPlayers + Instance.Configuration.Instance.ReservedSlotCount >= maxPlayers)
                    {
                        if (!CheckReserved(CSteamID))
                        {
                            rejectionReason = ESteamRejection.SERVER_FULL;
                        }
                    }
                }
            }
        }

        private bool CheckReserved(CSteamID CSteamID)
        {
            if (SteamAdminlist.checkAdmin(CSteamID))
            {
                return true;
            }
            else
            {
                foreach (RocketPermissionsGroup group in R.Permissions.GetGroups(new RocketPlayer() { Id = CSteamID.ToString(), DisplayName = "" }, true))
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
