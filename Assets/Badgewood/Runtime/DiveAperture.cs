using UnityEngine;

namespace Badgewood
{
    // Pure geometry shared by the course and its boundary tests. No trigger-sized sphere.
    public static class DiveAperture
    {
        public static bool Crossed(Vector3 from, Vector3 to, Vector3 center, Vector3 normal, out Vector3 crossing)
        {
            crossing = Vector3.zero;
            float a = Vector3.Dot(from - center, normal);
            float b = Vector3.Dot(to - center, normal);
            if (a > 0.0001f || b < 0f || b - a < 0.000001f) return false;
            crossing = Vector3.Lerp(from, to, Mathf.Clamp01(-a / (b - a)));
            return true;
        }

        public static bool Fits(Vector3 point, Vector3 center, Vector3 normal, float openingRadius, float bodyRadius, float forgiveness)
        {
            float usable = Mathf.Max(0f, openingRadius - bodyRadius + forgiveness);
            Vector3 radial = Vector3.ProjectOnPlane(point - center, normal);
            return radial.sqrMagnitude <= usable * usable;
        }
    }
}
