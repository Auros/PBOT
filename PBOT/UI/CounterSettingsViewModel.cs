using BeatSaberMarkupLanguage.Attributes;
using UnityEngine;

namespace PBOT.UI;

internal class CounterSettingsViewModel
{
    private readonly Config _config;

    public CounterSettingsViewModel(Config config)
    {
        _config = config;
    }

    [UIValue("precision")]
    public int Precision
    {
        get => _config.Precision;
        set => _config.Precision = value;
    }

    [UIValue("default-color")]
    public Color DefaultColor
    {
        get
        {
            if (ColorUtility.TryParseHtmlString(_config.DefaultColor, out var color))
                return color;
            return Color.white;
        }
        set => _config.DefaultColor = "#" + ColorUtility.ToHtmlStringRGBA(value);
    }

    [UIValue("beating-score-color")]
    public Color BeatingScoreColor
    {
        get
        {
            if (ColorUtility.TryParseHtmlString(_config.BeatingFrameColor, out var color))
                return color;
            return Color.white;
        }
        set => _config.BeatingFrameColor = "#" + ColorUtility.ToHtmlStringRGBA(value);
    }

    [UIValue("show-difference")]
    public bool ShowDifference
    {
        get => _config.ShowDifference;
        set => _config.ShowDifference = value;
    }
}