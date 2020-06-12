using System;
using ThunderRoad;
using UnityEngine;

namespace QuickHealSpell
{
    public class Spell_QuickHealSpell : SpellCastCharge
    {
        // General
        public float baseHeal = 15f;
        public float healChargePercent = 50f;

        public string spellHealType = "smash"; // [crush, smash, constant]
        private static SpellHealType spellHealTypeInternal = SpellHealType.Smash;

        // Crush
        public float gripThreshold = 0.7f; // [0, 1]

        // Smash
        public float smashDistance = 0.37f;
        public float smashVelocity = 0.45f;

        // Constant
        public float constantBaseHeal = 5f;
        public float constantExchangeRateConsumption = 1f;

        private enum SpellHealType
        {
            Crush,
            Smash,
            Constant
        }

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            imbueEnabled = false;
            VerifyType();
        }

        private void VerifyType()
        {
            foreach (SpellHealType healType in Enum.GetValues(typeof(SpellHealType)))
            {
                if (healType.ToString().ToLower().Contains(spellHealType.ToLower()))
                {
                    spellHealTypeInternal = healType;
                    return;
                }
            }
            Debug.LogError("No valid type has been assigned to QuickHealSpell.");
        }

        public override void UpdateCaster()
        {
            base.UpdateCaster();

            if (this.currentCharge < (healChargePercent / 100f)) return;

            foreach (Side hand in Enum.GetValues(typeof(Side)))
            {
                if (PlayerControl.GetHand(hand).gripPressed &&
                    PlayerControl.GetHand(hand).GetAverageCurlNoThumb() > gripThreshold &&
                    spellHealTypeInternal == SpellHealType.Crush)
                {
                    HealSelf(true);
                }
                if (spellHealTypeInternal == SpellHealType.Constant)
                {
                    HealSelf(true);
                }
                if (spellHealTypeInternal == SpellHealType.Smash)
                {
                    Vector3 spell = spellCaster.magicSource.position;
                    Vector3 chest = Creature.player.animator.GetBoneTransform(HumanBodyBones.Chest).position;

                    float dist = Vector3.Distance(spell, chest);
                    Vector3 dir = spell - chest;

                    if (dist < smashDistance && Vector3.Dot(PlayerControl.GetHand(hand).GetHandVelocity(), dir) > smashVelocity)
                        HealSelf(true);
                }
            }
        }

        public override void Fire(bool active)
        {
            base.Fire(active);

            if (active) return;
            base.spellCaster.isFiring = false;
            base.spellCaster.grabbedFire = false;
            base.spellCaster.telekinesis.TryRelease(false);
        }

        private void HealSelf(bool active)
        {
            if (!active ||
                this.currentCharge < (healChargePercent / 100f) ||
                this.spellCaster.mana.currentMana <= 0 ||
                this.spellCaster.isFiring == false)
                return;

            if (spellHealTypeInternal == SpellHealType.Crush ||
                spellHealTypeInternal == SpellHealType.Smash)
            {
                HealSelfInstant();
            }
            else if (spellHealTypeInternal == SpellHealType.Constant)
            {
                HealSelfConstant();
            }
        }

        private void HealSelfInstant()
        {
            Fire(false);
            Creature.player.health.Heal(this.currentCharge * baseHeal, Creature.player);
            currentCharge = 0;
        }

        private void HealSelfConstant()
        {
            Creature.player.health.Heal(Time.deltaTime * baseHeal / constantBaseHeal, Creature.player);
            this.spellCaster.mana.currentMana -= Time.deltaTime * constantExchangeRateConsumption * (baseHeal / constantBaseHeal); // mana consumption 1:x1 with health
            if (this.spellCaster.mana.currentMana <= 0 || Creature.player.health.currentHealth >= Creature.player.health.maxHealth)
            {
                Fire(false);

            }
        }
    }
}
