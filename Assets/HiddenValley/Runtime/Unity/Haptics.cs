using System.Runtime.InteropServices;

namespace HiddenValley.Unity
{
    /// <summary>
    /// iOS haptics, wired to the beats the code already detects: light on interact and
    /// dialogue advance, medium on pickup, success on quest complete, tick on landing.
    /// On a phone, haptics are half of "polished" and cost the frame budget nothing —
    /// the generators run inside the OS, off-thread. No-ops everywhere but device.
    /// </summary>
    public static class Haptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _hv_hapticImpact(int strength);
        [DllImport("__Internal")] private static extern void _hv_hapticSuccess();
        [DllImport("__Internal")] private static extern void _hv_hapticTick();

        public static void Light() => _hv_hapticImpact(0);
        public static void Medium() => _hv_hapticImpact(1);
        public static void Success() => _hv_hapticSuccess();
        public static void Tick() => _hv_hapticTick();
#else
        public static void Light() { }
        public static void Medium() { }
        public static void Success() { }
        public static void Tick() { }
#endif
    }
}
