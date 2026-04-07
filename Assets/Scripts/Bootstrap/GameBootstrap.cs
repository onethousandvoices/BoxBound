using BoxBound.Gameplay;
using BoxBound.Infrastructure;
using BoxBound.Input;
using BoxBound.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoxBound.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string MOBILE_CONTROLS_PREFAB_PATH = "Prefabs/MobileControls";
        private const string PLATFORM_PREFAB_PATH = "Prefabs/Platform";
        private const string PLAYER_PREFAB_PATH = "Prefabs/Player";

        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private InputActionAsset _inputActions;

        private GameContainer _container;
        private Sprite _sharedSprite;

        private void Awake()
        {
            _container = new();
            _sharedSprite = CreateSharedSprite();

            var playerModel = new PlayerModel();
            var inputController = new InputController(_inputActions);
            var platformView = InstantiatePrefab<PlatformView>(PLATFORM_PREFAB_PATH);
            var playerView = InstantiatePrefab<PlayerView>(PLAYER_PREFAB_PATH);

            platformView.Initialize(_sharedSprite);
            playerView.Initialize(platformView, playerModel, inputController, _sharedSprite);

            _container.Register(_mainCanvas);
            _container.Register(_inputActions);
            _container.Register(playerModel);
            _container.Register(inputController);
            _container.Register(platformView);
            _container.Register(playerView);

#if UNITY_ANDROID || UNITY_IOS
            InstantiateMobileControls();
#endif
        }

        private void OnDestroy()
        {
            _container?.Dispose();

            if (!_sharedSprite)
                return;

            var texture = _sharedSprite.texture;
            Destroy(_sharedSprite);

            if (texture && texture != Texture2D.whiteTexture) Destroy(texture);
        }

        private static Sprite CreateSharedSprite()
        {
            var texture = Texture2D.whiteTexture;
            var size = texture.width;
            return Sprite.Create(texture, new(0f, 0f, size, size), new(0.5f, 0.5f), size);
        }

        private static T InstantiatePrefab<T>(string resourcePath) where T : Component
        {
            var prefab = Resources.Load<T>(resourcePath);
            return !prefab ? throw new($"Missing prefab at Resources/{resourcePath}") : Instantiate(prefab);
        }

#if UNITY_ANDROID || UNITY_IOS
        private void InstantiateMobileControls()
        {
            var mobileControls = InstantiatePrefab<RectTransform>(MOBILE_CONTROLS_PREFAB_PATH);
            mobileControls.SetParent(_mainCanvas.transform, false);
        }
#endif
    }
}
