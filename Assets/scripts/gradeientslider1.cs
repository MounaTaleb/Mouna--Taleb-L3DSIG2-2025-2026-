using UnityEngine;
using UnityEngine.UI;

public class GradientSlider1 : MonoBehaviour
{
    [Header("Composants UI")]
    public Slider targetSlider;
    public Image fillImage;

    void Start()
    {
        // Génère une texture 256x1 avec le vrai dégradé visuel
        Texture2D tex = new Texture2D(256, 1);

        for (int x = 0; x < 256; x++)
        {
            float t = x / 255f;
            // Cyan (UI/Piliers) → Magenta/Rose (Fond/Néon)
            Color c = Color.Lerp(
                new Color(0f, 0.85f, 1f),   // Cyan à gauche
                new Color(0.9f, 0.1f, 0.9f),  // Magenta/Rose néon à droite
                t
            );
            tex.SetPixel(x, 0, c);
        }

        tex.Apply();

        // Applique la texture sur le Fill
        fillImage.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 256, 1),
            new Vector2(0.5f, 0.5f)
        );

        // IMPORTANT : couleur blanche pour ne pas teinter la texture
        fillImage.color = Color.white;
    }
}