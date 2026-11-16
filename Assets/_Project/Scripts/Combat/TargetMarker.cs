using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 目標標記：在「現在按左鍵會打到的目標」和「現在按 E 會互動的物件」頭上，顯示一個尖端朝下、慢慢旋轉、上下浮動的倒金字塔。
/// 掛在 Player（和 PlayerAttack、PlayerInteractor 同一個物件）上。
/// 預設用程式產生金字塔；之後 B 可以在 Inspector 指定自己做的 Prefab 替換外觀。
/// </summary>
public class TargetMarker : MonoBehaviour
{
    [Header("外觀（可選）")]
    [Tooltip("攻擊目標標記的 Prefab。留空 = 用程式產生的倒金字塔。注意：Prefab 不要有 Collider，否則會擋住攻擊與互動判定")]
    [SerializeField] private GameObject _attackMarkerPrefab;

    [Tooltip("互動目標標記的 Prefab。留空 = 用程式產生的倒金字塔。注意：Prefab 不要有 Collider")]
    [SerializeField] private GameObject _interactMarkerPrefab;

    [Header("顏色（只對程式產生的金字塔有效）")]
    [Tooltip("攻擊目標標記的顏色")]
    [SerializeField] private Color _attackColor = new Color(1f, 0.35f, 0.1f);

    [Tooltip("互動目標標記的顏色")]
    [SerializeField] private Color _interactColor = new Color(0.2f, 0.9f, 0.4f);

    [Header("大小與位置")]
    [Tooltip("金字塔底面的寬度（公尺）")]
    [SerializeField] private float _size = 0.35f;

    [Tooltip("金字塔的高度（公尺）")]
    [SerializeField] private float _height = 0.4f;

    [Tooltip("尖端離目標頭頂多高（公尺）")]
    [SerializeField] private float _heightAboveTarget = 0.3f;

    [Tooltip("攻擊和互動目標是同一個物件時，互動標記再往上多疊多高（公尺）")]
    [SerializeField] private float _stackOffset = 0.6f;

    [Header("動態")]
    [Tooltip("旋轉速度（度／秒）")]
    [SerializeField] private float _rotateSpeed = 90f;

    [Tooltip("上下浮動的幅度（公尺）")]
    [SerializeField] private float _bobAmplitude = 0.08f;

    [Tooltip("上下浮動的速度（每秒來回幾次）")]
    [SerializeField] private float _bobFrequency = 1.5f;

    private PlayerAttack _attack;
    private PlayerInteractor _interactor;
    private Marker _attackMarker;
    private Marker _interactMarker;

    private static Mesh _pyramidMesh;
    private static Material _defaultMaterial;

    private void Awake()
    {
        _attack = GetComponent<PlayerAttack>();
        _interactor = GetComponent<PlayerInteractor>();

        _attackMarker = new Marker(CreateMarkerObject("AttackTargetMarker", _attackMarkerPrefab, _attackColor));
        _interactMarker = new Marker(CreateMarkerObject("InteractTargetMarker", _interactMarkerPrefab, _interactColor));
    }

    private void OnEnable()
    {
        if (_attack != null)
        {
            _attack.OnTargetChanged += HandleAttackTargetChanged;
            HandleAttackTargetChanged(_attack.CurrentTarget);
        }

        if (_interactor != null)
        {
            _interactor.OnFocusChanged += HandleInteractTargetChanged;
            HandleInteractTargetChanged(_interactor.CurrentTarget);
        }
    }

    private void OnDisable()
    {
        if (_attack != null)
        {
            _attack.OnTargetChanged -= HandleAttackTargetChanged;
        }

        if (_interactor != null)
        {
            _interactor.OnFocusChanged -= HandleInteractTargetChanged;
        }

        _attackMarker.SetTarget(null);
        _interactMarker.SetTarget(null);
    }

    private void OnDestroy()
    {
        _attackMarker?.DestroyObject();
        _interactMarker?.DestroyObject();
    }

    private void HandleAttackTargetChanged(IHittable target)
    {
        _attackMarker.SetTarget(target as Component);
    }

    private void HandleInteractTargetChanged(IInteractable target)
    {
        _interactMarker.SetTarget(target as Component);
    }

    private void LateUpdate()
    {
        // 用 LateUpdate：等目標這一幀移動完再擺標記，才不會抖動
        float bob = Mathf.Sin(Time.time * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;
        Quaternion spin = Quaternion.Euler(0f, Time.time * _rotateSpeed, 0f);

        _attackMarker.UpdatePose(_heightAboveTarget + bob, spin);

        float interactExtra = _attackMarker.HasSameTarget(_interactMarker) ? _stackOffset : 0f;
        _interactMarker.UpdatePose(_heightAboveTarget + interactExtra + bob, spin);
    }

    private GameObject CreateMarkerObject(string markerName, GameObject prefab, Color color)
    {
        GameObject markerObject;
        if (prefab != null)
        {
            markerObject = Instantiate(prefab);
            markerObject.name = markerName;
        }
        else
        {
            markerObject = new GameObject(markerName);
            markerObject.AddComponent<MeshFilter>().sharedMesh = GetPyramidMesh(_size, _height);

            MeshRenderer meshRenderer = markerObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetDefaultMaterial();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // 用 MaterialPropertyBlock 改顏色：不會複製材質，也不會改到共用材質
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(block);
        }

        // 標記放在 Ignore Raycast Layer。真正讓它不擋判定的是「沒有 Collider」，這只是保險
        markerObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        markerObject.SetActive(false);
        return markerObject;
    }

    /// <summary>
    /// 產生尖端朝下的四角金字塔。原點在尖端，底面在上方。
    /// 每個面用各自的頂點，看起來是有稜角的平面著色。
    /// </summary>
    private static Mesh GetPyramidMesh(float size, float height)
    {
        if (_pyramidMesh != null)
        {
            return _pyramidMesh;
        }

        float w = size * 0.5f;
        Vector3 tip = Vector3.zero;
        Vector3 a = new Vector3(-w, height, -w);
        Vector3 b = new Vector3(w, height, -w);
        Vector3 c = new Vector3(w, height, w);
        Vector3 d = new Vector3(-w, height, w);

        Vector3[] vertices =
        {
            a, b, tip,   // 側面 -Z
            b, c, tip,   // 側面 +X
            c, d, tip,   // 側面 +Z
            d, a, tip,   // 側面 -X
            a, d, c,     // 頂面（底座）
            a, c, b,
        };

        int[] triangles = new int[vertices.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = i;
        }

        _pyramidMesh = new Mesh { name = "TargetMarkerPyramid" };
        _pyramidMesh.vertices = vertices;
        _pyramidMesh.triangles = triangles;
        _pyramidMesh.RecalculateNormals();
        _pyramidMesh.RecalculateBounds();
        return _pyramidMesh;
    }

    /// <summary>
    /// 借用 Unity 內建方塊的預設材質（目前渲染管線一定有，打包後也不會不見）。
    /// </summary>
    private static Material GetDefaultMaterial()
    {
        if (_defaultMaterial != null)
        {
            return _defaultMaterial;
        }

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _defaultMaterial = temp.GetComponent<Renderer>().sharedMaterial;

        // 立刻刪掉，不讓這個暫時方塊的碰撞體在場景裡多留一幀
        DestroyImmediate(temp);
        return _defaultMaterial;
    }

    /// <summary>一個標記：記住目前跟著哪個目標，每幀擺到目標頭頂。</summary>
    private class Marker
    {
        private readonly GameObject _object;
        private Component _target;
        private Collider[] _targetColliders;

        public Marker(GameObject markerObject)
        {
            _object = markerObject;
        }

        public void SetTarget(Component target)
        {
            _target = target;
            _targetColliders = null;

            if (target != null)
            {
                _targetColliders = GetSolidColliders(target);
            }

            if (_object != null)
            {
                _object.SetActive(target != null);
            }
        }

        public bool HasSameTarget(Marker other)
        {
            return _target != null && other._target != null && _target.transform == other._target.transform;
        }

        public void UpdatePose(float heightAboveTop, Quaternion rotation)
        {
            if (_object == null)
            {
                return;
            }

            // 目標被刪除（例如假目標被打倒）時隱藏
            if (_target == null)
            {
                if (_object.activeSelf)
                {
                    _object.SetActive(false);
                }

                return;
            }

            Vector3 top = GetTopCenter();
            _object.transform.SetPositionAndRotation(top + Vector3.up * heightAboveTop, rotation);
        }

        public void DestroyObject()
        {
            if (_object != null)
            {
                Object.Destroy(_object);
            }
        }

        /// <summary>目標所有碰撞體合起來的頂部中心。沒有碰撞體就用物件位置。</summary>
        private Vector3 GetTopCenter()
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds();
            if (_targetColliders != null)
            {
                foreach (Collider targetCollider in _targetColliders)
                {
                    if (targetCollider == null || !targetCollider.enabled)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = targetCollider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(targetCollider.bounds);
                    }
                }
            }

            if (!hasBounds)
            {
                return _target.transform.position;
            }

            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        /// <summary>
        /// 優先用非 Trigger 的碰撞體算高度（菸怪可能有很大的 Trigger 範圍，不適合拿來算頭頂）。
        /// 全部都是 Trigger 時才用 Trigger。
        /// </summary>
        private static Collider[] GetSolidColliders(Component target)
        {
            Collider[] all = target.GetComponentsInChildren<Collider>();
            int solidCount = 0;
            foreach (Collider targetCollider in all)
            {
                if (!targetCollider.isTrigger)
                {
                    solidCount++;
                }
            }

            if (solidCount == 0 || solidCount == all.Length)
            {
                return all;
            }

            Collider[] solid = new Collider[solidCount];
            int index = 0;
            foreach (Collider targetCollider in all)
            {
                if (!targetCollider.isTrigger)
                {
                    solid[index++] = targetCollider;
                }
            }

            return solid;
        }
    }
}
