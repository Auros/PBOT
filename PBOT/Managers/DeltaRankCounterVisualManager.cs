using CountersPlus.Counters.Custom;
using PBOT.Models;
using PBOT.Services;
using Polyglot;
using TMPro;
using UnityEngine;

namespace PBOT.Managers;

internal class DeltaRankCounterVisualManager : BasicCustomCounter
{
    private readonly Config _config;
    private readonly IDeltaPlaybackService _deltaPlaybackService;
    private readonly RelativeScoreAndImmediateRankCounter _relativeScoreAndImmediateRankCounter;

    private TMP_Text? _text;
    private string _precisionTemplate = string.Empty;
    private Color _goodColor = new(122f / 255, 1f, 131f / 255);
    private Color _normalColor = Color.white.ColorWithAlpha(0.502f);

    public DeltaRankCounterVisualManager(Config config, IDeltaPlaybackService deltaPlaybackService, RelativeScoreAndImmediateRankCounter relativeScoreAndImmediateRankCounter)
    {
        _config = config;
        _deltaPlaybackService = deltaPlaybackService;
        _relativeScoreAndImmediateRankCounter = relativeScoreAndImmediateRankCounter;
    }

    public override void CounterInit()
    {
        _deltaPlaybackService.OnFrameUpdated += FrameUpdated;
        _text = CanvasUtility.CreateTextFromSettings(Settings);
        _text.color = _normalColor;
        _text.text = string.Empty;
        _text.fontSize = 3;

        if (ColorUtility.TryParseHtmlString(_config.BeatingFrameColor, out var goodColor))
            _goodColor = goodColor;

        if (ColorUtility.TryParseHtmlString(_config.DefaultColor, out var defaultColor))
            _normalColor = defaultColor;

        _precisionTemplate = "{0:P" + _config.Precision + "}";
    }

    private void FrameUpdated(DeltaFrame frame)
    {
        if (_text == null)
            return;

        var beatingFrame = _relativeScoreAndImmediateRankCounter.relativeScore >= frame.Current;
        var format = _config.ShowDifference ? (beatingFrame ? "+" : string.Empty) + _precisionTemplate : _precisionTemplate;
        var currentText = string.Format(Localization.Instance.SelectedCultureInfo, format, _config.ShowDifference ? _relativeScoreAndImmediateRankCounter.relativeScore - frame.Current : frame.Current);

        Color color = beatingFrame ? _goodColor : _normalColor;
        _text.text = currentText;
        _text.color = color;
    }

    public override void CounterDestroy()
    {
        _deltaPlaybackService.OnFrameUpdated -= FrameUpdated;
    }
}