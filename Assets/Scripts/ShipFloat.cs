using UnityEngine;
using Crest;

public class ShipFloat : MonoBehaviour
{
    public float buoyancyHeightOffset = 0f;
    public float smooth = 3f;

    // Utilitaire pour interroger la hauteur de la surface
    readonly SampleHeightHelper _heightHelper = new SampleHeightHelper();

    void Update()
    {
        var ocean = OceanRenderer.Instance;
        if (ocean == null)
            return;

        Vector3 position = transform.position;

        // 1️⃣  Initialise la requête : position, zone de recherche, forcer la mise à jour
        _heightHelper.Init(position, 1f, true);

        // 2️⃣  Récupère la hauteur d’eau à cette position
        float waterHeight;
        if (_heightHelper.Sample(out waterHeight))
        {
            float targetY = waterHeight + buoyancyHeightOffset;
            position.y = Mathf.Lerp(position.y, targetY, Time.deltaTime * smooth);
            transform.position = position;
        }
    }
}
