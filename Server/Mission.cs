using System.Collections.Generic;
using FGMM.SDK.Server.Models;
using FGMM.SDK.Core.Models;
using System.IO;
using System.Xml.Serialization;

namespace FGMM.Gamemode.DM.Server
{
    public class Mission : IMission
    {
        public string Name { get; set; }
        public string Gamemode { get; set; }
        public int Duration { get; set; }
        public SelectionData SelectionData { get; set; }
        public Team Team { get; set; }

        public static Mission Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Mission));
            using (StreamReader reader = new StreamReader(path))
            {
                Mission mission = (Mission)serializer.Deserialize(reader);
                reader.Close();
                mission.SelectionData.Teams = new List<string>();
                mission.SelectionData.Teams.Add(mission.Team.Name);
                return mission;
            }
        }

    }
}
