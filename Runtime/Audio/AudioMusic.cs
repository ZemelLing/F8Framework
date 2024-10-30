using UnityEngine;

namespace F8Framework.Core
{
    public class AudioMusic
    {
        // 背景音乐播放完成回调
        public System.Action OnComplete;
        private float _progress = 0;
        private string _url = null;
        private bool _isPlay = false;
        public int Priority;
        public AudioSource MusicSource;
        public BaseTween AudioTween;

        // 获取音乐播放进度
        public float Progress
        {
            get
            {
                if (MusicSource.clip && MusicSource.clip.length > 0)
                {
                    _progress = MusicSource.time / MusicSource.clip.length;
                }
                return _progress;
            }
            set
            {
                _progress = value;
                MusicSource.time = value * MusicSource.clip.length;
            }
        }

        // 加载音乐并播放
        public void Load(string url, System.Action callback = null, bool loop = false, int priority = 0, float fadeDuration = 0f)
        {
            MusicSource.loop = loop;
            Priority = priority;
            AssetManager.Instance.LoadAsync<AudioClip>(url, (audioClip) =>
            {
                if (MusicSource.isPlaying)
                {
                    _isPlay = false;
                    MusicSource.Stop();
                    OnComplete?.Invoke();
                }

                MusicSource.clip = audioClip;

                OnComplete = callback;

                MusicSource.Play();
            
                _url = url;
                
                if (fadeDuration > 0f)
                {
                    float tempVolume = MusicSource.volume;
                    MusicSource.volume = 0f;
                    AudioTween = Tween.Instance.ValueTween(0f, tempVolume, fadeDuration).SetOnUpdateFloat((float v) =>
                    {
                        MusicSource.volume = v;
                    });
                }
            });
        }

        public void Tick()
        {
            if (MusicSource.clip && MusicSource.time > 0)
            {
                _isPlay = true;
            }
            if (_isPlay && !MusicSource.isPlaying)
            {
                _isPlay = false;
                OnComplete?.Invoke();
            }
        }

        // 释放当前背景音乐资源
        public void Release()
        {
            if (!string.IsNullOrEmpty(_url))
            {
                AssetManager.Instance.Unload(_url, true);
                _url = null;
            }
        }
    }
}