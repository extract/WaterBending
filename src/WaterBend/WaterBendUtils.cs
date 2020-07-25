using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.Utilities;
using System.Runtime.CompilerServices;
using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Animations;
using System.Collections;
using MEC;
using System.Reflection;

namespace WaterBendSpell
{
    public static class WaterBendUtils
    {
        public static int vfxQualitySetting;
        public static GameObject vfxAsset;

        public static List<T> LoadResources<T>(string[] names, string assetName) where T : class
        {
            FileInfo[] files = new DirectoryInfo(BetterStreamingAssets.Root + "/WaterBending/Bundles").GetFiles(assetName + ".assets", SearchOption.AllDirectories);
            AssetBundle assetBundle;
            if (AssetBundle.GetAllLoadedAssetBundles().Count() > 0)
            {
                if (AssetBundle.GetAllLoadedAssetBundles().Where(x => files[0].Name.Contains(x.name)).Count() == 0)
                {
                    assetBundle = AssetBundle.LoadFromFile(files[0].FullName);
                }
                else
                {
                    assetBundle = AssetBundle.GetAllLoadedAssetBundles().Where(x => files[0].Name.Contains(x.name)).First();
                }
            }
            else
            {
                assetBundle = AssetBundle.LoadFromFile(files[0].FullName);
            }
            List<T> objects = new List<T>();
            foreach (string k in assetBundle.GetAllAssetNames())
            {
                foreach (string j in names)
                    if (k.Contains(j))
                    {
                        objects.Add(assetBundle.LoadAsset(k) as T);
                        Debug.Log(Assembly.GetExecutingAssembly().GetName() + " loaded asset: " + k);
                    }
            }
            if (objects.Count == 0)
            {
                Debug.LogError(Assembly.GetExecutingAssembly().GetName() + " found no objects in array. The functions may not work as intended.");
                return null;
            }
            return objects;
        }
        public static IEnumerator<float> DoActionAfter(float seconds, System.Action action)
        {
            float time = 0;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / seconds;
                yield return Time.fixedDeltaTime;
            }

            action();
        }

        public enum HandPoseBend
        {
            Default = 0,
            IndexPointing = 1,
            FlatPalm = 2,
            Spiderman = 3,
            Rude = 4,
            Metal = 5,
            Crush = 6
        }

        public static HandPoseBend HandPoseChecker(SpellCaster spellCaster)
        {
            HandPoseBend handPoseBend = HandPoseBend.Default;

            var hand = PlayerControl.GetHand(spellCaster.bodyHand.side);
            var index = hand.indexCurl > 0.5f;
            var middle = hand.middleCurl > 0.5f;
            var ring = hand.ringCurl > 0.5f;
            var little = hand.littleCurl > 0.5f;
            var thumb = hand.thumbCurl > 0.5f; // thumb only matters for spiderman vs metal

            if (!index && middle && ring && little)
                handPoseBend = HandPoseBend.IndexPointing;
            if (!middle && !ring && !little)
                handPoseBend = HandPoseBend.FlatPalm;
            if (thumb && !index && middle && ring && !little)
                handPoseBend = HandPoseBend.Spiderman;
            if (index && !middle && ring && little)
                handPoseBend = HandPoseBend.Rude;
            if (!thumb && !index && middle && ring && !little)
                handPoseBend = HandPoseBend.Metal;
            if (PlayerControl.GetHand(spellCaster.bodyHand.side).GetAverageCurlNoThumb() > 0.9)
                handPoseBend = HandPoseBend.Crush;

            return handPoseBend;
        }

        public static void SetQuality(string vfxQuality)
        {
            switch (vfxQuality.ToLower())
            {
                case "mid":
                case "medium":
                case "m":
                    vfxQualitySetting = 1;
                    break;
                case "low":
                case "l":
                    vfxQualitySetting = 2;
                    break;
                default:
                    vfxQualitySetting = 0;
                    break;
            }
        }
    }
}
