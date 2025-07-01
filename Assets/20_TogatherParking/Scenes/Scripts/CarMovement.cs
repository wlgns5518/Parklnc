using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarMovement : MonoBehaviour
{
    private Vector2 inputValue;
    private Vector3 moveDirection;
    private bool isStopped = false; // 충돌 후 멈추는 상태 플래그

    [SerializeField] private float moveSpeed = 3f;
    //[SerializeField] private float acceleration = 2f; // 가속도 추가
    [SerializeField] private float rotationSpeed = 5f; // 회전 속도 추가
    [SerializeField] private float stopDuration = 1f; // 충돌 후 멈출 시간
    [Header("effect")]
    [SerializeField] private ParticleSystem dustRun; // DustRun 파티클 추가
    [SerializeField] private ParticleSystem hit; // Hit 파티클 추가
    [SerializeField] private Animator carAnimator; // 애니메이터 추가

    private int collidedId1 = Animator.StringToHash("Collided Trigger");
    private int collidedId2 = Animator.StringToHash("Collided 90 Degrees Trigger");
    private int collidedId3 = Animator.StringToHash("Collided 180 Degrees Trigger");
    private int collidedId4 = Animator.StringToHash("Collided 270 Degrees Trigger");
    private Rigidbody carRigidbody;

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        //if (isStopped) return; // 충돌 후 멈춘 상태에서는 이동 X

        //if (CarController.Instance == null || !CarController.Instance.HasSelectedCar(this))
        //    return; // 플레이어가 이 차를 선택하지 않았으면 움직이지 않음

        MoveCar();
    }

    private void OnMove(InputValue value)
    {
        if (isStopped) return; // 충돌 후 멈춘 상태에서는 입력 X

        //if (CarController.Instance == null || !CarController.Instance.HasSelectedCar(this))
        //    return;

        moveSpeed = 3f;
        inputValue = value.Get<Vector2>();
    }

    public void MoveForward()
    {
        if (isStopped) return;
        inputValue += Vector2.up; // 앞으로 이동 (y=1)
    }

    public void MoveBackward()
    {
        if (isStopped) return;
        inputValue += Vector2.down; // 뒤로 이동 (y=-1)
    }

    public void MoveLeft()
    {
        if (isStopped) return;
        inputValue += Vector2.left; // 왼쪽 이동 (x=-1)
    }

    public void MoveRight()
    {
        if (isStopped) return;
        inputValue += Vector2.right; // 오른쪽 이동 (x=1)
    }

    // 앞/뒤(Forward/Backward) 정지
    public void StopMoveVertical()
    {
        inputValue.y = 0f;
    }

    // 좌/우(Left/Right) 정지
    public void StopMoveHorizontal()
    {
        inputValue.x = 0f;
    }

    private void MoveCar()
    {
        // 차량이 움직이는 방향을 현재 바라보는 방향으로 설정
        moveDirection = transform.forward * inputValue.y;

        if (moveDirection != Vector3.zero)
        {
            //moveSpeed += acceleration * Time.fixedDeltaTime; // 점진적 가속
            //moveSpeed = Mathf.Clamp(moveSpeed, 3f, 10f); // 최대 속도 제한

            // A, D 키를 누르는 동안만 회전 적용
            if (inputValue.x != 0)
            {
                // 후진 시 회전 방향을 반대로 설정
                int rotationDirection = (inputValue.y < 0) ? -1 : 1;
                float rotationAmount = inputValue.x * rotationSpeed * Time.fixedDeltaTime * 90f * rotationDirection; // 회전 속도 조정
                Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y + rotationAmount, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
            carRigidbody.MovePosition(transform.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
            CarMoveEffect();
        }
        else
        {
            CarStopEffect();
        }
    }

    private void CarMoveEffect()
    {
        SoundManager.Instance.PlayCarSound();
        if (dustRun != null && !dustRun.isPlaying)
        {
            dustRun.Play();
        }
    }
    private void CarStopEffect()
    {
        if (dustRun != null && dustRun.isPlaying)
        {
            dustRun.Stop();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 9) // Layer 9와 충돌 시
        {
            // 충돌 방향 계산
            Vector3 contactNormal = collision.contacts[0].normal;
            Vector3 collisionDir = contactNormal.normalized;

            // 자동차의 전방 벡터와 충돌 방향의 각도 계산 (XZ 평면 기준)
            Vector3 forward = transform.forward;
            collisionDir.y = 0;
            forward.y = 0;
            float angle = Vector3.SignedAngle(forward, collisionDir, Vector3.up);
            angle = (angle + 360f) % 360f; // 0~360도로 변환

            // 각도에 따라 애니메이션 트리거 선택
            if (angle < 45f || angle >= 315f)
            {
                carAnimator.SetTrigger(collidedId1); // 정면
            }
            else if (angle >= 45f && angle < 135f)
            {
                carAnimator.SetTrigger(collidedId2); // 오른쪽(90도)
            }
            else if (angle >= 135f && angle < 225f)
            {
                carAnimator.SetTrigger(collidedId3); // 뒤(180도)
            }
            else // 225~315
            {
                carAnimator.SetTrigger(collidedId4); // 왼쪽(270도)
            }
            SoundManager.Instance.PlayCarHitSound();
            // Hit 파티클 실행
            if (hit != null)
            {
                hit.transform.position = collision.contacts[0].point;
                hit.Play();
            }

            // 자동차를 충돌 방향(법선 방향)으로 밀려나게 함
            carRigidbody.AddForce(contactNormal * moveSpeed * 0.1f, ForceMode.Impulse);

            // 충돌 후 멈추는 코루틴 실행
            StartCoroutine(StopCarTemporarily());
        }
    }

    private IEnumerator StopCarTemporarily()
    {
        isStopped = true; //  이동 정지
        ResetCar();

        yield return new WaitForSeconds(stopDuration); //  일정 시간 대기

        isStopped = false; //  다시 이동 가능
        moveSpeed = 3f; //  속도 복원
    }
    private void ResetCar()
    {
        moveDirection = Vector3.zero; //  이동 방향 초기화
        moveSpeed = 0f; //  속도 초기화
        inputValue = Vector2.zero; // ⬅️ 입력값 초기화
    }

    //public void OnPointerClick(PointerEventData eventData)
    //{
    //    if (CarController.Instance != null && this.gameObject.layer == 3)
    //    {
    //        if (CarController.Instance.HasSelectedCar(this))
    //        {
    //            ResetCar();
    //            CarController.Instance.DeselectCar();
    //            return;
    //        }
    //        CarController.Instance.SelectCar(this);
    //    }
    //}
}