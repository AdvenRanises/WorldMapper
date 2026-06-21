using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace WorldMapper
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
        public override string Name => "World Mapper";
        public override string Author => "James Puleo";
        public override string Description => "Generates a PNG map of the entire world";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private Config _config;

        public Plugin(Main game) : base(game) { }

        public override void Initialize()
        {
            _config = File.Exists(Config.DefaultPath)
                ? JsonConvert.DeserializeObject<Config>(File.ReadAllText(Config.DefaultPath))
                : new Config();

            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);

            Commands.ChatCommands.Add(new Command("worldmapper.generatemap", args =>
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var fileName = args.Parameters.Count >= 1
                    ? args.Parameters[0]
                    : string.Format(_config.MapFileNameFormat, Main.worldName, "manual", now);

                using var bitmap = MapGenerator.Create();
                bitmap.Save(fileName);
                TShock.Log.ConsoleInfo($"Map generated and saved as {fileName}");
                args.Player.SendSuccessMessage($"Map saved as {fileName}");
            }, "generatemap"));
        }

        private void OnPostInit(EventArgs args)
        {
            if (_config.SaveMapOnWorldLoad)
                DoAutomaticGenerate("load");

            if (_config.SaveMapOnWorldSave)
                HookWorldSave();
        }

        private void HookWorldSave()
        {
            try
            {
                var saveMethod = typeof(Terraria.IO.WorldFile).GetMethod("SaveWorld", new[] { typeof(bool), typeof(bool) });
                if (saveMethod == null)
                {
                    TShock.Log.ConsoleError("[WorldMapper] Could not hook world save.");
                    return;
                }

                var hook = new MonoMod.RuntimeDetour.Hook(
                    saveMethod,
                    typeof(Plugin).GetMethod(nameof(OnSaveWorld), BindingFlags.NonPublic | BindingFlags.Instance),
                    this
                );
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[WorldMapper] Save hook failed: {ex.Message}");
            }
        }

        private void OnSaveWorld(Action<bool, bool> orig, bool useCloudSaving, bool resetTime)
        {
            orig(useCloudSaving, resetTime);
            DoAutomaticGenerate("save");
        }

        private void DoAutomaticGenerate(string why)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            using var bitmap = MapGenerator.Create();
            bitmap.Save(string.Format(_config.MapFileNameFormat, Main.worldName, why, now));
            TShock.Log.ConsoleInfo($"Map auto-generated ({why})");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
            base.Dispose(disposing);
        }
    }
}
