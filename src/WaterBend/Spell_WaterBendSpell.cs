using System;
using ThunderRoad;
using UnityEngine;

namespace QuickHealSpell
{
    public class Spell_QuickHealSpell : SpellCastProjectile
    {
        
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            imbueEnabled = false;
        }

        public override void UpdateCaster()
        {
            base.UpdateCaster();
        }

        public override void Fire(bool active)
        {
            base.Fire(active);

            if (active) return;
            base.spellCaster.isFiring = false;
            base.spellCaster.grabbedFire = false;
            base.spellCaster.telekinesis.TryRelease(false);
        }
    }
}
