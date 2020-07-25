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

namespace WaterBendSpell
{
    public class SpellMergeWaterBend : SpellMergeData
    {
        private GameObject vfxObj;
        private VfxUtils vfx;
        private ItemData itemData;
        private Quaternion rotDirection;
        private const float defaultVfxSize = 0.52f;
        private const float defaultVfxThickness = 1f;
        private const float defaultVfxPeriodicTime = 3f;
        private const float resurrectionCooldownTime = 2f;
        private const float resurrectionTime = 2f;

        public string propItemId = "WaterBendMergeDynamicProjectile";
        private bool isCasting;

        public event FinishedEvent OnFinishedEvent;
        public delegate void FinishedEvent();
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            Debug.Log("Merge catalog added");
            itemData = Catalog.GetData<ItemData>(propItemId);
            //vfxObj = WaterBendUtils.LoadResources<GameObject>(new string[]{ "watervfx.prefab" }, "laliquid").FirstOrDefault();
        }
        
        public override void Unload()
        {
            base.Unload();
        }

        public override void Load(Mana mana)
        {
            base.Load(mana);
        }
        public override void Merge(bool active)
        {
            base.Merge(active);

            if (!CanMerge()) return;

            if (active)
            {
                
                OnFinishedEvent += ((SpellCastWaterBend)mana.casterLeft.spellInstance).SpellCastWaterBend_OnFinishedEvent;
                OnFinishedEvent += ((SpellCastWaterBend)mana.casterRight.spellInstance).SpellCastWaterBend_OnFinishedEvent;
                ((SpellCastWaterBend)mana.casterRight.spellInstance).vfx.Merge(mana.mergePoint);
                ((SpellCastWaterBend)mana.casterLeft.spellInstance).vfx.Merge(mana.mergePoint);
                Debug.Log("Cast Merge!");
            }
            else
            {
                isCasting = true;
                ((SpellCastWaterBend)mana.casterRight.spellInstance).vfx.MergeFire();
                ((SpellCastWaterBend)mana.casterLeft.spellInstance).vfx.MergeFire();
                Debug.Log("Merging!");
                vfx = new VfxUtils();
                vfx.InitiateVfx(vfxAsset, mana.mergePoint);

                // Spawn item
                Item item = CatalogPooler.local.Spawn(itemData, CatalogPooler.Pool.Type.Item) as Item;
                if (item == null) throw new ArgumentNullException("Item does not exist");
                item.transform.position = mana.mergePoint.position;

                item.OnTelekinesisReleaseEvent += Item_OnTelekinesisReleaseEvent;

                mana.casterRight.telekinesis.StartTargeting(item.GetMainHandle(mana.casterRight.bodyHand.side));
                mana.casterRight.telekinesis.TryCatch();

                mana.casterLeft.telekinesis.StartTargeting(item.GetMainHandle(mana.casterLeft.bodyHand.side));
                mana.casterLeft.telekinesis.TryCatch();

                // Attach VFX to item
                vfx.ResetVfx(0f, item.transform);
                vfx.SetVfx(0.24f, new VfxUtils.VfxSettings
                {
                    periodicTime = 5f,
                    size = 1f,
                    thickness = 2f
                });
                vfx.SetRadius(1.1f);
                
                
            }
        }

        public override bool CanMerge()
        {
            if (isCasting) return false;
            return base.CanMerge();
        }

        private void Item_OnTelekinesisReleaseEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            vfx.DeactivateVfx();
            OnFinishedEvent();
            OnFinishedEvent -= ((SpellCastWaterBend)mana.casterLeft.spellInstance).SpellCastWaterBend_OnFinishedEvent;
            OnFinishedEvent -= ((SpellCastWaterBend)mana.casterRight.spellInstance).SpellCastWaterBend_OnFinishedEvent;
            isCasting = false;
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
