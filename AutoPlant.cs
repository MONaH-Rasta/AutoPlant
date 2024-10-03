// #define DEBUG

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Facepunch;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Auto Plant", "Egor Blagov / rostov114", "1.2.7")]
    [Description("Automation of your plantations")]
    class AutoPlant : RustPlugin
    {
        #region Variables
        public static AutoPlant _instance;
        private List<ulong> _activeUse = new List<ulong>();
        #endregion

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

            [JsonProperty(PropertyName = "Auto fertilizer permission")]
            public string autoFertilizer = "autoplant.fertilizer.use";
            
            [JsonProperty(PropertyName = "Auto fertilizer configuration")]
            public FertilizerConfiguration fertilizer = new FertilizerConfiguration();
            
            public class FertilizerConfiguration
            {
                [JsonProperty(PropertyName = "Maximum distance from the PlanterBox")]
                public int maxDistance = 5;

                [JsonProperty(PropertyName = "Default fertilizer amount")]
                public int defaultAmount = 100;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                SaveConfig();
            }
            catch
            {
                PrintError("Error reading config, please check!");

                Unsubscribe(nameof(OnGrowableGather));
                Unsubscribe(nameof(CanTakeCutting));
                Unsubscribe(nameof(OnRemoveDying));
                Unsubscribe(nameof(OnEntityBuilt));
                Unsubscribe(nameof(OnActiveItemChanged));
                Unsubscribe(nameof(OnPlayerInput));
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        #region Data
        private Dictionary<ulong, int> _data = new Dictionary<ulong, int>();
        public static class Data
        {
            public static int Get(ulong userID)
            {
                if (_instance._data.ContainsKey(userID))
                {
                    return _instance._data[userID];
                }

                return _instance._config.fertilizer.defaultAmount;
            }

            public static void Set(ulong userID, int amount)
            {
                _instance._data[userID] = amount;
                Data.Save();
            }

            public static void Save()
            {
                Interface.Oxide.DataFileSystem.WriteObject(_instance.Name, _instance._data);
            }

            public static void Load()
            {
                try
                {
                    _instance._data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>(_instance.Name);
                }
                catch (Exception e)
                {
                    _instance.PrintError(e.Message);

                    _instance.Unsubscribe(nameof(OnActiveItemChanged));
                    _instance.Unsubscribe(nameof(OnPlayerInput));
                }
            }
        }
        #endregion

        #region Language
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"notAllowed", "You are not allowed to use this command!"},
                {"changeAmount", "The amount of fertilizer transferred has been changed to {0} pcs."},
                {"currentAmount", "Current amount of fertilizer transferred: {0} pcs."}
            }, this, "en");
            
            lang.RegisterMessages(new Dictionary<string, string>() 
            {
                {"notAllowed", "Вам не разрешено использование данной команды!" },
                {"changeAmount", "Количество перекладываемого удобрения изменено на {0} шт." },
                {"currentAmount", "Текущее количество перекладываемого удобрения: {0} шт." }
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"notAllowed", "Вам не дозволено використання цієї команди!" },
                {"changeAmount", "Кількість добрива, що перекладається, змінено на: {0} шт." },
                {"currentAmount", "Поточна кількість добрива, що перекладається: {0} шт." }
            }, this, "uk");
        }

        private string _(BasePlayer player, string key, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
        }
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            permission.RegisterPermission(_config.autoPlant, this);
            permission.RegisterPermission(_config.autoGather, this);
            permission.RegisterPermission(_config.autoCutting, this);
            permission.RegisterPermission(_config.autoDying, this);
            permission.RegisterPermission(_config.autoFertilizer, this);

            checkSubscribeHooks();
        }

        private void Loaded()
        {
            _instance = this;
            Data.Load();
        }

        private object OnGrowableGather(GrowableEntity plant, BasePlayer player)
        {
            if (plant.IsBusy())
                return null;

            List<BaseEntity> growables;
            if (!this.GetGrowables(player, plant, _config.autoGather, out growables))
                return null;

            foreach (BaseEntity growable in growables)
            {
                if (growable != null && growable is GrowableEntity)
                {
                    GrowableEntity _growable = growable as GrowableEntity;
                    if (_growable != null)
                    {
                        _growable.SetFlag(BaseEntity.Flags.Busy, true, false, false);
                        _growable.PickFruit(player);
                        _growable.SetFlag(BaseEntity.Flags.Busy, false, false, false);
                    }
                }
            }

            return true;
        }

        private object CanTakeCutting(BasePlayer player, GrowableEntity plant)
        {
            if (plant.IsBusy())
                return null;

            List<BaseEntity> growables;
            if (!this.GetGrowables(player, plant, _config.autoCutting, out growables))
                return null;

            foreach (BaseEntity growable in growables)
            {
                if (growable != null && growable is GrowableEntity)
                {
                    GrowableEntity _growable = growable as GrowableEntity;
                    if (_growable != null)
                    {
                        _growable.SetFlag(BaseEntity.Flags.Busy, true, false, false);
                        _growable.TakeClones(player);
                        _growable.SetFlag(BaseEntity.Flags.Busy, false, false, false);
                    }
                }
            }

            return true;
        }

        private object OnRemoveDying(GrowableEntity plant, BasePlayer player)
        {
            if (plant.IsBusy())
                return null;

            List<BaseEntity> growables;
            if (!this.GetGrowables(player, plant, _config.autoDying, out growables))
                return null;

            foreach (BaseEntity growable in growables)
            {
                if (growable != null && growable is GrowableEntity)
                {
                    GrowableEntity _growable = growable as GrowableEntity;
                    if (_growable != null)
                    {
                        _growable.SetFlag(BaseEntity.Flags.Busy, true, false, false);
                        _growable.RemoveDying(player);
                        _growable.SetFlag(BaseEntity.Flags.Busy, false, false, false);
                    }
                }
            }

            return true;
        }

        private void OnEntityBuilt(Planner plan, GameObject seed) 
        {
            if (plan.IsBusy())
                return;

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

                    plan.SetFlag(BaseEntity.Flags.Busy, true, false, false);
                    foreach (Construction.Target target in targets) 
                    {
                        plan.DoBuild(target, construction);
                        if (held.amount == 0)
                            break;
                    }
                    plan.SetFlag(BaseEntity.Flags.Busy, false, false, false);

                    Pool.FreeList(ref targets);
                }
            });
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null)
                return;

            if (newItem != null && newItem.info.shortname == "fertilizer")
            {
                if (permission.UserHasPermission(player.UserIDString, _config.autoFertilizer))
                {
                    _activeUse.Add(player.userID.Get());
                    checkSubscribeHooks();
                }
            }
            else
            {
                if (_activeUse.Contains(player.userID.Get()))
                {
                    _activeUse.Remove(player.userID.Get());
                    checkSubscribeHooks();
                }
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (_activeUse.Contains(player.userID.Get()) && input.WasJustReleased(BUTTON.FIRE_PRIMARY))
            {
                Item activeItem = player.GetActiveItem();
                if (activeItem == null)
                    return;

                if (activeItem.info.shortname != "fertilizer")
                    return;

                PlanterBox planterBox;
                if (IsLookingPlanterBox(player, out planterBox))
                {
                    int amount = Data.Get(player.userID.Get());
                    Item moveItem = (amount >= activeItem.amount) ? activeItem : activeItem.SplitItem(amount);

                    player.Command("note.inv", (object)moveItem.info.itemid, (object)-moveItem.amount);
                    EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/impacts/physics/phys-impact-meat-soft.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);

                    if (!moveItem.MoveToContainer(planterBox.inventory, -1, true))
                    {
                        moveItem.Drop(player.GetDropPosition(), player.GetDropVelocity(), default(Quaternion));
                    }
                }
            }
        }
#if DEBUG
        private void OnHammerHit(BasePlayer player, HitInfo info) 
        {
            if (player == null || info == null || info?.HitEntity == null) 
                return;

            if (player.IsAdmin && player.serverInput.IsDown(BUTTON.FIRE_SECONDARY) && (info.HitEntity is PlanterBox))
            {
                (info.HitEntity as PlanterBox).DoSplash(ItemManager.FindItemDefinition("water"), 9000);
            }
        }
#endif
        #endregion

        #region Chat Command
        [ChatCommand("fertilizer")]
        private void fertilizer_command(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, _config.autoFertilizer))
            {
                SendReply(player, _(player, "notAllowed"));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, _(player, "currentAmount", Data.Get(player.userID.Get())));
                return;
            }

            int amount = _config.fertilizer.defaultAmount;
            try
            {
                amount = Int32.Parse(args[0]);
            }
            catch { }

            Data.Set(player.userID.Get(), amount);
            SendReply(player, _(player, "changeAmount", amount));
        }
        #endregion

        #region Helpers
        public void checkSubscribeHooks()
        {
            if (_activeUse.Count > 0)
            {
                Subscribe(nameof(OnPlayerInput));
            }
            else
            {
                Unsubscribe(nameof(OnPlayerInput));
            }
        }

        public bool IsFree(Construction common, Construction.Target target) 
        {
            List<Socket_Base> list = Facepunch.Pool.GetList<Socket_Base>();
            common.FindMaleSockets(target, list);
            Socket_Base socketBase = list[0];
            Facepunch.Pool.FreeList(ref list);
            Construction.Placement placement = socketBase.DoPlacement(target);
            return !target.entity.IsOccupied(target.socket) && socketBase.CheckSocketMods(ref placement);
        }

        public bool GetGrowables(BasePlayer player, GrowableEntity plant, string perm, out List<BaseEntity> growables)
        {
            growables = null;

            if (player == null || plant == null || !permission.UserHasPermission(player.UserIDString, perm))
                return false;

            if (player.serverInput.IsDown(BUTTON.SPRINT))
            {
                PlanterBox planterBox = plant.GetPlanter();
                if (planterBox == null || planterBox?.children == null || planterBox.children.Count == 0)
                    return false;

                growables = planterBox.children.ToList();
                return true;
            }

            return false;
        }

        private bool IsLookingPlanterBox(BasePlayer player, out PlanterBox planterBox)
        {
            RaycastHit hit;
            planterBox = null;

            if (Physics.Raycast(player.eyes.HeadRay(), out hit, _config.fertilizer.maxDistance, LayerMask.GetMask("Deployed")))
            {
                BaseEntity entity = hit.GetEntity();
                if (entity is PlanterBox)
                {
                    planterBox = entity as PlanterBox;
                }
            }

            return planterBox != null;
        }
        #endregion
    }
}