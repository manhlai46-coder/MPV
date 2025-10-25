using MPV.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace MPV.Service
{
    public class RoiManager
    {
        private readonly string _filePath;
        public RoiManager(string filePath)
        {
            _filePath = filePath;
        }

        public List<RoiRegion> Load()
        {
            if (!File.Exists(_filePath)) return new List<RoiRegion>();

            var json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<List<RoiRegion>>(json) ?? new List<RoiRegion>();
        }

        public void Save(List<RoiRegion> roiList)
        {
            var json = JsonConvert.SerializeObject(roiList, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void Add(RoiRegion roi)
        {
            var list = Load();
            list.Add(roi);
            Save(list);
        }

        public void DeleteAt(int index)
        {
            var list = Load();
            if (index >= 0 && index < list.Count)
            {
                list.RemoveAt(index);
                Save(list);
            }
        }
    }
}
