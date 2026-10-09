using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AudioSwitch
{
    public class AudioDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeviceState State { get; set; }
        public bool IsDefault { get; set; }
    }

    public enum TargetDeviceType
    {
        AT2020,
        BenQ,
        Momentum4
    }

    public class TargetDeviceDefinition
    {
        public TargetDeviceType Type { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SearchKeyword { get; set; } = string.Empty;
        public string? IconResourcePath { get; set; }
        public int IconResourceIndex { get; set; }
    }

    public static class AudioManager
    {
        private static readonly PropertyKey PKEY_Device_FriendlyName = new PropertyKey(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);

        private static readonly string MmresPath = System.IO.Path.Combine(Environment.SystemDirectory, "mmres.dll");

        public static readonly List<TargetDeviceDefinition> TargetDevices = new()
        {
            new TargetDeviceDefinition
            {
                Type = TargetDeviceType.BenQ,
                Key = "benq",
                DisplayName = "BenQ モニター",
                SearchKeyword = "BenQ",
                IconResourcePath = MmresPath,
                IconResourceIndex = -3016
            },
            new TargetDeviceDefinition
            {
                Type = TargetDeviceType.AT2020,
                Key = "at2020",
                DisplayName = "AT2020 イヤホン",
                SearchKeyword = "AT2020",
                IconResourcePath = MmresPath,
                IconResourceIndex = -3015
            },
            new TargetDeviceDefinition
            {
                Type = TargetDeviceType.Momentum4,
                Key = "momentum",
                DisplayName = "MOMENTUM 4",
                SearchKeyword = "MOMENTUM 4",
                IconResourcePath = MmresPath,
                IconResourceIndex = -3012
            }
        };

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
