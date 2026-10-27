using UnityEngine;

// 말하는 플레이어의 머리 위에 스피커 아이콘을 띄운다(RoomPlayer, GamePlayer 공용).
// 표시해도 되는지는 VoiceManager.IsSpeakingVisible이 정한다(로비: 조건 없음 / 게임 중: 내 시야 안의 플레이어만)
public class VoiceSpeakingIcon : MonoBehaviour
{
    [SerializeField] private GameObject icon;
    [SerializeField] private float refreshInterval = 0.1f;

    private RoomPlayer _roomPlayer;
    private GamePlayer _gamePlayer;
    private float _nextRefresh;

    private void Awake()
    {
        _roomPlayer = GetComponent<RoomPlayer>();
        _gamePlayer = GetComponent<GamePlayer>();

        if (icon != null) icon.SetActive(false);
    }

    private void Update()
    {
        if (icon == null || Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + refreshInterval;

        string voiceId = _gamePlayer != null ? _gamePlayer.voiceId
            : _roomPlayer != null ? _roomPlayer.voiceId
            : null;

        var voice = VoiceManager.Instance;
        bool show = voice != null && voice.IsSpeakingVisible(voiceId);

        if (icon.activeSelf != show) icon.SetActive(show);
    }
}
