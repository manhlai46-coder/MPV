using Newtonsoft.Json;

namespace MPV.Models
{
    public class HsvRange
    {
        [JsonProperty("hMin")]
        public int HMin { get; set; } = 0;

        [JsonProperty("hMax")]
        public int HMax { get; set; } = 179;

        [JsonProperty("sMin")]
        public int SMin { get; set; } = 0;

        [JsonProperty("sMax")]
        public int SMax { get; set; } = 255;

        [JsonProperty("vMin")]
        public int VMin { get; set; } = 0;

        [JsonProperty("vMax")]
        public int VMax { get; set; } = 255;

        public HsvRange()
        {
        }

        public HsvRange(int hMin, int hMax, int sMin, int sMax, int vMin, int vMax)
        {
            HMin = hMin;
            HMax = hMax;
            SMin = sMin;
            SMax = sMax;
            VMin = vMin;
            VMax = vMax;
        }
    }
}