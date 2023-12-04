using System;

namespace SK8Controller.Maths
{
    [Serializable]
    public class ADSRCurve
    {
        public float attack = 1.0f;
        public float decay = 1.0f;
        public float sustain = 0.5f;
        public float release = 1.0f;
        public float delay = 0.0f;
        public float duration = 1.0f;

        public float Sample(float t)
        {
            if (attack < 0.0f) attack = 0.0f;
            if (decay < 0.0f) decay = 0.0f;
            if (sustain < 0.0f) sustain = 0.0f;
            if (release < 0.0f) release = 0.0f;
            
            var total = attack + decay + release;
            var p = (t - delay) / duration;
            if (p <= 0.0f || p >= 1.0f) return 0.0f;

            p *= total;
            
            if (p < attack) return p / attack;
            if (p < attack - decay * (sustain - 1)) return (attack - p) / decay + 1.0f;
            if (p < total - release * sustain) return sustain;
            return (total - p) / release;
        }
    }
}