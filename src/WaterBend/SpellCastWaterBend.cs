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
using static WaterBendSpell.WaterBendUtils;
using TelekinesisPlus;

namespace WaterBendSpell
{
    public class SpellCastWaterBend : SpellCastCharge
    {
        private ItemData itemData;
        private Quaternion rotDirection;
        private const float resurrectionCooldownTime = 2f;
        private const float resurrectionTime = 2f;

        public string vfxQuality = "High";
        public string propItemId = "WaterBendDynamicProjectile";
        public bool telekinesisOnlyForWaterbend = false;
        private HandPoseBend lastHandPose;
        private float vfxSize;
        private Vector3 targetPos;
        private bool isResurrecting = false;
        public VfxUtils vfx;
        public new SpellCastWaterBend Clone()
        {
            return base.MemberwiseClone() as SpellCastWaterBend;
        }
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            EventManager.onPossessionEvent += EventManager_onPossessionEvent;
            itemData = Catalog.GetData<ItemData>(propItemId);
            var vfxAsset = WaterBendUtils.LoadResources<GameObject>(new string[]{ "watervfx.prefab" }, "laliquid").FirstOrDefault();
            WaterBendUtils.vfxAsset = vfxAsset;
            WaterBendUtils.SetQuality(vfxQuality);
            
        }

        private void EventManager_onPossessionEvent(Body oldBody, Body newBody)
        {
            if (newBody != null)
            {
                if (telekinesisOnlyForWaterbend)
                    TelekinesisPlus.TelekinesisPlusModule.local.active = false;
            }
        }

        public override void Unload()
        {
            vfx.DeactivateVfx();
            if (telekinesisOnlyForWaterbend)
                TelekinesisPlus.TelekinesisPlusModule.local.active = false;
            base.Unload();
        }

        public override void Load(SpellCaster spellCaster)
        {
            base.spellCaster = spellCaster;
            
            base.Load(spellCaster);
            vfx = new VfxUtils();
            Debug.Log("spellcaster = " + spellCaster + spellCaster.bodyHand.side);
            vfx.InitiateVfx(vfxAsset, spellCaster);
            if (telekinesisOnlyForWaterbend)
                TelekinesisPlus.TelekinesisPlusModule.local.active = true;
        }

        

        public override void UpdateCaster()
        {
            base.UpdateCaster();

            if (spellCaster.bodyHand.interactor.grabbedHandle != null)
            {
                vfx.ResetVfx(0.3f);
                return;
            }

            if(spellCaster.telekinesis.catchedHandle != null && spellCaster.telekinesis.catchedHandle.item.data.id == "WaterBendDynamicProjectile")
            {
                var vel = spellCaster.telekinesis.catchedHandle.item.rb.velocity;
                vfx.SetRadius(Mathf.Min(1 / vel.magnitude, 1f));
                
                return;
            }
            

            HandPoseBend handPose = HandPoseChecker(spellCaster);
            if (!isResurrecting && handPose != lastHandPose)
            {
                VfxUtils.VfxSettings setting;
                switch (handPose)
                {
                    case HandPoseBend.IndexPointing:
                    case HandPoseBend.FlatPalm:
                        setting = new VfxUtils.VfxSettings
                        {
                            rotDir = Quaternion.Euler(0, 0, 0),
                            size = VfxUtils.defaultVfxSettings.size / 4f,
                            localPosition = VfxUtils.defaultVfxSettings.localPosition / 4f,
                        };
                        break;
                    default:
                        setting = VfxUtils.defaultVfxSettings;
                        break;
                }
                vfx.SetVfx(1.6f, setting);


                lastHandPose = handPose; 
                //Debug.Log(handPose);
            }

            if (handPose == HandPoseBend.FlatPalm &&
                Vector3.Angle(spellCaster.transform.TransformDirection(Vector3.down).normalized, Vector3.down) < 30f)
            {
                foreach (Creature creature in Creature.list)
                {
                    bool sliced = false;
                    foreach(var part in creature.ragdoll.parts)
                    {
                        if (part.isSliced == true)
                        {
                            sliced = true;
                            break;
                        }
                    }
                    if (creature == Player.local.body.creature || sliced) continue;
                    if (!(Vector3.Distance(creature.ragdoll.hipsPart.transform.position.ToXZ(), spellCaster.transform.position.ToXZ()) <= 1f)) continue;
                    if (Mathf.Abs(creature.ragdoll.hipsPart.transform.position.y - spellCaster.transform.position.y) > 0.6f) continue;
                    
                    Resurrect(creature);
                }
            }
        }

        public void Resurrect(Creature creature)
        {
            Timing.RunCoroutine(ResurrectCoroutine(creature, resurrectionTime, resurrectionCooldownTime));
        }

        public IEnumerator<float> ResurrectCoroutine(Creature creature, float resurrectionTime, float cooldownTime)
        {
            if (isResurrecting == false)
            {
                float time = 0;
                bool failedResurrect = false;
                isResurrecting = true;
                var setting = new VfxUtils.VfxSettings
                {
                    thickness = VfxUtils.defaultVfxSettings.thickness / 2f,
                    periodicTime = VfxUtils.defaultVfxSettings.periodicTime / 2f
                };
                vfx.SetVfx(1f, setting);
                //vfxObj.transform.Rotate(vfxObj.transform.InverseTransformDirection(Vector3.up), 180);
                PlayerControl.GetHand(spellCaster.bodyHand.side).HapticPlayClip(new GameData.HapticClip(AnimationCurve.Linear(0f, 0.05f, 1f, 0.2f), 1f, resurrectionTime));
                while (time < 1f)
                {

                    time += Time.fixedDeltaTime / resurrectionTime;
                    if (!failedResurrect)
                    {
                        HandPoseBend handPose = HandPoseChecker(spellCaster);

                        if (!(handPose == HandPoseBend.FlatPalm) ||
                            !(Vector3.Angle(spellCaster.transform.TransformDirection(Vector3.down).normalized, Vector3.down) < 30f) ||
                            !(Vector3.Distance(creature.ragdoll.hipsPart.transform.position.ToXZ(), spellCaster.transform.position.ToXZ()) < 1f) ||
                            !(Mathf.Abs(creature.ragdoll.hipsPart.transform.position.y - spellCaster.transform.position.y) < 0.6f))
                        {
                            failedResurrect = true;
                            //vfxObj.transform.Rotate(vfxObj.transform.InverseTransformDirection(Vector3.up), 180);

                            PlayerControl.GetHand(spellCaster.bodyHand.side).hapticPlayClipEnabled = false;
                            Timing.RunCoroutine(CooldownResurrect(cooldownTime));
                        }
                    }
                    yield return Time.fixedDeltaTime;
                }
                if (!failedResurrect)
                {
                    PlayerControl.GetHand(spellCaster.bodyHand.side).HapticPlayClip(new GameData.HapticClip(AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f), 1f, resurrectionTime));
                    //vfxObj.transform.Rotate(vfxObj.transform.InverseTransformDirection(Vector3.up), 180);
                    creature.ragdoll.SetState(Creature.State.Alive);
                    creature.ragdoll.SetState(Creature.State.Destabilized);
                    creature.ragdoll.knockoutDuration = 0.1f;
                    creature.health.Resurrect(creature.health.maxHealth, Creature.player);
                    creature.SetFaction(2);
                    creature.StartBrain();
                    lastHandPose = HandPoseBend.Default;
                    Timing.RunCoroutine(CooldownResurrect(cooldownTime));
                }
            }
        }
        public IEnumerator<float> CooldownResurrect(float resurrectionCooldownTime)
        {
            float time = 0f;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / resurrectionCooldownTime;
                yield return Time.fixedDeltaTime;
            }
            isResurrecting = false;
        }

        public override void Fire(bool active)
        {
            base.Fire(active);

            if (active) return;
            
            // Reset spell caster
            spellCaster.isFiring = false;
            spellCaster.grabbedFire = false;
            spellCaster.telekinesis.TryRelease(false); // is this necessary?

            if (spellCaster.mana.mergeActive) return;

            // Spawn item
            Item item = CatalogPooler.local.Spawn(itemData, CatalogPooler.Pool.Type.Item) as Item;
            if (item == null) throw new ArgumentNullException("Item does not exist");
            item.transform.position = spellCaster.magicSource.position;

            item.OnTelekinesisReleaseEvent += Item_OnTelekinesisReleaseEvent;
            
            this.spellCaster.telekinesis.StartTargeting(item.GetMainHandle(spellCaster.bodyHand.side));
            this.spellCaster.telekinesis.TryCatch();
            
            // Attach VFX to item
            vfx.ResetVfx(0f, item.transform);
        }

        public void SpellCastWaterBend_OnFinishedEvent()
        {
            vfx = new VfxUtils();
            vfx.InitiateVfx(vfxAsset, spellCaster);
        }

        private void Item_OnTelekinesisReleaseEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            handle.item.OnTelekinesisReleaseEvent -= Item_OnTelekinesisReleaseEvent;
            vfx.ResetVfx(0f);

            if (handle.item.isPooled)
            {
                handle.item.Despawn();
            }
        }
    }
}
