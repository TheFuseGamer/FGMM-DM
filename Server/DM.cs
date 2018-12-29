using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using FGMM.SDK.Server.Diagnostics;
using FGMM.SDK.Server.Gamemodes;
using FGMM.SDK.Server.Models;
using FGMM.SDK.Server.Events;
using FGMM.SDK.Server.RPC;
using FGMM.SDK.Server.Controllers;
using FGMM.SDK.Core.Diagnostics;
using FGMM.SDK.Core.RPC.Events;
using CitizenFX.Core.Native;
using System.Timers;
using FGMM.Gamemode.DM.Server.Controllers;
using FGMM.Gamemode.DM.Shared.Events;

namespace FGMM.Gamemode.DM.Server
{
    class DM : Controller, IGamemode
    {
        public IMission Mission { get; set; }
        public int RemaingingTime { get; set; }

        private Mission _Mission { get; set; }

        private Timer MissionTimer { get; set; }

        private GameController GameController { get; set; }

        private bool GameStarted = false;

        public DM(ILogger logger, IEventManager events, IRpcHandler rpc) : base(logger, events, rpc)
        {
            Events = events;
            Rpc = rpc;
            GameController = new GameController(new Logger("DM | GameController"), Events, Rpc);
            MissionTimer = new Timer(1000);
            MissionTimer.AutoReset = true;
            MissionTimer.Elapsed += MissionTimer_Elapsed;
        }

        public void Start(string mission)
        {
            Logger.Info($"Loading mission: {mission}");
            string Path = $"{API.GetResourcePath(API.GetCurrentResourceName())}/Missions/{mission}";
            Mission = Server.Mission.Load(Path);
            _Mission = Mission as Mission;
            RemaingingTime = Mission.Duration;
            if (_Mission.SelectionData.Teams.Count != 1)
                throw new Exception("This DM gamemode requires 1 team.");

            GameController.StartMission(_Mission);

            // Start game timer          
            MissionTimer.Start();

            Logger.Debug($"Loaded: {_Mission.Name}");
        }

        private void MissionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RemaingingTime--;
            if (RemaingingTime <= 0)
            {
                if (GameController.IsGameTied() && !GameStarted)
                {
                    GameController.StartGame();
                    GameStarted = true;
                }
                else
                {
                    Logger.Debug("Ending mission");
                    Events.Raise(ServerEvents.EndMission);
                }

                MissionTimer.Stop();
            }
        }

        public void Stop()
        {
            Logger.Info("Stopping DM gamemode...");
            GameStarted = false;
            MissionTimer?.Stop();
        }

        public void HandleDeath(Player player, Player killer)
        {
            GameController?.ProcessDeathEvent(player, killer);
        }

        public void HandleDisconnect(Player player)
        {
            throw new NotImplementedException();
        }

        public bool HandleTeamJoinRequest(Player player, int team)
        {
            if (GameStarted)
            {
                Rpc.Event(DMEvents.Spectate).Trigger();
                return true;
            }
            Rpc.Event(DMEvents.UpdateTimer).Trigger(RemaingingTime);
            return GameController.AddPlayerToTeam(player);
        }
    }
}
