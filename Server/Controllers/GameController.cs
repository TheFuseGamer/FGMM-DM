using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FGMM.SDK.Server.Controllers;
using FGMM.SDK.Core.Diagnostics;
using FGMM.SDK.Server.Events;
using FGMM.SDK.Server.RPC;
using FGMM.Gamemode.DM.Shared.Models;
using FGMM.SDK.Core.RPC.Events;
using CitizenFX.Core;
using FGMM.Gamemode.DM.Shared.Events;

namespace FGMM.Gamemode.DM.Server.Controllers
{
    class GameController : Controller
    {
        private Mission Mission { get; set; }

        private List<Player> AlivePlayers { get; set; }
        private Dictionary<Player, int> Scores { get; set; }


        public GameController(ILogger logger, IEventManager events, IRpcHandler rpc) : base(logger, events, rpc)
        {          
        }

        public void StartMission(Mission mission)
        {
            Mission = mission;
            AlivePlayers = new List<Player>();
            Scores = new Dictionary<Player, int>();
        }

        public async void StartGame()
        {
            await BaseScript.Delay(0);
            Logger.Debug("Starting the game");
            foreach(Player player in AlivePlayers)
                Rpc.Event(DMEvents.Start).Trigger(player);
        }

        public void SpawnPlayer(Player player)
        {
            SpawnData spawnData = new SpawnData()
            {
                Position = Mission.Team.GetRandomSpawnPoint(),
                Skin = Mission.Team.GetRandomSkin(),
                Loadout = Mission.Team.Loadout
            };
            Rpc.Event(DMEvents.Spawn).Trigger(player, spawnData);
        }

        public bool AddPlayerToTeam(Player player)
        {
            AlivePlayers.Add(player);
            Scores.Add(player, 0);
            SpawnPlayer(player);
            return true;
        }

        public void ProcessDeathEvent(Player player, Player killer)
        {
            if (player == null)
                return;
            AlivePlayers.Remove(player);
            if (killer != null)
                Scores[killer]++;

            if (AlivePlayers.Count <= 1)
                Events.Raise(ServerEvents.EndMission);
            else
                Rpc.Event(DMEvents.Spectate).Trigger(player);
        }

        public void ProcessDisconnectionEvent(Player player)
        {
            if (player == null)
                return;
            AlivePlayers.Remove(player);

            if (AlivePlayers.Count <= 1)
                Events.Raise(ServerEvents.EndMission);
        }

        public bool IsGameTied()
        {
            return AlivePlayers.Count > 1;
        }
    }
}
