using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FGMM.SDK.Client.Events;
using FGMM.SDK.Client.RPC;
using FGMM.SDK.Client.Services;
using FGMM.SDK.Core.Diagnostics;
using FGMM.SDK.Core.RPC.Events;
using FGMM.Gamemode.DM.Shared.Models;
using FGMM.Gamemode.DM.Shared.Events;
using FGMM.SDK.Client.UI;
using FGMM.SDK.Core.RPC;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;


namespace FGMM.Gamemode.DM.Client.Services
{
    class GameService : Service
    {
        private int RemainingTime = 0;

        public GameService(ILogger logger, IEventManager events, IRpcHandler rpc, ITickManager tickManager) : base(logger, events, rpc, tickManager)
        {
            Logger.Info("Game service started.");
            Rpc.Event(DMEvents.Spawn).On<SpawnData>(OnSpawnRequested);
            Rpc.Event(DMEvents.Start).On(OnGameStarted);
            Rpc.Event(DMEvents.UpdateTimer).On<int>(OnTimerUpdated);
            Rpc.Event(ServerEvents.MissionEnded).On(OnMissionEnded);

            TickManager.Attach(MissionTimerTick);
        }

        private async Task MissionTimerTick()
        {
            if (RemainingTime > 0)
            {
                RemainingTime--;
                UpdateStartMessage(RemainingTime);
            }
            await BaseScript.Delay(1000);
        }

        private void OnTimerUpdated(IRpcEvent rpc, int time)
        {
            RemainingTime = time;
            if (RemainingTime == 0)
                return;
            UpdateStartMessage(RemainingTime);
        }

        private void OnGameStarted(IRpcEvent obj)
        {
            Game.Player.CanControlCharacter = true;
            Game.PlayerPed.IsPositionFrozen = false;
            API.ClearPedTasksImmediately(Game.PlayerPed.Handle);

            ToggleStartMessage(false);
            Screen.Hud.IsRadarVisible = true;
        }

        private void OnMissionEnded(IRpcEvent obj)
        {
            ToggleStartMessage(false);
        }

        private async void OnSpawnRequested(IRpcEvent rpc, SpawnData data)
        {
            await SpawnPlayer(data);
        }

        private async Task SpawnPlayer(SpawnData data, bool respawn = false)
        {
            Screen.Fading.FadeOut(0);

            API.RenderScriptCams(false, false, 0, false, false);
            Game.PlayerPed.Resurrect();

            if (!respawn)
            {
                while (!await Game.Player.ChangeModel(new Model((PedHash)Enum.Parse(typeof(PedHash), data.Skin, true)))) await BaseScript.Delay(500);
                API.SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
            }

            EquipLoadout(data.Loadout);
            Game.PlayerPed.PositionNoOffset = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
            Game.PlayerPed.Rotation = new Vector3(0, 0, data.Position.A);

            Screen.Hud.IsRadarVisible = true;

            while (!API.HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle))
                await BaseScript.Delay(100);

            API.ClearPedTasksImmediately(Game.PlayerPed.Handle);

            Screen.Fading.FadeIn(1000);
            UpdateStartMessage(RemainingTime);
            ToggleStartMessage(true);
        }

        private void EquipLoadout(List<SDK.Core.Models.Weapon> loadout)
        {
            foreach (SDK.Core.Models.Weapon weapon in loadout)
                Game.PlayerPed.Weapons.Give((WeaponHash)Enum.Parse(typeof(WeaponHash), weapon.Hash, true), (int)weapon.Ammo, false, true);

            Game.PlayerPed.Weapons.Select(Game.PlayerPed.Weapons.BestWeapon);
        }

        private void ToggleStartMessage(bool toggle)
        {
            Serializer serializer = new Serializer();
            Dictionary<string, object> message = new Dictionary<string, object>()
            {
                { "type", "ToggleDMStartMessage"},
                { "toggle", toggle}
            };
            API.SendNuiMessage(serializer.Serialize(message));
        }

        private async void UpdateStartMessage(int seconds)
        {
            await BaseScript.Delay(0);
            string msg = $"Waiting for players ({seconds}s)...";
            Serializer serializer = new Serializer();
            Dictionary<string, object> message = new Dictionary<string, object>()
            {
                { "type", "UpdateDMStartMessageText"},
                { "message", msg}
            };
            API.SendNuiMessage(serializer.Serialize(message));
        }
    }
}
