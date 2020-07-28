using MEC;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using Timer = System.Timers.Timer;

namespace WaterBendSpell
{
    public class Gesture
    {
        public List<GestureSequence> gestureSequences = new List<GestureSequence>();
        public event FinishedEvent OnFinishedEvent;
        public delegate void FinishedEvent(GestureSequence gestureSequence);

        public class GestureDefinition
        {
            public Vector3 direction;
            public float minDistance = 0.4f;
            public float currentDistance = 0f;
            public float velocity; // Min velocity
            public Vector3 forward = Vector3.zero;
        }
        public class GestureSequence : MonoBehaviour
        {
            public string gestureName;
            public Side hand;
            public List<GestureDefinition> gestures = new List<GestureDefinition>();
            public float timeOut = 1f; // Time out in seconds
            public int currentGestureId = 0;
            public Vector3[] lastPosition = new Vector3[Enum.GetValues(typeof(Side)).Length];
            // Add event OnFinishSequence
            public event FinishedEvent OnFinishedEvent;
            private float actualTimer = 0f;
            public delegate void FinishedEvent(GestureSequence gestureSequence);

            public static List<GestureSequence> list = new List<GestureSequence>();
            public GestureSequence(string gestureName, Side side, List<GestureDefinition> gestureDefinitions, float timeOut = 1f)
            {
                this.gestureName = gestureName;
                this.gestures = gestureDefinitions;
                this.timeOut = timeOut;
                hand = side;
                list.Add(this);
            }

            public void RecordGesture()
            {
                if(currentGestureId < gestures.Count)
                    GetGesture(gestures[currentGestureId]);
                else
                    OnFinishSequence();
            }

            public void GetGesture(GestureDefinition gesture)
            {
                if (Vector3.Dot(Player.local.transform.rotation * PlayerControl.GetHand(hand).GetHandVelocity(), gesture.direction) > gesture.velocity)
                {
                    if (lastPosition[(int)hand] != Vector3.zero)
                    {
                        gesture.currentDistance += Vector3.Distance(Player.local.GetHand(hand).bodyHand.transform.position, lastPosition[(int)hand]);
                    }
                    lastPosition[(int)hand] = Player.local.GetHand(hand).bodyHand.transform.position;
                    if(gesture.currentDistance >= gesture.minDistance)
                    {
                        gestures[currentGestureId].currentDistance = 0;
                        currentGestureId++;

                    }
                    actualTimer = timeOut;
                    return;
                }
                actualTimer -= Time.deltaTime;
                if (actualTimer < 0)
                {
                    ResetSequence();
                }
            }
            public void ResetSequence()
            {
                currentGestureId = 0;
            }
            public void OnFinishSequence()
            {
                ResetSequence();
                OnFinishedEvent(this);
            }
        }
        public Gesture(Side side)
        {
            // Load this another way
            gestureSequences.Add(new GestureSequence("upDown", side, new List<GestureDefinition> {
                new GestureDefinition { direction = Vector3.down, velocity = 2f },
                new GestureDefinition { direction = Vector3.up, velocity = 2f }
            }));

            gestureSequences.Add(new GestureSequence("leftRight", side, new List<GestureDefinition> {
                new GestureDefinition { direction = Vector3.left, velocity = 1f },
                new GestureDefinition { direction = Vector3.right, velocity = 1f }
            }));

            gestureSequences.Add(new GestureSequence("swipeLeft", side, new List<GestureDefinition> {
                new GestureDefinition { direction = Vector3.left, velocity = 3f }
            }));
            foreach(GestureSequence gestureSeq in gestureSequences)
            {
                gestureSeq.OnFinishedEvent += Gesture_OnFinishedEvent;
            }
        }

        private void Gesture_OnFinishedEvent(GestureSequence gestureSequence)
        {
            OnFinishedEvent(gestureSequence);
        }

        public void GestureUpdate()
        {
            foreach(var gestureSequence in gestureSequences)
                gestureSequence.RecordGesture();
        }
    }
}
