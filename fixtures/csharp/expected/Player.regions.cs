using UnityEngine;
using VContainer;

public class Player : MonoBehaviour, IDamageable
{
    #region DEPENDENCIES

    [Inject] private readonly IPlayerStats _stats;

    #endregion

    #region CONSTANTS

    public const float MaxHealth = 100f;

    private const string Tag = "Player";

    #endregion

    #region STATIC FIELDS

    public static int Count;

    private static int _seed;

    #endregion

    #region FIELDS

    private int _director;

    #endregion

    #region SERIALIZED FIELDS

    [SerializeField] private float speed = 10f;

    [SerializeField] private bool alive;

    #endregion

    #region CTORS

    public Player() { }

    ~Player() { }

    #endregion

    #region PUBLIC EVENTS

    public delegate void PlayerDelegate();

    public event System.Action OnDied;

    #endregion

    #region PRIVATE EVENTS

    private event System.Action<int> OnScore;

    #endregion

    #region PUBLIC PROPERTIES

    public string Name { get; set; }

    public int Level { get; private set; }

    #endregion

    #region PRIVATE PROPERTIES

    private int _tick { get; set; }

    #endregion

    #region INTERFACE IMPLEMENTATIONS

    void IDamageable.ApplyDamage(int amount) { }

    #endregion

    #region UNITY METHODS

    void Awake() { }

    void Update() { }

    protected virtual void Reset() { }

    void OnCollisionEnter(Collision collision) { }

    #endregion

    #region PUBLIC METHODS

    public void Move() { }

    public static Player Create() => new Player();

    #endregion
}

public interface IDamageable
{
    void ApplyDamage(int amount);
}