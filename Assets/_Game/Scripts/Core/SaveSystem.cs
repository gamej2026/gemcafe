using System.IO;
using UnityEngine;

namespace GemCafe.Core
{
    public static class SaveSystem
    {
        public static string SavePath => Application.persistentDataPath + "/save.json";

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public static void Save(SaveData data)
        {
            var json = JsonUtility.ToJson(data);
            File.WriteAllText(SavePath, json);
        }

        public static SaveData Load()
        {
            if (!HasSave())
            {
                return null;
            }

            var json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }

        public static void Delete()
        {
            if (HasSave())
            {
                File.Delete(SavePath);
            }
        }
    }
}