using UnityEngine;
using UnityEngine.UI;

public class volume : MonoBehaviour
{
    public Slider slider;
    public AudioSource audioSource;
    public string volumeKey = "MusicVolume";

    void Start()
    {
        // Charger le volume sauvegardé (par défaut 0.7 si aucune sauvegarde)
        float savedVolume = PlayerPrefs.GetFloat(volumeKey, 0.7f);
        
        // Appliquer le volume à l'AudioSource
        audioSource.volume = savedVolume;
        
        // Mettre à jour la position du slider
        slider.value = savedVolume;
        
        // Ajouter un écouteur pour détecter les changements du slider
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    public void OnSliderValueChanged(float value)
    {
        // Mettre à jour le volume de l'audio
        audioSource.volume = value;
        
        // Sauvegarder le volume
        PlayerPrefs.SetFloat(volumeKey, value);
        PlayerPrefs.Save(); // Forcer la sauvegarde immédiate
    }

    void OnDestroy()
    {
        // Nettoyer l'écouteur quand l'objet est détruit
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}