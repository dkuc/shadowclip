using PropertyChanged;

namespace ShadowClip.services
{
    [AddINotifyPropertyChangedInterface]
    public class Segment
    {
        public double Start { get; set; }
        public double End { get; set; }
        public decimal Speed { get; set; }
        public int Zoom { get; set; }
    }
}