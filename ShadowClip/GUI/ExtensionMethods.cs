using System.Reflection;
using System.Windows.Controls;

namespace ShadowClip.GUI
{
    public static class ExtensionMethods
    {
        //Nasty Hack because microsoft only allows the media state to be observed in Windows 10 Universal apps, not WPF
        public static MediaState GetMediaState(this MediaElement mediaElement)
        {
            var hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            var helperObject = hlp.GetValue(mediaElement);
            var stateField = helperObject.GetType()
                .GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            var state = (MediaState) stateField.GetValue(helperObject);
            return state;
        }
    }
}