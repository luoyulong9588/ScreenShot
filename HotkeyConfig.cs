using System.IO;
using System.Windows.Input;

namespace ScreenShot
{
    public class HotkeyConfig
    {
        public Key Key { get; set; } = Key.A;
        public bool Ctrl { get; set; } = true;
        public bool Shift { get; set; } = true;
        public bool Alt { get; set; } = false;

        private static readonly string ConfigPath =
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "hotkey.json");

        public string DisplayText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (Ctrl) parts.Add("Ctrl");
                if (Shift) parts.Add("Shift");
                if (Alt) parts.Add("Alt");
                parts.Add(Key.ToString());
                return string.Join(" + ", parts);
            }
        }

        public uint GetModifiers()
        {
            uint mod = 0;
            if (Ctrl) mod |= NativeMethods.MOD_CTRL;
            if (Shift) mod |= NativeMethods.MOD_SHIFT;
            if (Alt) mod |= 0x0001; // MOD_ALT
            return mod;
        }

        public uint GetKeyCode()
        {
            return (uint)KeyInterop.VirtualKeyFromKey(Key);
        }

        public void Save()
        {
            var lines = new[]
            {
                "{",
                $"  \"Key\": \"{Key}\",",
                $"  \"Ctrl\": {Ctrl.ToString().ToLower()},",
                $"  \"Shift\": {Shift.ToString().ToLower()},",
                $"  \"Alt\": {Alt.ToString().ToLower()}",
                "}"
            };
            File.WriteAllText(ConfigPath, string.Join("\n", lines));
        }

        public static HotkeyConfig Load()
        {
            if (!File.Exists(ConfigPath))
                return new HotkeyConfig();

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = new HotkeyConfig();

                foreach (var line in json.Split('\n'))
                {
                    var trimmed = line.Trim().TrimEnd(',');
                    if (trimmed.Contains("\"Key\""))
                    {
                        var val = trimmed.Split(':')[1].Trim().Trim('"');
                        if (System.Enum.TryParse<Key>(val, out var k))
                            config.Key = k;
                    }
                    else if (trimmed.Contains("\"Ctrl\""))
                    {
                        config.Ctrl = trimmed.Contains("true");
                    }
                    else if (trimmed.Contains("\"Shift\""))
                    {
                        config.Shift = trimmed.Contains("true");
                    }
                    else if (trimmed.Contains("\"Alt\""))
                    {
                        config.Alt = trimmed.Contains("true");
                    }
                }
                return config;
            }
            catch
            {
                return new HotkeyConfig();
            }
        }
    }
}
