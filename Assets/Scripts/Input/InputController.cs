using System;
using UnityEngine.InputSystem;

namespace BoxBound.Input
{
    public sealed class InputController : IDisposable
    {
        private readonly InputAction _jumpAction;
        private readonly InputActionMap _playerMap;
        private readonly InputAction _moveAction;

        private bool _jumpRequested;

        public float Move => _moveAction.ReadValue<float>();

        public InputController(InputActionAsset inputActions)
        {
            _playerMap = inputActions.FindActionMap("Player", true);
            _moveAction = _playerMap.FindAction("Move", true);
            _jumpAction = _playerMap.FindAction("Jump", true);

            _jumpAction.performed += OnJumpPerformed;
            _playerMap.Enable();
        }

        public bool ConsumeJump()
        {
            if (!_jumpRequested)
                return false;

            _jumpRequested = false;
            return true;
        }

        public void Dispose()
        {
            _jumpAction.performed -= OnJumpPerformed;
            _playerMap.Disable();
        }

        private void OnJumpPerformed(InputAction.CallbackContext _) => _jumpRequested = true;
    }
}
