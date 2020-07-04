using System.Linq;
using Caliburn.Micro;
using PropertyChanged;

namespace ShadowClip.services
{
    [AddINotifyPropertyChangedInterface]
    public class Segment
    {
        private readonly SegmentCollection _parent;

        public Segment(SegmentCollection parent)
        {
            _parent = parent;
        }

        public double Start { get; set; }
        public double End { get; set; }
        public decimal Speed { get; set; }
        public int Zoom { get; set; }
        public bool IsLast => _parent.Last() == this;
    }

    public class SegmentCollection : BindableCollection<Segment>
    {
    }
}