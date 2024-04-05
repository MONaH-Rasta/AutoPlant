using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Facepunch;
using System.Linq;

namespace Oxide.Plugins {
    [Info("Auto Plant", "Egor Blagov / rostov114", "1.1.0")]
    [Description("Automation of your plantations")]
    class AutoPlant : RustPlugin {

        #region Configuration
        private Configuration _config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "Auto Plant permission")]
            public string autoPlant = "autoplant.use";

            [JsonProperty(PropertyName = "Auto Gather permission")]
            public string autoGather = "autoplant.gather.use";

            [JsonProperty(PropertyName = "Auto Cutting permission")]
            public string autoCutting = "autoplant.cutting.use";

            [JsonProperty(PropertyName = "Auto Remove Dying permission")]
            public string autoDying = "autoplant.removedying.use";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintError("Error reading config, please check!");

                Unsubscribe(nameof(OnGrowableGather));
                Unsubscribe(nameof(CanTakeCutting));
                Unsubscribe(nameof(OnRemoveDying));
                Unsubscribe(nameof(OnEntityBuilt));
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        #region Oxide Hooks
        private void Init() 
        {
            permission.RegisterPermission(_config.autoPlant, this);
            permission.RegisterPermission(_config.autoGather, this);
            permission.RegisterPermission(_config.autoCutting, this);
            permission.RegisterPermission(_config.autoDying, this);
        }

        private object OnGrowableGather(GrowableEntity plant, BasePlayer player)
        {
            if (!this.CheckUsing(player, plant, _config.autoGather))
                return null;

            PlanterBox planterBox = plant.GetParentEntity() as PlanterBox;

            Unsubscribe(nameof(OnGrowableGather));
            foreach (GrowableEntity growable in planterBox.children.ToList())
            {
                growable.PickFruit(player);
            }
            Subscribe(nameof(OnGrowableGather));

            return true;
        }

        private object CanTakeCutting(BasePlayer player, GrowableEntity plant)
        {
            if (!this.CheckUsing(player, plant, _config.autoCutting))
                return null;

            PlanterBox planterBox = plant.GetParentEntity() as PlanterBox;

            Unsubscribe(nameof(CanTakeCutting));
            foreach (GrowableEntity growable in planterBox.children.ToList())
            {
                if (growable.CanClone())
                {
                    growable.TakeClones(player);
                }
            }
            Subscribe(nameof(CanTakeCutting));

            return true;
        }

        private object OnRemoveDying(GrowableEntity plant, BasePlayer player)
        {
            if (!this.CheckUsing(player, plant, _config.autoDying))
                return null;

            PlanterBox planterBox = plant.GetParentEntity() as PlanterBox;

            Unsubscribe(nameof(OnRemoveDying));
            foreach (GrowableEntity growable in planterBox.children.ToList())
            {
                growable.RemoveDying(player);
            }
            Subscribe(nameof(OnRemoveDying));

            return true;
        }

        private void OnEntityBuilt(Planner plan, GameObject seed) 
        {
            BasePlayer player = plan.GetOwnerPlayer();
            GrowableEntity plant = seed.GetComponent<GrowableEntity>();
            if (player == null || plant == null || !permission.UserHasPermission(player.UserIDString, _config.autoPlant))
                return;

            NextTick(() => 
            {
                Item held = player.GetActiveItem();
                if (held == null || held.amount == 0)
                    return;

                if (player.serverInput.IsDown(BUTTON.SPRINT) && plant.GetParentEntity() is PlanterBox)
                {
                    PlanterBox planterBox = plant.GetParentEntity() as PlanterBox;
                    Construction construction = PrefabAttribute.server.Find<Construction>(plan.GetDeployable().prefabID);
                    List<Construction.Target> targets = Pool.GetList<Construction.Target>();
                    foreach (Socket_Base sock in PrefabAttribute.server.FindAll<Socket_Base>(planterBox.prefabID)) 
                    {
                        if (!sock.female)
                            continue;

                        Vector3 socketPoint = planterBox.transform.TransformPoint(sock.worldPosition);
                        Construction.Target target = new Construction.Target();

                        target.entity = planterBox;
                        target.ray = new Ray(socketPoint + Vector3.up * 1.0f, Vector3.down);
                        target.onTerrain = false;
                        target.position = socketPoint;
                        target.normal = Vector3.up;
                        target.rotation = new Vector3();
                        target.player = player;
                        target.valid = true;
                        target.socket = sock;
                        target.inBuildingPrivilege = true;

                        if (!this.IsFree(construction, target))
                            continue;

                        targets.Add(target);
                    }

                    Unsubscribe(nameof(OnEntityBuilt));
                    foreach (Construction.Target target in targets) 
                    {
                        plan.DoBuild(target, construction);
                        if (held.amount == 0)
                            break;
                    }
                    Subscribe(nameof(OnEntityBuilt));

                    Pool.FreeList(ref targets);
                }
            });
        }
        #endregion

        #region Helpers
        public bool IsFree(Construction common, Construction.Target target) 
        {
            List<Socket_Base> list = Facepunch.Pool.GetList<Socket_Base>();
            common.FindMaleSockets(target, list);
            Socket_Base socketBase = list[0];
            Facepunch.Pool.FreeList(ref list);
            return !target.entity.IsOccupied(target.socket) && socketBase.CheckSocketMods(socketBase.DoPlacement(target));
        }

        public bool CheckUsing(BasePlayer player, GrowableEntity plant, string perm)
        {
            if (player == null || plant == null || !permission.UserHasPermission(player.UserIDString, perm))
                return false;

            if (player.serverInput.IsDown(BUTTON.SPRINT) && plant.GetParentEntity() is PlanterBox)
                return true;

            return false;
        }
        #endregion
    }
}
