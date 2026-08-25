using UnityEngine;
using VContainer;

public class Player : MonoBehaviour, IDamageable
{
    [Inject] private IPlayerStats _stats;
    [SerializeField] private float speed = 10f;
    private int _director;

    public const float MaxHealth = 100f;
    private const string Tag = "Player";

    public static int Count;
    private static int _seed;

    void Awake() { }
    [SerializeField] private bool alive;
    public event System.Action OnDied;
    private event System.Action<int> OnScore;
    public delegate void PlayerDelegate();

    public string Name { get; set; }
    public int Level { get; private set; }
    private int _tick { get; set; }

    public Player() { }
    ~Player() { }

    public void Move() { }
    public static Player Create() => new Player();
    void Update() { }
    protected virtual void Reset() { }
    void IDamageable.ApplyDamage(int amount) { }
    void OnCollisionEnter(Collision collision) { }
}

public interface IDamageable
{
    void ApplyDamage(int amount);
}