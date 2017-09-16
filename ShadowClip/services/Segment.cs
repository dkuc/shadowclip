using PropertyChanged;

namespace ShadowClip.services
{
    [AddINotifyPropertyChangedInterface]
    public class Segment
    {
        public double Start { get; set; }
        public double End { get; set; }
    }
}