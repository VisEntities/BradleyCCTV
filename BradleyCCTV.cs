using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Bradley CCTV", "Dana", "2.0.1")]
    [Description("The ultimate surveillance upgrade for your Bradley APCs.")]
    public class BradleyCCTV : RustPlugin
    {
        #region Fields

        private static BradleyCCTV instance;
        private static Configuration config;

        private Dictionary<BradleyAPC, List<string>> surveilledBradleys = new Dictionary<BradleyAPC, List<string>>();

        private const string cctvCameraPrefab = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab";

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty(PropertyName = "Version")]
            public string Version { get; set; }

            [JsonProperty(PropertyName = "Front Camera")]
            public CameraOptions FrontCamera { get; set; }

            [JsonProperty(PropertyName = "Back Camera")]
            public CameraOptions BackCamera { get; set; }
        }

        private class CameraOptions
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; }

            [JsonProperty(PropertyName = "Static")]
            public bool Static { get; set; }

            [JsonProperty(PropertyName = "Up Down Rotation")]
            public float UpDownRotation { get; set; }

            [JsonProperty(PropertyName = "Right Left Rotation")]
            public float RightLeftRotation { get; set; }

            [JsonProperty(PropertyName = "Position")]
            public Vector3 Position { get; set; }

            [JsonProperty(PropertyName = "Rotation")]
            public Vector3 Rotation { get; set; }
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                FrontCamera = new CameraOptions
                {
                    Enabled = true,
                    Static = true,
                    UpDownRotation = 4f,
                    RightLeftRotation = 0f,
                    Position = new Vector3(-0.03f, 1.8f, 2.15f),
                    Rotation = Quaternion.Euler(0, 0, 0).eulerAngles
                },
                BackCamera = new CameraOptions
                {
                    Enabled = true,
                    Static = true,
                    UpDownRotation = -3f,
                    RightLeftRotation = 0f,
                    Position = new Vector3(0.0f, 1.7f, -2.93f),
                    Rotation = Quaternion.Euler(13, 180, 0).eulerAngles
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();

            if (string.Compare(config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Detected changes in configuration! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(config.Version, "1.0.0") < 0)
                config = defaultConfig;

            PrintWarning("Configuration update complete! Updated from version " + config.Version + " to " + Version.ToString());
            config.Version = Version.ToString();
        }

        #endregion Configuration

        #region Oxide Hooks

        /// <summary>
        /// Hook: Called after server startup is complete or when the plugin is hotloaded while the server is running.
        /// </summary>
        private void OnServerInitialized()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                BradleyAPC bradley = entity as BradleyAPC;

                if (!bradley.IsValid() || HasCamerasAttached(bradley))
                    continue;

                surveilledBradleys.Add(bradley, new List<string>());
                SetupCameras(bradley);
            }
        }

        /// <summary>
        /// Hook: Called when a plugin is being initialized.
        /// </summary>
        private void Init()
        {
            instance = this;
            Permission.Register();
        }

        /// <summary>
        /// Hook: Called when a plugin is being unloaded.
        /// </summary>
        private void Unload()
        {
            CleanupCameras();
            instance = null;
            config = null;
        }

        /// <summary>
        /// Hook: Called after any networked entity has spawned.
        /// </summary>
        /// <param name="bradley"> The bradley apc that was spawned. </param>
        private void OnEntitySpawned(BradleyAPC bradley)
        {
            if (!bradley.IsValid() || bradley.IsDestroyed)
                return;

            surveilledBradleys.Add(bradley, new List<string>());
            SetupCameras(bradley);
        }

        private void OnEntityKill(BradleyAPC bradley)
        {
            if (bradley != null && surveilledBradleys.ContainsKey(bradley))
            {
                List<string> cameraIdentifiers = surveilledBradleys[bradley];
                if (cameraIdentifiers.Count > 0)
                {
                    List<string> identifiersToRemove = Pool.GetList<string>();
                    foreach (string identifier in cameraIdentifiers)
                        identifiersToRemove.Add(identifier);

                    Puts("Bradley killed. " + identifiersToRemove.Count + " camera identifiers removed!");
                    identifiersToRemove.Clear();

                    Pool.FreeList(ref identifiersToRemove);
                }

                surveilledBradleys.Remove(bradley);
            }
        }

        #endregion Oxide Hooks

        #region Functions

        private void SetupCameras(BradleyAPC bradley)
        {
            if (!bradley.IsValid())
                return;

            SpawnCamera(bradley, config.FrontCamera);
            SpawnCamera(bradley, config.BackCamera);
        }

        private void SpawnCamera(BradleyAPC bradley, CameraOptions cameraOptions)
        {
            if (!cameraOptions.Enabled)
                return;

            CCTV_RC cctvCamera = GameManager.server.CreateEntity(cctvCameraPrefab, bradley.transform.position, bradley.transform.rotation, true) as CCTV_RC;
            if (cctvCamera)
            {
                UnityEngine.Object.DestroyImmediate(cctvCamera.GetComponent<DestroyOnGroundMissing>());

                cctvCamera.SetParent(bradley);
                cctvCamera.Spawn();

                cctvCamera.transform.localPosition = cameraOptions.Position;
                cctvCamera.transform.localRotation = Quaternion.Euler(cameraOptions.Rotation);
                cctvCamera.pitchAmount = cameraOptions.UpDownRotation;
                cctvCamera.yawAmount = cameraOptions.RightLeftRotation;
                cctvCamera.isStatic = cameraOptions.Static;

                string identifier = GenerateIdentifier();

                cctvCamera.UpdateIdentifier(identifier);
                cctvCamera.UpdateHasPower(5, 1);
                cctvCamera.SendNetworkUpdate();

                surveilledBradleys[bradley].Add(identifier);
                Puts("Camera '" + identifier + "' successfully attached to Bradley!");
            }
        }

        private void CleanupCameras()
        {
            if (!surveilledBradleys.Any())
                return;

            int removedCameras = 0;
            foreach (BradleyAPC bradley in surveilledBradleys.Keys)
            {
                if (bradley.IsValid() && !bradley.IsDestroyed)
                {
                    List<CCTV_RC> cctvCameras = Pool.GetList<CCTV_RC>();
                    cctvCameras = GetCamerasAttached(bradley, cctvCameras);

                    if (cctvCameras.Any())
                    {
                        foreach (CCTV_RC cctvCamera in cctvCameras)
                        {
                            cctvCamera?.Kill();
                            removedCameras++;
                        }
                    }
                    Pool.FreeList(ref cctvCameras);
                }
            }
            Puts("Cleanup complete. " + removedCameras + " cctv cameras removed!");
            surveilledBradleys.Clear();
        }

        private bool HasCamerasAttached(BradleyAPC bradley)
        {
            List<CCTV_RC> cctvCameras = Pool.GetList<CCTV_RC>();
            cctvCameras = GetCamerasAttached(bradley, cctvCameras);

            bool result = false;
            if (cctvCameras.Any())
                result = true;

            Pool.FreeList(ref cctvCameras);
            return result;
        }

        private List<CCTV_RC> GetCamerasAttached(BradleyAPC bradley, List<CCTV_RC> cctvCameras)
        {
            foreach (BaseEntity child in bradley.children)
            {
                CCTV_RC cctvCamera = child as CCTV_RC;
                if (cctvCamera != null && child.PrefabName == cctvCameraPrefab)
                {
                    cctvCameras.Add(cctvCamera);
                }
            }
            return cctvCameras;
        }

        private string GenerateIdentifier()
        {
            const int maxLength = 12;
            const string prefix = "BRADLEY";

            string identifier = prefix + Random.Range(0, int.MaxValue).ToString();
            if (identifier.Length > maxLength)
                identifier = identifier.Substring(0, maxLength);

            return identifier;
        }

        #endregion Functions

        #region Permissions

        /// <summary>
        /// Contains utility methods for checking and registering plugin permissions.
        /// </summary>
        private static class Permission
        {
            // Permission required to use commands.
            public const string Use = "bradleycctv.use";

            /// <summary>
            /// Registers permissions used by the plugin.
            /// </summary>
            public static void Register()
            {
                instance.permission.RegisterPermission(Use, instance);
            }

            /// <summary>
            /// Determines whether the given player has the specified permission.
            /// </summary>
            /// <param name="player"> The player to check. </param>
            /// <param name="permissionName"> The name of the permission to check. </param>
            /// <returns> True if the player has the permission, false otherwise. </returns>
            public static bool Verify(BasePlayer player, string permissionName)
            {
                if (instance.permission.UserHasPermission(player.UserIDString, permissionName))
                    return true;

                instance.SendReply(player, "You do not have the necessary permission to use the command.");
                return false;
            }
        }

        #endregion Permissions

        #region Commands

        private static class Command
        {
            public const string Identifier = "bradley.cctv";
        }

        [ChatCommand(Command.Identifier)]
        private void cmdIdentifier(BasePlayer player, string cmd, string[] args)
        {
            // Don't proceed if the player does not have permission to use the command.
            if (!Permission.Verify(player, Permission.Use))
                return;

            Item note = ItemManager.CreateByName("note", 1);
            if (note != null)
            {
                note.text = "Camera Identifiers:\n\n";
                foreach (BradleyAPC bradley in surveilledBradleys.Keys)
                {
                    List<string> cameraIdentifiers = surveilledBradleys[bradley];
                    note.text += string.Format("Bradley [{0}]\n", bradley.net.ID);
                    foreach (string identifier in cameraIdentifiers)
                    {
                        note.text += string.Format("- {0}\n", identifier);
                    }
                    note.text += "\n";
                }
                note.MarkDirty();
                player.GiveItem(note, BaseEntity.GiveItemReason.PickedUp);
            }
        }

        #endregion Commands
    }
}