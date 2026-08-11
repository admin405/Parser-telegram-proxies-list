using System;
using System.Collections.Generic;

namespace TelegramProxyParser
{
    public static class ProxySources
    {
        public class SourceInfo
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Description { get; set; }
            public bool Enabled { get; set; } = true;
        }

        public static readonly List<SourceInfo> AllSources = new List<SourceInfo>
        {
            new SourceInfo
            {
                Name = "kort0881",
                Url = "https://github.com/kort0881/telegram-proxy-collector/blob/main/proxy_all_mtproto.txt",
                Description = "Прокси от kort0881"
            },
            
            new SourceInfo
            {
                Name = "SurfboardV2ray",
                Url = "https://raw.githubusercontent.com/Surfboardv2ray/TGProto/refs/heads/main/proxies-tested.txt",
                Description = "Прокси от SurfboardV2ray"
            },
            new SourceInfo
            {
                Name = "SoliSpirit",
                Url = "https://raw.githubusercontent.com/SoliSpirit/mtproto/master/all_proxies.txt",
                Description = "Прокси от SoliSpirit"
            },
            new SourceInfo
            {
                Name = "Therealwh",
                Url = "https://raw.githubusercontent.com/Therealwh/MTPproxyLIST/refs/heads/main/verified/proxy_all_verified.txt",
                Description = "Прокси от Therealwh"
            }
        };

        public static List<SourceInfo> GetActiveSources()
        {
            return AllSources.FindAll(s => s.Enabled);
        }

        public static List<string> GetActiveUrls()
        {
            return AllSources.FindAll(s => s.Enabled).ConvertAll(s => s.Url);
        }

        public static SourceInfo FindSource(string url)
        {
            return AllSources.Find(s => s.Url == url);
        }
    }
}