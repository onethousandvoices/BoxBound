using BoxBound.Gameplay;
using BoxBound.Input;
using UnityEngine;

namespace BoxBound.Player
{
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerView : MonoBehaviour
    {
        private const float AIR_NORMAL_RATE = 18f;
        private const float AIR_POSITION_RATE = 16f;
        private const float CORNER_SAMPLE_OFFSET = 0.001f;
        private const float GROUND_NORMAL_RATE = 28f;
        private const float GROUND_POSITION_RATE = 24f;

        [SerializeField] private CircleCollider2D _circleCollider;
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _radius = 0.35f;
        [SerializeField] private Color _color = new(1f, 0.62f, 0.21f, 1f);

        private InputController _inputController;
        private PlayerModel _playerModel;
        private PlatformView _platformView;
        private float _jumpOffset;
        private float _jumpVelocity;
        private float _surfaceDistance;
        private float _transferProgress;
        private int _faceIndex;
        private int _transferDirection;
        private bool _hasPose;
        private bool _isJumping;
        private Vector2 _smoothedNormal;
        private Vector2 _smoothedPosition;

        private void OnValidate()
        {
            _circleCollider = GetComponent<CircleCollider2D>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(PlatformView platformView, PlayerModel playerModel, InputController inputController, Sprite sprite)
        {
            _platformView = platformView;
            _playerModel = playerModel;
            _inputController = inputController;

            _circleCollider.offset = Vector2.zero;
            _circleCollider.radius = 0.5f;

            _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody2D.freezeRotation = true;

            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = _color;
            _spriteRenderer.sortingOrder = 1;

            transform.localScale = new(_radius * 2f, _radius * 2f, 1f);

            _faceIndex = 0;
            _surfaceDistance = _platformView.HalfWidth;
            _jumpOffset = 0f;
            _jumpVelocity = 0f;
            _transferProgress = 0f;
            _transferDirection = 0;
            _hasPose = false;
            _isJumping = false;

            ApplyTargetPose(Time.fixedDeltaTime, true);
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;
            var moveInput = _inputController.Move;

            if (!_isJumping && _inputController.ConsumeJump())
            {
                _isJumping = true;
                _jumpVelocity = Mathf.Sqrt(2f * _playerModel.Gravity * _playerModel.JumpHeight);
            }

            if (_transferDirection == 0)
                MoveAlongSurface(moveInput * _playerModel.Speed * deltaTime);
            else
                AdvanceTransfer(deltaTime);

            SimulateJump(deltaTime);
            ApplyTargetPose(deltaTime, false);
        }

        private void ApplyTargetPose(float deltaTime, bool snapImmediately)
        {
            var frame = GetAnimatedFrame();
            var targetPosition = frame.Position + frame.Normal * (GetWorldRadius() + _jumpOffset);

            if (snapImmediately || !_hasPose)
            {
                _hasPose = true;
                _smoothedNormal = frame.Normal;
                _smoothedPosition = targetPosition;
                _rigidbody2D.position = _smoothedPosition;
                _rigidbody2D.rotation = Vector2.SignedAngle(Vector2.up, _smoothedNormal);
                return;
            }

            var positionLerp = GetPoseLerp(deltaTime, _isJumping ? AIR_POSITION_RATE : GROUND_POSITION_RATE);
            var normalLerp = GetPoseLerp(deltaTime, _isJumping ? AIR_NORMAL_RATE : GROUND_NORMAL_RATE);

            _smoothedPosition = Vector2.Lerp(_smoothedPosition, targetPosition, positionLerp);
            _smoothedNormal = Normalize(Vector2.Lerp(_smoothedNormal, frame.Normal, normalLerp), frame.Normal);

            SetPose(_smoothedPosition, _smoothedNormal);
        }

        private void AdvanceTransfer(float deltaTime)
        {
            var transferDistance = GetCornerBlendDistance();
            var transferProgress = _transferProgress + _playerModel.Speed * deltaTime / transferDistance;

            if (transferProgress < 1f)
            {
                _transferProgress = transferProgress;
                return;
            }

            if (_transferDirection > 0)
            {
                _faceIndex = WrapFaceIndex(_faceIndex + 1);
                _surfaceDistance = Mathf.Min(transferDistance, GetFaceLength(_faceIndex));
            }
            else
            {
                _faceIndex = WrapFaceIndex(_faceIndex - 1);
                _surfaceDistance = Mathf.Max(0f, GetFaceLength(_faceIndex) - transferDistance);
            }

            _transferDirection = 0;
            _transferProgress = 0f;
        }

        private SurfaceFrame GetAnimatedFrame()
        {
            if (_transferDirection == 0)
                return GetFaceFrame(_faceIndex, _surfaceDistance);

            var transferDistance = GetCornerBlendDistance();

            if (_transferDirection > 0)
            {
                var nextFaceIndex = WrapFaceIndex(_faceIndex + 1);
                var startFrame = GetFaceFrame(_faceIndex, GetFaceLength(_faceIndex));
                var endFrame = GetFaceFrame(nextFaceIndex, transferDistance);
                return LerpFrame(startFrame, endFrame, _transferProgress);
            }

            var previousFaceIndex = WrapFaceIndex(_faceIndex - 1);
            var previousFaceLength = GetFaceLength(previousFaceIndex);
            var fromFrame = GetFaceFrame(_faceIndex, 0f);
            var toFrame = GetFaceFrame(previousFaceIndex, previousFaceLength - GetCornerBlendDistance());
            return LerpFrame(fromFrame, toFrame, _transferProgress);
        }

        private float GetPoseLerp(float deltaTime, float baseRate)
        {
            var adaptiveRate = baseRate + _playerModel.Speed * 2f + _playerModel.Gravity * 0.15f;
            return 1f - Mathf.Exp(-adaptiveRate * deltaTime);
        }

        private float GetWorldRadius() => _circleCollider.radius * transform.lossyScale.x;

        private float GetCornerBlendDistance() => GetWorldRadius();

        private SurfaceFrame GetFaceFrame(int faceIndex, float localDistance)
        {
            var faceLength = GetFaceLength(faceIndex);
            var clampedLocalDistance = Mathf.Clamp(localDistance, 0f, faceLength);
            var interiorOffset = Mathf.Min(CORNER_SAMPLE_OFFSET, faceLength * 0.5f);
            var maxSampleDistance = faceLength - interiorOffset;
            var sampleLocalDistance = maxSampleDistance > interiorOffset
                ? Mathf.Clamp(clampedLocalDistance, interiorOffset, maxSampleDistance)
                : clampedLocalDistance;
            var sampleFrame = _platformView.EvaluateFrame(GetFaceStartDistance(faceIndex) + sampleLocalDistance);

            if (Mathf.Abs(sampleLocalDistance - clampedLocalDistance) <= Mathf.Epsilon)
                return sampleFrame;

            return new(
                sampleFrame.Position + sampleFrame.Tangent * (clampedLocalDistance - sampleLocalDistance),
                sampleFrame.Normal,
                sampleFrame.Tangent);
        }

        private float GetFaceLength(int faceIndex) => (faceIndex & 1) == 0 ? _platformView.HalfWidth * 2f : _platformView.HalfHeight * 2f;

        private float GetFaceStartDistance(int faceIndex)
        {
            var width = _platformView.HalfWidth * 2f;
            var height = _platformView.HalfHeight * 2f;

            return faceIndex switch
            {
                0 => 0f,
                1 => width,
                2 => width + height,
                _ => width + height + width
            };
        }

        private static SurfaceFrame LerpFrame(SurfaceFrame startFrame, SurfaceFrame endFrame, float blend)
        {
            var clampedBlend = Mathf.Clamp01(blend);
            var fallbackNormal = clampedBlend < 0.5f ? startFrame.Normal : endFrame.Normal;
            var fallbackTangent = clampedBlend < 0.5f ? startFrame.Tangent : endFrame.Tangent;

            return new(
                Vector2.Lerp(startFrame.Position, endFrame.Position, clampedBlend),
                Normalize(Vector2.Lerp(startFrame.Normal, endFrame.Normal, clampedBlend), fallbackNormal),
                Normalize(Vector2.Lerp(startFrame.Tangent, endFrame.Tangent, clampedBlend), fallbackTangent));
        }

        private void MoveAlongSurface(float deltaDistance)
        {
            var faceLength = GetFaceLength(_faceIndex);
            var nextDistance = _surfaceDistance + deltaDistance;

            switch (deltaDistance)
            {
                case > 0f when nextDistance >= faceLength:
                    StartTransfer(1, nextDistance - faceLength);
                    return;
                case < 0f when nextDistance <= 0f:
                    StartTransfer(-1, -nextDistance);
                    return;
                default:
                    _surfaceDistance = Mathf.Clamp(nextDistance, 0f, faceLength);
                    break;
            }
        }

        private static Vector2 Normalize(Vector2 value, Vector2 fallback)
        {
            var magnitudeSqr = value.sqrMagnitude;

            if (magnitudeSqr <= Mathf.Epsilon * Mathf.Epsilon)
                return fallback;

            return value / Mathf.Sqrt(magnitudeSqr);
        }

        private void SetPose(Vector2 position, Vector2 up)
        {
            _rigidbody2D.MovePosition(position);
            _rigidbody2D.MoveRotation(Vector2.SignedAngle(Vector2.up, up));
        }

        private void SimulateJump(float deltaTime)
        {
            if (!_isJumping)
            {
                _jumpOffset = 0f;
                _jumpVelocity = 0f;
                return;
            }

            _jumpVelocity -= _playerModel.Gravity * deltaTime;
            _jumpOffset += _jumpVelocity * deltaTime;

            if (_jumpOffset > 0f)
                return;

            _jumpOffset = 0f;
            _jumpVelocity = 0f;
            _isJumping = false;
        }

        private void StartTransfer(int direction, float overshootDistance)
        {
            _surfaceDistance = direction > 0 ? GetFaceLength(_faceIndex) : 0f;
            _transferDirection = direction;
            _transferProgress = Mathf.Clamp01(overshootDistance / GetCornerBlendDistance());
        }

        private static int WrapFaceIndex(int faceIndex) => faceIndex switch
        {
            < 0 => 3,
            > 3 => 0,
            _   => faceIndex
        };
    }
}
