using System;
using System.IO;
using MPV.Config;
using Newtonsoft.Json;

namespace MPV.Services
{
    public static class CameraConfigLoader
    {
        public static CameraConfig LoadOrCreateDefault()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string cfgDir = Path.Combine(baseDir, "Config");
            string cfgPath = Path.Combine(cfgDir, "camera.config.json");

            EnsureDefaultConfig(cfgDir, cfgPath);
            return Load(cfgPath);
        }

        private static CameraConfig Load(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var cfg = JsonConvert.DeserializeObject<CameraConfig>(json);
                return cfg ?? new CameraConfig();
            }
            catch
            {
                return new CameraConfig();
            }
        }

        private static void EnsureDefaultConfig(string dir, string path)
        {
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(path)) return;

                var defaultJson =
"{\n  \"CameraMode\": 1,\n  \"Cam1Id\": \"025071123047\",\n  \"Cam2Id\": \"025021223098\",\n  \"Cam3Id\": \"\"\n}";
                File.WriteAllText(path, defaultJson);
            }
            catch { }
        }
    }
}
