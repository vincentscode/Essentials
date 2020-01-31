namespace Xamarin.Essentials
{
    // TODO
    public static partial class Preferences
    {
        static bool PlatformContainsKey(string key, string sharedName) => false;

        static void PlatformRemove(string key, string sharedName) {

        }

        static void PlatformClear(string sharedName) {

        }

        static void PlatformSet<T>(string key, T value, string sharedName) {
        
        }

        static T PlatformGet<T>(string key, T defaultValue, string sharedName) => defaultValue;
    }
}
