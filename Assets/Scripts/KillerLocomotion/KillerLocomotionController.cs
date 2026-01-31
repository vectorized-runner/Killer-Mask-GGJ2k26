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

        public Sequence IncomingMovementSequence;
        
        private Sequence _movementSequence;
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
            _movementSequence = DOTween.Sequence();
            Vector3 previousPosition = transform.position;
            for (int i = 0; i < _waypoints_out.Length; i++)
            {
                int animIndex = i;
                Vector3 targetPosition = _waypoints_out[i].position;
                Quaternion targetRotation = _waypoints_out[i].rotation;
                float distance = Vector3.Distance(previousPosition, targetPosition);
                float duration = (distance < 0.01f) ? 0f : distance / _speed;
                _movementSequence.AppendCallback(() => {
                    if (_animations_out.Length > animIndex)
                    {
                        _animator.Play(_animations_out[animIndex].name);
                    }
                });
                _movementSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.Linear));
                _movementSequence.Join(transform.DORotateQuaternion(targetRotation, duration).SetEase(Ease.Linear));
                previousPosition = targetPosition;
            }
            _movementSequence.Play();
        }
        
        public void ResetMovementLocomotion()
        {
            _currentWaypointIndex = 0;
            transform.position = _waypoints[_currentWaypointIndex].position;
            transform.rotation = _waypoints[_currentWaypointIndex].rotation;
        }
    }
}
