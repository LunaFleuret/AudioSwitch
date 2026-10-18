using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AudioSwitch
{
    public class AudioDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeviceState State { get; set; }
        public bool IsDefault { get; set; }
    }

    public class TargetDeviceDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SearchKeyword { get; set; } = string.Empty;
        public string? IconResourcePath { get; set; }
        public int IconResourceIndex { get; set; }
    }

    public class AppConfig
    {
        public List<ConfigDeviceItem>? Devices { get; set; }
    }

    public class ConfigDeviceItem
    {
        public string? Key { get; set; }
        public string? DisplayName { get; set; }
        public string? SearchKeyword { get; set; }
        public string? IconPath { get; set; }
        public int? IconIndex { get; set; }
    }

    public static class AudioManager
    {
        private static readonly PropertyKey PKEY_Device_FriendlyName = new PropertyKey(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);

        private static readonly string MmresPath = Path.Combine(Environment.SystemDirectory, "mmres.dll");

        public static List<TargetDeviceDefinition> TargetDevices { get; private set; } = new();

        public static void LoadConfig()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "config.json");

            if (!File.Exists(configPath))
            {
                string currentDirConfig = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
                if (File.Exists(currentDirConfig))
                {
                    configPath = currentDirConfig;
                }
            }

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (config?.Devices != null && config.Devices.Count > 0)
                    {
                        TargetDevices = config.Devices.Select((d, idx) => new TargetDeviceDefinition
                        {
                            Key = string.IsNullOrWhiteSpace(d.Key) ? $"device{idx + 1}" : d.Key.Trim().ToLowerInvariant(),
                            DisplayName = string.IsNullOrWhiteSpace(d.DisplayName) ? (d.SearchKeyword ?? $"Device {idx + 1}") : d.DisplayName.Trim(),
                            SearchKeyword = string.IsNullOrWhiteSpace(d.SearchKeyword) ? (d.DisplayName ?? "") : d.SearchKeyword.Trim(),
                            IconResourcePath = string.IsNullOrWhiteSpace(d.IconPath) ? MmresPath : d.IconPath,
                            IconResourceIndex = d.IconIndex ?? -3016
                        }).ToList();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load config.json: {ex.Message}");
                }
            }

            // config.json が見つからない場合は接続中デバイスから自動構成して生成
            AutoConfigureDevices(configPath);
        }

        private static void AutoConfigureDevices(string savePath)
        {
            var activeDevices = GetRenderDevices().Where(d => d.State.HasFlag(DeviceState.Active)).ToList();
            var list = new List<TargetDeviceDefinition>();
            var configItems = new List<ConfigDeviceItem>();

            for (int i = 0; i < activeDevices.Count; i++)
            {
                var dev = activeDevices[i];
                string name = dev.Name;
                string lower = name.ToLowerInvariant();
                int icon = (lower.Contains("headphone") || lower.Contains("headset") || lower.Contains("earphone") || lower.Contains("イヤホン") || lower.Contains("ヘッドホン"))
                    ? -3012
                    : -3016;

                string key = $"dev{i + 1}";
                list.Add(new TargetDeviceDefinition
                {
                    Key = key,
                    DisplayName = name,
                    SearchKeyword = name,
                    IconResourcePath = MmresPath,
                    IconResourceIndex = icon
                });

                configItems.Add(new ConfigDeviceItem
                {
                    Key = key,
                    DisplayName = name,
                    SearchKeyword = name,
                    IconIndex = icon
                });
            }

            if (list.Count == 0)
            {
                list.Add(new TargetDeviceDefinition
                {
                    Key = "speakers",
                    DisplayName = "スピーカー",
                    SearchKeyword = "Speakers",
                    IconResourcePath = MmresPath,
                    IconResourceIndex = -3016
                });
                configItems.Add(new ConfigDeviceItem
                {
                    Key = "speakers",
                    DisplayName = "スピーカー",
                    SearchKeyword = "Speakers",
                    IconIndex = -3016
                });
            }

            TargetDevices = list;

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                };
                string json = JsonSerializer.Serialize(new AppConfig { Devices = configItems }, options);
                File.WriteAllText(savePath, json);
            }
            catch { }
        }

        public static List<AudioDeviceInfo> GetRenderDevices()
        {
            var list = new List<AudioDeviceInfo>();
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();

            string defaultId = string.Empty;
            try
            {
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out var defDev);
                if (defDev != null)
                {
                    defDev.GetId(out defaultId);
                }
            }
            catch { }

            enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceState.All, out var collection);
            collection.GetCount(out int count);

            for (int i = 0; i < count; i++)
            {
                collection.Item(i, out var dev);
                dev.GetId(out string id);
                dev.GetState(out DeviceState state);

                string name = "Unknown";
                try
                {
                    dev.OpenPropertyStore(0, out var store);
                    var pv = new PropVariant();
                    var key = PKEY_Device_FriendlyName;
                    store.GetValue(ref key, out pv);
                    if (pv.vt == 31 && pv.pwszVal != IntPtr.Zero)
                    {
                        name = Marshal.PtrToStringUni(pv.pwszVal) ?? "Unknown";
                    }
                }
                catch { }

                list.Add(new AudioDeviceInfo
                {
                    Id = id,
                    Name = name,
                    State = state,
                    IsDefault = (id == defaultId)
                });
            }

            return list;
        }

        public static (bool Success, string Message) SetDefaultDeviceByTarget(TargetDeviceDefinition target)
        {
            var devices = GetRenderDevices();
            // Prioritize ACTIVE devices first, then fallback to any matching device (e.g. UNPLUGGED Bluetooth)
            var matched = devices.FirstOrDefault(d => d.State.HasFlag(DeviceState.Active) && d.Name.Contains(target.SearchKeyword, StringComparison.OrdinalIgnoreCase))
                       ?? devices.FirstOrDefault(d => d.Name.Contains(target.SearchKeyword, StringComparison.OrdinalIgnoreCase));

            if (matched == null)
            {
                return (false, $"{target.DisplayName} が見つかりませんでした。");
            }

            if (!matched.State.HasFlag(DeviceState.Active))
            {
                return (false, $"{target.DisplayName} は現在未接続です。\n(Bluetoothや電源を確認してください)");
            }

            return SetDefaultDevice(matched.Id, target.DisplayName);
        }

        public static (bool Success, string Message) SetDefaultDevice(string deviceId, string displayName)
        {
            try
            {
                var policy = (IPolicyConfig)new PolicyConfigClient();
                // Set for standard audio and multimedia
                policy.SetDefaultEndpoint(deviceId, ERole.eConsole);
                policy.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
                // Also set communications if desired
                policy.SetDefaultEndpoint(deviceId, ERole.eCommunications);

                return (true, $"{displayName} に切り替えました。");
            }
            catch (Exception ex)
            {
                return (false, $"切り替えに失敗しました: {ex.Message}");
            }
        }

        public static (bool Success, string Message) ToggleNextDevice()
        {
            var devices = GetRenderDevices();
            var currentDefault = devices.FirstOrDefault(d => d.IsDefault);

            // Find which target devices are currently ACTIVE
            var availableTargets = new List<TargetDeviceDefinition>();
            foreach (var target in TargetDevices)
            {
                var dev = devices.FirstOrDefault(d => d.State.HasFlag(DeviceState.Active) && d.Name.Contains(target.SearchKeyword, StringComparison.OrdinalIgnoreCase));
                if (dev != null)
                {
                    availableTargets.Add(target);
                }
            }

            if (availableTargets.Count == 0)
            {
                return (false, "切り替え可能な対象オーディオ機器が見つかりませんでした。");
            }

            if (availableTargets.Count == 1)
            {
                var single = availableTargets[0];
                return SetDefaultDeviceByTarget(single);
            }

            // Determine current index
            int currentIndex = -1;
            if (currentDefault != null)
            {
                for (int i = 0; i < availableTargets.Count; i++)
                {
                    if (currentDefault.Name.Contains(availableTargets[i].SearchKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            // Next target
            int nextIndex = (currentIndex + 1) % availableTargets.Count;
            var nextTarget = availableTargets[nextIndex];

            return SetDefaultDeviceByTarget(nextTarget);
        }
    }
}
