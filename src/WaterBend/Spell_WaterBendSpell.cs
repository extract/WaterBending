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
        private const float defaultVfxSize = 0.4f;
        private const float defaultVfxThickness = 1f;
        private const float defaultVfxPeriodicTime = 3f;
        private const float resurrectionCooldownTime = 2f;
        private const float resurrectionTime = 2f;

        public string propItemId = "WaterBendDynamicProjectile";
        private HandPoseBend lastHandPose;
        private float vfxSize;
        private Vector3 targetPos;
        private bool isResurrecting = false;

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

            //GameObject.Instantiate(GameObject.Find("Distortion Smoke"), vfx.transform);
        }

        public override void UpdateCaster()
        {
            base.UpdateCaster();

            HandPoseBend handPose = HandPoseChecker();
            if (handPose != lastHandPose && !isResurrecting)
            {
                switch (handPose)
                {
                    case HandPoseBend.IndexPointing:
                    case HandPoseBend.FlatPalm:
                        rotDirection = Quaternion.Euler(0, 0, 0);
                        vfxSize = defaultVfxSize / 4f;
                        targetPos = Vector3.forward * defaultVfxSize / 3f;
                        break;
                    default:
                        rotDirection = Quaternion.Euler(90f, 90f, 0);
                        vfxSize = defaultVfxSize;
                        targetPos = Vector3.zero;
                        break;
                }
                
                Timing.RunCoroutine(Lerp(vfx.transform.localRotation, rotDirection, vfx.transform));
                Timing.RunCoroutine(Lerp("size", vfx.GetComponent<VisualEffect>().GetFloat("size"), vfxSize, vfx.GetComponent<VisualEffect>()));
                Timing.RunCoroutine(Lerp(vfx.transform.localPosition, targetPos, vfx.transform));
                Timing.RunCoroutine(Lerp("periodicTime", vfx.GetComponent<VisualEffect>().GetFloat("periodicTime"), defaultVfxPeriodicTime, vfx.GetComponent<VisualEffect>()));
                Timing.RunCoroutine(Lerp("thickness", vfx.GetComponent<VisualEffect>().GetFloat("thickness"), defaultVfxThickness, vfx.GetComponent<VisualEffect>()));

                lastHandPose = handPose; 
                Debug.Log(handPose);
            }

            if (handPose == HandPoseBend.FlatPalm &&
                Vector3.Angle(spellCaster.transform.TransformDirection(Vector3.down).normalized, Vector3.down) < 30f)
            {
                Debug.LogWarning("Facing down and open palm mom's spaghetti");
                foreach (Creature creature in Creature.list)
                {
                    foreach(var part in creature.ragdoll.parts)
                    {
                        if (part.isSliced == true) continue;
                    }
                    if (creature == Player.local.body.creature) continue;
                    if (!(Vector3.Distance(creature.ragdoll.hipsPart.transform.position.ToXZ(), spellCaster.transform.position.ToXZ()) <= 1f)) continue;
                    if (Mathf.Abs(creature.ragdoll.hipsPart.transform.position.y - spellCaster.transform.position.y) > 0.6f) continue;
                    Debug.Log("Start resurrect");
                    Timing.RunCoroutine(Resurrect(creature));

                }
            }
        }
        private IEnumerator<float> Resurrect(Creature creature)
        {
            if (isResurrecting == false)
            {
                float time = 0;
                bool continuous = true;
                isResurrecting = true;
                vfx.GetComponent<VisualEffect>().SetInt("mode", 1);
                Timing.RunCoroutine(Lerp("thickness", vfx.GetComponent<VisualEffect>().GetFloat("thickness"), defaultVfxThickness / 2f, vfx.GetComponent<VisualEffect>()));
                Timing.RunCoroutine(Lerp("periodicTime", vfx.GetComponent<VisualEffect>().GetFloat("periodicTime"), defaultVfxPeriodicTime / 2f, vfx.GetComponent<VisualEffect>()));
                
                PlayerControl.GetHand(spellCaster.bodyHand.side).HapticPlayClip(new GameData.HapticClip(AnimationCurve.Linear(0f, 0.05f, 1f, 0.2f), 1f, resurrectionTime));
                while (time < 1f)
                {

                    time += Time.fixedDeltaTime / resurrectionTime;
                    HandPoseBend handPose = HandPoseChecker();

                    if (!(handPose == HandPoseBend.FlatPalm) ||
                        !(Vector3.Angle(spellCaster.transform.TransformDirection(Vector3.down).normalized, Vector3.down) < 30f) ||
                        !(Vector3.Distance(creature.ragdoll.hipsPart.transform.position.ToXZ(), spellCaster.transform.position.ToXZ()) < 1f) ||
                        !(Mathf.Abs(creature.ragdoll.hipsPart.transform.position.y - spellCaster.transform.position.y) < 0.6f))
                    {
                        lastHandPose = HandPoseBend.Default;
                        continuous = false;
                        PlayerControl.GetHand(spellCaster.bodyHand.side).hapticPlayClipEnabled = false;
                        break;
                    }
                    yield return Time.fixedDeltaTime;
                }
                if (continuous)
                {
                    creature.ragdoll.SetState(Creature.State.Alive);
                    creature.ragdoll.SetState(Creature.State.Destabilized);
                    creature.ragdoll.knockoutDuration = 0.1f;
                    creature.health.Resurrect(creature.health.maxHealth, Creature.player);
                    creature.SetFaction(2);
                }
                lastHandPose = HandPoseBend.Default;
                Timing.RunCoroutine(CooldownResurrect());
            }
        }
        private IEnumerator<float> CooldownResurrect()
        {
            float time = 0f;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / resurrectionCooldownTime;
                yield return Time.fixedDeltaTime;
            }
            isResurrecting = false;
        }
        private IEnumerator<float> Lerp(Quaternion startRot, Quaternion targetRot, Transform vfx)
        {
            float scalingTime = 0.6f;
            float time = 0;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime * scalingTime;
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
                time += Time.fixedDeltaTime * scalingTime;
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
                time += Time.fixedDeltaTime * scalingTime;
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
            vfx.GetComponent<VisualEffect>().SetFloat("size", defaultVfxSize);

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
