namespace MPV.Models
{
    // Represents a single HSV triplet (OpenCV style ranges: H 0-179, S 0-255, V 0-255)
    public class HsvValue
    {
        public int H { get; set; }
        public int S { get; set; }
        public int V { get; set; }

        public HsvValue() { }

        public HsvValue(int h, int s, int v)
        {
            H = h; S = s; V = v;
        }

        public HsvValue Clone() => new HsvValue(H, S, V);
    }
}