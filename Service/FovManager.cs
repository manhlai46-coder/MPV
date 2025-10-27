using MPV.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPV.Service
{
    internal class FovManager
    {
        private readonly string _path;
        public FovManager(string path)
        {
            _path = path;
        }
        public List<FovRegion> Load() 
        {
            if(!File.Exists(_path)) return new List<FovRegion>();
            string json = File.ReadAllText(_path);
            return JsonConvert.DeserializeObject<List<FovRegion>>(json) ?? new List<FovRegion>();
        }
        public void Save(List<FovRegion> fovs) 
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(fovs, Formatting.Indented));
        }
        public void Add(FovRegion fov)
        {
            var fovs = Load();
            fovs.Add(fov);
            Save(fovs);
        }
        public void DeleteAt(int index)
        {
            var fovs = Load();
            if (index >= 0 && index < fovs.Count)
            {
                fovs.RemoveAt(index);
                Save(fovs);
            }
        }
    }
}
