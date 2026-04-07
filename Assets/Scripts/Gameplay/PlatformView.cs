using UnityEngine;

namespace BoxBound.Gameplay
{
    public readonly struct SurfaceFrame
    {
        public readonly Vector2 Position;
        public readonly Vector2 Normal;
        public readonly Vector2 Tangent;

        public SurfaceFrame(Vector2 position, Vector2 normal, Vector2 tangent)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
        }
    }

    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlatformView : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D _boxCollider;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Vector2 _size = new(7f, 4f);
        [SerializeField] private Color _color = new(0.12f, 0.15f, 0.2f, 1f);

        public float HalfHeight => _size.y * 0.5f;
        public float HalfWidth => _size.x * 0.5f;

        private void OnValidate()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public SurfaceFrame EvaluateFrame(float distance)
        {
            var perimeter = (_size.x + _size.y) * 2f;
            var wrappedDistance = distance % perimeter;

            if (wrappedDistance < 0f) wrappedDistance += perimeter;

            var center = (Vector2)transform.position;

            if (wrappedDistance < _size.x)
                return new(center + new Vector2(-HalfWidth + wrappedDistance, HalfHeight), Vector2.up, Vector2.right);

            wrappedDistance -= _size.x;

            if (wrappedDistance < _size.y)
                return new(center + new Vector2(HalfWidth, HalfHeight - wrappedDistance), Vector2.right, Vector2.down);

            wrappedDistance -= _size.y;

            if (wrappedDistance < _size.x)
                return new(center + new Vector2(HalfWidth - wrappedDistance, -HalfHeight), Vector2.down, Vector2.left);

            wrappedDistance -= _size.x;
            return new(center + new Vector2(-HalfWidth, -HalfHeight + wrappedDistance), Vector2.left, Vector2.up);
        }

        public void Initialize(Sprite sprite)
        {
            transform.position = Vector3.zero;
            transform.localScale = new(_size.x, _size.y, 1f);

            _boxCollider.size = Vector2.one;
            _boxCollider.offset = Vector2.zero;

            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = _color;
            _spriteRenderer.drawMode = SpriteDrawMode.Simple;
            _spriteRenderer.sortingOrder = 0;
        }
    }
}
