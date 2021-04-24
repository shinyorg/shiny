using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


namespace Docs.Shortcodes
{
    public static class Utils
    {
        const string PackageDirectory = "./PackageConfigs";


        public static string ToNugetShield(string packageName, bool usePackageLabel)
        {
            var imageUrl = $"https://img.shields.io/nuget/v/{packageName}.svg?style=for-the-badge";
            if (usePackageLabel)
                imageUrl += $"&label={packageName}";

            var hrefUrl = $"https://www.nuget.org/packages/{packageName}/";

            return $"<a href=\"{hrefUrl}\" target=\"{packageName}\"><img src=\"{imageUrl}\" /></a>";
        }


        static T FileToObj<T>(string path)
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json)!;
        }


        public static Package GetPackage(string packageName)
        {
            var path = $"{PackageDirectory}/{packageName}.json";
            if (!File.Exists(path))
                throw new ArgumentException($"Package '{packageName}' Not Found");

            return FileToObj<Package>(path);
        }


        public static List<Package> GetAllPackages()
        {
            var list = new List<Package>();
            var files = Directory.GetFiles(PackageDirectory, "*.json");
            foreach (var file in files)
            {
                var obj = FileToObj<Package>(file);
                list.Add(obj);
            }
            return list;
        }
    }
}
