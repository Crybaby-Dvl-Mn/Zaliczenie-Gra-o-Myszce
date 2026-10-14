using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClimbTrigger : MonoBehaviour
{
    [Header("Ustawienia")]
    public float maxClimbHeight = 2.5f;
    public float climbSpeed = 1.5f;
    public float forwardStep = 0.7f;
    public float wallCheckDistance = 1.2f;

    [Header("Odepchniêcie od pud³a (anty-clipping kamery)")]
    [Tooltip("Minimalna odleg³oœæ gracza od œrodka triggera, poni¿ej której gracz zostanie cofniêty")]
    public float minDistanceFromTrigger = 1.0f;
    [Tooltip("O ile cofn¹æ gracza, jeœli jest za blisko")]
    public float pushBackDistance = 0.5f;

    private Transform playerTransform;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerTransform = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerTransform = null;
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var controller = FindFirstObjectByType<ClimbController>();
            if (controller != null && !controller.IsClimbing)
            {
                // Kierunek od gracza do œrodka triggera (czyli do pud³a)
                Vector3 dir = transform.position - playerTransform.position;
                dir.y = 0f;
                dir.Normalize();

                // Sprawdzenie odleg³oœci – jeœli gracz za blisko, cofnij go
                float distToTrigger = Vector3.Distance(
                    new Vector3(playerTransform.position.x, 0f, playerTransform.position.z),
                    new Vector3(transform.position.x, 0f, transform.position.z)
                );

                if (distToTrigger < minDistanceFromTrigger)
                {
                    // Cofamy gracza w kierunku PRZECIWNYM do pud³a
                    Vector3 pushDir = -dir;
                    Vector3 push = pushDir * pushBackDistance;

                    // Wy³¹czamy CharacterController na chwilê, ¿eby przesuniêcie by³o natychmiastowe
                    var cc = playerTransform.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        cc.enabled = false;
                        playerTransform.position += push;
                        cc.enabled = true;
                    }
                    else
                    {
                        playerTransform.position += push;
                    }
                }

                controller.StartClimb(maxClimbHeight, climbSpeed, forwardStep, wallCheckDistance, dir);
            }
        }
    }
}