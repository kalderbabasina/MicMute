using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MicMute
{
    public sealed class AudioDeviceInfo
    {
        public string Id { get; private set; }
        public string Name { get; private set; }

        public AudioDeviceInfo(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class AudioController
    {
        private const uint ClsctxAll = 23;
        private const uint DeviceStateActive = 1;
        private static readonly Guid EndpointVolumeId = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

        public static List<AudioDeviceInfo> GetAvailableMicrophones()
        {
            IMMDeviceEnumerator enumerator = null;
            IMMDeviceCollection devices = null;
            try
            {
                enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                Marshal.ThrowExceptionForHR(enumerator.EnumAudioEndpoints(1, DeviceStateActive, out devices));
                uint count;
                Marshal.ThrowExceptionForHR(devices.GetCount(out count));
                var result = new List<AudioDeviceInfo>();
                for (uint index = 0; index < count; index++)
                {
                    IMMDevice device = null;
                    try
                    {
                        Marshal.ThrowExceptionForHR(devices.Item(index, out device));
                        string id;
                        Marshal.ThrowExceptionForHR(device.GetId(out id));
                        string name = GetFriendlyName(device);
                        result.Add(new AudioDeviceInfo(id, string.IsNullOrEmpty(name) ? id : name));
                    }
                    finally
                    {
                        Release(device);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to enumerate active microphone devices. Check Windows audio services and device availability.", ex);
            }
            finally
            {
                Release(devices);
                Release(enumerator);
            }
        }

        public static bool? GetMuteState(string deviceId)
        {
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            IAudioEndpointVolume volume = null;
            try
            {
                volume = OpenEndpoint(deviceId, out enumerator, out device);
                bool muted;
                Marshal.ThrowExceptionForHR(volume.GetMute(out muted));
                return muted;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                Release(volume);
                Release(device);
                Release(enumerator);
            }
        }

        public static bool SetMuteState(string deviceId, bool muted)
        {
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            IAudioEndpointVolume volume = null;
            try
            {
                volume = OpenEndpoint(deviceId, out enumerator, out device);
                Guid context = Guid.Empty;
                Marshal.ThrowExceptionForHR(volume.SetMute(muted, ref context));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Release(volume);
                Release(device);
                Release(enumerator);
            }
        }

        private static IAudioEndpointVolume OpenEndpoint(string deviceId, out IMMDeviceEnumerator enumerator, out IMMDevice device)
        {
            device = null;
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            // An explicit device ID must never fall back to a different microphone.
            int result = string.IsNullOrEmpty(deviceId)
                ? enumerator.GetDefaultAudioEndpoint(1, 2, out device)
                : enumerator.GetDevice(deviceId, out device);
            Marshal.ThrowExceptionForHR(result);
            int dataFlow;
            Marshal.ThrowExceptionForHR(((IMMEndpoint)device).GetDataFlow(out dataFlow));
            if (dataFlow != 1)
                throw new InvalidOperationException("The selected device is not a capture endpoint.");
            object activated = null;
            Guid interfaceId = EndpointVolumeId;
            try
            {
                Marshal.ThrowExceptionForHR(device.Activate(ref interfaceId, ClsctxAll, IntPtr.Zero, out activated));
                return (IAudioEndpointVolume)activated;
            }
            catch
            {
                Release(activated);
                throw;
            }
        }

        private static string GetFriendlyName(IMMDevice device)
        {
            IPropertyStore properties = null;
            IntPtr variant = IntPtr.Zero;
            try
            {
                Marshal.ThrowExceptionForHR(device.OpenPropertyStore(0, out properties));
                var key = new PropertyKey(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);
                // PROPVARIANT is 24 bytes on x64 and 16 on x86. Allocate enough for either.
                variant = Marshal.AllocCoTaskMem(24);
                for (int offset = 0; offset < 24; offset += 8)
                    Marshal.WriteInt64(variant, offset, 0);
                Marshal.ThrowExceptionForHR(properties.GetValue(ref key, variant));
                ushort type = (ushort)Marshal.ReadInt16(variant);
                IntPtr value = Marshal.ReadIntPtr(variant, 8);
                if (type == 31)
                    return Marshal.PtrToStringUni(value);
                if (type == 8)
                    return Marshal.PtrToStringBSTR(value);
                return null;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (variant != IntPtr.Zero)
                {
                    PropVariantClear(variant);
                    Marshal.FreeCoTaskMem(variant);
                }
                Release(properties);
            }
        }

        private static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(IntPtr variant);

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, uint stateMask, out IMMDeviceCollection devices);
            [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
            [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
            [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
            [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int Item(uint index, out IMMDevice device);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid interfaceId, uint classContext, IntPtr activationParameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
            [PreserveSig] int OpenPropertyStore(uint access, out IPropertyStore properties);
            [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            [PreserveSig] int GetState(out uint state);
        }

        [ComImport, Guid("1BE09788-6894-4089-8586-9A2A6C265AC5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMEndpoint
        {
            [PreserveSig] int GetDataFlow(out int dataFlow);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid FormatId;
            public uint PropertyId;
            public PropertyKey(Guid formatId, uint propertyId)
            {
                FormatId = formatId;
                PropertyId = propertyId;
            }
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int GetAt(uint index, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, IntPtr value);
            [PreserveSig] int SetValue(ref PropertyKey key, IntPtr value);
            [PreserveSig] int Commit();
        }

        [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int GetChannelCount(out uint count);
            [PreserveSig] int SetMasterVolumeLevel(float level, ref Guid context);
            [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid context);
            [PreserveSig] int GetMasterVolumeLevel(out float level);
            [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
            [PreserveSig] int SetChannelVolumeLevel(uint channel, float level, ref Guid context);
            [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid context);
            [PreserveSig] int GetChannelVolumeLevel(uint channel, out float level);
            [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
            [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, ref Guid context);
            [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
            [PreserveSig] int GetVolumeStepInfo(out uint step, out uint stepCount);
            [PreserveSig] int VolumeStepUp(ref Guid context);
            [PreserveSig] int VolumeStepDown(ref Guid context);
            [PreserveSig] int QueryHardwareSupport(out uint mask);
            [PreserveSig] int GetVolumeRange(out float minimum, out float maximum, out float increment);
        }
    }
}
