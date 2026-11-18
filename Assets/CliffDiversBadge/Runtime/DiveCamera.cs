using UnityEngine;

namespace CliffDiversBadge
{
    public sealed class DiveCamera : MonoBehaviour
    {
        public ScoutDiver target;
        public Vector3 offset = new Vector3(0, 7, -10);
        public void Snap()
        {
            if (target == null) return;
            transform.position = target.transform.position + offset;
            Aim();
        }
        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, target.transform.position + offset,
                1f - Mathf.Exp(-14f * Time.deltaTime));
            Aim();
        }
        private void Aim()
        {
            Vector3 look = target.transform.position + new Vector3(0, -1, 4);
            transform.rotation = Quaternion.LookRotation(look - transform.position);
        }
    }
}
