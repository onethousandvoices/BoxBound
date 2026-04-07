using System;
using UnityEngine;

namespace BoxBound.Player
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "BoxBound/Player Config")]
    public sealed class PlayerConfig : ScriptableObject
    {
        public event Action Changed;

        [SerializeField] private float _speed = 4f;
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private float _gravity = 14f;

        public float Speed => _speed;
        public float JumpHeight => _jumpHeight;
        public float Gravity => _gravity;

        private void OnValidate()
        {
            if (_speed < 0f) _speed = 0f;
            if (_jumpHeight < 0.1f) _jumpHeight = 0.1f;
            if (_gravity < 0.1f) _gravity = 0.1f;
            Changed?.Invoke();
        }
    }
}
