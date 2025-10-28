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
        public bool IsDetected { get; set; } = false;
        public string BarcodeText { get; set; } = "";
        public bool IsHidden { get; set; } = false;
 
        public BarcodeAlgorithm Algorithm { get; set; } = BarcodeAlgorithm.QRCode;



    }
}