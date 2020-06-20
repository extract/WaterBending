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
    public class Spell_WaterBendSpell : SpellCastCharge
    {
        private GameObject vfxObj;
        private GameObject vfx;
        private ItemData itemData;
        private Quaternion rotDirection;
        private float defaultSize = 0.4f;
        public string propItemId = "WaterBendDynamicProjectile";
        private HandPoseBend lastHandPose;
        private float vfxSize;
        private Vector3 targetPos;

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            imbueEnabled = false;
            
            itemData = Catalog.GetData<ItemData>(propItemId);
            vfxObj = LoadResources<GameObject>(new string[]{ "watervfx.prefab" }, "laliquid").FirstOrDefault();
            
        }

        public override void Unload()
        {
            GameObject.Destroy(vfx);
            base.Unload();
        }

        public override void Load(SpellCaster spellCaster)
        {
            base.Load(spellCaster);
            vfx = GameObject.Instantiate(vfxObj, spellCaster.magicSource.transform);
            vfx.transform.localPosition = Vector3.zero;

            vfx.GetComponent<VisualEffect>().playRate = 1.3f;
            vfx.GetComponent<VisualEffect>().SetFloat("size", default);
        }

        public override void UpdateCaster()
        {
            base.UpdateCaster();

            HandPoseBend handPose = HandPoseChecker();
            if (handPose != lastHandPose)
            {
                switch (handPose)
                {
                    case HandPoseBend.IndexPointing:
                    case HandPoseBend.FlatPalm:
                        rotDirection = Quaternion.Euler(0, 0, 0);
                        vfxSize = defaultSize / 4f;
                        targetPos = Vector3.forward * defaultSize / 3f;
                        break;
                    default:
                        rotDirection = Quaternion.Euler(90f, 90f, 0);
                        vfxSize = defaultSize;
                        targetPos = Vector3.zero;
                        break;
                }
                
                vfx.GetComponent<VisualEffect>().SetInt("mode", handPose == HandPoseBend.Metal ? 1 : 0);
                Timing.RunCoroutine(Lerp(vfx.transform.localRotation, rotDirection, vfx.transform));
                Timing.RunCoroutine(Lerp("size", vfx.GetComponent<VisualEffect>().GetFloat("size"), vfxSize, vfx.GetComponent<VisualEffect>()));
                Timing.RunCoroutine(Lerp(vfx.transform.localPosition, targetPos, vfx.transform));
                lastHandPose = handPose;
                Debug.Log(handPose);
            }
            
            
        }

        private IEnumerator<float> Lerp(Quaternion startRot, Quaternion targetRot, Transform vfx)
        {
            float scalingTime = 0.6f;
            float time = 0;
            while (time < 1f)
            {
                time += Time.deltaTime * scalingTime;
                vfx.transform.localRotation = Quaternion.Lerp(startRot, targetRot, time);
                yield return Time.fixedDeltaTime;
            }
        }
        private IEnumerator<float> Lerp(Vector3 startPos, Vector3 targetPos, Transform vfx)
        {
            float scalingTime = 0.6f;
            float time = 0;
            while (time < 1f)
            {
                time += Time.deltaTime * scalingTime;
                vfx.transform.localPosition = Vector3.Lerp(startPos, targetPos, time);
                yield return Time.fixedDeltaTime;
            }
        }
        private IEnumerator<float> Lerp(string parameter, float startSize, float endSize, VisualEffect vfx)
        {
            float scalingTime = 0.6f;
            float time = 0;
            while (time < 1f)
            {
                time += Time.deltaTime * scalingTime;
                var curSize = Mathf.Lerp(startSize, endSize, time);
                vfx.SetFloat(parameter, curSize);
                yield return Time.fixedDeltaTime;
            }
        }

        public override void Fire(bool active)
        {
            base.Fire(active);

            if (active) return;
            
            // Reset spell caster
            base.spellCaster.isFiring = false;
            base.spellCaster.grabbedFire = false;
            base.spellCaster.telekinesis.TryRelease(false); // is this necessary?

            // Spawn item
            Item item = CatalogPooler.local.Spawn(itemData, CatalogPooler.Pool.Type.Item) as Item;
            item.transform.position = spellCaster.magicSource.position;

            item.OnTelekinesisReleaseEvent += Item_OnTelekinesisReleaseEvent;
            
            if (item == null) Debug.LogError("Item does not exist");
            this.spellCaster.telekinesis.StartTargeting(item.GetMainHandle(spellCaster.bodyHand.side));
            this.spellCaster.telekinesis.TryCatch();
            
            // Attach VFX to item
            vfx.transform.SetParent(item.transform);
            vfx.transform.localPosition = Vector3.zero;
        }

        private void Item_OnTelekinesisReleaseEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            handle.item.OnTelekinesisReleaseEvent -= Item_OnTelekinesisReleaseEvent;
            vfx.transform.SetParent(spellCaster.magicSource.transform);
            vfx.transform.localPosition = Vector3.zero;
            vfx.GetComponent<VisualEffect>().SetFloat("size", defaultSize);

            if (handle.item.isPooled)
            {
                handle.item.Despawn();
            }
        }

        enum HandPoseBend
        {
            Default = 0,
            IndexPointing = 1,
            FlatPalm = 2, // Spin facing up
            Spiderman = 3,
            Rude = 4,
            Metal = 5,
            Crush = 6
        }

        private HandPoseBend HandPoseChecker()
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

        private static List<T> LoadResources<T>(string[] names, string assetName) where T : class
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
                        Debug.Log("Added " + k);
                    }
            }
            if (objects.Count == 0)
            {
                Debug.LogWarning("No objects in array..?");
                return null;
            }
            return objects;
        }
    }
}
