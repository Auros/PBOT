using IPA.Utilities;
using PBOT.Models;
using PBOT.Services;
using Polyglot;
using TMPro;
using UnityEngine;
using Zenject;

namespace PBOT.Managers;

internal class DeltaRankUIPanelVisualManager : IInitializable, System.IDisposable
{
    private readonly Config _config;
    private readonly CoreGameHUDController.InitData _initData;
    private readonly IDeltaPlaybackService _deltaPlaybackService;
    private readonly RelativeScoreAndImmediateRankCounter _relativeScoreAndImmediateRankCounter;

    private TextMeshProUGUI? _text;
    private string _precisionTemplate = string.Empty;
    private Color _goodColor = new(122f / 255, 1f, 131f / 255);
    private Color _normalColor = Color.white.ColorWithAlpha(0.502f);

	public DeltaRankUIPanelVisualManager(Config config, CoreGameHUDController.InitData initData, IDeltaPlaybackService deltaPlaybackService, RelativeScoreAndImmediateRankCounter relativeScoreAndImmediateRankCounter)
	{
        _config = config;
        _initData = initData;
        _deltaPlaybackService = deltaPlaybackService;
        _relativeScoreAndImmediateRankCounter = relativeScoreAndImmediateRankCounter;
	}

    public void Initialize()
    {
        if (_initData.hide)
            return;

        _deltaPlaybackService.OnFrameUpdated += SetFrame;
        var panel = Object.FindObjectOfType<CoreGameHUDController>().GetComponentInChildren<ImmediateRankUIPanel>();
        var originalText = panel.GetField<TextMeshProUGUI, ImmediateRankUIPanel>("_relativeScoreText");

        var gameObject = Object.Instantiate(originalText.gameObject, originalText.transform.parent);
        gameObject.transform.localPosition += new Vector3(0f, -35f, 0f);
        _text = gameObject.GetComponent<TextMeshProUGUI>();
        _text.text = string.Empty;

        if (ColorUtility.TryParseHtmlString(_config.BeatingFrameColor, out var goodColor))
            _goodColor = goodColor;

        if (ColorUtility.TryParseHtmlString(_config.DefaultColor, out var defaultColor))
            _normalColor = defaultColor;

        _precisionTemplate = "{0:P" + _config.Precision + "}";
        gameObject.SetActive(_initData.advancedHUD);
    }

    public void SetFrame(DeltaFrame frame)
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

    public void Dispose()
    {
        _deltaPlaybackService.OnFrameUpdated -= SetFrame;
    }
}