using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MPV.Models
{
    public class FovRegion
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string CameraMode { get; set; } = "Camera1";
        public int ExposureTime { get; set; } = 0;

        [JsonIgnore]
        public string ImagePath { get; set; }
        // Base64 PNG of the FOV image, used when ImagePath is unavailable
        [JsonIgnore]
        public string ImageBase64 { get; set; }

        public List<RoiRegion> Rois { get; set; } = new List<RoiRegion>();
        public bool IsHidden { get; set; } = false;
    }
}
