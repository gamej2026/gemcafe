using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GemCafe.Data;

namespace GemCafe.Core
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        public static AudioManager Instance { get; private set; }

        // SFX
        private AudioClip _click;
        private AudioClip _bell;
        private AudioClip _pour;
        private AudioClip _grind;
        private AudioClip _offer;
        private AudioClip _drink;
        private AudioClip _resultGreat;
        private AudioClip _resultSuccess;
        private AudioClip _resultFail;

        // BGM
        private AudioClip _bgmCafe;
        private AudioClip _bgmLobby;
        private AudioClip _bgmStage1;
        private AudioClip _bgmEndingA;
        private AudioClip _bgmEndingB;
        private AudioClip _bgmEndingC;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadClips();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        // Resources/Sounds 폴더의 클립을 이름으로 로드한다.
        // 한글 파일명의 유니코드 정규화(NFC/NFD) 차이를 흡수하기 위해 FormC로 정규화하여 매칭한다.
        private void LoadClips()
        {
            var map = new Dictionary<string, AudioClip>();
            foreach (var clip in Resources.LoadAll<AudioClip>("Sounds"))
            {
                if (clip == null)
                {
                    continue;
                }

                map[clip.name.Normalize(NormalizationForm.FormC)] = clip;
            }

            AudioClip Get(string clipName)
            {
                map.TryGetValue(clipName.Normalize(NormalizationForm.FormC), out var c);
                return c;
            }

            _click = Get("기본 클릭");
            _bell = Get("문 열리는 딸랑");
            _pour = Get("물따르는 소리");
            _grind = Get("사발가는소리");
            _offer = Get("잔 내려놓는 소리");
            _drink = Get("물 꿀꺽");
            _resultGreat = Get("결과-대성공");
            _resultSuccess = Get("결과-성공");
            _resultFail = Get("결과-실패");

            _bgmCafe = Get("BGM_카페");
            _bgmLobby = Get("BGM_신나는 한국 전통 분위기의 국악 배경음악");
            _bgmStage1 = Get("BGM_삼도천");
            _bgmEndingA = Get("BGM_엔딩A");
            _bgmEndingB = Get("BGM_배 노 젓는 소리 - 엔딩 B");
            _bgmEndingC = Get("BGM_천둥 소리 - 엔딩 C");
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (bgmSource == null || clip == null)
            {
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource == null)
            {
                return;
            }

            bgmSource.Stop();
        }

        public void PlayClick() => PlaySfx(_click);
        public void PlayCustomerBell() => PlaySfx(_bell);
        public void PlayPour() => PlaySfx(_pour);
        public void PlayGrind() => PlaySfx(_grind);
        public void PlayOffer() => PlaySfx(_offer);
        public void PlayDrink() => PlaySfx(_drink);

        public void PlayResult(DrinkResult result)
        {
            if (result == DrinkResult.GreatSuccess)
            {
                PlaySfx(_resultGreat);
            }
            else if (result == DrinkResult.Success)
            {
                PlaySfx(_resultSuccess);
            }
            else
            {
                PlaySfx(_resultFail);
            }
        }

        public void PlayCafeBgm() => PlayBgm(_bgmCafe);
        public void PlayLobbyBgm() => PlayBgm(_bgmLobby);
        public void PlayStage1Bgm() => PlayBgm(_bgmStage1);

        public void PlayEndingBgm(EndingKind kind)
        {
            if (kind == EndingKind.A)
            {
                PlayBgm(_bgmEndingA);
            }
            else if (kind == EndingKind.C)
            {
                PlayBgm(_bgmEndingC);
            }
            else
            {
                PlayBgm(_bgmEndingB);
            }
        }
    }
}
