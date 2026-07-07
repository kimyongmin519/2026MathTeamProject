using System.Collections.Generic;
using KimLIb.EventSystem;
using KimLIb.ObjectPool.Runtime;
using UnityEngine;

namespace KimLIb.SoundSystem
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO soundItem;

        [field: SerializeField] public EventChannelSO SoundChannel { get; private set; }

        private Dictionary<int, SoundPlayer> _soundPlayerDict = new();

        private void Awake()
        {
            SoundChannel.AddListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundChannel.AddListener<StopSoundEvent>(HandleStopSoundEvent);
        }

        private void OnDestroy()
        {
            SoundChannel.RemoveListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundChannel.RemoveListener<StopSoundEvent>(HandleStopSoundEvent);
        }

        private void HandlePlaySoundEvent(PlaySoundEvent evt)
        {
            SoundPlayer player = poolManager.Pop<SoundPlayer>(soundItem);
            player.transform.position = evt.Position;
            player.PlaySound(evt.ClipData);
            player.OnSoundFinished += HandleSoundFinish;

            if (evt.ChannelNumber > 0 && evt.ClipData.loop)
            {
                if (_soundPlayerDict.TryGetValue(evt.ChannelNumber, out SoundPlayer beforePlayer))
                {
                    if (beforePlayer != null)
                    {
                        beforePlayer.ForceStopSound();
                        beforePlayer.OnSoundFinished -= HandleSoundFinish;
                        poolManager.Push(beforePlayer);
                    }

                    _soundPlayerDict.Remove(evt.ChannelNumber);
                }
                _soundPlayerDict.Add(evt.ChannelNumber, player);
            }
            else if (evt.ChannelNumber <= 100 && evt.ClipData.loop)
            {
                Debug.LogWarning($"대충 클남 이거 -> {evt.ClipData.name}");
            }
        }

        private void HandleSoundFinish(SoundPlayer player)
        {
            player.OnSoundFinished -= HandleSoundFinish;
            poolManager.Push(player);
        }

        private void HandleStopSoundEvent(StopSoundEvent evt)
        {
            if (_soundPlayerDict.TryGetValue(evt.ChannelNumber, out SoundPlayer beforePlayer))
            {
                if (beforePlayer != null)
                {
                    beforePlayer.ForceStopSound();
                    beforePlayer.OnSoundFinished -= HandleSoundFinish;
                    poolManager.Push(beforePlayer);
                }

                _soundPlayerDict.Remove(evt.ChannelNumber);
            }
        }

    }
}
