using UnityEngine;

public class hover : MonoBehaviour
{
    [SerializeField] private float floatHeight = 0.25f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 120f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = startPosition + new Vector3(0f, yOffset, 0f);

        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }
}