using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ReservedSlots
{
    public class ReservedSlotsConfig : IRocketPluginConfiguration
    {
        public bool ReservedSlotEnable;
        [XmlArray("Groups"), XmlArrayItem(ElementName = "Group")]
        public List<string> Groups;
        public int ReservedSlotCount;
        public bool AllowFill;

        public void LoadDefaults()
        {
            ReservedSlotEnable = true;
            Groups = new List<string>
            {
                "moderator",
                "admin"
            };
            ReservedSlotCount = 2;
            AllowFill = false;
        }
    }
}