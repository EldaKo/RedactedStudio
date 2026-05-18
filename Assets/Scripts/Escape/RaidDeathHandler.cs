using System.Collections;
using NeoFPS;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어가 레이드 중 죽으면 마지막 저장된 Hideout 상태로 복귀.
/// 레이드에서 주운 아이템은 폐기 (저장하지 않고 복원).
/// </summary>
public class RaidDeathHandler : MonoBehaviour
{
    [Tooltip("죽었을 때 돌아갈 Hideout 씬 이름")]
    public string hideoutSceneName = "Hideout";

    [Tooltip("죽은 후 화면 전환까지 대기 (unscaled)")]
    public float deathDelay = 2f;

    private ICharacter _character;
    private bool _subscribed;
    private bool _handling;

    void Start()
    {
        StartCoroutine(WaitForCharacter());
    }

    private IEnumerator WaitForCharacter()
    {
        while (_character == null)
        {
            foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            {
                if (mb is ICharacter c && c.isPlayerControlled)
                {
                    _character = c;
                    break;
                }
            }
            if (_character == null) yield return new WaitForSeconds(0.5f);
        }

        _character.onIsAliveChanged += OnAliveChanged;
        _subscribed = true;
    }

    private void OnAliveChanged(ICharacter character, bool alive)
    {
        if (alive || _handling) return;
        _handling = true;
        StartCoroutine(ReturnToHideout());
    }

    private IEnumerator ReturnToHideout()
    {
        yield return new WaitForSecondsRealtime(deathDelay);

        // 레이드 진행 상황은 폐기. 마지막 저장본만 복원
        var saved = SaveManager.Load();
        SaveManager.PendingLoad = saved;
        SaveManager.SaveOnHideoutLoad = false;

        Time.timeScale = 1f;
        SceneManager.LoadScene(hideoutSceneName);
    }

    void OnDestroy()
    {
        if (_subscribed && _character != null)
            _character.onIsAliveChanged -= OnAliveChanged;
    }
}
