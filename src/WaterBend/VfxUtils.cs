using MEC;
using MirzaBeig.ParticleSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;

namespace WaterBendSpell
{
    public class VfxUtils
    {
        public static VfxSettings defaultVfxSettings = new VfxSettings();
        
        public class VfxSettings
        {
            public Quaternion rotDir = Quaternion.Euler(90f, 90f, 0);
            public float size = 0.52f;
            public Vector3 localPosition = Vector3.zero;
            public float periodicTime = 3f;
            public float thickness = 1f;
            public int mode = 0;
        }
        private GameObject vfx;
        public bool isResurrecting;
        private SpellCaster spellCaster;

        public void DeactivateVfx()
        {
            if (vfx == null) return;
            GameObject.Destroy(vfx);
        }

        public void InitiateVfx(GameObject vfxAsset, SpellCaster spellCaster)
        {
            if (vfx == null && spellCaster != this.spellCaster)
            {
                Debug.Log("spawning vfx on side " + spellCaster.bodyHand.side);
                this.spellCaster = spellCaster;
                vfx = GameObject.Instantiate(vfxAsset);
            }
            ResetVfx(0);
        }

        public void InitiateVfx(GameObject vfxAsset, Transform mergePoint)
        {
            
            vfx = GameObject.Instantiate(vfxAsset);
            
            ResetVfx(0, mergePoint);
        }

        public void ResetVfx(float lerpTime, Transform parent = null)
        {
            vfx.transform.SetParent(parent == null ? spellCaster.magicSource.transform : parent);
            vfx.GetComponent<VisualEffect>().playRate = 1.3f;
            vfx.GetComponent<VisualEffect>().SetInt("quality", WaterBendUtils.vfxQualitySetting);
            Timing.RunCoroutine(LerpRotation(lerpTime, vfx.transform.localRotation, defaultVfxSettings.rotDir));
            Timing.RunCoroutine(LerpParam(lerpTime, "size", vfx.GetComponent<VisualEffect>().GetFloat("size"), defaultVfxSettings.size));
            Timing.RunCoroutine(LerpPosition(lerpTime, vfx.transform.localPosition, defaultVfxSettings.localPosition));
            Timing.RunCoroutine(LerpParam(lerpTime, "periodicTime", vfx.GetComponent<VisualEffect>().GetFloat("periodicTime"), defaultVfxSettings.periodicTime));
            Timing.RunCoroutine(LerpParam(lerpTime, "thickness", vfx.GetComponent<VisualEffect>().GetFloat("thickness"), defaultVfxSettings.thickness));
            vfx.GetComponent<VisualEffect>().SetInt("mode", defaultVfxSettings.mode);
        }

        public void SetVfx(float lerpTime, VfxSettings settings)
        {
            Timing.RunCoroutine(LerpRotation(lerpTime, vfx.transform.localRotation, settings.rotDir));
            Timing.RunCoroutine(LerpParam(lerpTime, "size", vfx.GetComponent<VisualEffect>().GetFloat("size"), settings.size));
            Timing.RunCoroutine(LerpPosition(lerpTime, vfx.transform.localPosition, settings.localPosition));
            Timing.RunCoroutine(LerpParam(lerpTime, "periodicTime", vfx.GetComponent<VisualEffect>().GetFloat("periodicTime"), settings.periodicTime));
            Timing.RunCoroutine(LerpParam(lerpTime, "thickness", vfx.GetComponent<VisualEffect>().GetFloat("thickness"), settings.thickness));
        }

        public void SetRadius(float radius)
        {
            try
            {
                vfx.GetComponent<VisualEffect>().SetFloat("scaleX", radius);
                vfx.GetComponent<VisualEffect>().SetFloat("scaleY", radius);
                vfx.GetComponent<VisualEffect>().SetFloat("scaleZ", radius);
            }
            catch (DivideByZeroException)
            {
                Debug.LogError("I hope this isnt needed..");
                vfx.GetComponent<VisualEffect>().SetFloat("scaleX", 1f);
                vfx.GetComponent<VisualEffect>().SetFloat("scaleY", 1f);
                vfx.GetComponent<VisualEffect>().SetFloat("scaleZ", 1f);
            }
        }

        public void Merge(Transform mergePoint)
        {
            Timing.RunCoroutine(MergeLerp(0.5f, mergePoint));
        }
        public void MergeFire()
        {
            GameObject.Destroy(vfx);
        }

        private IEnumerator<float> MergeLerp(float lerpTime, Transform transform)
        {
            ResetVfx(0.2f, transform);
            // Wait for reset
            float time = 0;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / (0.4f*lerpTime);
                yield return Time.fixedDeltaTime;
            }

            
            Timing.RunCoroutine(LerpRadius(lerpTime, 1f, 0.3f));
            Timing.RunCoroutine(LerpParam(lerpTime, "periodicTime", defaultVfxSettings.periodicTime, 1f));
            // Set mode low quality?
            
        }


        

        private IEnumerator<float> LerpRadius(float lerpTime, float startSize, float endSize)
        {
            float time = 0;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / lerpTime;
                SetRadius(Mathf.Lerp(startSize, endSize, time));
                yield return Time.fixedDeltaTime;
            }
        }



        private IEnumerator<float> LerpRotation(float lerpTime, Quaternion startRot, Quaternion targetRot)
        {
            float time = 0;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / lerpTime;
                vfx.transform.localRotation = Quaternion.Lerp(startRot, targetRot, time);
                yield return Time.fixedDeltaTime;
            }
        }
        private IEnumerator<float> LerpPosition(float lerpTime, Vector3 startPos, Vector3 targetPos)
        {
            float time = 0;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / lerpTime;
                vfx.transform.localPosition = Vector3.Lerp(startPos, targetPos, time);
                yield return Time.fixedDeltaTime;
            }
        }
        private IEnumerator<float> LerpParam(float lerpTime, string parameter, float startSize, float endSize)
        {
            float time = 0;
            while (time < 1f)
            {
                time += Time.fixedDeltaTime / lerpTime;
                var curSize = Mathf.Lerp(startSize, endSize, time);
                vfx.GetComponent<VisualEffect>().SetFloat(parameter, curSize);
                yield return Time.fixedDeltaTime;
            }
        }
    }
}
