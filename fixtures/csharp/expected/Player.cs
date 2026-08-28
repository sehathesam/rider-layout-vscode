using UnityEngine;
using VContainer;

public class Player : MonoBehaviour, IDamageable
{
    [Inject] private readonly IPlayerStats _stats;

    public const float MaxHealth = 100f;

    private const string Tag = "Player";

    public static int Count;

    private static int _seed;

    private int _director;

    [SerializeField] private float speed = 10f;

    [SerializeField] private bool alive;

    public Player() { }

    ~Player() { }

    public delegate void PlayerDelegate();

    public event System.Action OnDied;

    private event System.Action<int> OnScore;

    public string Name { get; set; }

    public int Level { get; private set; }

    private int _tick { get; set; }

    void IDamageable.ApplyDamage(int amount) { }

    void Awake() { }

    void Update() { }

    protected virtual void Reset() { }

    void OnCollisionEnter(Collision collision) { }

    public void Move() { }

    public static Player Create() => new Player();
}

public interface IDamageable
{
    void ApplyDamage(int amount);
}