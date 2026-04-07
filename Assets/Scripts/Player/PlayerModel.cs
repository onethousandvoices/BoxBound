using System;
using UnityEngine;

namespace BoxBound.Player
{
    public sealed class PlayerModel : IDisposable
    {
        private const string PLAYER_CONFIG_PATH = "Configs/PlayerConfig";

        private readonly PlayerConfig _config;

        private float _gravity;
        private float _jumpHeight;
        private float _speed;

        public float Speed => _speed;
        public float JumpHeight => _jumpHeight;
        public float Gravity => _gravity;

        public PlayerModel()
        {
            _config = Resources.Load<PlayerConfig>(PLAYER_CONFIG_PATH);

            if (!_config)
                throw new($"Missing player config at Resources/{PLAYER_CONFIG_PATH}");

            _config.Changed += OnConfigChanged;
            Load();
        }

        public void Dispose() => _config.Changed -= OnConfigChanged;

        private void OnConfigChanged() => Load();

        private void Load()
        {
            _speed = _config.Speed;
            _jumpHeight = _config.JumpHeight;
            _gravity = _config.Gravity;
        }
    }
}
