using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPV.Models
{
    public class FovRegion
    {
        public string ImagePath { get; set; }

        public List<RoiRegion> Rois { get; set; } = new List<RoiRegion>();
        public bool IsHidden { get; set; } = false;
    }
}
