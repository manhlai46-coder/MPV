using MPV.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPV.Models
{
    public class RoiRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsHidden { get; set; } = false;
        public int ExpectedLength { get; set; } = 0;  // default 0 nghĩa là không kiểm tra


        // Mode: "Barcode" hoặc "HSV"
        public string Mode { get; set; } = "Barcode";

        // Transient barcode settings (not persisted if you wish – remove if not needed)
        public BarcodeAlgorithm? Algorithm { get; set; } = BarcodeAlgorithm.QRCode;

        // HSV automatic thresholds (only used when Mode == "HSV")
        public HsvValue Lower { get; set; }
        public HsvValue Upper { get; set; }
    }
}