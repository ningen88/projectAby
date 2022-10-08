using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public string testSceneName;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropDown;
    [SerializeField] private TMP_Dropdown qualityDropDown;
    [SerializeField] TMP_Text applyText;

    private float defaultVolume = 0.8f;
    private bool defaultFullscreen = true;
    private int defaultQualityLevel = 1;
    private int defaultResW = 1920;
    private int defaultResH = 1080;
    private bool isFullscreen;
    private int qualityLevel;
    private int resolutionIndex;
    private Resolution currentResolution;
    private Resolution[] resolutions;

    private void Awake()
    {
        GetAllResolutions();
        LoadSavedOptions();
    }

    private void GetAllResolutions()
    {
        resolutions = Screen.resolutions;
        
        resolutionDropDown.ClearOptions();
        List<string> resNames = new List<string>();
        int index = 0;

        for(int i = 0; i < resolutions.Length; i++)
        {
            string name = resolutions[i].width + "x" + resolutions[i].height + " " + resolutions[i].refreshRate + "Hz";
            resNames.Add(name);
            if(resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                index = i;
            }
        }

        resolutionDropDown.AddOptions(resNames);
        resolutionDropDown.value = index;
        resolutionDropDown.RefreshShownValue();
    }

    private void LoadSavedOptions()
    {
        int savedResW = defaultResW;
        int savedResH = defaultResH;
        int index = resolutions.Length;

        if (PlayerPrefs.HasKey("Volume"))
        {
            volumeSlider.value = PlayerPrefs.GetFloat("Volume");
            AudioListener.volume = volumeSlider.value;
        }
        if (PlayerPrefs.HasKey("Quality"))
        {
            qualityDropDown.value = PlayerPrefs.GetInt("Quality");
            QualitySettings.SetQualityLevel(qualityDropDown.value);
        }
        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            int fullscreenVal = PlayerPrefs.GetInt("Fullscreen");

            if(fullscreenVal == 1)
            {
                Screen.fullScreen = true;
                fullscreenToggle.isOn = true;
            }
            else
            {
                Screen.fullScreen = false;
                fullscreenToggle.isOn = false;
            }
        }
        if (PlayerPrefs.HasKey("ResWidth"))
        {
            savedResW = PlayerPrefs.GetInt("ResWidth");
        }
        if (PlayerPrefs.HasKey("ResHeight"))
        {
            savedResH = PlayerPrefs.GetInt("ResHeight");
        }
        if (PlayerPrefs.HasKey("ResIndex"))
        {
            index = PlayerPrefs.GetInt("ResIndex");
        }

        Screen.SetResolution(savedResW, savedResH, Screen.fullScreen);
        resolutionDropDown.value = index;
    }

    public void StartTestScene()
    {
        SceneManager.LoadScene(testSceneName);
    }

    public void ApplyOptions()
    {
        PlayerPrefs.SetFloat("Volume", AudioListener.volume);

        PlayerPrefs.SetInt("Quality", qualityLevel);
        QualitySettings.SetQualityLevel(qualityLevel);

        PlayerPrefs.SetInt("Fullscreen", (isFullscreen ? 1 : 0));
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt("ResWidth", currentResolution.width);
        PlayerPrefs.SetInt("ResHeight", currentResolution.height);
        PlayerPrefs.SetInt("ResIndex", resolutionIndex);
        Screen.SetResolution(currentResolution.width, currentResolution.height, Screen.fullScreen);

        StartCoroutine(ShowApplyText());
    }

    public void ResetOptions()
    {
        AudioListener.volume = defaultVolume;
        volumeSlider.value = defaultVolume;

        qualityLevel = defaultQualityLevel;
        qualityDropDown.value = qualityLevel;

        isFullscreen = defaultFullscreen;
        fullscreenToggle.isOn = isFullscreen;

        currentResolution.width = defaultResW;
        currentResolution.height = defaultResH;
        resolutionIndex = resolutions.Length;
        resolutionDropDown.value = resolutionIndex;

        ApplyOptions();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetFullscreen(bool fullscreen)
    {
        isFullscreen = fullscreen;
    }

    public void SetResolution(int index)
    {
        resolutionIndex = index;
        currentResolution = resolutions[index];
        Screen.SetResolution(currentResolution.width, currentResolution.height, Screen.fullScreen);
    }

    public void SetQuality(int quality)
    {
        qualityLevel = quality;
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private IEnumerator ShowApplyText()
    {
        applyText.SetText("Changes are applied");
        yield return new WaitForSeconds(5.0f);
        applyText.SetText("");
    }
}
