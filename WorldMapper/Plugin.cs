using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using On.Terraria.IO;

namespace WorldMapper
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
        public override string Name => "World Mapper";
        public override string Author => "James Puleo";
        public override string Description => "Generates a PNG map of the entire world";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

        private Config? _config;

        public Plugin(Main game) : base(game) { }

        public override void Initialize()
        {
            _config = File.Exists(Config.DefaultPath)
                ? JsonConvert.DeserializeObject<Config>(File.ReadAllText(Config.DefaultPath)) ?? new Config()
                : new Config();

            WorldFile.LoadWorld += OnLoadWorld;
            WorldFile.SaveWorld += OnSaveWorld;

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WorldFile.LoadWorld -= OnLoadWorld;
                WorldFile.SaveWorld -= OnSaveWorld;
            }
            base.Dispose(disposing);
        }

        private void OnLoadWorld(On.Terraria.IO.WorldFile.orig_LoadWorld orig, bool loadFromCloud)
        {
            orig(loadFromCloud);
            if (_config?.SaveMapOnWorldLoad == true)
                DoAutomaticGenerate("load");
        }

        private void OnSaveWorld(On.Terraria.IO.WorldFile.orig_SaveWorld orig, bool useCloudSaving, bool resetTime)
        {
            orig(useCloudSaving, resetTime);
            if (_config?.SaveMapOnWorldSave == true)
                DoAutomaticGenerate("save");
        }

        private void DoAutomaticGenerate(string why)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            using var bitmap = MapGenerator.Create();
            bitmap.Save(string.Format(_config!.MapFileNameFormat, Main.worldName, why, now));
            TShock.Log.ConsoleInfo($"Map auto-generated ({why})");
        }
    }
}
