using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AirPodsSwitch
{
    public sealed class Endpoint
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name; }
    }

    // Core Audio COM interfaces must retain their native vtable ordering.
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class EnumeratorClass { }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumerator
    {
        void EnumAudioEndpoints(int flow, uint states, out ICollection devices);
        void GetDefaultAudioEndpoint(int flow, int role, out IDevice device);
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IDevice device);
        void RegisterEndpointNotificationCallback(INotification callback);
        void UnregisterEndpointNotificationCallback(INotification callback);
    }
    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ICollection { void GetCount(out uint count); void Item(uint index, out IDevice device); }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDevice
    {
        void Activate(ref Guid iid, uint context, IntPtr parameters, out IntPtr result);
        void OpenPropertyStore(uint access, out IProperties properties);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        void GetState(out uint state);
    }
    [StructLayout(LayoutKind.Sequential)] struct PropertyKey { public Guid Format; public uint Id; }
    [StructLayout(LayoutKind.Explicit, Size = 24)] struct Variant
    {
        [FieldOffset(0)] public ushort Type;
        [FieldOffset(8)] public IntPtr Pointer;
    }
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IProperties
    {
        void GetCount(out uint count); void GetAt(uint index, out PropertyKey key);
        void GetValue(ref PropertyKey key, out Variant value);
        void SetValue(ref PropertyKey key, ref Variant value); void Commit();
    }
    [ComVisible(true), Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INotification
    {
        [PreserveSig] int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string id, uint state);
        [PreserveSig] int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int OnDefaultDeviceChanged(int flow, int role, [MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string id, NativePropertyKey key);
    }
    [StructLayout(LayoutKind.Sequential)] public struct NativePropertyKey { public Guid Format; public uint Id; }
    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")] class PolicyClass { }
    // Undocumented Windows interface: only SetDefaultEndpoint is invoked.
    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPolicy
    {
        void GetMixFormat(); void GetDeviceFormat(); void ResetDeviceFormat(); void SetDeviceFormat();
        void GetProcessingPeriod(); void SetProcessingPeriod(); void GetShareMode(); void SetShareMode();
        void GetPropertyValue(); void SetPropertyValue();
        void SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);
        void SetEndpointVisibility();
    }
    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class Audio : INotification, IDisposable
    {
        readonly IEnumerator enumerator = (IEnumerator)new EnumeratorClass();
        public event Action Changed;
        public Audio() { enumerator.RegisterEndpointNotificationCallback(this); }
        [DllImport("ole32.dll")] static extern int PropVariantClear(ref Variant value);
        static void Release(object value) { if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
        public List<Endpoint> List()
        {
            ICollection collection = null;
            var result = new List<Endpoint>();
            try
            {
                enumerator.EnumAudioEndpoints(0, 1, out collection);
                uint count; collection.GetCount(out count);
                for (uint i = 0; i < count; i++)
                {
                    IDevice device = null; IProperties properties = null;
                    try
                    {
                        collection.Item(i, out device); string id; device.GetId(out id);
                        device.OpenPropertyStore(0, out properties);
                        var key = new PropertyKey { Format = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), Id = 14 };
                        Variant value; properties.GetValue(ref key, out value);
                        try { result.Add(new Endpoint { Id = id, Name = value.Type == 31 ? Marshal.PtrToStringUni(value.Pointer) : id }); }
                        finally { PropVariantClear(ref value); }
                    }
                    finally { Release(properties); Release(device); }
                }
                return result;
            }
            finally { Release(collection); }
        }
        public string Default(int role)
        {
            IDevice device = null;
            try { enumerator.GetDefaultAudioEndpoint(0, role, out device); string id; device.GetId(out id); return id; }
            catch (COMException e) { if (e.HResult == unchecked((int)0x80070490)) return null; throw; }
            finally { Release(device); }
        }
        public void Switch(string id)
        {
            if (!List().Exists(e => e.Id == id)) throw new InvalidOperationException("That output is no longer connected. Refresh and try again.");
            var policy = (IPolicy)new PolicyClass();
            try
            {
                for (int role = 0; role < 3; role++) policy.SetDefaultEndpoint(id, role);
                for (int role = 0; role < 3; role++)
                    if (Default(role) != id) throw new InvalidOperationException("Windows did not confirm the output change for every audio role.");
            }
            finally { Release(policy); }
        }
        int Notify()
        {
            // Never let managed exceptions cross the native notification boundary.
            try { var handler = Changed; if (handler != null) handler(); } catch { }
            return 0;
        }
        public int OnDeviceStateChanged(string id, uint state) { return Notify(); }
        public int OnDeviceAdded(string id) { return Notify(); }
        public int OnDeviceRemoved(string id) { return Notify(); }
        public int OnDefaultDeviceChanged(int flow, int role, string id) { return Notify(); }
        public int OnPropertyValueChanged(string id, NativePropertyKey key) { return Notify(); }
        public void Dispose() { enumerator.UnregisterEndpointNotificationCallback(this); Release(enumerator); }
    }
}
