using MPV.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MPV.Models
{
    public class RoiRegion
    {
        public int Id { get; set; } // new
        public string Name { get; set; } // new
        public bool IsEnabled { get; set; } = true; // new (inverse of IsHidden)
        public string Type { get; set; } = "Unknown"; // new

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsHidden { get; set; } = false;

        // Vị trí gốc để bù lệch từ Marking
        public int BaseX { get; set; }
        public int BaseY { get; set; }

        public int ExpectedLength { get; set; } = 0;

        public string Mode { get; set; } = "Barcode";
        public BarcodeAlgorithm? Algorithm { get; set; } = BarcodeAlgorithm.QRCode;
        public HsvValue Lower { get; set; }
        public HsvValue Upper { get; set; }
        [JsonIgnore]
        public Bitmap Template { get; set; }
        // Remove TemplateBase64 from persisted JSON
        [JsonIgnore]
        public string TemplateBase64 { get; set; }
        public double MatchScore { get; set; } = 0;
        [JsonIgnore]
        public Rectangle MatchRect { get; set; } = Rectangle.Empty;

        // Score range (0-100) instead of single OkScore
        public int OkScoreLower { get; set; } = 80;
        public int OkScoreUpper { get; set; } = 100;
        public bool ReverseSearch { get; set; } = false;
        public int LastScore { get; set; } = 0;

        // Backwards compatibility if old OkScore exists in JSON
        public int OkScore
        {
            get => OkScoreLower; // treat as lower bound
            set => OkScoreLower = value;
        }
    }
}
