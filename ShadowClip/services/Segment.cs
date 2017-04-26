using PropertyChanged;

namespace ShadowClip.services
{
    [ImplementPropertyChanged]
    public class Segment
    {
        public double Start { get; set; }
        public double End { get; set; }
    }
}