using DG.Tweening;
using UnityEngine;

namespace KillerLocomotion
{
    public class KillerLocomotionController : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private Transform[] _waypoints_out;
        [SerializeField] private AnimationClip[] _animations;
        [SerializeField] private AnimationClip[] _animations_out;
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speed = 2f;
        [SerializeField] private AnimationClip _maskEquipAnimation;
        [SerializeField] private Transform _handMaskHoldPoint;
        [SerializeField] private Transform _faceMaskHoldPoint;

        public Sequence IncomingMovementSequence;
        public Sequence OutgoingMovementSequence;
        
        private int _currentWaypointIndex;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                StartInComingMovementLocomotion();
            }
            
            if(Input.GetKeyDown(KeyCode.O))
            {
                StartOutGoingMovementLocomotion();
            }
            
            if(Input.GetKeyDown(KeyCode.R))
            {
                ResetMovementLocomotion();
            }
        }
        
        public void StartInComingMovementLocomotion()
        {
            IncomingMovementSequence = DOTween.Sequence();
            Vector3 previousPosition = transform.position;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                int animIndex = i;
                Vector3 targetPosition = _waypoints[i].position;
                Quaternion targetRotation = _waypoints[i].rotation;
                float distance = Vector3.Distance(previousPosition, targetPosition);
                float duration = (distance < 0.01f) ? 0f : distance / _speed;
                IncomingMovementSequence.AppendCallback(() => {
                    if (_animations.Length > animIndex)
                    {
                        _animator.Play(_animations[animIndex].name);
                    }
                });
                IncomingMovementSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.Linear));
                IncomingMovementSequence.Join(transform.DORotateQuaternion(targetRotation, duration).SetEase(Ease.Linear));
                previousPosition = targetPosition;
            }
            IncomingMovementSequence.Play();
        }
        
        public void StartOutGoingMovementLocomotion()
        {
            OutgoingMovementSequence = DOTween.Sequence();
            Vector3 previousPosition = transform.position;
            for (int i = 0; i < _waypoints_out.Length; i++)
            {
                int animIndex = i;
                Vector3 targetPosition = _waypoints_out[i].position;
                Quaternion targetRotation = _waypoints_out[i].rotation;
                float distance = Vector3.Distance(previousPosition, targetPosition);
                float duration = (distance < 0.01f) ? 0f : distance / _speed;
                OutgoingMovementSequence.AppendCallback(() => {
                    if (_animations_out.Length > animIndex)
                    {
                        _animator.Play(_animations_out[animIndex].name);
                    }
                });
                OutgoingMovementSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.Linear));
                OutgoingMovementSequence.Join(transform.DORotateQuaternion(targetRotation, duration).SetEase(Ease.Linear));
                previousPosition = targetPosition;
            }
            OutgoingMovementSequence.Play();
        }
        
        public void PlayMaskEquipAnimation(GameObject mask)
        {
            // 1. Maskeyi duplicate et
            GameObject maskInstance = Instantiate(mask);
            maskInstance.transform.SetPositionAndRotation(_handMaskHoldPoint.position, _handMaskHoldPoint.rotation);
            maskInstance.transform.SetParent(_handMaskHoldPoint, worldPositionStays: true);
            mask.SetActive(false);
            
            // 2. Hand hold point'e DOTween ile pozisyon ve rotasyon animasyonu (gerekirse parent'ı world'de bırak)
            float handLerpDuration = 0.3f;
            float animDuration = _maskEquipAnimation.length;
            float faceLerpDelay = animDuration * 0.8f;
            float faceLerpDuration = 0.3f;

            Sequence maskSeq = DOTween.Sequence();
            // Hand'e doğru hızlıca lerple
            maskSeq.Append(maskInstance.transform.DOMove(_handMaskHoldPoint.position, handLerpDuration).SetEase(Ease.InOutSine));
            maskSeq.Join(maskInstance.transform.DORotateQuaternion(_handMaskHoldPoint.rotation, handLerpDuration).SetEase(Ease.InOutSine));
            // 3. Mask equip animasyonunu başlat
            maskSeq.AppendCallback(() => _animator.Play(_maskEquipAnimation.name));
            // 4. Animasyonun %80'inde maskeyi yüze taşı
            maskSeq.AppendInterval(faceLerpDelay - handLerpDuration); // handLerp'den kalan süre kadar bekle
            maskSeq.Append(maskInstance.transform.DOMove(_faceMaskHoldPoint.position, faceLerpDuration).SetEase(Ease.InOutSine));
            maskSeq.Join(maskInstance.transform.DORotateQuaternion(_faceMaskHoldPoint.rotation, faceLerpDuration).SetEase(Ease.InOutSine));
            // 5. Maskeyi parent olarak yüze al, animasyon bitince maskeyi yok et
            maskSeq.AppendCallback(() => maskInstance.transform.SetParent(_faceMaskHoldPoint, worldPositionStays: true));
            maskSeq.AppendInterval(animDuration - faceLerpDelay - faceLerpDuration);
            maskSeq.AppendCallback(() => Destroy(maskInstance));
            maskSeq.Play();
        }
        
        public void ResetMovementLocomotion()
        {
            _currentWaypointIndex = 0;
            transform.position = _waypoints[_currentWaypointIndex].position;
            transform.rotation = _waypoints[_currentWaypointIndex].rotation;
        }
    }
}
