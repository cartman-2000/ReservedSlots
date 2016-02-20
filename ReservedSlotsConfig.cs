using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ReservedSlots
{
    public class ReservedSlotsConfig : IRocketPluginConfiguration
    {
        public bool ReservedSlotEnable = true;
        [XmlArray("Groups"), XmlArrayItem(ElementName = "Group")]
        public List<string> Groups = new List<string>();
        public byte ReservedSlotCount = 2;
        public bool AllowFill = false;
        public bool AllowDynamicMaxSlot = false;
        public byte MaxSlotCount = 24;
        public byte MinSlotCount = 16;

        public void LoadDefaults()
        {
            Groups = new List<string>
            {
                "moderator",
                "admin"
            };
        }
    }
}