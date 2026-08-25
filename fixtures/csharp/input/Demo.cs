using UnityEngine;

public class Demo : MonoBehaviour
{
    public void ZMethod() { }

    [SerializeField]
    private float speed;

    public float Speed => speed;

    public Demo() { }

    private void Awake() { }

    public void AMethod() { }
}
