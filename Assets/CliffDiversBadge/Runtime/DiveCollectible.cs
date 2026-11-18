using UnityEngine;

namespace CliffDiversBadge
{
    public sealed class DiveCollectible : MonoBehaviour
    {
        [Min(0.1f)] public float pickupRadius = 1.15f;
        public bool Collected { get; private set; }

        public void Restore()
        {
            Collected = false;
            gameObject.SetActive(true);
        }

        public void Collect()
        {
            Collected = true;
            gameObject.SetActive(false);
        }

        // A swept segment avoids skipping a pickup when a frame spans its position.
        public bool Intersects(Vector3 from, Vector3 to)
        {
            Vector3 line = to - from;
            float t = line.sqrMagnitude < 0.000001f ? 0f :
                Mathf.Clamp01(Vector3.Dot(transform.position - from, line) / line.sqrMagnitude);
            return (from + line * t - transform.position).sqrMagnitude <= pickupRadius * pickupRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
