using System.Collections.Generic;
using UnityEngine;

namespace Badgewood
{
    // Rebuild single-submesh generated terrain with fresh runtime bounds/LOD metadata.
    // Does not alter the saved art assets or any scene placements.
    public sealed class TerrainBoundsRefresh : MonoBehaviour
    {
        public MeshFilter[] surfaces;
        private readonly List<Mesh> copies = new List<Mesh>();
        private void Awake()
        {
            foreach (MeshFilter filter in surfaces)
            {
                if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable || filter.sharedMesh.subMeshCount != 1) continue;
                Mesh source = filter.sharedMesh;
                Mesh mesh = new Mesh { name = source.name + " (refreshed bounds)", indexFormat = source.indexFormat };
                mesh.vertices = source.vertices;
                mesh.triangles = source.triangles;
                if (source.normals.Length == source.vertexCount) mesh.normals = source.normals;
                else mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                filter.sharedMesh = mesh;
                MeshCollider collider = filter.GetComponent<MeshCollider>();
                if (collider != null) collider.sharedMesh = mesh;
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.ResetBounds();
                    renderer.localBounds = mesh.bounds;
                    renderer.allowOcclusionWhenDynamic = false;
                }
                copies.Add(mesh);
            }
        }
        private void OnDestroy() { foreach (Mesh mesh in copies) if (mesh != null) Destroy(mesh); }
    }
}
