using UnityEngine;

public class ShipMaterialToggle : MonoBehaviour
{
    [Header("Matériau du navire à modifier")]
    public Material shipMaterial;

    // État actuel : false = normal, true = métallique
    private bool metallicMode = false;

    void Start()
    {
        if (shipMaterial == null)
        {
            Debug.LogError("❌ Aucun matériau assigné à ShipMaterialToggle !");
            return;
        }

        // État initial : comme sur l’image
        shipMaterial.SetFloat("_Metallic", 0f);
        shipMaterial.SetFloat("_Glossiness", 1f);
    }

    void Update()
    {
        // 🔁 Touche O pour basculer entre les deux états
        if (Input.GetKeyDown(KeyCode.O))
        {
            metallicMode = !metallicMode;

            if (metallicMode)
            {
                // Mode métallique (actif)
                shipMaterial.SetFloat("_Metallic", 1f);
                shipMaterial.SetFloat("_Glossiness", 0.4f);
                Debug.Log("⚙️ Mode métallique activé (Metallic=1, Smoothness=0.4)");
            }
            else
            {
                // Mode par défaut (image d'origine)
                shipMaterial.SetFloat("_Metallic", 0f);
                shipMaterial.SetFloat("_Glossiness", 1f);
                Debug.Log("🌊 Mode par défaut restauré (Metallic=0, Smoothness=1)");
            }
        }
    }
}
