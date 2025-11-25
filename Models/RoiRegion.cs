using MPV.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MPV.Models
{
    public class RoiRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsHidden { get; set; } = false;

        public int ExpectedLength { get; set; } = 0;

  
        public string Mode { get; set; } = "Barcode";
        public BarcodeAlgorithm? Algorithm { get; set; } = BarcodeAlgorithm.QRCode;
        public HsvValue Lower { get; set; }
        public HsvValue Upper { get; set; }
        [JsonIgnore]
        public Bitmap Template { get; set; }
        public double MatchScore { get; set; } = 0;
        [JsonIgnore]
        public Rectangle MatchRect { get; set; } = Rectangle.Empty;
    }
}
